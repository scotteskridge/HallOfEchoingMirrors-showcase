using System.Collections.Generic;
using System.IO;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    public class SaveTests : SimulationTestBase
    {
        private GameContent _content;
        private TaskDefinition _explore;
        private TaskDefinition _chase;
        private ResourceDefinition _mirrors;
        private SwitchDefinition _five;
        private StoryBeat _opening;

        // A small world: exploring finds mirrors (kept); at 5 a switch flips, unlocking Amber and
        // retiring the chase; there's an opening story.
        [SetUp]
        public void SetUp()
        {
            _mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            _explore = MakeTask("Explore", 1f, ClaraAttribute.Perception);
            _explore.gives.Add(new ResourceAmount { resource = _mirrors, amount = 1 });
            _chase = MakeTask("Chase", 1f);

            _opening = MakeStory("Opening\nShe ran.");

            _five = Make<SwitchDefinition>();
            _five.trigger = SwitchTrigger.ResourceReached;
            _five.resourceToHold = _mirrors;
            _five.triggerAmount = 5;
            _five.unlocksPools.Add(Hue.Amber);
            _five.locksTasks.Add(_chase);

            _content = Make<GameContent>();
            _content.openingStory = _opening;
            _content.tasks.Add(_explore);
            _content.tasks.Add(_chase);
            _content.switches.Add(_five);
        }

        private LoopSettings Settings()
        {
            var settings = MakeLoopSettings(vitalityMax: 10f);
            settings.pools.Add(HuePool(Hue.Amber, 50f, startsUnlocked: false));
            return settings;
        }

        private Simulation NewGame() => new Simulation(Settings(), TicksPerSecond, _content);

        /// <summary>Save to JSON text and load it back into a brand-new game, as a real save would.</summary>
        private Simulation SaveAndReload(Simulation sim, List<string> warnings = null)
        {
            return new Simulation(Settings(), TicksPerSecond, _content, SaveAndLoad(sim, _content, warnings));
        }

        /// <summary>A game that has played two loops of exploring: 6 mirrors, the switch flipped.</summary>
        private Simulation PlayedGame()
        {
            var sim = NewGame();
            Plan(_explore, 3);
            RunLoop(sim);
            RunLoop(sim);
            return sim;
        }

        [Test]
        public void GameTimePlayed_AddsUpEveryRun_AndSurvivesSavingAndLoading()
        {
            var sim = NewGame();
            long before = sim.TotalTicksPlayed;
            Plan(_explore, 3);
            RunLoop(sim);
            sim.EndRunEarly(); // if it hadn't ended on its own
            long first = sim.Loop.TicksElapsed;
            RunLoop(sim);
            sim.EndRunEarly();
            long second = sim.Loop.TicksElapsed;

            Assert.That(before, Is.EqualTo(0));
            Assert.That(sim.TotalTicksPlayed, Is.EqualTo(first + second));
            Assert.That(SaveAndReload(sim).TotalTicksPlayed, Is.EqualTo(first + second));
        }

        [Test]
        public void EverythingPermanent_SurvivesSavingAndLoading()
        {
            var sim = PlayedGame();

            var loaded = SaveAndReload(sim);

            Assert.That(loaded.AmountOf(_mirrors), Is.EqualTo(6));
            Assert.That(loaded.IsFlipped(_five), Is.True);
            Assert.That(loaded.IsUnlocked(_chase), Is.False, "still retired");
            Assert.That(loaded.Persistent.Journal, Is.EqualTo(new[] { _opening }));
            Assert.That(loaded.MasteryOf(ClaraAttribute.Perception), Is.EqualTo(sim.MasteryOf(ClaraAttribute.Perception)));
            Assert.That(loaded.Queue.Count, Is.EqualTo(0), "the queue belongs to a run, so it isn't saved");
        }

        [Test]
        public void UnlockedTasks_AndTheResourceCapBonus_SurviveSavingAndLoading()
        {
            var sim = NewGame();
            sim.Persistent.UnlockedTasks.Add(_chase);
            sim.Persistent.ResourceCapBonus[_mirrors] = 3;

            var restored = SaveAndLoad(sim, _content);

            Assert.That(restored.UnlockedTasks, Is.EquivalentTo(new[] { _chase }));
            Assert.That(restored.ResourceCapBonus[_mirrors], Is.EqualTo(3));
        }

        [Test]
        public void LockedTasks_Hues_PackedItems_RoomRuns_AndFoundWays_SurviveSavingAndLoading()
        {
            var lantern = MakeObject("Lantern", max: 1, lasts: ResourceLifetime.Carried);
            var hall = MakeNode("The hall");
            var dark = MakeNode("The dark");
            var content = MakePlaces(hall, dark);
            content.tasks.Add(_chase);
            content.tasks.Add(MakeGatherTask("Take the lantern", 1f, lantern)); // the index finds items through the tasks that use them
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.Persistent.LockedTasks.Add(_chase);
            sim.Persistent.UnlockedHues.Add(Hue.Amber);
            sim.Persistent.Packed.Add(lantern);
            sim.Persistent.RoomRuns[hall] = 3;
            sim.Persistent.FoundWays.Add((hall, dark));

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, content, warnings);

            Assert.That(warnings, Is.Empty, "everything was found by its ID");
            Assert.That(restored.LockedTasks, Is.EquivalentTo(new[] { _chase }));
            Assert.That(restored.UnlockedHues, Does.Contain(Hue.Amber));
            Assert.That(restored.Packed, Is.EquivalentTo(new[] { lantern }));
            Assert.That(restored.RoomRuns[hall], Is.EqualTo(3));
            Assert.That(restored.FoundWays, Does.Contain((hall, dark)));
        }

        [Test]
        public void SavedHuesAndAttributes_ThatNoLongerExist_AreSkippedWithWarnings()
        {
            var data = SaveSerializer.Capture(NewGame());
            data.unlockedHues.Add(999);
            data.expertiseXp.Add(new SavedAttributeValue { attribute = 999, value = 1f });
            var warnings = new List<string>();

            SaveSerializer.Restore(data, new ContentIndex(_content), warnings);

            Assert.That(warnings.Count, Is.EqualTo(2));
            Assert.That(warnings, Has.Some.Contain("unlocked hue"));
            Assert.That(warnings, Has.Some.Contain("mastery"));
        }

        [Test]
        public void ASaveWithNoVersion_IsTreatedAsTheOldest_AndGoesThroughEveryUpgrade()
        {
            // The v21 upgrade restarts the search in a room a flipped switch reopens: it only runs if the
            // upgrades do, which a missing version used to skip.
            var room = MakeNode("The lab");
            room.exploresToFill = 4;
            var reopens = Make<SwitchDefinition>();
            reopens.reopensSearch.Add(room);
            _content.nodes.Add(room);
            _content.switches.Add(reopens);
            var json = "{ \"flippedSwitches\": [\"" + reopens.Id + "\"], \"exploredRooms\": [ { \"id\": \"" + room.Id + "\", \"value\": 4 } ] }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.loadedVersion, Is.GreaterThan(0), "it counts as a loaded old file");
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.Explored[room], Is.EqualTo(0f), "the version 21 upgrade ran");
        }

        [Test]
        public void Upgrade22To23_LoadsWithoutTheCapBonus()
        {
            // A version-22 file still carries the old per-stat cap bonus; it is ignored, the rest loads.
            var json = "{ \"version\": 22, \"keptVitality\": 4, \"expertiseXp\": [ { \"attribute\": " +
                       (int)ClaraAttribute.Perception + ", \"value\": 75 } ], \"expertiseCapBonus\": [ { \"attribute\": " +
                       (int)ClaraAttribute.Perception + ", \"value\": 2 } ] }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.AttributeMasteryXpOf(ClaraAttribute.Perception), Is.EqualTo(75f));
            Assert.That(restored.KeptVitality, Is.EqualTo(4f));
        }

        [Test]
        public void Upgrade23To24_LoadsWithNoKeptValues()
        {
            var skill = MakeSkill("Wayfinding");
            _content.skills.Add(skill);
            var json = "{ \"version\": 23, \"skillMasteryXp\": [ { \"id\": \"" + skill.Id + "\", \"value\": 300 } ] }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.SkillMasteryXpOf(skill), Is.EqualTo(300f), "the rest loads");
            Assert.That(restored.HasLastRunStart, Is.False, "no run has ended since the upgrade");
            Assert.That(restored.SkillMasteryXpAtLastRunStartOf(skill), Is.Null);
        }

        [Test]
        public void KeptSkillValues_RoundTrip()
        {
            var trained = MakeSkill("Wayfinding");
            var untrained = MakeSkill("Studying");
            _content.skills.Add(trained);
            _content.skills.Add(untrained);
            var sim = NewGame();
            sim.Persistent.SkillMasteryXpAtLastRunStart[trained] = 250f;
            sim.Persistent.HasLastRunStart = true;

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, _content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(restored.HasLastRunStart, Is.True);
            Assert.That(restored.SkillMasteryXpAtLastRunStartOf(trained), Is.EqualTo(250f));
            Assert.That(restored.SkillMasteryXpAtLastRunStartOf(untrained), Is.EqualTo(0f), "no entry, but a run has ended: 0");
        }

        [Test]
        public void ACapturedSave_CarriesTheCurrentVersion()
        {
            Assert.That(SaveSerializer.Capture(NewGame()).version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(new SaveData().version, Is.EqualTo(0), "a new SaveData is unversioned until Capture stamps it");
        }

        [Test]
        public void SavesReach_KeptExploration_AndFinds()
        {
            var shard = MakeResource("Shard", ResourceLifetime.Forever, max: 10);   // only a find gives it
            var lantern = MakeObject("Lantern", max: 1, lasts: ResourceLifetime.Carried); // only a find gives it
            var room = MakeNode("The dark");
            room.exploresToFill = 1;
            room.foundBySearching.Add(new RoomFind { task = MakeGatherTask("Chip a shard", 1f, shard), atSearched = 100 });
            room.foundBySearching.Add(new RoomFind { task = MakeGatherTask("Take the lantern", 1f, lantern), atSearched = 100 });
            var content = MakePlaces(room);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.Persistent.Resources[shard] = 2;
            sim.Persistent.Stash[lantern] = 1;
            sim.Persistent.Explored[room] = 0.5f;

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, content, warnings);

            Assert.That(warnings, Is.Empty, "everything was found by its ID");
            Assert.That(restored.ResourceOf(shard), Is.EqualTo(2));
            Assert.That(restored.Stash[lantern], Is.EqualTo(1));
            Assert.That(restored.Explored[room], Is.EqualTo(0.5f), "the bar is kept, partway included");
        }

        [Test]
        public void ALoadedGame_OpensInPlanning_ReadyForTheNextLoop()
        {
            var loaded = SaveAndReload(PlayedGame());

            Assert.That(loaded.Phase, Is.EqualTo(LoopPhase.BetweenRuns));
            Assert.That(loaded.NextLoopNumber, Is.EqualTo(3), "two loops played");
            Assert.That(loaded.Loop.FindPool(Hue.Amber), Is.Not.Null, "unlocked hues are there from the first loaded loop");
        }

        [Test]
        public void ANewGameSavedAtOnce_LoadsBackAtLoopOne()
        {
            var loaded = SaveAndReload(NewGame());

            Assert.That(loaded.NextLoopNumber, Is.EqualTo(1));
        }

        [Test]
        public void SavingMidRun_CountsThatRunAsPlayed()
        {
            var sim = NewGame();
            sim.Schedule(_explore, 3);
            sim.BeginLoop();
            RunTicks(sim, 15); // one mirror found, mid-run

            var loaded = SaveAndReload(sim);

            Assert.That(loaded.NextLoopNumber, Is.EqualTo(2));
            Assert.That(loaded.AmountOf(_mirrors), Is.EqualTo(1), "progress made before the save is kept");
        }

        [Test]
        public void TheOpeningStory_IsNotShownAgainAfterLoading()
        {
            var sim = NewGame();
            sim.MarkRead(_opening); // the player has read it

            var loaded = SaveAndReload(sim);

            Assert.That(loaded.UnreadStories, Is.Empty);
        }

        [Test]
        public void TheAutosaveWhenASwitchFlips_AlreadyIncludesItsStory()
        {
            var mirrorsStory = MakeStory("Mirrors in the Dark\nThey hung in the dark.");
            _five.story = mirrorsStory;
            var sim = NewGame();
            SaveData autosave = null;
            sim.SwitchFlipped += _ => autosave = SaveSerializer.Capture(sim); // what GameController does
            Plan(_explore, 5);

            RunLoop(sim);

            Assert.That(autosave, Is.Not.Null, "the switch flipped");
            Assert.That(autosave.journal, Has.Member(mirrorsStory.Id), "the story is in that save");
            Assert.That(autosave.unreadStories, Has.Member(mirrorsStory.Id), "and still waiting to be read");
        }

        [Test]
        public void AnUnreadStory_IsStillWaitingAfterLoading()
        {
            var sim = NewGame(); // the opening story is revealed, not yet read

            var loaded = SaveAndReload(sim);

            Assert.That(loaded.UnreadStories, Is.EqualTo(new[] { _opening }));
        }

        [Test]
        public void AStoryOnlyCountsAsRead_WhenClosed()
        {
            var sim = NewGame();
            Assert.That(sim.UnreadStories.Count, Is.EqualTo(1));

            sim.MarkRead(_opening);

            Assert.That(sim.UnreadStories, Is.Empty);
            Assert.That(sim.Persistent.Journal, Has.Member(_opening), "still in the journal");
        }

        [Test]
        public void ContentThatNoLongerExists_IsSkipped_WithAWarning()
        {
            var sim = PlayedGame();
            var data = SaveSerializer.Capture(sim);
            data.flippedSwitches.Add("deleted-switch-id");

            var warnings = new List<string>();
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), warnings);

            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(restored.FlippedSwitches.Count, Is.EqualTo(1), "the missing switch is left out");
        }

        // A save written by an older version of the game, as it looked on disk: it still has the old
        // build fields (versions 1 and 2), which are simply ignored now.
        private const string OldSave = @"{
            ""version"": VERSION,
            ""savedAtUtc"": ""2026-09-24T12:00:00Z"",
            ""loopsCompleted"": 4,
            ""buildName"": ""Build 1"",
            ""buildEndsWith"": 1,
            ""build"": [ { ""task"": ""some-old-task"", ""count"": 3, ""destination"": """" } ],
            ""flippedSwitches"": [],
            ""journal"": []
        }";

        [Test]
        public void AnOlderSave_StillLoads_AndIsUpgradedToThisVersion([Values(1, 2, 3)] int version)
        {
            var data = SaveSerializer.FromJson(OldSave.Replace("VERSION", version.ToString()), out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));

            var warnings = new List<string>();
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), warnings);
            Assert.That(warnings, Is.Empty, "the old build fields are ignored, not warned about");
            Assert.That(restored.LoopNumber, Is.EqualTo(4));
            Assert.That(restored.SkillMasteryXp, Is.Empty, "no skills before version 2");
        }

        [Test]
        public void RunHistory_RoundTrips()
        {
            var sim = NewGame();
            sim.Persistent.RunHistory.Add(new RunRecord(1, 120, 4, LoopEndReason.Exhausted));
            sim.Persistent.RunHistory.Add(new RunRecord(2, 300, 9, LoopEndReason.EndedByPlayer));

            var loaded = SaveAndReload(sim);

            var history = loaded.Persistent.RunHistory;
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That((history[0].LoopNumber, history[0].Ticks, history[0].ActionsCompleted, history[0].EndReason), Is.EqualTo((1, 120L, 4, LoopEndReason.Exhausted)));
            Assert.That((history[1].LoopNumber, history[1].Ticks, history[1].ActionsCompleted, history[1].EndReason), Is.EqualTo((2, 300L, 9, LoopEndReason.EndedByPlayer)));
        }

        [Test]
        public void RunRecordMilestones_RoundTrip()
        {
            var sim = NewGame();
            sim.Persistent.RunHistory.Add(new RunRecord(1, 120, 4, LoopEndReason.Exhausted, new (ContentAsset, float)[] { (_five, 12.5f) }));
            sim.Persistent.RunHistory.Add(new RunRecord(2, 300, 9, LoopEndReason.EndedByPlayer));

            var loaded = SaveAndReload(sim);

            var history = loaded.Persistent.RunHistory;
            Assert.That(history[0].Milestones.Count, Is.EqualTo(1));
            Assert.That(history[0].TimeOf(_five), Is.EqualTo(12.5f));
            Assert.That(history[1].Milestones, Is.Empty);
            Assert.That(history[1].TimeOf(_five), Is.Null);
        }

        [Test]
        public void KeptVitality_RoundTrips()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.Persistent.KeptVitality = 12.5f;

            var restored = SaveAndLoad(sim, _content);

            Assert.That(restored.KeptVitality, Is.EqualTo(12.5f));
        }

        [Test]
        public void Upgrade18To19_StartsBankAtZero()
        {
            const string json = @"{ ""version"": 18 }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.KeptVitality, Is.EqualTo(0f), "older saves had no bank");
        }

        [Test]
        public void Version11Save_LoadsWithNoMilestoneHistory()
        {
            const string json = @"{ ""version"": 11, ""runHistory"": [ { ""loopNumber"": 1, ""ticks"": 100, ""actionsCompleted"": 2, ""endReason"": 0 } ] }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.RunHistory.Count, Is.EqualTo(1), "the run itself is kept");
            Assert.That(restored.RunHistory[0].Milestones, Is.Empty);
        }

        [Test]
        public void Version8Save_LoadsWithEmptyHistory()
        {
            var data = SaveSerializer.FromJson(OldSave.Replace("VERSION", "8"), out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.RunHistory, Is.Empty, "old saves kept no per-run lengths");
        }

        [Test]
        public void Version10Save_LoadsWithEmptySkillTicks()
        {
            var data = SaveSerializer.FromJson(OldSave.Replace("VERSION", "10"), out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.run.skillTicks, Is.Empty);
            Assert.That(data.run.otherTicks, Is.EqualTo(0L));
        }

        [Test]
        public void RoomMilestones_RoundTrip()
        {
            var room = MakeNode("The hall");
            room.firstEntry = MakeStory("The hall\nDusty.");
            _content.nodes.Add(room);
            var sim = NewGame();
            sim.Persistent.RoomsEntered.Add(room);
            sim.Persistent.RunHistory.Add(new RunRecord(1, 120, 4, LoopEndReason.Exhausted, new (ContentAsset, float)[] { (room, 3.5f), (_five, 12.5f) }));
            var warnings = new List<string>();

            var loaded = SaveAndReload(sim, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(loaded.Persistent.RoomsEntered, Is.EquivalentTo(new[] { room }));
            var record = loaded.Persistent.RunHistory[0];
            Assert.That(record.TimeOf(room), Is.EqualTo(3.5f), "a node Id in the milestone list finds its room");
            Assert.That(record.TimeOf(_five), Is.EqualTo(12.5f));
        }

        [Test]
        public void MilestoneThatIsNotASwitchOrRoom_IsSkippedWithAWarning()
        {
            var sim = NewGame();
            sim.Persistent.RunHistory.Add(new RunRecord(1, 120, 4, LoopEndReason.Exhausted, new (ContentAsset, float)[] { (_explore, 3f), (_five, 12.5f) }));
            var warnings = new List<string>();

            var loaded = SaveAndReload(sim, warnings);

            Assert.That(loaded.Persistent.RunHistory[0].Milestones.Count, Is.EqualTo(1), "only the switch is kept");
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void V14Save_Upgrades_WithNoRoomsEntered()
        {
            const string json = @"{ ""version"": 14, ""runHistory"": [ { ""loopNumber"": 1, ""ticks"": 100, ""actionsCompleted"": 2, ""endReason"": 0 } ] }";

            var data = SaveSerializer.FromJson(json, out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.roomsEntered, Is.Empty);
            var restored = SaveSerializer.Restore(data, new ContentIndex(_content), new List<string>());
            Assert.That(restored.RoomsEntered, Is.Empty, "each room's next entry counts as its first");
            Assert.That(restored.RunHistory.Count, Is.EqualTo(1));
        }

        [Test]
        public void SavesFromANewerVersion_AreRefused()
        {
            var data = SaveSerializer.Capture(NewGame());
            data.version = SaveData.CurrentVersion + 1;

            var result = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("newer version"));
        }

        [Test]
        public void ADamagedFile_IsReportedNotCrashed()
        {
            var result = SaveSerializer.FromJson("{ this is not a save", out string error);

            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void SlotFiles_WriteReadAndDelete()
        {
            string original = SaveSlots.Folder;
            string folder = Path.Combine(Path.GetTempPath(), "hoem-save-tests-" + System.Guid.NewGuid().ToString("N"));
            SaveSlots.Folder = folder;
            try
            {
                Assert.That(SaveSlots.Exists(1), Is.False);
                Assert.That(SaveSlots.Read(1, out string emptyError), Is.Null);
                Assert.That(emptyError, Is.EqualTo(GameText.Get("menu.slot_empty")), "the menu shows this to the player");

                Assert.That(SaveSlots.Write(1, SaveSerializer.Capture(PlayedGame()), out string writeError), Is.True, writeError);
                Assert.That(SaveSlots.Exists(1), Is.True);
                Assert.That(SaveSlots.Exists(0), Is.False, "other slots untouched");

                var read = SaveSlots.Read(1, out string readError);
                Assert.That(readError, Is.Null);
                Assert.That(read.loopsCompleted, Is.EqualTo(2));

                Assert.That(SaveSlots.Delete(1, out string deleteError), Is.True, deleteError);
                Assert.That(SaveSlots.Exists(1), Is.False);
            }
            finally
            {
                SaveSlots.Folder = original;
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
        }

        [Test]
        public void SlotFiles_DamagedSaveCantBeReadButStillFillsItsSlot()
        {
            // The menu relies on this to ask before starting over a damaged save, and to offer Delete.
            string original = SaveSlots.Folder;
            string folder = Path.Combine(Path.GetTempPath(), "hoem-save-tests-" + System.Guid.NewGuid().ToString("N"));
            SaveSlots.Folder = folder;
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(SaveSlots.PathFor(0), "{ not a save");

                Assert.That(SaveSlots.Read(0, out string error), Is.Null);
                Assert.That(error, Is.Not.Null);
                Assert.That(SaveSlots.Exists(0), Is.True);

                Assert.That(SaveSlots.Delete(0, out string deleteError), Is.True, deleteError);
                Assert.That(SaveSlots.Exists(0), Is.False);
            }
            finally
            {
                SaveSlots.Folder = original;
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
        }
    }
}
