using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Plan 030a: every task's time comes from one curve (family × standard trip × roomStep^depth ×
    /// the task's own multiplier), and its XP follows that final time.
    /// </summary>
    public class CostCurveTests : SimulationTestBase
    {
        private const float Trip = 15f, Step = 1.1f;

        private Simulation Begin(GameContent content = null)
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.BeginLoop();
            return sim;
        }

        private NodeDefinition MakeRoom(string name, int depth)
        {
            var room = MakeNode(name);
            room.depth = depth;
            return room;
        }

        [Test]
        public void BaseSeconds_IsCoefficientTimesTripTimesStepToDepth()
        {
            Assert.That(CostCurve.BaseSeconds(1f, 15f, 1.1f, 0, 1f), Is.EqualTo(15f).Within(0.0001f), "a trip-long task in the first room");
            Assert.That(CostCurve.BaseSeconds(0.5f, 15f, 1.1f, 2, 2f), Is.EqualTo(0.5f * 15f * 1.21f * 2f).Within(0.0001f),
                "coefficient, trip, 1.1 twice over for depth 2, and the task's own ×2");
            Assert.That(CostCurve.BaseSeconds(0.2f, 15f, 1f, 4, 1f), Is.EqualTo(3f).Within(0.0001f), "a room step of 1 flattens depth");
        }

        [Test]
        public void MultiplierKeeping_ReturnsTodaysSeconds()
        {
            foreach (var (seconds, coefficient, depth) in new[] { (10f, 0.7f, 0), (22.5f, 1.5f, 3), (0.5f, 0.1f, 2), (40f, 1.2f, 4) })
            {
                float multiplier = CostCurve.MultiplierKeeping(seconds, coefficient, Trip, Step, depth);
                Assert.That(CostCurve.BaseSeconds(coefficient, Trip, Step, depth, multiplier), Is.EqualTo(seconds).Within(0.0001f),
                    $"{seconds} s at depth {depth}");
            }
        }

        [Test]
        public void MakeTask_KeepsItsSeconds_SoTheOlderTestsKeepTheirTimes()
        {
            var sim = Begin();
            Assert.That(sim.PriceOf(MakeTask("Read", 7.5f), null).Seconds, Is.EqualTo(7.5f).Within(0.0001f));
        }

        [Test]
        public void DurationMultiplier_ScalesTimeAndXpTogether()
        {
            var sim = Begin();
            var plain = MakeTask("Plain", 10f);
            var slow = MakeTask("Slow", 10f);
            slow.durationMultiplier *= 2f;

            Assert.That(sim.PriceOf(slow, null).Seconds, Is.EqualTo(2f * sim.PriceOf(plain, null).Seconds).Within(0.0001f));
            Assert.That(sim.XpRewardOf(plain, null), Is.EqualTo(15f), "1.5 a second of a 10 s task");
            Assert.That(sim.XpRewardOf(slow, null), Is.EqualTo(30f), "a ×2 duration pays ×2 XP");
        }

        [Test]
        public void XpMultiplier_ChangesXpNotTime()
        {
            var sim = Begin();
            var plain = MakeTask("Plain", 10f);
            var rich = MakeTask("Rich", 10f);
            rich.xpMultiplier = 2f;

            Assert.That(sim.PriceOf(rich, null).Seconds, Is.EqualTo(sim.PriceOf(plain, null).Seconds).Within(0.0001f));
            Assert.That(sim.XpRewardOf(rich, null), Is.EqualTo(2f * sim.XpRewardOf(plain, null)));
        }

        [Test]
        public void SharedTask_UsesDepthOfRoomWhereDone()
        {
            var shallow = MakeRoom("Shallow", 0);
            var deep = MakeRoom("Deep", 2);
            var content = MakePlaces(shallow, deep);
            var sim = Begin(content);
            var shared = MakeTask("Shared", 10f);

            float here = sim.PriceOf(shared, shallow).Seconds;
            Assert.That(sim.PriceOf(shared, deep).Seconds, Is.EqualTo(here * Step * Step).Within(0.0001f), "1.1 a room deeper, twice");
            Assert.That(sim.XpRewardOf(shared, deep), Is.EqualTo(Mathf.Ceil(1.5f * here * Step * Step - 0.0001f)), "XP follows the final time");

            // A trip is priced by the room she leaves, whichever way she goes.
            Assert.That(sim.PriceOf(content.travelVerb, deep, shallow).Seconds,
                Is.EqualTo(sim.PriceOf(content.travelVerb, shallow, deep).Seconds * Step * Step).Within(0.0001f));
        }

        [Test]
        public void NoRooms_UsesDepthZero()
        {
            var sim = Begin();
            var task = MakeTask("Anywhere", 6f);

            Assert.That(sim.PriceOf(task, null).Seconds, Is.EqualTo(6f).Within(0.0001f));
        }

        [Test]
        public void RunningATask_TakesTheRoomsTime_AndPaysTheRoomsXp()
        {
            var deep = MakeRoom("Deep", 3);
            var content = MakePlaces(deep);
            var skill = MakeSkill("Study");
            var sim = Begin(content);
            var task = MakeTask("Read", 10f);
            task.skill = skill;
            sim.Schedule(task, 1);
            float seconds = 10f * Mathf.Pow(Step, 3);

            RunSeconds(sim, seconds + 0.2f);

            Assert.That(sim.Loop.CompletionLog.Count, Is.EqualTo(1), "done after its depth-3 time");
            Assert.That(sim.Loop.XpOf(skill), Is.EqualTo(Mathf.Ceil(1.5f * seconds - 0.0001f)).Within(0.05f));
        }

        [Test]
        public void TaskWithoutFamily_FailsLoudly_AndCantStart()
        {
            var sim = Begin();
            var broken = MakeTask("Broken", 5f);
            broken.family = null;
            string refusal = null, problem = null;
            sim.ActionRefused += (_, _, why) => refusal = why;
            sim.ContentProblem = (message, _) => problem = message; // the game sends this to the Console

            sim.Schedule(broken, 1);

            Assert.That(problem, Does.Match("Broken.*family"), "the developer is told, loudly");
            Assert.That(refusal, Is.Not.Null, "it is refused, not started");
            Assert.That(sim.Loop.CurrentTask, Is.Null);
        }
    }
}
