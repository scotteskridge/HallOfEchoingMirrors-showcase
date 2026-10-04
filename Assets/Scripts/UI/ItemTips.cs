using System.Collections.Generic;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// What an item is and does, in words: the hover pop-ups for things she carries, knows or
    /// keeps. Shared by every panel that shows items, so they always explain them the same way.
    /// </summary>
    public static class ItemTips
    {
        /// <summary>A container's pop-up: what it is and what it holds.</summary>
        public static string ContainerTip(ResourceDefinition container)
        {
            var names = new List<string>();
            foreach (var held in container.holds)
                if (held != null)
                    names.Add(held.DisplayName);
            string tip = UiStyle.Heading(container.DisplayName);
            if (!string.IsNullOrWhiteSpace(container.description))
                tip += "\n" + container.description;
            return tip + "\n" + GameText.Get("inventory.tip.container", ("max", container.holdsHowMany), ("items", UiText.List(names)));
        }

        /// <summary>An item's pop-up: what it is, how long it lasts, and what it does.</summary>
        public static string ItemTip(Simulation sim, ResourceDefinition item, int amount)
        {
            string tip = UiStyle.Heading(item.DisplayName);
            if (!string.IsNullOrWhiteSpace(item.description))
                tip += "\n" + item.description;
            tip += "\n" + GameText.Get(item.IsCarried ? "inventory.tip.carried" : item.IsKept ? "inventory.tip.kept" : "inventory.tip.this_run");
            if (item.IsPocketed)
                tip += "\n" + GameText.Get("inventory.tip.in_pocket");
            if (item.addsPockets > 0)
                tip += "\n" + GameText.Get("inventory.tip.adds_pockets", ("pockets", item.addsPockets));
            if (item.HoldsOffTheDarkness)
                tip += "\n" + GameText.Get("inventory.tip.holds_off", ("times", UiText.Rate(item.DrainMultiplierFor(amount))),
                    ("full", UiText.Rate(item.drainAtFull)), ("count", item.countsUpTo > 0 ? item.countsUpTo : item.startingMax));
            if (item.addsFloorSpace > 0)
                tip += "\n" + (item.startingMax > 0
                    ? GameText.Get("inventory.tip.adds_floor_in_step", ("now", item.FloorSpaceFor(amount)),
                        ("space", item.addsFloorSpace), ("max", item.startingMax))
                    : GameText.Get("inventory.tip.adds_floor", ("space", item.addsFloorSpace)));
            if (item.Restores)
                tip += "\n" + GameText.Get("inventory.tip.restores",
                    ("amount", UiText.Number(item.restoreVitality)), ("seconds", UiText.Number(item.restoreSeconds)));
            float carryCost = sim.CarryCostOf(item);
            if (carryCost > 0f)
                tip += "\n" + GameText.Get("inventory.tip.carry_cost", ("cost", UiText.Setting(carryCost)));
            return tip;
        }

        /// <summary>"this run", "kept" or "carried": how long it lasts, as a short note.</summary>
        public static string LastsName(ResourceDefinition item) =>
            item.IsCarried ? GameText.Get("inventory.lasts.carried") :
            item.IsKept ? GameText.Get("inventory.lasts.kept") :
            GameText.Get("inventory.lasts.this_run");
    }
}
