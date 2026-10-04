using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Reading the blurbs file, and choosing which bucket speaks and which line.</summary>
    public class BlurbTests : SimulationTestBase
    {
        private GameContent _content;
        private NodeDefinition _corridor, _dark;
        private TaskDefinition _explore, _search;
        private BlurbLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _corridor = Make<NodeDefinition>();
            _corridor.kind = NodeKind.Hall;
            _dark = Make<NodeDefinition>();
            _dark.kind = NodeKind.Constructed;
            _corridor.ways.Add(new Way { to = _dark });

            _explore = MakeTask("Explore", 10f);
            _search = MakeTask("Search", 10f);
            _search.kind = TaskKind.Search;
            _corridor.tasks.Add(_search);

            _content = Make<GameContent>();
            _content.travelVerb = MakeTask("Travel", 10f);
            _content.exploreVerb = _explore;
            _content.startNode = _corridor;
            _content.nodes.AddRange(new[] { _corridor, _dark });
            _corridor.exploresToFill = 10;

            _library = Make<BlurbLibrary>();
        }

        private BlurbBucket Bucket(string id, int priority, params string[] lines)
        {
            var bucket = Make<BlurbBucket>();
            bucket.bucketId = id;
            bucket.priority = priority;
            bucket.lines.AddRange(lines);
            _library.buckets.Add(bucket);
            return bucket;
        }

        private Simulation Running(params TaskDefinition[] build)
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            foreach (var task in build)
                sim.Schedule(task, 1);
            sim.BeginLoop();
            sim.Tick(); // the first task starts
            return sim;
        }

        [Test]
        public void TheFile_SplitsIntoBuckets_SkippingCommentsAndBlanks()
        {
            var sections = BlurbFile.Parse(
                "# a note about the file\n\n## hall_corridor\n# The mirrored corridor.\nThe frames go on.\n\n# another note\nA corridor only continues.\n## searching\nNothing.\n");

            Assert.That(sections.Count, Is.EqualTo(2));
            Assert.That(sections[0].Id, Is.EqualTo("hall_corridor"));
            Assert.That(sections[0].Description, Is.EqualTo("The mirrored corridor."));
            Assert.That(sections[0].Lines, Is.EqualTo(new[] { "The frames go on.", "A corridor only continues." }));
            Assert.That(sections[1].Lines, Is.EqualTo(new[] { "Nothing." }));
        }

        [Test]
        public void TheActivity_ComesFromWhatSheIsDoing()
        {
            Assert.That(BlurbPicker.ActivityOf(Running(_explore)), Is.EqualTo(BlurbActivity.Exploring));
            Assert.That(BlurbPicker.ActivityOf(Running(_search)), Is.EqualTo(BlurbActivity.Searching));
            Assert.That(BlurbPicker.ActivityOf(Running()), Is.EqualTo(BlurbActivity.Idle));
        }

        [Test]
        public void StartingBuckets_OnlySpeakAsAnActionBegins()
        {
            var starting = Bucket("start_explore", 20, "Looking around, I see nothing at first.");
            starting.moment = BlurbMoment.Starting;
            Bucket("during", 10, "still looking");
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, 0f, BlurbMoment.Starting), Is.EqualTo("Looking around, I see nothing at first."));
            Assert.That(picker.Pick(_library, sim, 0f), Is.EqualTo("still looking"), "the timer never uses a starting bucket");
        }

        [Test]
        public void Pick_ABucketForParticularTasks_IsOnlyChosenWhileThoseTasksRun()
        {
            var chase = MakeTask("Chase Roland", 10f);
            var chasing = Bucket("start_chase", 30, "I run.");
            chasing.moment = BlurbMoment.Starting;
            chasing.tasks.Add(chase);
            var picker = new BlurbPicker(seed: 1);

            Assert.That(picker.Pick(_library, Running(_explore), 0f, BlurbMoment.Starting), Is.Null, "not chasing");
            Assert.That(picker.Pick(_library, Running(chase), 0f, BlurbMoment.Starting), Is.EqualTo("I run."));
        }

        [Test]
        public void ABucketWithATopic_FitsAnyTaskWithThatTopic()
        {
            var gather = MakeTask("Gather a wisp", 10f);
            gather.blurbTopic = BlurbTopic.Gathering;
            var bucket = Bucket("gathering", 10, "gathering");
            bucket.topics.Add(BlurbTopic.Gathering);

            Assert.That(BlurbPicker.Fits(bucket, Running(gather), BlurbActivity.Working), Is.True,
                "fits a task tagged with this topic it has never seen before, with no task-list entry");
        }

        [Test]
        public void ABucketWithATopic_DoesNotFitADifferentTopic()
        {
            var instantiate = MakeTask("Instantiate a candle", 10f);
            instantiate.blurbTopic = BlurbTopic.Instantiating;
            var bucket = Bucket("gathering", 10, "gathering");
            bucket.topics.Add(BlurbTopic.Gathering);

            Assert.That(BlurbPicker.Fits(bucket, Running(instantiate), BlurbActivity.Working), Is.False);
        }

        [Test]
        public void ATaskSharingTheKindButNotTheTopic_DoesNotFit()
        {
            // The bug plan 018 first shipped: TaskKind is shared by unrelated tasks (Pick up and
            // Gather a wisp are both TaskKind.Gather), so matching by Kind swept in tasks with no
            // story in common. BlurbTopic is the separate, opt-in label that avoids that.
            var pickUp = MakeTask("Pick up", 5f);
            pickUp.kind = TaskKind.Gather;
            pickUp.blurbTopic = BlurbTopic.None;
            var bucket = Bucket("gathering", 10, "gathering");
            bucket.topics.Add(BlurbTopic.Gathering);

            Assert.That(BlurbPicker.Fits(bucket, Running(pickUp), BlurbActivity.Working), Is.False,
                "same TaskKind as a Gathering task, but untagged, so it stays silent");
        }

        [Test]
        public void AnEmptyTopicsList_IsUnrestricted()
        {
            var bucket = Bucket("anywhere", 10, "anywhere");
            Assert.That(BlurbPicker.Fits(bucket, Running(_search), BlurbActivity.Searching), Is.True);
        }

        [Test]
        public void TopicAndExplicitTask_BothMustMatchWhenBothSet()
        {
            var gather = MakeTask("Gather a wisp", 10f);
            gather.blurbTopic = BlurbTopic.Gathering;
            var otherGather = MakeTask("Gather something else", 10f);
            otherGather.blurbTopic = BlurbTopic.Gathering;

            var bucket = Bucket("this wisp only", 10, "this wisp only");
            bucket.topics.Add(BlurbTopic.Gathering);
            bucket.tasks.Add(gather);

            Assert.That(BlurbPicker.Fits(bucket, Running(gather), BlurbActivity.Working), Is.True);
            Assert.That(BlurbPicker.Fits(bucket, Running(otherGather), BlurbActivity.Working), Is.False,
                "right topic but not the specific task");
        }

        [Test]
        public void TheHighestPriorityBucketThatFits_Speaks()
        {
            Bucket("anywhere", 10, "general");
            var searching = Bucket("searching", 25, "looking");
            searching.activities = BlurbActivity.Searching;
            var picker = new BlurbPicker(seed: 1);

            Assert.That(picker.Pick(_library, Running(_search), 0f), Is.EqualTo("looking"));
            Assert.That(picker.Pick(_library, Running(_explore), 0f), Is.EqualTo("general"), "searching doesn't fit while exploring");
        }

        [Test]
        public void RoomsAndKinds_LimitWhereABucketSpeaks()
        {
            var dark = Bucket("hall_dark", 20, "the dark");
            dark.rooms.Add(_dark);
            var corridor = Bucket("hall_corridor", 20, "the corridor");
            corridor.roomKinds.Add(NodeKind.Hall);
            var picker = new BlurbPicker(seed: 1);

            Assert.That(picker.Pick(_library, Running(_explore), 0f), Is.EqualTo("the corridor"));
        }

        [Test]
        public void AQuietBucket_LetsTheNextOneSpeak()
        {
            var never = Bucket("never", 50, "urgent");
            never.chance = 0f;
            Bucket("fallback", 10, "quiet");

            Assert.That(new BlurbPicker(seed: 1).Pick(_library, Running(_explore), 0f), Is.EqualTo("quiet"));
        }

        [Test]
        public void LowVitality_OnlyFitsWhenShesLow()
        {
            var low = Bucket("low_vitality", 50, "cold hands");
            low.vitalityBelow = 0.3f;
            var sim = Running(_explore);

            Assert.That(BlurbPicker.Fits(low, sim, BlurbActivity.Exploring), Is.False);
        }

        [Test]
        public void ABucket_EndsWhenItsSwitchFlips_AndCanBeSwitchedOff()
        {
            var gem = Make<SwitchDefinition>();
            var reaching = Bucket("reaching", 30, "I reach for amber");
            reaching.endsWhenFlipped.Add(gem);
            var sim = Running(_explore);
            Assert.That(BlurbPicker.Fits(reaching, sim, BlurbActivity.Exploring), Is.True);

            sim.Persistent.FlippedSwitches.Add(gem);
            Assert.That(BlurbPicker.Fits(reaching, sim, BlurbActivity.Exploring), Is.False);

            reaching.enabled = false;
            _library.buckets.Clear();
            _library.buckets.Add(reaching);
            sim.Persistent.FlippedSwitches.Clear();
            Assert.That(new BlurbPicker(seed: 1).Pick(_library, sim, 0f), Is.Null, "switched off");
        }

        [Test]
        public void Tapering_FallsAwayBetweenTwoLoops()
        {
            var reaching = Make<BlurbBucket>();
            reaching.chance = 0.8f;
            reaching.taperFromLoop = 3;
            reaching.silentFromLoop = 5;

            Assert.That(BlurbPicker.ChanceOf(reaching, 2), Is.EqualTo(0.8f));
            Assert.That(BlurbPicker.ChanceOf(reaching, 4), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(BlurbPicker.ChanceOf(reaching, 5), Is.EqualTo(0f));
        }

        // Cooldowns count real seconds (the clock BlurbTeller hands in), so a fast game
        // doesn't make a blurb come round faster.
        [Test]
        public void Pick_SkipsBucketOnCooldown()
        {
            var bucket = Bucket("anywhere", 10, "a", "b");
            _library.defaultCooldownSeconds = 5f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.Not.Null, "it fires once");
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 1f), Is.Null, "still cooling down, even though it's the only fitting bucket");
        }

        [Test]
        public void Pick_CooldownIgnoresGameTime()
        {
            var bucket = Bucket("anywhere", 10, "a", "b");
            _library.defaultCooldownSeconds = 5f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.Not.Null);
            RunSeconds(sim, 8f); // a fast game: plenty of game time, but only 1 real second
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 1f), Is.Null, "real seconds are what count");
        }

        [Test]
        public void Pick_FallsThroughToNextBucketDuringCooldown()
        {
            var top = Bucket("top", 30, "urgent");
            var fallback = Bucket("fallback", 10, "quiet");
            _library.defaultCooldownSeconds = 5f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.EqualTo("urgent"));
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 1f), Is.EqualTo("quiet"), "top is cooling down, so fallback is heard");
        }

        [Test]
        public void Pick_BucketAvailableAfterCooldownElapses()
        {
            var bucket = Bucket("anywhere", 10, "a", "b");
            _library.defaultCooldownSeconds = 5f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.Not.Null);
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 5f), Is.Not.Null, "the cooldown has fully elapsed");
        }

        [Test]
        public void Pick_CooldownUsesBucketOverrideNotDefault()
        {
            var bucket = Bucket("anywhere", 10, "a", "b");
            bucket.cooldownSeconds = 1f;
            _library.defaultCooldownSeconds = 60f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);

            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.Not.Null);
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 1f), Is.Not.Null, "its own short cooldown, not the library's long default");
        }

        [Test]
        public void Pick_CooldownResetsEachRun()
        {
            var bucket = Bucket("anywhere", 10, "a", "b");
            _library.defaultCooldownSeconds = 60f;
            var picker = new BlurbPicker(seed: 1);
            var sim = Running(_explore);
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 0f), Is.Not.Null);

            sim.BeginLoop(); // a fresh run, a moment later
            sim.Schedule(_explore, 1);
            sim.Tick();
            Assert.That(picker.Pick(_library, sim, nowRealSeconds: 1f), Is.Not.Null, "a new run always finds it free");
        }

        [Test]
        public void Pick_FreeAgainOnADifferentSimulationEvenOnTheSameLoopNumber()
        {
            // BlurbTeller keeps one picker for the game's whole life, so a loaded save or a new game
            // hands it a different Simulation, possibly at the same loop number.
            var bucket = Bucket("anywhere", 10, "a", "b");
            _library.defaultCooldownSeconds = 60f;
            var picker = new BlurbPicker(seed: 1);
            var lateRun = Running(_explore);
            Assert.That(picker.Pick(_library, lateRun, nowRealSeconds: 0f), Is.Not.Null);

            var freshGame = Running(_explore); // loop number 1 again
            Assert.That(picker.Pick(_library, freshGame, nowRealSeconds: 1f), Is.Not.Null, "a different Simulation, not just a later moment in the same one");
        }

        [Test]
        public void NoLineRepeats_UntilTheWholeBucketHasBeenUsed()
        {
            var bucket = Make<BlurbBucket>();
            bucket.lines.AddRange(new[] { "a", "b", "c", "d", "e" });
            var picker = new BlurbPicker(seed: 7);

            var firstRound = new HashSet<string>();
            for (int i = 0; i < 5; i++)
                firstRound.Add(picker.Next(bucket));
            Assert.That(firstRound.Count, Is.EqualTo(5), "all five, no repeats");

            // Across many rounds, a line is never heard twice in a row.
            string last = null;
            for (int i = 0; i < 100; i++)
            {
                string line = picker.Next(bucket);
                Assert.That(line, Is.Not.EqualTo(last));
                last = line;
            }
        }
    }
}
