using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Rooms known by heart run the clock faster (plan 025): the multiplier per room, a trip at the
    /// slower end, and the hold that drops back to the player's speed when a decision is needed.
    /// Core only computes the number; the engine multiplies real time by it.
    /// </summary>
    public class RoomSpeedTests : SimulationTestBase
    {
        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _hall, _dark;
        private TaskDefinition _sweep, _dust, _blocked;
        private ResourceDefinition _quickened; // Feed your hours: what unlocks room speed

        // By heart at 4 runs, full speed at 8, ×5: 1.8× at 4 runs and 5× at 8.
        [SetUp]
        public void SetUp()
        {
            _settings = MakeLoopSettings();
            _settings.byHeartRuns = 4;
            _settings.fullSpeedRuns = 8;
            _settings.roomSpeedCap = 5f;

            _quickened = MakeResource("Quickened Hours", ResourceLifetime.Forever, max: 1);
            _quickened.unlocksRoomSpeed = true;
            var moon = MakeObject("Moon", max: 5);
            _sweep = MakeTask("Sweep", 10f);
            _dust = MakeTask("Dust", 10f);
            _blocked = MakeTask("Catch the moon", 1f);
            _blocked.needs.Add(new ResourceAmount { resource = moon, amount = 1 });

            _hall = MakeNode("The hall");
            _dark = MakeNode("The dark");
            Join(_hall, _dark);
            _hall.tasks.AddRange(new[] { _sweep, _blocked });
            _dark.tasks.Add(_dust);

            _content = MakePlaces(_hall, _dark);
            _content.tasks.AddRange(new[] { _sweep, _dust, _blocked });
            _content.tasks.Add(MakeGatherTask("Feed your hours", 1f, _quickened)); // reaches the item for saves
        }

        // Room speed is earned by Feeding the hours, so most tests start with it.
        private Simulation NewGame(int hallRuns = 0, int darkRuns = 0, bool unlocked = true)
        {
            var sim = new Simulation(_settings, TicksPerSecond, _content);
            if (unlocked) sim.Persistent.Resources[_quickened] = 1;
            if (hallRuns > 0) sim.Persistent.RoomRuns[_hall] = hallRuns;
            if (darkRuns > 0) sim.Persistent.RoomRuns[_dark] = darkRuns;
            return sim;
        }

        [Test]
        public void BelowByHeart_IsOne()
        {
            var sim = NewGame(hallRuns: 3);
            Assert.That(sim.RoomSpeed(_hall), Is.EqualTo(1f));
            Assert.That(sim.RoomSpeed(_dark), Is.EqualTo(1f));
        }

        [Test]
        public void Ramp_HitsCap_AtFullSpeedRuns()
        {
            var sim = NewGame(hallRuns: 4);
            Assert.That(sim.RoomSpeed(_hall), Is.EqualTo(1f + 4f * 1f / 5f).Within(0.001f), "first by-heart run is already a step up");

            sim.Persistent.RoomRuns[_hall] = 8;
            Assert.That(sim.RoomSpeed(_hall), Is.EqualTo(5f).Within(0.001f));

            sim.Persistent.RoomRuns[_hall] = 20;
            Assert.That(sim.RoomSpeed(_hall), Is.EqualTo(5f).Within(0.001f), "never past the cap");
        }

        [Test]
        public void NothingRunning_IsOne()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f));
        }

        [Test]
        public void RoomSpeedNow_IsOne_InByHeartRoom_UntilUnlocked()
        {
            var sim = NewGame(hallRuns: 8, unlocked: false);
            sim.BeginLoop();
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 1f);
            Assert.That(sim.RoomSpeedUnlocked, Is.False);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f));
            Assert.That(sim.RoomSpeed(_hall), Is.EqualTo(5f).Within(0.001f), "the would-be value stays");
        }

        [Test]
        public void RoomSpeedNow_ByHeartRoom_AfterUnlock_IsCapped()
        {
            var sim = NewGame(hallRuns: 8, unlocked: false);
            sim.BeginLoop();
            sim.Persistent.Resources[_quickened] = 1;
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 1f);
            Assert.That(sim.RoomSpeedUnlocked, Is.True);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void RoomSpeedUnlocked_KeptThroughSave()
        {
            var sim = NewGame(hallRuns: 8);
            var warnings = new System.Collections.Generic.List<string>();
            var loaded = new Simulation(_settings, TicksPerSecond, _content, SaveAndLoad(sim, _content, warnings));
            Assert.That(warnings, Is.Empty);
            Assert.That(loaded.RoomSpeedUnlocked, Is.True);
        }

        [Test]
        public void ActionInAByHeartRoom_UsesItsSpeed()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 1f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Trip_UsesSlowerRoom()
        {
            var sim = NewGame(hallRuns: 8, darkRuns: 0);
            sim.BeginLoop();
            sim.ScheduleTrip(_dark);
            RunSeconds(sim, 0.5f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f), "from the hall by heart into the unknown dark");

            sim.Persistent.RoomRuns[_dark] = 4;
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1.8f).Within(0.001f), "the dark is the slower end now");
        }

        // The hold covers the action that follows the event, so the player sees what it did at their own speed.

        [Test]
        public void RefusedAction_HoldsSpeed_ThroughTheNextAction()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            sim.Schedule(_sweep, 3); // three 10 s sweeps
            RunSeconds(sim, 1f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f));

            sim.PlayNow(_blocked); // can't be started and nothing supplies the moon: refused
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f));

            RunSeconds(sim, 9.5f); // the second sweep has started
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f), "still held: the action after the refusal");

            RunSeconds(sim, 10f); // the third has
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f), "released once that action was done");
        }

        [Test]
        public void SkippedAction_HoldsSpeed_ThroughTheActionThatFollows()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            sim.Schedule(_sweep, 1);
            sim.Schedule(_blocked, 1); // fine when queued: skipped when its turn comes
            sim.Schedule(_sweep, 2);
            RunSeconds(sim, 5f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f), "the first sweep, nothing held yet");

            RunSeconds(sim, 10f); // the skip happened; the next sweep is running
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f), "the action after the skip");

            RunSeconds(sim, 10f); // that one is done, the last sweep is running
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Milestone_HoldsSpeed_ThroughTheActionThatFollows()
        {
            var sim = NewGame(hallRuns: 8, darkRuns: 8);
            sim.BeginLoop();
            sim.ScheduleTrip(_dark); // entering a room for the first time is a milestone
            sim.Schedule(_dust, 2);
            RunSeconds(sim, 5f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(1f), "the first dusting, after the arrival");

            RunSeconds(sim, 10f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f), "the second dusting");
        }

        [Test]
        public void HeldSpeed_IsLetGo_WhenTheNextRunBegins()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            sim.Schedule(_sweep, 3);
            RunSeconds(sim, 1f);
            sim.PlayNow(_blocked); // refused: held
            Assert.That(sim.SpeedHeld, Is.True);

            sim.EndRunEarly();
            sim.Schedule(_sweep, 1);
            sim.BeginLoop();
            Assert.That(sim.SpeedHeld, Is.False, "a new run starts unheld");
            RunSeconds(sim, 1f);
            Assert.That(sim.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void HeldSpeed_IsNotSaved_AResumedRunStartsUnheld()
        {
            var sim = NewGame(hallRuns: 8);
            sim.BeginLoop();
            sim.Schedule(_sweep, 3);
            RunSeconds(sim, 1f);
            sim.PlayNow(_blocked); // refused: held
            Assert.That(sim.SpeedHeld, Is.True);

            var warnings = new System.Collections.Generic.List<string>();
            var reopened = Reopen(sim, _settings, _content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.SpeedHeld, Is.False);
            Assert.That(reopened.RoomSpeedNow, Is.EqualTo(5f).Within(0.001f), "the sweep carries on at room speed");
        }

        [Test]
        public void RoomSpeed_DoesNotChangeDrainPerGameSecond()
        {
            // Core counts in ticks: a by-heart room changes how fast ticks arrive, not what one tick does.
            _settings.vitalityDrainPerSecond = 1f;
            var unknown = NewGame();
            var known = NewGame(hallRuns: 8);
            foreach (var sim in new[] { unknown, known })
            {
                sim.BeginLoop();
                sim.Schedule(_sweep, 1);
                RunSeconds(sim, 5f);
            }
            Assert.That(known.Loop.Vitality.Current, Is.EqualTo(unknown.Loop.Vitality.Current).Within(0.001f));
            Assert.That(known.Loop.CurrentTaskWorkDone, Is.EqualTo(unknown.Loop.CurrentTaskWorkDone).Within(0.001f));
        }
    }
}
