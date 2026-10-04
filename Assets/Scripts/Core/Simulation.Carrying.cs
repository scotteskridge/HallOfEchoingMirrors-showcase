using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Carried items: what she holds in the hall, what walking out keeps and being yanked out
    // loses, the stash in the real lab, packing for the next run, and items that tax while held.
    public partial class Simulation
    {

        public int StashOf(ResourceDefinition item) =>
            item != null && Persistent.Stash.TryGetValue(item, out int held) ? held : 0;

        public bool IsPacked(ResourceDefinition item) => item != null && Persistent.Packed.Contains(item);

        /// <summary>Chooses whether a stashed item goes into the hall with her next run (and is at risk there).</summary>
        public void SetPacked(ResourceDefinition item, bool packed)
        {
            if (item == null || !item.IsCarried)
                return;
            if (packed)
                Persistent.Packed.Add(item);
            else
                Persistent.Packed.Remove(item);
            MarkPlanWarningsStale(); // what she takes in can answer a need ahead
        }

        /// <summary>At the start of a run: packed items leave the stash and go in with her.</summary>
        private void TakePackedItemsIn()
        {
            foreach (var item in Persistent.Packed)
            {
                int amount = StashOf(item);
                if (amount <= 0)
                    continue;
                // As many as fit in her pockets; the rest wait in the stash.
                int taken = System.Math.Min(amount, RoomFor(item));
                if (taken <= 0)
                    continue;
                if (taken == amount)
                    Persistent.Stash.Remove(item);
                else
                    Persistent.Stash[item] = amount - taken;
                Loop.ToolsAndStats[item] = AmountOf(item) + taken;
            }
        }

        /// <summary>
        /// At the end of a run: walking out keeps what she carries (into the stash); anything else
        /// (the anchor pulling her back) loses it. Knowledge is in her head, so it always stays.
        /// </summary>
        private void SettleCarriedItems(LoopEndReason reason)
        {
            foreach (var entry in Loop.ToolsAndStats)
            {
                var item = entry.Key;
                if (!item.IsCarried || entry.Value <= 0)
                    continue;
                if (reason == LoopEndReason.WalkedOut)
                {
                    int total = Math.Min(ResourceCapOf(item), StashOf(item) + entry.Value);
                    Persistent.Stash[item] = total;
                    Loop.CarriedKept.Add((item, entry.Value));
                }
                else
                {
                    Loop.CarriedLost.Add((item, entry.Value));
                }
            }

            // Anything carried that she put down is left behind, whichever way the run ends.
            foreach (var pile in Loop.Floor.Values)
                foreach (var entry in pile)
                    if (entry.Key.IsCarried && entry.Value > 0)
                        Loop.CarriedLost.Add((entry.Key, entry.Value));
        }

        /// <summary>
        /// Every second of any action costs extra for each taxing item she holds. An Always Charged
        /// item (the ring) takes it straight from vitality, like the chase. The rest are paid like
        /// hall travel, and only while carry costs are on: split across her pools, and whatever they
        /// can't cover (or all of it, while she has none) from vitality. Returns what vitality must take.
        /// </summary>
        private float SpendCarryTax(float work)
        {
            float toVitality = 0f, fromPools = 0f;
            foreach (var entry in Loop.ToolsAndStats)
            {
                float perSecond = CarryCostOf(entry.Key);
                if (perSecond <= 0f)
                    continue;
                float amount = perSecond * work / _ticksPerSecond;
                if (entry.Key.alwaysCharged)
                    toVitality += amount;
                else
                    fromPools += amount;
            }

            if (fromPools <= 0f)
                return toVitality;
            if (Loop.Pools.Count == 0)
                return toVitality + fromPools;
            float share = fromPools / Loop.Pools.Count * HueCostMultiplier;
            foreach (var pool in Loop.Pools)
                toVitality += DrainAndReport(pool, share);
            return toVitality;
        }

        /// <summary>What her carried items cost per second of action, after Composure softens it.</summary>
        public float CarryCostPerSecond()
        {
            float perSecond = 0f;
            foreach (var entry in Loop.ToolsAndStats)
                perSecond += CarryCostOf(entry.Key);
            return perSecond;
        }

        /// <summary>
        /// What one kind of carried item costs her per second of action now, after Composure: 0 if
        /// its cost isn't being charged (carry costs off, and it isn't Always Charged).
        /// </summary>
        public float CarryCostOf(ResourceDefinition item) =>
            item != null && item.IsCarried && (_settings.chargeCarryCosts || item.alwaysCharged)
                ? item.carryCostPerSecond * AmountOf(item) * CarryCostMultiplier
                : 0f;
    }
}
