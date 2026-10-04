using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>When the game pauses for the player, and when queuing something carries on.</summary>
    public class PauseControlTests : SimulationTestBase
    {
        private readonly List<bool> _changes = new List<bool>();

        private (Simulation sim, PauseControl pause) Begin(LoopSettings settings = null, GameContent content = null)
        {
            _changes.Clear();
            var sim = new Simulation(settings ?? MakeLoopSettings(), TicksPerSecond, content);
            var pause = new PauseControl(sim, settings ?? MakeLoopSettings());
            pause.PausedChanged += paused => _changes.Add(paused);
            sim.BeginLoop();
            return (sim, pause);
        }

        [Test]
        public void ARunWithNothingQueued_PausesForThePlayer_WithAReason()
        {
            var (sim, pause) = Begin();

            sim.Tick();

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.Reason, Is.EqualTo(GameText.Get("run.pause_reason.queue_empty")));
            Assert.That(_changes, Is.EqualTo(new[] { true }), "the clock is told once");
        }

        [Test]
        public void QueuingSomething_WhileSheWaits_CarriesOnAtOnce()
        {
            var (sim, pause) = Begin();
            sim.Tick();

            sim.Schedule(MakeTask("Search", 1f), 1);

            Assert.That(pause.IsPaused, Is.False);
            Assert.That(pause.Reason, Is.Null);
        }

        [Test]
        public void APauseThePlayerChose_StaysWhenSomethingIsQueued()
        {
            var (sim, pause) = Begin();
            sim.Tick(); // paused by the game: nothing queued
            pause.Pause(); // now the player's own pause

            sim.Schedule(MakeTask("Search", 1f), 1);

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.Reason, Is.EqualTo(GameText.Get("run.pause_reason.paused")));
        }

        [Test]
        public void ASingleAction_PausesWhenItsDone()
        {
            var (sim, pause) = Begin();
            var pull = MakeTask("Pull the lever", 1f);
            pull.singleAction = true;
            sim.Schedule(pull);
            sim.Schedule(MakeTask("Look", 5f), 1); // more queued: it still stops, so she can choose

            RunSeconds(sim, 1f);

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.Reason, Is.EqualTo(GameText.Get("run.pause_reason.single_action", ("task", "Pull the lever"))));
        }

        [Test]
        public void AFoundWay_Pauses_OnlyIfLoopSettingsSaysSo([Values(true, false)] bool pauseWhenAWayIsFound)
        {
            var explore = MakeTask("Explore", 1f);
            var hall = MakeNode("The hall");
            hall.exploresToFill = 1;
            var junction = MakeNode("The junction");
            hall.ways.Add(new Way { to = junction, foundAtExplored = 100 });
            var content = Make<GameContent>();
            content.travelVerb = MakeTask("Travel", 1f);
            content.exploreVerb = explore;
            content.startNode = hall;
            content.nodes.AddRange(new[] { hall, junction });
            var settings = MakeLoopSettings();
            settings.pauseWhenAWayIsFound = pauseWhenAWayIsFound;
            var (sim, pause) = Begin(settings, content);
            sim.Schedule(explore, 1);
            sim.Schedule(MakeTask("Look", 5f), 1); // still busy after, so only the way could pause it

            RunSeconds(sim, 1f);

            Assert.That(pause.IsPaused, Is.EqualTo(pauseWhenAWayIsFound));
        }

        [Test]
        public void Resuming_TellsTheClockOnce()
        {
            var (sim, pause) = Begin();
            pause.Pause();
            sim.Schedule(MakeTask("Search", 1f), 1);

            pause.Resume();
            pause.Resume();

            Assert.That(_changes, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Resuming_WithNothingQueued_StaysPaused_AndSaysWhy()
        {
            var (sim, pause) = Begin();
            sim.Schedule(MakeTask("Search", 1f), 1);
            RunSeconds(sim, 2f); // done, and the queue has run out

            pause.Resume();

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.Reason, Is.EqualTo(GameText.Get("run.pause_reason.queue_empty")));
            sim.Schedule(MakeTask("Look", 1f), 1);
            Assert.That(pause.IsPaused, Is.False, "queuing something still carries on");
        }
    }
}
