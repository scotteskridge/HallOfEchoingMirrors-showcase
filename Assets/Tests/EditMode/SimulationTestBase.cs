using System.Collections.Generic;
using System.IO;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Shared helpers for simulation tests: making throwaway content (cleaned up after each test),
    /// running ticks and whole runs, and saving and loading. Each test class adds only the setup
    /// that's specific to it.
    /// </summary>
    public abstract class SimulationTestBase
    {
        protected const int TicksPerSecond = 10;

        private readonly List<Object> _created = new List<Object>();

        /// <summary>The real game text, so messages read as the player sees them.</summary>
        [SetUp]
        public void LoadGameText() => GameText.Load(File.ReadAllText("Assets/Text/game_text.txt"));

        /// <summary>
        /// A skip reason as the game text file words it, e.g. Reason("needs", ("amount", 1), ("item", "Crowbar")).
        /// Tests compare against this rather than fixed English, so rewording the file never breaks them.
        /// </summary>
        protected static string Reason(string key, params (string name, object value)[] values)
        {
            // A missing key reads "[reasons.x?]" on both sides of the comparison, so check it's really there.
            Assert.That(GameText.Has("reasons." + key), Is.True, $"no line called reasons.{key} in the text file");
            return GameText.Get("reasons." + key, values);
        }

        [TearDown]
        public void DestroyCreatedContent()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
            _testFamily = null;
            _plan.Clear();
        }

        /// <summary>Remembers an object so it's destroyed after the test.</summary>
        protected T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        protected T Make<T>() where T : ScriptableObject => Track(ScriptableObject.CreateInstance<T>());

        /// <summary>
        /// Loop settings for a test: no pools. By default action and carry costs are charged and there's
        /// no passive drain, so the numbers stay exact (most tests check costs). With
        /// <paramref name="asShipped"/>, the game's own rules instead: costs off, the drain on.
        /// </summary>
        /// <param name="pockets">Pockets a run starts with (unset: the game's own).</param>
        /// <param name="floor">How many of each object a room's floor holds (unset: the game's own).</param>
        protected LoopSettings MakeLoopSettings(float vitalityMax = 100f, bool asShipped = false, int? pockets = null, int? floor = null)
        {
            var settings = Make<LoopSettings>();
            settings.vitalityMax = vitalityMax;
            if (pockets.HasValue)
                settings.pocketSlots = pockets.Value;
            if (floor.HasValue)
                settings.floorSpace = floor.Value;
            settings.pools = new List<LoopSettings.PoolSettings>();
            if (asShipped)
                return settings; // the field defaults are the agreed numbers
            settings.chargeActionCosts = true;
            settings.chargeCarryCosts = true;
            settings.masteryXpForFirstLevel = settings.xpForFirstLevel; // one curve for both, unless a test says otherwise
            // Endurance and Composure train on their own; off, like the drain, so stats stay exact.
            settings.enduranceXpPerVitalityLost = 0f;
            settings.composureXpPerSecond = 0f;
            settings.composureXpPerSecondCarrying = 0f;
            settings.enduranceBankShare = 0f; // a run's end banks nothing, so every run starts as long as the last
            settings.vitalityDrainPerSecond = 0f;
            settings.drainGrowthPerMinute = 0f;
            return settings;
        }

        protected static LoopSettings.PoolSettings HuePool(Hue hue, float max, bool startsUnlocked = true) =>
            new LoopSettings.PoolSettings { hue = hue, max = max, startsUnlocked = startsUnlocked };

        /// <param name="trains">A stat for its XP, given by choosing the kind that trains it: Perception (Search),
        /// Scholarship (Study) or Attunement (Instantiate). Endurance and Composure train on their own:
        /// set their XP directly (sim.Loop.AttributeXp) instead.</param>
        protected TaskDefinition MakeTask(string name, float seconds, ClaraAttribute trains = ClaraAttribute.None, bool startsUnlocked = true)
        {
            var task = Make<TaskDefinition>();
            task.displayName = name;
            // Every task has a family now; this one is x1 of the standard trip, stretched so the task takes
            // exactly its seconds in a first-depth room (or none), as the tests were written.
            task.family = TestFamily();
            task.durationMultiplier = CostCurve.MultiplierKeeping(seconds, task.family.durationCoefficient, DefaultSetting(s => s.standardTripSeconds), DefaultSetting(s => s.roomStep), 0);
            task.kind = trains switch
            {
                ClaraAttribute.None => TaskKind.Other,
                ClaraAttribute.Perception => TaskKind.Search,
                ClaraAttribute.Scholarship => TaskKind.Study,
                ClaraAttribute.Attunement => TaskKind.Instantiate,
                _ => throw new System.ArgumentException($"No kind of action trains {trains}: set its XP directly."),
            };
            task.startsUnlocked = startsUnlocked;
            return task;
        }

        private CostFamily _testFamily;

        /// <summary>A family of coefficient 1, shared by every test task made in this test.</summary>
        private CostFamily TestFamily()
        {
            if (_testFamily == null)
                _testFamily = Make<CostFamily>();
            return _testFamily;
        }

        // A loop setting as the game ships it, without needing a LoopSettings in the test.
        private static float DefaultSetting(System.Func<LoopSettings, float> read)
        {
            var settings = ScriptableObject.CreateInstance<LoopSettings>();
            float value = read(settings);
            Object.DestroyImmediate(settings);
            return value;
        }

        protected ResourceDefinition MakeResource(string name, ResourceLifetime lasts = ResourceLifetime.ThisRun, int max = 0)
        {
            var resource = Make<ResourceDefinition>();
            resource.displayName = name;
            resource.lasts = lasts;
            resource.startingMax = max;
            resource.goesInPocket = false; // tests that are about pockets ask for them (MakeObject)
            return resource;
        }

        /// <summary>An object: an item that takes a pocket (a wisp, a candle).</summary>
        protected ResourceDefinition MakeObject(string name, int max = 0, ResourceLifetime lasts = ResourceLifetime.ThisRun)
        {
            var item = MakeResource(name, lasts, max);
            item.goesInPocket = true;
            return item;
        }

        /// <summary>A task that gives something each time it's done, e.g. "Gather a wisp" giving 1 wisp.</summary>
        protected TaskDefinition MakeGatherTask(string name, float seconds, ResourceDefinition gives, int amount = 1)
        {
            var task = MakeTask(name, seconds);
            task.gives.Add(new ResourceAmount { resource = gives, amount = amount });
            return task;
        }

        protected static void SetCost(TaskDefinition task, float total, params (CostSource source, float percent)[] shares)
        {
            task.cost = total;
            foreach (var (source, percent) in shares)
                task.costShares.Add(new TaskDefinition.CostShare { source = source, percent = percent });
        }

        /// <summary>
        /// A task that costs vitality steadily: with nothing draining over time, this is how a test
        /// makes a run end after a known time.
        /// </summary>
        protected TaskDefinition MakeTiringTask(string name, float seconds, float vitalityCost)
        {
            var task = MakeTask(name, seconds);
            SetCost(task, vitalityCost, (CostSource.Vitality, 100f));
            return task;
        }

        protected SkillDefinition MakeSkill(string name)
        {
            var skill = Make<SkillDefinition>();
            skill.displayName = name;
            return skill;
        }

        /// <summary>A story beat from text: its first line is the title, the rest the passage.</summary>
        protected StoryBeat MakeStory(string text)
        {
            var beat = Make<StoryBeat>();
            beat.text = Track(new TextAsset(text));
            return beat;
        }

        protected NodeDefinition MakeNode(string name)
        {
            var node = Make<NodeDefinition>();
            node.displayName = name;
            return node;
        }

        /// <summary>
        /// Content with places: a 1s Travel verb, the start room and the other rooms. Join them with
        /// Join; add tasks with content.tasks.
        /// </summary>
        protected GameContent MakePlaces(NodeDefinition start, params NodeDefinition[] rooms)
        {
            var content = Make<GameContent>();
            content.travelVerb = MakeTask("Travel", 1f);
            content.startNode = start;
            content.nodes.Add(start);
            content.nodes.AddRange(rooms);
            return content;
        }

        /// <summary>A way from one room to another (both ways and open, unless told otherwise).</summary>
        protected static Way Join(NodeDefinition from, NodeDefinition to, bool bothWays = true, bool startsOpen = true)
        {
            var way = new Way { to = to, bothWays = bothWays, startsOpen = startsOpen };
            from.ways.Add(way);
            return way;
        }

        // ---------- Whole runs ----------

        // The plan each run follows: every run starts with an empty queue, so RunLoop queues it anew.
        private readonly List<(TaskDefinition task, int times)> _plan = new List<(TaskDefinition, int)>();

        /// <summary>Adds to the plan that every RunLoop queues at the start of its run.</summary>
        protected void Plan(TaskDefinition task, int times) => _plan.Add((task, times));

        protected void ClearPlan() => _plan.Clear();

        /// <summary>One run of the plan: begins it, queues the plan, and runs until it ends (or 20 seconds).</summary>
        protected void RunLoop(Simulation sim, float maxSeconds = 20f)
        {
            sim.BeginLoop();
            foreach (var (task, times) in _plan)
                sim.Schedule(task, times);
            for (int i = 0; i < maxSeconds * TicksPerSecond && sim.Phase == LoopPhase.Running; i++)
                sim.Tick();
        }

        // ---------- Saving ----------

        /// <summary>Saves to JSON text and reads it back, as a real save would. Give the result to a new Simulation.</summary>
        protected static PersistentState SaveAndLoad(Simulation sim, GameContent content, List<string> warnings = null)
        {
            string json = SaveSerializer.ToJson(SaveSerializer.Capture(sim));
            var data = SaveSerializer.FromJson(json, out string error);
            Assert.That(error, Is.Null);
            return SaveSerializer.Restore(data, new ContentIndex(content), warnings ?? new List<string>());
        }

        /// <summary>
        /// Saves the game to JSON and loads it back as a new simulation, as closing and reopening the
        /// game would: a run under way resumes, otherwise the queue held between runs comes back.
        /// </summary>
        protected static Simulation Reopen(Simulation sim, LoopSettings settings, GameContent content, List<string> warnings = null)
        {
            var data = SaveSerializer.FromJson(SaveSerializer.ToJson(SaveSerializer.Capture(sim)), out string error);
            Assert.That(error, Is.Null);
            return SaveSerializer.Load(data, new ContentIndex(content), settings, TicksPerSecond, content, warnings ?? new List<string>(), out _);
        }

        protected static void RunTicks(Simulation sim, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                sim.Tick();
        }

        protected static void RunSeconds(Simulation sim, float seconds) =>
            RunTicks(sim, Mathf.RoundToInt(seconds * TicksPerSecond));

        /// <summary>
        /// Queues a free action lasting this long (after anything already queued), for tests of what
        /// happens as time passes: with nothing queued, no time passes at all.
        /// </summary>
        protected void KeepBusy(Simulation sim, float seconds) => sim.Schedule(MakeTask("Wait", seconds), 1);

        /// <summary>
        /// Ticks until the run ends, and fails the test if it hasn't within <paramref name="maxSeconds"/>:
        /// a test that needs the run to end must not pass silently when it never does.
        /// </summary>
        protected static void RunUntilOver(Simulation sim, float maxSeconds)
        {
            for (int i = 0; i < maxSeconds * TicksPerSecond && !sim.Loop.IsOver; i++)
                sim.Tick();
            Assert.That(sim.Loop.IsOver, Is.True, $"the run was still going after {maxSeconds}s");
        }

        /// <summary>
        /// Listens for every refusal, whether an action was refused as it was asked for or skipped when its
        /// turn came, and returns the live list. Each reads "task: reason" (compare with Reason(...)), or just
        /// the reason with <paramref name="withTaskName"/> off.
        /// </summary>
        protected static List<string> RecordRefusals(Simulation sim, bool withTaskName = true)
        {
            var refusals = new List<string>();
            sim.TaskSkipped += (task, reason) => refusals.Add(withTaskName ? $"{task.displayName}: {reason}" : reason);
            sim.ActionRefused += (task, _, reason) => refusals.Add(withTaskName ? $"{task.displayName}: {reason}" : reason);
            return refusals;
        }

        /// <summary>What a room offers for this task, or null if it isn't offered there.</summary>
        protected static ActionOffer? OfferOf(Simulation sim, NodeDefinition room, TaskDefinition task)
        {
            var offers = new List<ActionOffer>();
            sim.OffersAt(room, offers);
            foreach (var offer in offers)
                if (offer.Task == task)
                    return offer;
            return null;
        }

        /// <summary>The queue's stops, as the queue drawer shows them.</summary>
        protected static List<QueueStop> StopsOf(Simulation sim)
        {
            var stops = new List<QueueStop>();
            sim.QueueStops(stops);
            return stops;
        }
    }
}
