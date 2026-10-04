using System;

namespace HallOfEchoingMirrors.Core
{
    // For the dev panel only (plan 027d): jumping the kept state ahead to a stage of the game.
    public partial class Simulation
    {
        /// <summary>
        /// Puts the kept state at a <see cref="DevJumpStage"/>: flips its switches through the real
        /// <see cref="Flip"/> (so their effects apply, once), then raises kept items, searches and skill
        /// mastery to at least the stage's, never lowering anything. Every story this reveals goes in the
        /// journal already read. Between runs, or before a run's first tick; adds to the game, never undoes.
        /// </summary>
        public void JumpTo(DevJumpStage stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            if (Phase == LoopPhase.Running && Loop.TicksElapsed > 0)
                throw new InvalidOperationException("Jump between runs: a run under way would mix the stage with what she's done this run.");
            CheckJumpStage(stage);

            int unreadBefore = Persistent.UnreadStories.Count;
            foreach (var @switch in stage.switches)
                Flip(@switch);
            foreach (var kept in stage.keptItems)
                if (AmountOf(kept.resource) < kept.amount)
                    Persistent.Resources[kept.resource] = Math.Min(kept.amount, ResourceCapOf(kept.resource));
            foreach (var searched in stage.roomsSearched)
                JumpSearch(searched.room, searched.percent);
            foreach (var mastery in stage.skillMastery)
            {
                float xp = AttributeMath.XpForLevel(mastery.level, MasteryCurve);
                if (Persistent.SkillMasteryXpOf(mastery.skill) < xp)
                    Persistent.SkillMasteryXp[mastery.skill] = xp;
            }

            // What the stage now meets (holding the earrings, a search's discovery) flips as it would in play.
            CheckSwitches(SwitchTrigger.ResourceReached);
            CheckSwitches(SwitchTrigger.RoomExplored);
            CheckForNewWays();
            Persistent.UnreadStories.RemoveRange(unreadBefore, Persistent.UnreadStories.Count - unreadBefore);

            // A run not yet begun (straight after loading, or at its first tick) would credit itself with the
            // jump: it starts from the stage instead. An ended run's report is already made, so it's left alone.
            if (LoopIsFresh)
            {
                Loop.SwitchesFlipped.Clear();
                Loop.Milestones.Clear();
                NoteWhatTheRunStartsWith();
            }
        }

        private void JumpSearch(NodeDefinition room, int percent)
        {
            float steps = room.exploresToFill * percent / 100f;
            if (ExploresDoneIn(room) < steps)
                Persistent.Explored[room] = steps;
            RememberFoundWays(room);
            // A room she has searched is one she has been in: its first-entry story is in the journal, read.
            if (Persistent.RoomsEntered.Add(room) && room.firstEntry != null && !Persistent.Journal.Contains(room.firstEntry))
                Persistent.Journal.Add(room.firstEntry);
        }

        private static void CheckJumpStage(DevJumpStage stage)
        {
            bool broken = stage.switches.Contains(null) ||
                          stage.keptItems.Exists(k => k?.resource == null) ||
                          stage.roomsSearched.Exists(r => r?.room == null) ||
                          stage.skillMastery.Exists(s => s?.skill == null);
            if (broken)
                throw new InvalidOperationException($"Jump stage {stage.DisplayName} has an empty entry: fill it in or remove it.");
            var notKept = stage.keptItems.Find(k => !k.resource.IsKept);
            if (notKept != null)
                throw new InvalidOperationException(
                    $"Jump stage {stage.DisplayName} lists {notKept.resource.DisplayName}, which isn't kept: a run would only lose it.");
        }
    }
}
