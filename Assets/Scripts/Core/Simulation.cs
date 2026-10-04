using System;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// BetweenRuns: no run under way (the game has just opened, or one has ended and the Summary
    /// shows). Nothing drains; anything queued is for the next run.
    /// Running: a run is under way and ticks advance it.
    /// </summary>
    public enum LoopPhase
    {
        BetweenRuns,
        Running,
    }

    /// <summary>
    /// The game rules. Knows nothing about Unity scenes or UI: it just advances one tick at a time.
    /// Split across files by topic (it's one class, marked "partial"):
    ///   Simulation.cs              the loop itself: phases, the tick, the passive drain, beginning and ending a run
    ///   Simulation.Tasks.cs        the action queue: adding, removing, clearing
    ///   Simulation.QueueInRoom.cs  Schedule, Play and Carry in a room, and where they land
    ///   Simulation.Starting.cs     starting the top entry, stopping or suspending the running one
    ///   Simulation.CanStart.cs     why an action can't start now
    ///   Simulation.Repeating.cs    when a queued entry is done repeating, and whether what it gives has room
    ///   Simulation.Supplying.cs    supplying a blocked entry, and what an action gives or needs
    ///   Simulation.Paying.cs       paying for tasks (prices, costs) and finishing them
    ///   Simulation.Attributes.cs   Clara's stats and skills: XP, speed, drain and mastery
    ///   Simulation.Resources.cs    holding things: pockets, containers, one-of-a-kind objects
    ///   Simulation.Unlocks.cs      switches, unlocks and story
    ///   Simulation.Restoring.cs    restoration items and holding off the darkness
    ///   Simulation.Places.cs       rooms, ways and trips
    ///   Simulation.Offers.cs       what each room offers, and why an action can't be done there
    ///   Simulation.Exploring.cs    searching rooms (the bar is kept, and a switch can reopen it) and what it finds
    ///   Simulation.Floor.cs        floor piles and picking up
    ///   Simulation.Carrying.cs     carried items (the ring), the stash and packing
    ///   Simulation.Route.cs        the queue read as a route: stops, trips, time left
    ///   Simulation.Reorder.cs      moving queue entries and stops
    ///   Simulation.ByHeart.cs      rooms known by heart, room speed (its hold is RoomSpeedHold, its own class), and carrying the plan to the next run
    ///   Simulation.PlanWarnings.cs warnings ahead on the plan (worked out by PlanWarningsTracker, its own class)
    ///   Simulation.DevJump.cs      the dev panel's Jump to
    ///   Simulation.Report.cs       the end-of-run report (made by RunReportBuilder, its own class), and best-ever milestone times
    ///   Simulation.SinceLastRun.cs how much faster mastery has made an action since the last run began
    /// </summary>
    public partial class Simulation
    {
        private readonly LoopSettings _settings;
        private readonly int _ticksPerSecond;
        private readonly GameContent _content;

        // Guards against rounding: 1.2 added 100 times can land a hair under 120.
        const float WorkEpsilon = 0.0001f;

        public PersistentState Persistent { get; }
        public LoopState Loop { get; private set; }

        /// <summary>The game opens between runs: nothing drains until the first run begins.</summary>
        public LoopPhase Phase { get; private set; } = LoopPhase.BetweenRuns;

        /// <summary>A run is under way: begun and not yet over. The one rule the screens ask "is something running?" by.</summary>
        public bool RunUnderWay => Phase == LoopPhase.Running && !Loop.IsOver;

        /// <summary>Puts back the queue made between runs, when loading a save (before any run begins). See <see cref="ResumeRun"/>.</summary>
        public void RestoreNextQueue(Action<ActionQueue> fill)
        {
            if (!LoopIsFresh || Phase != LoopPhase.BetweenRuns)
                throw new InvalidOperationException("The queue between runs can only be restored straight after loading, before any run begins.");
            fill(Loop.Queue);
            MarkPlanWarningsStale();
        }

        /// <summary>The number the next BeginLoop will run: the current loop if it hasn't started yet.</summary>
        public int NextLoopNumber => LoopIsFresh ? Persistent.LoopNumber : Persistent.LoopNumber + 1;

        private bool LoopIsFresh => Loop.TicksElapsed == 0 && !Loop.IsOver;

        // Actions queued after a run has ended, for the next one (each run starts with its own queue).
        private ActionQueue _nextQueue = new ActionQueue();

        public event Action LoopStarted;
        public event Action<Pool> PoolEmptied;
        public event Action LoopEnded;
        public event Action<TaskDefinition> TaskStarted;
        public event Action<TaskDefinition> TaskCompleted;
        /// <summary>A trip ended: she's now in this room. The flag is true when it's her first entry into it this run (a milestone card goes in the feed instead).</summary>
        public event Action<NodeDefinition, bool> Arrived;
        /// <summary>An explore finished. Carries the room and how full its bar is now (0 to 1).</summary>
        public event Action<NodeDefinition, float> RoomExplored;
        /// <summary>A way just became usable (found by exploring, or opened by a switch): from, to.</summary>
        public event Action<NodeDefinition, NodeDefinition> WayFound;
        /// <summary>A queued task couldn't start. The string says why, e.g. "needs 1 Tool A".</summary>
        public event Action<TaskDefinition, string> TaskSkipped;
        /// <summary>The queue has run dry and Clara is waiting. Fires once until more is added.</summary>
        public event Action QueueRanOut;
        /// <summary>Something was added to, moved in or taken from the queue.</summary>
        public event Action QueueChanged;
        /// <summary>The player queued something (Play or Schedule): unlike QueueChanged, not when entries finish.</summary>
        public event Action ActionQueued;
        /// <summary>The player asked for an action that can't be done now: it wasn't queued (the reason why).</summary>
        public event Action<TaskDefinition, NodeDefinition, string> ActionRefused;
        /// <summary>The queue put an action on top to supply a blocked one: (the supplier, the blocked task, the item).</summary>
        public event Action<TaskDefinition, TaskDefinition, ResourceDefinition> SupplyQueued;
        /// <summary>A Single Action task was just done: the game pauses so the player can choose what's next.</summary>
        public event Action<TaskDefinition> SingleActionDone;
        public event Action<SwitchDefinition> SwitchFlipped;
        /// <summary>
        /// A milestone (a switch with a story, or a room she entered) was reached this run: it, the run
        /// time, and whether it's the first time ever (its story is new) or a benchmark reached again.
        /// </summary>
        public event Action<ContentAsset, float, bool> MilestoneReached;
        /// <summary>An attribute gained a level this run. Carries the new level.</summary>
        public event Action<ClaraAttribute, int> AttributeLevelledUp;
        /// <summary>An attribute's permanent mastery went up. Carries the new mastery level.</summary>
        public event Action<ClaraAttribute, int> AttributeMasteryGained;
        /// <summary>A skill gained a level this run. Carries the new level.</summary>
        public event Action<SkillDefinition, int> SkillLevelledUp;
        /// <summary>A skill's permanent mastery went up. Carries the new mastery level.</summary>
        public event Action<SkillDefinition, int> SkillMasteryGained;
        /// <summary>A restoration item started giving vitality back (one was used up).</summary>
        public event Action<ResourceDefinition> RestoreStarted;
        /// <summary>A task held off the darkness: the drain is lower for the rest of the run.</summary>
        public event Action<TaskDefinition> DarknessHeldOff;
        /// <summary>Something was taken from her (e.g. she put the ring down, or used a wisp). Carries how many.</summary>
        public event Action<ResourceDefinition, int> ResourceLost;
        /// <summary>A resource was gained. Carries how many were actually added (0 if already at the maximum).</summary>
        public event Action<ResourceDefinition, int> ResourceGained;
        /// <summary>She has just earned planning between runs (see <see cref="PlanningUnlocked"/>): fires once, when the first thing that gives it arrives.</summary>
        public event Action PlanningUnlockedNow;


        /// <param name="saved">Progress loaded from a save, or null for a new game.</param>
        public Simulation(LoopSettings settings, int ticksPerSecond, GameContent content = null, PersistentState saved = null)
        {
            _settings = settings;
            _ticksPerSecond = ticksPerSecond;
            _content = content;
            Persistent = saved ?? new PersistentState();
            _reportBuilder = new RunReportBuilder(this, ticksPerSecond, content);
            _speedHold = new RoomSpeedHold(this); // before the plan's warnings, so its handlers run first as before
            _planWarnings = new PlanWarningsTracker(this, settings, content);
            StartNewLoop();
            LearnKnownWays();
            // A search kept in a save (or filled by upgrading an old one) may already be past a discovery.
            CheckSwitches(SwitchTrigger.RoomExplored);

            if (content != null && content.openingStory != null)
                Reveal(content.openingStory);
        }

        /// <summary>
        /// Leaves planning and starts running. Always starts from a fresh loop: if the current one
        /// has already run (or ended), a new one replaces it.
        /// </summary>
        public void BeginLoop()
        {
            if (!LoopIsFresh)
                StartNewLoop();

            Phase = LoopPhase.Running;
            TakePackedItemsIn(); // now, so changes to the packing list made while planning count
            LoopStarted?.Invoke();
        }

        /// <summary>
        /// Carries on a run from a save: builds it the way a new run starts (without counting another
        /// run), lets <paramref name="fill"/> put back what the run had, and carries on from there.
        /// The game makes this from the save's permanent state first.
        /// </summary>
        public void ResumeRun(Action<LoopState> fill)
        {
            // Made from the save, this game set up a fresh next run and counted it; the saved run
            // replaces that one, so it isn't counted.
            if (!LoopIsFresh || Phase != LoopPhase.BetweenRuns)
                throw new InvalidOperationException("A run can only be resumed straight after loading, before any run begins.");
            Persistent.LoopNumber = Math.Max(0, Persistent.LoopNumber - 1);
            StartNewLoop(countIt: false);
            fill(Loop);
            Phase = LoopPhase.Running;
            // Ways already usable aren't news.
            _knownWays.Clear();
            LearnKnownWays();
            LoopStarted?.Invoke();
        }

        /// <param name="countIt">False when putting back a saved run: it's the same run, not another.</param>
        private void StartNewLoop(bool countIt = true)
        {
            // A run restarted before it ended (dev tools) still took its time, and is in the history
            // as ended early. Recorded before the count goes up, under its own loop number.
            if (Loop != null && !Loop.IsOver && Loop.TicksElapsed > 0)
            {
                Persistent.TicksPlayed += Loop.TicksElapsed;
                Persistent.RunHistory.Add(new RunRecord(Persistent.LoopNumber, Loop.TicksElapsed,
                    Loop.Steps.Count, LoopEndReason.EndedByPlayer, Loop.Milestones));
                KeepThisRunsStart();
            }
            if (countIt)
                Persistent.LoopNumber++;

            // What Endurance has banked lengthens the bar from the start, and it stays that long all run.
            var vitality = new Pool(GameText.Get("names.vitality"), MaxVitality);
            _speedHold.Reset();
            Loop = new LoopState(vitality, _nextQueue); // whatever was queued between runs
            _nextQueue = new ActionQueue();
            Loop.CurrentNode = _content != null ? _content.startNode : null;
            NoteWhatTheRunStartsWith();

            // Pools unlocked mid-run appear from the next loop.
            foreach (var pool in _settings.pools)
            {
                if (pool.hue == Hue.None)
                    throw new InvalidOperationException("A pool setting has no hue, so it would have no name: give every pool in LoopSettings a hue.");
                if (OpensAtRunStart(pool))
                    Loop.Pools.Add(new Pool(pool.hue, pool.max));
            }
        }

        /// <summary>What she keeps as the run begins (mastery, kept things, searches), for the run's report to compare against.</summary>
        private void NoteWhatTheRunStartsWith()
        {
            foreach (var entry in Persistent.AttributeMasteryXp)
                Loop.AttributeMasteryAtStart[entry.Key] = entry.Value;
            foreach (var entry in Persistent.SkillMasteryXp)
                Loop.SkillMasteryAtStart[entry.Key] = entry.Value;
            foreach (var entry in Persistent.Resources)
                Loop.KeptAtStart[entry.Key] = entry.Value;
            foreach (var entry in Persistent.Explored)
                Loop.ExploredAtStart[entry.Key] = entry.Value;
        }

        /// <summary>Whether a run beginning now starts with this pool: open from the start, or its hue learned.</summary>
        internal bool OpensAtRunStart(LoopSettings.PoolSettings pool) =>
            pool.startsUnlocked || (pool.hue != Hue.None && Persistent.UnlockedHues.Contains(pool.hue));

        /// <summary>Game time played in this game, all runs together (the run under way included), in ticks.</summary>
        public long TotalTicksPlayed => Persistent.TicksPlayed + (Loop.IsOver ? 0 : Loop.TicksElapsed);

        /// <summary>
        /// How much the whole gem holds: the bar her pathos pools share.
        /// Placeholder rule: every pool's maximum added up, so the gem grows as she learns hues.
        /// If the gem gets a capacity of its own, only this changes.
        /// </summary>
        public float GemCapacity
        {
            get
            {
                float total = 0f;
                foreach (var pool in Loop.Pools)
                    total += pool.Max;
                return total;
            }
        }

        /// <summary>The player ends the run early. The anchor pulls her back, but it isn't a collapse.</summary>
        public void EndRunEarly()
        {
            if (Phase == LoopPhase.Running && !Loop.IsOver)
                EndLoop(LoopEndReason.EndedByPlayer);
        }

        private void EndLoop(LoopEndReason reason)
        {
            Loop.IsOver = true;
            Loop.EndReason = reason;
            Persistent.TicksPlayed += Loop.TicksElapsed;
            Phase = LoopPhase.BetweenRuns;
            SettleCarriedItems(reason);
            // Any ending banks, then settles the run's stat XP into mastery (a dev restart doesn't come through here, so it does neither).
            float banked = BankVitality();
            SettleStatMastery(reason);

            // Only a real collapse counts for "failed while doing X" switches (e.g. the chase).
            if (reason == LoopEndReason.Exhausted)
                CheckSwitches(SwitchTrigger.LoopEndedDuringTask);

            // After the switch check, so a switch flipped by the collapse is in the report.
            LastRun = _reportBuilder.Build();
            LastRun.KeptVitalityGained = banked;
            // Every run goes in the history, however it ended; the longest-run record skips runs ended early.
            Persistent.RunHistory.Add(new RunRecord(LastRun.LoopNumber, LastRun.Ticks, LastRun.ActionsCompleted, reason, Loop.Milestones));
            KeepThisRunsStart();

            // The count comes first: the run that makes a room known by heart already carries into the next.
            LastRun.KnownByHeartNow.AddRange(CountRoomsWorked());
            CarryPlanToNextRun();

            LoopEnded?.Invoke();
        }

        public void Tick()
        {
            if (Phase != LoopPhase.Running || Loop.IsOver)
                return;

            // 1. If Clara is free, pick up the top of the queue. With nothing she can do, she waits
            //    outside time (and the game pauses for the player): vitality never drains while
            //    nothing is queued, and the run clock stands.
            if (Loop.CurrentTask == null && !TryStartNextTask())
                return;

            Loop.TicksElapsed++;

            // How much work the running task gets done this tick: 1 at normal speed, more as its
            // skill levels up (even partway through the task).
            float work = 0f;
            bool finishesThisTick = false;
            if (Loop.CurrentTask != null)
            {
                work = SpeedMultiplierFor(Loop.CurrentTask);
                float remaining = WorkLeftOnCurrentTask();
                finishesThisTick = work >= remaining - WorkEpsilon;
                if (finishesThisTick)
                    work = remaining; // the last tick only does what's left, so XP comes out exact
            }

            // 2. Vitality drains on its own, a little every tick, faster the longer the run goes.
            float vitalityDrain = VitalityDrainPerSecond / _ticksPerSecond;

            // 3. Action and carry costs, when switched on in LoopSettings: the running task spends
            //    for this tick's work, and whatever a pool can't cover lands on vitality.
            if (Loop.CurrentTask != null)
            {
                vitalityDrain += SpendCurrentTaskCost(work, finishesThisTick);
                vitalityDrain += SpendCarryTax(work);
            }
            float vitalityBefore = Loop.Vitality.Current;
            Loop.Vitality.Drain(vitalityDrain);
            float vitalityLost = vitalityBefore - Loop.Vitality.Current;
            NoteVitalityLost(vitalityLost); // now, so a run that ends this tick (walking out) still counts it
            // Reaching zero is the end, even if an item in use would give some back this tick
            // (otherwise a wisp could hold her at a sliver above zero for its whole clock).
            bool spent = Loop.Vitality.IsEmpty;

            // Restoration items in use give some back; more start if she's missing enough for one.
            if (!spent)
            {
                Restore();
                StartRestoring();
            }

            // 4. The running task makes progress, and Clara earns its XP in step with the work, in
            //    its skill and its stat (and their mastery). A task is worth the same XP however
            //    fast she does it. This comes before the vitality check, so a task whose last tick
            //    is also the one that exhausts her still counts.
            if (Loop.CurrentTask != null)
            {
                var task = Loop.CurrentTask;
                Loop.CurrentTaskWorkDone += work;
                if (IsSearching(task))
                    AdvanceSearch(task, work, finishesThisTick); // its XP comes a step at a time
                else
                {
                    float xp = XpRewardOf(task, Loop.CurrentNode) * work / Loop.CurrentTaskWorkNeeded;
                    GainXp(task.skill, xp);
                    GainXp(StatTrainedBy(task), xp);
                }
                TallyTime(task.skill);
                // Still hers to finish? A level-up above can flip a switch that retires this very
                // task, which stops it on the spot.
                if (finishesThisTick && Loop.CurrentTask == task)
                    CompleteCurrentTask();
            }

            // She walked out this tick: the run is over and reported, so nothing more happens.
            if (Loop.IsOver)
                return;

            // Endurance and Composure train on their own, whatever she's doing; the drain grows.
            TrainEnduranceAndComposure(vitalityLost);
            GrowTheDrain();

            // 5. At zero vitality, the anchor pulls Clara back (unless she has just walked out).
            if (!Loop.IsOver && (spent || Loop.Vitality.IsEmpty))
                EndLoop(LoopEndReason.Exhausted);
        }

        private float DrainAndReport(Pool pool, float amount)
        {
            bool wasEmpty = pool.IsEmpty;
            float overflow = pool.Drain(amount);
            if (!wasEmpty && pool.IsEmpty)
                PoolEmptied?.Invoke(pool);
            return overflow;
        }
    }
}
