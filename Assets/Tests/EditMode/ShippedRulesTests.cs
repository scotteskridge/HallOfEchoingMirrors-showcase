using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The game as it's actually played: action and carry costs off, vitality draining on its own
    /// (1 a second, growing 56.25% a minute: the halved game-time numbers, plan 022b). Most other tests switch these the other way round to
    /// keep their sums exact; these make sure the real rules hang together.
    /// </summary>
    public class ShippedRulesTests : SimulationTestBase
    {
        private Simulation Begin(float vitality = 100f, GameContent content = null)
        {
            var sim = new Simulation(MakeLoopSettings(vitality, asShipped: true), TicksPerSecond, content);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void EveryTaskHasAFamily_EveryRoomADepth()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
            var tasks = new HashSet<TaskDefinition>(content.tasks) { content.travelVerb, content.exploreVerb, content.pickUpVerb, content.putDownVerb };
            foreach (var node in content.PlayableNodes)
                tasks.UnionWith(node.tasks);
            foreach (var task in tasks)
                Assert.That(task.family, Is.Not.Null, $"{task.name} has no cost family");

            // Each room's depth as agreed on 2026-09-30 (plan 030a); the start room is the first.
            var depths = new Dictionary<string, int>
            {
                ["TheSmokyMirror"] = 0, ["Junction"] = 1, ["LeftCorridor"] = 2, ["RightCorridor"] = 2, ["HangingMirrors"] = 3, ["OtherLaboratory"] = 4,
            };
            foreach (var node in content.PlayableNodes)
            {
                Assert.That(depths.ContainsKey(node.name), Is.True, $"{node.name} has no agreed depth: add it here and set it on the room");
                Assert.That(node.depth, Is.EqualTo(depths[node.name]), $"{node.name}'s depth");
            }
            Assert.That(content.startNode.depth, Is.EqualTo(0));
        }

        [Test]
        public void Costs_AreOff_SoAnOrdinaryTaskOnlyCostsTime()
        {
            var sim = Begin();
            sim.Schedule(MakeTiringTask("Search", 10f, vitalityCost: 30f), 1);

            RunSeconds(sim, 10f);

            // Only the drain: about 1 a second for 10 seconds (a little more as it grows), not the task's 30.
            Assert.That(100f - sim.Loop.Vitality.Current, Is.EqualTo(10.4f).Within(0.4f));
        }

        [Test]
        public void ATaskThatRepeats_GoesOnUntilTheDrainEndsTheRun()
        {
            var sim = Begin(vitality: 10f);
            sim.Schedule(MakeTask("Wait", 1f)); // free, forever

            RunUntilOver(sim, 60f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(sim.SecondsThisRun, Is.EqualTo(9.6f).Within(1f), "10 vitality at 1 a second, growing");
            Assert.That(sim.Loop.CompletionLog.Count, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void TheChase_AlwaysCharged_ExhaustsHerMidChase_AndFlipsLostRoland()
        {
            var chase = MakeTiringTask("Chase Roland", 20f, vitalityCost: 300f);
            chase.alwaysCharged = true;
            var lost = Make<SwitchDefinition>();
            lost.trigger = SwitchTrigger.LoopEndedDuringTask;
            lost.triggerTask = chase;
            var content = Make<GameContent>();
            content.tasks.Add(chase);
            content.switches.Add(lost);
            var sim = Begin(content: content);
            sim.Schedule(chase, 1);

            RunUntilOver(sim, 30f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(sim.SecondsThisRun, Is.LessThan(8f), "300 over 20s is 15 a second: gone in under 7");
            Assert.That(sim.IsFlipped(lost), Is.True);
        }

        [Test]
        public void TheRing_AlwaysCharged_CostsOnTopOfTheDrain()
        {
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 1f;
            ring.alwaysCharged = true;
            var sim = Begin();
            sim.Loop.ToolsAndStats[ring] = 1;
            sim.Schedule(MakeTask("Wait", 10f), 1);

            RunSeconds(sim, 10f);

            // About 1 a second of drain (growing a little) plus the ring's 1 a second (Composure softens it a little as she trains).
            Assert.That(100f - sim.Loop.Vitality.Current, Is.InRange(19f, 21.5f));
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.GreaterThan(0f), "carrying what costs her trains Composure");
        }

        [Test]
        public void ARestorationItem_PushesBackAgainstTheDrain()
        {
            var wisp = MakeResource("Wisp", max: 10);
            wisp.restoreVitality = 5f;
            wisp.restoreSeconds = 5f;
            var withWisp = Begin();
            withWisp.Loop.ToolsAndStats[wisp] = 1;
            var without = Begin();
            KeepBusy(withWisp, 20f);
            KeepBusy(without, 20f);

            RunSeconds(withWisp, 20f);
            RunSeconds(without, 20f);

            Assert.That(withWisp.AmountOf(wisp), Is.EqualTo(0), "used once she'd lost 5");
            Assert.That(withWisp.Loop.Vitality.Current - without.Loop.Vitality.Current, Is.EqualTo(5f).Within(0.2f));
        }

        // Plan 055: after the first lighting has opened the Dark Corridor for good, a later run still
        // can't go in until the Left Corridor's candles are lit again that run.
        [Test]
        public void DarkCorridor_NeedsTheCandlesLitThisRun()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
            var settings = AssetDatabase.LoadAssetAtPath<LoopSettings>("Assets/Data/LoopSettings.asset");
            var stage = AssetDatabase.LoadAssetAtPath<DevJumpStage>("Assets/Data/Dev/BeforeTheTalk.asset");
            var left = AssetDatabase.LoadAssetAtPath<NodeDefinition>("Assets/Data/Places/LeftCorridor.asset");
            var dark = AssetDatabase.LoadAssetAtPath<NodeDefinition>("Assets/Data/Places/HangingMirrors.asset");
            var corridorLit = AssetDatabase.LoadAssetAtPath<ResourceDefinition>("Assets/Data/Items/CorridorCandlelight.asset");
            var alight = AssetDatabase.LoadAssetAtPath<SwitchDefinition>("Assets/Data/Switches/AllTwentyFiveAlight.asset");
            var sim = new Simulation(settings, TicksPerSecond, content);
            sim.BeginLoop();
            sim.JumpTo(stage);
            Assert.That(sim.IsFlipped(alight), Is.True, "set-up: the way was opened in an earlier run");
            sim.Loop.CurrentNode = left;

            sim.ScheduleTrip(dark);
            RunSeconds(sim, 30f);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(left), "no candles lit this run: she can't go in");

            sim.Loop.ToolsAndStats[corridorLit] = alight.triggerAmount;
            sim.ScheduleTrip(dark);
            RunSeconds(sim, 30f);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(dark), "lit again: the way lets her through");
        }

        // The user's rule (2026-10-02): Time × values are kept to 3 decimal places, so the Balance Sheet
        // shows numbers someone chose rather than raw floats left by a migration (0.3333333).
        [Test]
        public void EveryTasksTimeMultiplier_HasAtMostThreeDecimals()
        {
            var tasks = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TaskDefinition", new[] { "Assets/Data" }))
            {
                var task = AssetDatabase.LoadAssetAtPath<TaskDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                double thousandths = task.durationMultiplier * 1000.0;
                if (System.Math.Abs(thousandths - System.Math.Round(thousandths)) > 0.001)
                    tasks.Add($"{task.name} ({task.durationMultiplier})");
            }
            Assert.That(tasks, Is.Empty, "Time × with more than 3 decimals: round it in the Balance Sheet");
        }

        [Test]
        public void ActI_StartsWithEveryStatAsleep_AndHomeWakesThem()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LoopSettings>("Assets/Data/LoopSettings.asset");
            var home = AssetDatabase.LoadAssetAtPath<SwitchDefinition>("Assets/Data/Switches/Home.asset");
            var takesTheRing = AssetDatabase.LoadAssetAtPath<SwitchDefinition>("Assets/Data/Switches/RolandTakesTheRing.asset");

            Assert.That(settings.asleepAttributes, Is.EquivalentTo(AttributeMath.All), "Act I has no stats");
            Assert.That(home.wakesAttributes, Is.EquivalentTo(AttributeMath.All), "the first walk out wakes all five");
            Assert.That(takesTheRing.wakesAttributes, Is.Empty, "the talk wakes nothing");
        }
    }
}
