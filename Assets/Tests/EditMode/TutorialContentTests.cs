using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Plays the real tutorial content (the assets in Assets/Data), not made-up test content,
    /// so a change to the data that breaks the opening shows up here. Read-only: nothing is saved.
    /// </summary>
    public class TutorialContentTests : SimulationTestBase
    {
        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _hallMirror, _junction;
        private TaskDefinition _chase;
        private SwitchDefinition _lostRoland;

        [SetUp]
        public void SetUp()
        {
            // The real words, whatever an earlier test loaded, so messages don't depend on test order.
            GameText.Load(System.IO.File.ReadAllText("Assets/Text/game_text.txt"));
            _content = Load<GameContent>("Assets/Data/GameContent.asset");
            _settings = Load<LoopSettings>("Assets/Data/LoopSettings.asset");
            _hallMirror = Load<NodeDefinition>("Assets/Data/Places/TheSmokyMirror.asset");
            _junction = Load<NodeDefinition>("Assets/Data/Places/Junction.asset");
            _chase = Load<TaskDefinition>("Assets/Data/Tasks/Tutorial/ChaseRoland.asset");
            _lostRoland = Load<SwitchDefinition>("Assets/Data/Switches/LostRoland.asset");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"missing {path}");
            return asset;
        }

        [Test]
        public void ShippedSettings_KnowARoomByHeartAfterFourRuns()
        {
            Assert.That(_settings.byHeartRuns, Is.EqualTo(4), "the agreed placeholder; if balancing changed it on purpose, change this too");
        }

        // Plan 032a: planning comes with Feed your hours; the Dark Corridor shows on the map once found.
        [Test]
        public void Quickened_UnlocksPlanning()
        {
            var quickened = Load<ResourceDefinition>("Assets/Data/Items/QuickenedHours.asset");
            Assert.That(quickened.unlocksPlanning, Is.True);
            Assert.That(quickened.unlocksRoomSpeed, Is.True);
        }

        [Test]
        public void CorridorWay_ShowsWhileShut()
        {
            var corridor = Load<NodeDefinition>("Assets/Data/Places/LeftCorridor.asset");
            var dark = Load<NodeDefinition>("Assets/Data/Places/HangingMirrors.asset");
            var way = corridor.ways.Find(w => w.to == dark);
            Assert.That(way, Is.Not.Null);
            Assert.That(way.startsOpen, Is.False, "the candles open it");
            Assert.That(way.showWhileShut, Is.True);
            Assert.That(GameText.Has(way.shutMessageKey), Is.True, $"the shut message key \"{way.shutMessageKey}\" is in game_text.txt");
        }

        [Test]
        public void Loop1_TheChaseFails_AndOpensTheHall()
        {
            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            Assert.That(sim.TasksAt(_hallMirror), Is.EqualTo(new[] { _chase }), "loop 1 offers only the chase");

            sim.Schedule(_chase, 1);
            sim.BeginLoop();
            RunUntilOver(sim, 300);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(sim.IsFlipped(_lostRoland), Is.True);
            Assert.That(sim.TasksAt(_hallMirror), Has.Member(sim.ExploreVerb), "Explore opens after the chase");
            var gatherWisp = Load<TaskDefinition>("Assets/Data/Tasks/Hall/GatherAWisp.asset");
            Assert.That(sim.IsUnlocked(gatherWisp), Is.True, "and so do wisps...");
            Assert.That(_hallMirror.Lists(gatherWisp) && !sim.TasksAt(_hallMirror).Contains(gatherWisp), Is.True,
                "...found by searching the smoky mirror, not on offer before it");
        }

        [Test]
        public void TheHallsCandles_AreLitOneAtATime_AndAllOfThemPushTheDarknessBack()
        {
            var light = Load<TaskDefinition>("Assets/Data/Tasks/Hall/LightTheCandles.asset");
            var hallLit = Load<ResourceDefinition>("Assets/Data/Items/Candlelight.asset"); // "Candles lit in A Dark Hall"

            Assert.That(_junction.Lists(light), Is.True, "lit in A Dark Hall");
            Assert.That(light.gives.Exists(g => g.resource == hallLit && g.amount == 1), Is.True, "one at a time");
            Assert.That(hallLit.IsPocketed, Is.False, "a count, not an object: it takes no pocket");
            Assert.That(hallLit.lasts, Is.EqualTo(ResourceLifetime.ThisRun), "lit again every run");
            Assert.That(hallLit.DrainMultiplierFor(hallLit.startingMax), Is.LessThan(1f), "all of them ease the drain");
            Assert.That(hallLit.FloorSpaceFor(hallLit.startingMax), Is.GreaterThan(0), "and make room on the floor");
        }

        [Test]
        public void TheDark_OpensForGood_TheFirstTimeCandlesCarriedToTheLeftCorridorAreLit()
        {
            var left = Load<NodeDefinition>("Assets/Data/Places/LeftCorridor.asset");
            var dark = Load<NodeDefinition>("Assets/Data/Places/HangingMirrors.asset");
            var makeCandle = Load<TaskDefinition>("Assets/Data/Tasks/Hall/InstantiateACandle.asset");
            var lightHere = Load<TaskDefinition>("Assets/Data/Tasks/Hall/LightACandleInTheCorridor.asset");
            var corridorLit = Load<ResourceDefinition>("Assets/Data/Items/CorridorCandlelight.asset");
            var alight = Load<SwitchDefinition>("Assets/Data/Switches/AllTwentyFiveAlight.asset");

            Assert.That(left.Lists(lightHere) && lightHere.gives.Exists(g => g.resource == corridorLit), Is.True,
                "the corridor has its own Light a candle and its own count");
            Assert.That(left.Lists(makeCandle), Is.False, "candles aren't made here: they must be carried from the hall");

            var way = left.ways.Find(w => w != null && w.to == dark);
            Assert.That(way, Is.Not.Null);
            Assert.That(way.startsOpen, Is.False, "shut until the puzzle is solved");
            // The user's rule, 2026-10-02 (plan 055): once open, the candles must still be lit again every run.
            Assert.That(way.needs.Exists(n => n.resource == corridorLit && n.amount == alight.triggerAmount), Is.True,
                "and once open, it needs all the corridor's candles lit again in every run");
            Assert.That(alight.trigger, Is.EqualTo(SwitchTrigger.ResourceReached));
            Assert.That(alight.resourceToHold, Is.EqualTo(corridorLit));
            Assert.That(alight.opensWays.Exists(w => w != null && w.from == left && w.to == dark), Is.True,
                "lighting them the first time opens the way for good");
            Assert.That(alight.triggerAmount, Is.LessThanOrEqualTo(corridorLit.startingMax), "reachable");
        }

        [Test]
        public void TheLeftCorridor_HidesASatchel_ThatAddsPockets()
        {
            var left = Load<NodeDefinition>("Assets/Data/Places/LeftCorridor.asset");

            var satchel = Load<ResourceDefinition>("Assets/Data/Items/Satchel.asset");

            var satchelFind = left.foundBySearching.Find(f => f?.task != null && f.task.gives.Exists(g => g.resource == satchel));

            Assert.That(satchelFind, Is.Not.Null, "found by searching the left corridor");
            Assert.That(satchel.addsPockets, Is.GreaterThan(0), "it adds pockets");
            Assert.That(satchel.IsPocketed, Is.False, "and takes none itself");
        }

        [Test]
        public void FeedYourHours_GivesQuickenedHours_WhichUnlocksRoomSpeed_AndDrainsWhileItRuns()
        {
            var feed = Load<TaskDefinition>("Assets/Data/Tasks/Hall/FeedYourHoursToTheFlames.asset");
            var quickened = Load<ResourceDefinition>("Assets/Data/Items/QuickenedHours.asset");
            Assert.That(feed.gives.Exists(give => give.resource == quickened), "Feed gives Quickened Hours");
            Assert.That(quickened.unlocksRoomSpeed, "which unlocks room speed");
            Assert.That(quickened.unlocksSpeed, Is.EqualTo(0f), "and no longer the ×2 tier");
            Assert.That(feed.extraDrainPerSecond, Is.GreaterThan(0f), "she pays in drain while she does it");
            Assert.That(feed.HasCost, Is.False, "and nothing up front");
        }

        [Test]
        public void Loop2_ExploringTheHallMirror_FindsTheWayToTheJunction()
        {
            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            sim.Schedule(_chase, 1);
            sim.BeginLoop();
            RunUntilOver(sim, 300);

            var found = new List<NodeDefinition>();
            sim.WayFound += (from, to) => found.Add(to);
            var skipped = RecordRefusals(sim);

            sim.Schedule(sim.ExploreVerb, _hallMirror.exploresToFill);
            sim.BeginLoop();
            for (int i = 0; i < 120 * TickEngine.TicksPerSecond && !sim.Loop.IsOver && found.Count == 0; i++)
                sim.Tick();

            Assert.That(skipped, Is.Empty);
            Assert.That(sim.ExploresDoneIn(_hallMirror), Is.EqualTo(_hallMirror.exploresToFill));
            Assert.That(found, Is.EqualTo(new[] { _junction }));
            Assert.That(sim.DestinationsFrom(_hallMirror), Has.Member(_junction));
            Assert.That(sim.RoomsOnMap(), Has.Member(_junction));
        }
    }
}
