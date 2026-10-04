using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Pushing back against the drain: restoration items (vitality back over a clock), and actions
    // that hold off the darkness (a lower drain for the rest of the run).
    public partial class Simulation
    {
        private readonly List<ResourceDefinition> _restorativesHeld = new List<ResourceDefinition>();

        /// <summary>Vitality coming back each second right now, from every restoration item in use.</summary>
        public float RestoringPerSecond
        {
            get
            {
                float total = 0f;
                foreach (var restoring in Loop.Restorings)
                    total += restoring.PerSecond;
                return total;
            }
        }

        /// <summary>Vitality the items in use have still to give back.</summary>
        public float RestoringStillToCome
        {
            get
            {
                float total = 0f;
                foreach (var restoring in Loop.Restorings)
                    total += restoring.Left;
                return total;
            }
        }

        /// <summary>
        /// The most vitality a second she could be getting back now: one of each kind of restorative she
        /// has to hand or is using, at its rate (a kind restores one at a time; different kinds add up).
        /// </summary>
        public float MaxRestorePerSecond
        {
            get
            {
                float total = RestoringPerSecond; // at most one of each kind is in use
                // Its own list: safe to ask while StartRestoring is walking the shared one.
                var toHand = new List<ResourceDefinition>();
                CollectRestorativesToHand(toHand);
                foreach (var item in toHand)
                    if (!IsRestoring(item))
                        total += item.restoreVitality * AttunementRestoreMultiplier / item.restoreSeconds;
                return total;
            }
        }

        /// <summary>
        /// Vitality she's losing each second at a steady rate now: the drain, plus what her carried items
        /// cost while she acts. One-off charges (a trip, a practice) aren't a rate, so they're left out.
        /// </summary>
        public float SteadyLossPerSecond =>
            VitalityDrainPerSecond + (Loop.CurrentTask != null ? CarryCostPerSecond() : 0f);

        /// <summary>Whether one of these is being used now (only one of each kind at a time).</summary>
        public bool IsRestoring(ResourceDefinition item)
        {
            // A plain loop: asked every tick, so no closure per call.
            foreach (var restoring in Loop.Restorings)
                if (restoring.Item == item)
                    return true;
            return false;
        }

        /// <summary>Seconds until this item's use ends and the next can start, or 0 if none is in use.</summary>
        public float RestoringSecondsLeft(ResourceDefinition item)
        {
            float longest = 0f;
            foreach (var restoring in Loop.Restorings)
                if (restoring.Item == item && restoring.PerSecond > 0f)
                    longest = System.Math.Max(longest, restoring.Left / restoring.PerSecond);
            return longest;
        }

        /// <summary>The items in use give back this tick's share, and finished ones are gone.</summary>
        private void Restore()
        {
            for (int i = Loop.Restorings.Count - 1; i >= 0; i--)
            {
                var restoring = Loop.Restorings[i];
                float amount = restoring.PerSecond / _ticksPerSecond;
                if (amount > restoring.Left)
                    amount = restoring.Left;
                restoring.Left -= amount;
                Loop.Vitality.Fill(amount);
                if (restoring.Left <= 0.0001f)
                    Loop.Restorings.RemoveAt(i);
            }
        }

        /// <summary>
        /// Starts using restoration items she holds, one of each kind at a time, while what she's missing,
        /// plus what she'll lose to the steady drain while the item gives back (the user's rule, 2026-10-02),
        /// is at least what it gives (counting what the items already in use will still give back), so
        /// none is wasted beyond what Endurance allows. A drain as fast as an item gives uses it at once.
        /// </summary>
        private void StartRestoring()
        {
            var vitality = Loop.Vitality;
            // Endurance lets her use one even if a little of it would be wasted.
            float missing = vitality.Max - vitality.Current - RestoringStillToCome + RestoreOverflow;
            float lossPerSecond = SteadyLossPerSecond;
            if (missing <= 0f && lossPerSecond <= 0f)
                return;

            CollectRestorativesToHand(_restorativesHeld);
            foreach (var item in _restorativesHeld)
            {
                // One of each kind at a time: the next wisp waits until this one has run its course.
                // Different kinds (a wisp and a bottled well) run side by side.
                if (IsRestoring(item))
                    continue;
                bool onFloor = OnFloor(Loop.CurrentNode, item) > 0;
                float gives = item.restoreVitality * AttunementRestoreMultiplier;
                // What the drain takes while it gives back would be lost anyway, so it isn't waste.
                float lostMeanwhile = lossPerSecond * item.restoreSeconds;
                if ((onFloor || AmountOf(item) > 0) && missing + lostMeanwhile >= gives - 0.001f) // a hair of slack for rounding
                {
                    if (onFloor)
                        TakeFromFloor(item, 1);
                    else
                        Take(item, 1);
                    Loop.Restorings.Add(new LoopState.Restoring
                    {
                        Item = item,
                        PerSecond = gives / item.restoreSeconds,
                        Left = gives,
                    });
                    missing -= gives;
                    RestoreStarted?.Invoke(item);
                }
            }
        }

        /// <summary>
        /// Fills <paramref name="into"/> with the kinds of restorative she can use here: what lies on the
        /// floor where she is first (so her pockets stay full), then what she holds this run, then kept
        /// ones; each in the order it came.
        /// </summary>
        private void CollectRestorativesToHand(List<ResourceDefinition> into)
        {
            into.Clear();
            if (Loop.CurrentNode != null && Loop.Floor.TryGetValue(Loop.CurrentNode, out var pile))
                foreach (var entry in pile)
                    if (entry.Value > 0 && entry.Key.Restores)
                        into.Add(entry.Key);
            foreach (var entry in Loop.ToolsAndStats)
                if (entry.Key.Restores && entry.Value > 0 && !into.Contains(entry.Key))
                    into.Add(entry.Key);
            foreach (var entry in Persistent.Resources)
                if (entry.Key.Restores && entry.Value > 0 && !into.Contains(entry.Key))
                    into.Add(entry.Key);
        }

        /// <summary>
        /// Everything holding off the darkness now: actions that did (for the rest of the run) times
        /// what she holds that does, in step with how many (candles lit). 1 = nothing.
        /// </summary>
        public float DrainHeldOffNow => Loop.DrainHeldOff * MultiplyOverHeld((item, held) => item.DrainMultiplierFor(held));

        /// <summary>A task that holds off the darkness lowers the drain for the rest of the run.</summary>
        private void HoldOffTheDarkness(TaskDefinition task)
        {
            if (task.drainTimes < 1f)
            {
                Loop.DrainHeldOff *= task.drainTimes;
                DarknessHeldOff?.Invoke(task);
            }
        }
    }
}
