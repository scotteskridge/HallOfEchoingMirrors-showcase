using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Searching (the common verb, called Explore in code): one long action fills the room's bar, and
    // the bar is kept between runs, partway included. A switch can reopen a room's search (the bar
    // starts again at 0%, a new round). Actions in a room's Found By Searching list appear once the
    // bar reaches them; hidden ways found stay found; discoveries with a story are switches
    // (trigger: RoomExplored), flipped once, for good.
    public partial class Simulation
    {
        public TaskDefinition ExploreVerb => _content != null ? _content.exploreVerb : null;

        /// <summary>Steps of this room's search done, ever. A tick can count for a part of a step (Wayfinding speeds it), so it's not always whole.</summary>
        public float ExploresDoneIn(NodeDefinition room) =>
            room != null && Persistent.Explored.TryGetValue(room, out float done) ? done : 0f;

        /// <summary>Whole steps of this room's search done, ever: each one gave what the room gives.</summary>
        public int SearchStepsDoneIn(NodeDefinition room) => WholeSteps(ExploresDoneIn(room));

        /// <summary>How full a room's exploration bar is, 0 to 1. Always 0 for a room with nothing to explore.</summary>
        public float ExploredFraction(NodeDefinition room) =>
            room != null && room.exploresToFill > 0 ? ExploresDoneIn(room) / room.exploresToFill : 0f;

        public bool HasSomethingToExplore(NodeDefinition room) => room != null && room.exploresToFill > 0;

        public bool IsFullyExplored(NodeDefinition room) =>
            HasSomethingToExplore(room) && ExploresDoneIn(room) >= room.exploresToFill;

        /// <summary>Why she can't explore here, or null if she can.</summary>
        private string CantExplore(NodeDefinition room)
        {
            if (!HasSomethingToExplore(room))
                return GameText.Get("reasons.nothing_to_explore");
            if (IsFullyExplored(room))
                return GameText.Get("reasons.fully_explored", ("room", GameText.TitleInSentence(room.DisplayName)));
            return null;
        }

        internal bool IsSearching(TaskDefinition task) => task != null && task == ExploreVerb && HasPlaces;

        /// <summary>
        /// Work (in ticks at normal speed) that one step of this room's bar takes: the Explore verb's
        /// base time there (the cost curve), changed by the room and by what she holds.
        /// </summary>
        private int StepWork(NodeDefinition room)
        {
            float timeTimes = (room != null ? room.exploring.time : 1f) * HeldModifierFor(ExploreVerb).time;
            return WorkNeededFor(ExploreVerb, room, timeTimes);
        }

        /// <summary>
        /// The work a search has left: a step's time for every step of the bar still to fill. Perception
        /// doesn't speed it (it reveals hidden finds: <c>needsPerception</c>); Wayfinding does.
        /// </summary>
        private float SearchWorkLeft(NodeDefinition room) => SearchStepsLeft(room) * StepWork(room);

        /// <summary>How many steps' worth of time (and cost) a search of this room still has: the steps left.</summary>
        private float SearchStepsLeft(NodeDefinition room) =>
            System.Math.Max(0f, room.exploresToFill - ExploresDoneIn(room));

        /// <summary>Whether a task is the search of a room that still has some of its bar to fill (its price and length are for what's left).</summary>
        private bool SearchesWhatIsLeft(TaskDefinition task, NodeDefinition room) =>
            IsSearching(task) && HasSomethingToExplore(room);

        /// <summary>Work still to do on the running task. A search's isn't fixed: it depends on the bar now.</summary>
        private float WorkLeftOnCurrentTask() =>
            SearchesWhatIsLeft(Loop.CurrentTask, Loop.CurrentNode)
                ? SearchWorkLeft(Loop.CurrentNode)
                : Loop.CurrentTaskWorkNeeded - Loop.CurrentTaskWorkDone;

        /// <summary>
        /// Puts <paramref name="work"/> into the room's bar. Each whole step crossed gives what the
        /// room gives and the XP of one search step; discoveries are checked whenever the bar passes
        /// a whole percent. A run that ends mid-step keeps the bar, but not that step's XP.
        /// </summary>
        /// <param name="finishing">The last tick: the bar is filled exactly, whatever the float sums say.</param>
        private void AdvanceSearch(TaskDefinition task, float work, bool finishing)
        {
            var room = Loop.CurrentNode;
            if (!HasSomethingToExplore(room))
                return;
            float before = ExploresDoneIn(room);
            float after = finishing
                ? room.exploresToFill
                : System.Math.Min(room.exploresToFill, before + work / StepWork(room));
            if (after <= before)
                return;
            Persistent.Explored[room] = after;

            int steps = WholeSteps(after) - WholeSteps(before);
            if (steps > 0)
            {
                // Paid on every completed step, a reopened search's second round included: decided
                // 2026-09-30 ("exp is awarded continuously as an action is completed"). Not once per room.
                foreach (var give in room.eachExploreGives)
                    Grant(give.resource, give.amount * steps);
                float xp = XpRewardOf(task, room) * steps;
                GainXp(task.skill, xp);
                GainXp(StatTrainedBy(task), xp);
            }

            if (steps > 0 || PercentOf(room, after) > PercentOf(room, before))
            {
                RememberFoundWays(room);
                RoomExplored?.Invoke(room, ExploredFraction(room));
                CheckSwitches(SwitchTrigger.RoomExplored);
                CheckForNewWays();
            }
            // A switch above may have retired this very search.
            if (Loop.CurrentTask != task)
                return;
            // What's left shifts as her skills speed the search, so the action's length is worked out again.
            Loop.CurrentTaskWorkNeeded = System.Math.Max(1,
                (int)System.MathF.Round(Loop.CurrentTaskWorkDone + SearchWorkLeft(room)));
        }

        // A hair of slack, so 3 steps of 1/3 each make exactly 1.
        private static int WholeSteps(float searches) => (int)System.Math.Floor(searches + 0.001f);

        private static int PercentOf(NodeDefinition room, float done) =>
            (int)System.Math.Floor((done * 100f + 0.001f) / room.exploresToFill);

        /// <summary>Ways out of this room that its search has reached are found for good.</summary>
        private void RememberFoundWays(NodeDefinition room)
        {
            foreach (var way in room.ways)
                if (way != null && way.to != null && !way.IntoPlannedRoom && way.foundAtExplored > 0 && SearchReaches(way, room))
                    Persistent.FoundWays.Add((room, way.to));
        }

        /// <summary>
        /// Perception rose: ways hidden in rooms already searched far enough are found now,
        /// remembered for good, and announced.
        /// </summary>
        private void FindWaysNowSeen()
        {
            foreach (var room in Persistent.Explored.Keys)
                RememberFoundWays(room);
            CheckForNewWays();
        }

        /// <summary>Whether the room's search has found this action.</summary>
        public bool IsFoundHere(TaskDefinition task, NodeDefinition room)
        {
            if (room == null || task == null)
                return false;
            foreach (var find in room.foundBySearching)
                if (find != null && find.task == task && IsFound(find, room))
                    return true;
            return false;
        }

        /// <summary>
        /// Whether one of a room's finds is found. A find of the round being searched now is found once
        /// the bar reaches it (and she sees well enough); a later round's waits for its switch.
        /// Placeholder rule: once a room is reopened, every find of the round before counts as found,
        /// even one the first bar never reached (in the lab, all were reached before the talk).
        /// </summary>
        private bool IsFound(RoomFind find, NodeDefinition room)
        {
            if (BarCountsFor(find, room))
                return IsExploredTo(room, find.atSearched) && SeesWell(find.needsPerception);
            return find.afterSwitch == null; // the first round, now behind her; or a round not begun
        }

        /// <summary>
        /// Whether the room's bar as it stands is the one this find waits on: the find is of the round
        /// being searched now. The one shared check for IsFound and the plan's "might be found" (a
        /// later round's find must not count the bar of a round before its switch).
        /// </summary>
        internal bool BarCountsFor(RoomFind find, NodeDefinition room) => find.afterSwitch == SearchRoundOf(room);

        /// <summary>The flipped switch that reopened this room's search (its round now), or null for the first round.</summary>
        private SwitchDefinition SearchRoundOf(NodeDefinition room)
        {
            SwitchDefinition round = null;
            foreach (var @switch in Persistent.FlippedSwitches)
            {
                if (!@switch.reopensSearch.Contains(room))
                    continue;
                // Flipped switches have no order, so a third round couldn't tell which came second.
                if (round != null)
                    throw new System.InvalidOperationException(
                        $"{room.DisplayName}'s search is reopened by both {round.displayName} and {@switch.displayName}: only one switch may reopen a room.");
                round = @switch;
            }
            return round;
        }

        /// <summary>
        /// A switch reopened this room's search: the bar starts again at 0%, once (Flip runs once). The
        /// run's starting point drops with it, so the Summary counts this run's search from the reset.
        /// </summary>
        private void ReopenSearch(NodeDefinition room)
        {
            Persistent.Explored[room] = 0f;
            Loop.ExploredAtStart[room] = 0f;
        }

        // Ways she already knew about when this game was started or loaded, so only new finds are announced.
        private readonly HashSet<Way> _knownWays = new HashSet<Way>();

        /// <summary>
        /// Remembers every way usable right now, without announcing any (a new or loaded game).
        /// </summary>
        private void LearnKnownWays()
        {
            foreach (var node in AllNodes)
                if (node != null)
                    foreach (var way in node.ways)
                        if (way != null && !way.IntoPlannedRoom && IsOpen(way, node) && IsFound(way, node))
                            _knownWays.Add(way);
        }

        /// <summary>
        /// Announces ways that have just become usable (found by exploring, or opened by a switch):
        /// WayFound fires, and the way's story is revealed.
        /// </summary>
        private void CheckForNewWays()
        {
            foreach (var node in AllNodes)
            {
                if (node == null)
                    continue;
                foreach (var way in node.ways)
                {
                    if (way == null || way.to == null || way.IntoPlannedRoom || _knownWays.Contains(way) || !IsUsable(way, node))
                        continue;
                    _knownWays.Add(way);
                    if (way.story != null)
                        Reveal(way.story);
                    WayFound?.Invoke(node, way.to);
                }
            }
        }

        /// <summary>
        /// Whether a room's bar has reached a percentage. Whole numbers, so 5 of 10 steps is
        /// exactly 50% with no rounding surprises.
        /// </summary>
        internal bool IsExploredTo(NodeDefinition room, int percent) =>
            HasSomethingToExplore(room) && ExploresDoneIn(room) * 100f + 0.001f >= percent * room.exploresToFill;

        /// <summary>Whether the owner's search has reached a hidden way, and she can see it.</summary>
        private bool SearchReaches(Way way, NodeDefinition owner) =>
            IsExploredTo(owner, way.foundAtExplored) && SeesWell(way.needsPerception);

        /// <summary>Whether her Perception reaches what a hidden thing needs (0 = nothing). An asleep Perception sees nothing hidden.</summary>
        private bool SeesWell(int needsPerception) =>
            needsPerception <= 0 || EffectStrengthOf(ClaraAttribute.Perception) >= needsPerception;
    }
}
