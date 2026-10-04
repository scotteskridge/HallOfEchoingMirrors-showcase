using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The Summary's per-task charge entries (ui-051): which rising charges are named, and how.</summary>
    public class RunSummaryTextTests : SimulationTestBase
    {
        private TaskDefinition Task(string name)
        {
            var task = Make<TaskDefinition>();
            task.displayName = name;
            return task;
        }

        private static RunReport Report() => new RunReport(1, 600, 10, LoopEndReason.Exhausted, 0, null);

        private static string Entry(string task, int goes, string vitality) =>
            GameText.Get("results.charge_suffix", ("task", task), ("goes", goes), ("vitality", vitality));

        [Test]
        public void NoCharges_GivesNothing()
        {
            Assert.AreEqual("", RunSummaryText.ChargeSuffixes(Report()));
        }

        [Test]
        public void OneChargedTask_ReadsLikeMoves()
        {
            var run = Report();
            run.Charges.Add((Task("Practise the cut"), 4, 9.43f));
            Assert.AreEqual(" " + Entry("Practise the cut", 4, "9.4"), RunSummaryText.ChargeSuffixes(run));
        }

        [Test]
        public void EveryChargedTask_GetsAnEntry_InOrder()
        {
            var run = Report();
            run.Charges.Add((Task("Practise the cut"), 4, 9.4f));
            run.Charges.Add((Task("Light a candle"), 2, 3f));
            Assert.AreEqual(" " + Entry("Practise the cut", 4, "9.4") + " " + Entry("Light a candle", 2, "3"),
                RunSummaryText.ChargeSuffixes(run));
        }

        [Test]
        public void AGoCutShort_StillShows_WhenItCostVitality()
        {
            var run = Report();
            run.Charges.Add((Task("Practise the cut"), 0, 1.2f));
            Assert.AreEqual(" " + Entry("Practise the cut", 0, "1.2"), RunSummaryText.ChargeSuffixes(run));
        }

        [Test]
        public void AnEntryThatRoundsToZero_IsSkipped()
        {
            var run = Report();
            run.Charges.Add((Task("Practise the cut"), 1, 0.04f));
            Assert.AreEqual("", RunSummaryText.ChargeSuffixes(run));
        }

        [Test]
        public void TripsAreNotListedHere()
        {
            var run = Report();
            // Both setters are internal to Core (only the report builder sets them), and tests are another assembly.
            typeof(RunReport).GetProperty(nameof(RunReport.Moves)).GetSetMethod(true).Invoke(run, new object[] { 9 });
            typeof(RunReport).GetProperty(nameof(RunReport.MoveVitality)).GetSetMethod(true).Invoke(run, new object[] { 31f });
            run.Charges.Add((Task("Practise the cut"), 4, 9.4f));
            StringAssert.DoesNotContain("Moves", RunSummaryText.ChargeSuffixes(run));
            StringAssert.DoesNotContain("31", RunSummaryText.ChargeSuffixes(run));
        }
    }
}
