using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// A task with no cost family is a content mistake. Core reports it through Simulation.ContentProblem
    /// (the game sends that to the Console), once per task, and carries on: the task is refused with its
    /// reason instead of crashing the run. The shipped content is checked before play.
    /// </summary>
    public class ContentProblemTests : SimulationTestBase
    {
        private Simulation _sim;
        private TaskDefinition _broken;
        private readonly List<string> _problems = new List<string>();
        private readonly List<UnityEngine.Object> _brokenAssets = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            _sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            _sim.BeginLoop();
            _broken = MakeTask("Broken", 5f);
            _broken.family = null;
            _problems.Clear();
            _brokenAssets.Clear();
        }

        private void Listen() => _sim.ContentProblem = (message, asset) =>
        {
            _problems.Add(message);
            _brokenAssets.Add(asset);
        };

        [Test]
        public void TaskWithoutFamily_IsReported_NamingTheTask()
        {
            Listen();

            _sim.PriceOf(_broken, null);

            Assert.That(_problems, Has.Count.EqualTo(1));
            Assert.That(_problems[0], Does.Contain("Broken").And.Contain("family"));
            Assert.That(_brokenAssets[0], Is.SameAs(_broken), "so clicking the Console line selects the task");
        }

        [Test]
        public void TaskWithoutFamily_IsReportedOnce_NotEveryTimeItIsPriced()
        {
            Listen();

            for (int i = 0; i < 5; i++)
                _sim.PriceOf(_broken, null); // the UI prices things every frame

            Assert.That(_problems, Has.Count.EqualTo(1));
        }

        [Test]
        public void TwoTasksWithoutAFamily_AreEachReported()
        {
            Listen();
            var other = MakeTask("Other broken", 5f);
            other.family = null;

            _sim.PriceOf(_broken, null);
            _sim.PriceOf(other, null);

            Assert.That(_problems, Has.Count.EqualTo(2));
        }

        [Test]
        public void TaskWithoutFamily_HasNoTime_AndIsRefusedWithItsReason()
        {
            string refusal = null;
            _sim.ActionRefused += (_, _, why) => refusal = why;

            _sim.Schedule(_broken, 1);

            Assert.That(_sim.PriceOf(_broken, null).Seconds, Is.EqualTo(0f));
            Assert.That(refusal, Is.EqualTo(Reason("no_cost_family")));
            Assert.That(_sim.Queue.Count, Is.EqualTo(0), "nothing queued");
            Assert.That(_sim.Loop.CurrentTask, Is.Null);
        }

        [Test]
        public void TaskWithoutFamily_WithNobodyListening_StillCarriesOn()
        {
            Assert.That(_sim.ContentProblem, Is.Null);
            var fine = MakeTask("Fine", 2f);

            Assert.DoesNotThrow(() => _sim.Schedule(_broken, 1));
            _sim.Schedule(fine, 1);
            RunSeconds(_sim, 2.5f);

            Assert.That(_sim.Loop.CompletionLog.Count, Is.EqualTo(1), "the run goes on with the other task");
        }

        [Test]
        public void EveryShippedTask_HasACostFamily()
        {
            var missing = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:TaskDefinition", new[] { "Assets/Data" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var task = AssetDatabase.LoadAssetAtPath<TaskDefinition>(path);
                if (task.family == null)
                    missing.Add(path);
            }

            Assert.That(missing, Is.Empty, "give each a Family in its Inspector or the Balance Sheet: " + string.Join(", ", missing));
        }
    }
}
