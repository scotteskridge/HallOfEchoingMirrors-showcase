using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How actions read on screen, wherever they appear (the action list, the queue, the map): their
    /// names, a line of details, their cost, how long they repeat, and the full hover explanation.
    /// </summary>
    public static class ActionText
    {
        /// <summary>An action on offer: "Travel to the junction", "Search the smoky mirror", or the task's own name.</summary>
        public static string NameOf(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room)
        {
            if (to != null)
                return QueueEntry.NameOf(task, to);
            if (task == sim.ExploreVerb && room != null)
                return GameText.Get("names.explore", ("verb", task.displayName), ("room", GameText.TitleInSentence(room.DisplayName)));
            return task.displayName;
        }

        /// <summary>A queue entry's name: an explore is named by the room the queue will have taken her to.</summary>
        public static string NameOf(Simulation sim, int index, QueueEntry entry) =>
            NameOf(sim, entry.Task, entry.Destination, sim.HasPlaces ? sim.PlannedNodeAfter(index) : null);

        /// <summary>
        /// How long it takes her now, skill included: "9.4s", or "3.2/s" once she does it more than
        /// once a second. (The icons beside it show which skill.)
        /// </summary>
        public static string Details(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room) =>
            Pace(sim.SecondsAtSpeedNow(task, room, to), "actions.details", "actions.details_per_second");

        // Seconds a go (key's {seconds}), or times a second (fastKey's {times}) when under a second.
        private static string Pace(float seconds, string key, string fastKey) =>
            seconds < 1f
                ? GameText.Get(fastKey, ("times", UiText.Number(1f / seconds)))
                : GameText.Get(key, ("seconds", UiText.Number(seconds)));

        /// <summary>E.g. "12 Vitality" or "5 Amber + 5 Citrine": who pays what, as charged. Empty if it's free.</summary>
        public static string CostText(ActionPrice price)
        {
            var parts = new List<string>();
            foreach (var (source, pool, amount) in price.Costs)
                parts.Add(GameText.Get("costs.part",
                    ("amount", UiText.Number(amount)), ("source", pool != null ? pool.Name : UiStyle.NameOf(source))));
            return UiText.List(parts, "costs.and");
        }

        /// <summary>How long a queued entry of it keeps repeating (Core's call, Simulation.RepeatKindOf; this only words it).</summary>
        public static string RepeatRule(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room)
        {
            switch (sim.RepeatKindOf(task, to, room))
            {
                case RepeatKind.Trip: return GameText.Get("actions.repeat.trip");
                case RepeatKind.SingleAction: return GameText.Get("actions.repeat.single");
                case RepeatKind.OncePerRun: return GameText.Get("actions.repeat.once");
                case RepeatKind.PickUp: return GameText.Get("actions.repeat.pick_up");
                case RepeatKind.PutDown: return GameText.Get("actions.repeat.put_down");
                case RepeatKind.Explore:
                    return GameText.Get("actions.repeat.explore", ("percent", UiText.Percent(sim.ExploredFraction(room))));
                case RepeatKind.UntilFull: return GameText.Get("actions.repeat.until_full");
                case RepeatKind.Forever: return GameText.Get("actions.repeat.forever");
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(task), "No repeat text for this kind of repeat.");
            }
        }

        /// <summary>
        /// The full hover explanation: how long it takes her now, what it gives and needs, what it
        /// costs; then, in small print, what it is, the skill that speeds it up, its XP, and how long
        /// it repeats.
        /// </summary>
        public static string Tip(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room)
        {
            var tip = new StringBuilder(UiStyle.Heading(NameOf(sim, task, to, room)));
            tip.Append('\n').Append(Pace(sim.SecondsAtSpeedNow(task, room, to), "actions.tip_time", "actions.tip_per_second"));

            string gives = Items(sim, task.gives);
            if (gives != null)
                tip.Append('\n').Append(GameText.Get("actions.tip_gives", ("items", gives)));
            string needs = Items(sim, task.needs);
            if (needs != null)
                tip.Append('\n').Append(GameText.Get("actions.tip_needs", ("items", needs)));
            foreach (var gate in task.requiresSkills)
                if (gate.skill != null)
                    tip.Append('\n').Append(GameText.Get("actions.tip_needs_skill",
                        ("skill", gate.skill.DisplayName), ("level", gate.level), ("has", sim.StrengthOf(gate.skill))));

            if (task.drainTimes < 1f)
                tip.Append('\n').Append(GameText.Get("actions.tip_hold_off", ("times", UiText.Setting(task.drainTimes))));
            var price = sim.PriceOf(task, room, to);
            if (price.HasCost)
                tip.Append('\n').Append(GameText.Get("actions.tip_cost", ("cost", CostText(price))));
            if (task.escalatingCharge > 0f && task.chargeGrowth > 1f)
                tip.Append('\n').Append(GameText.Get("actions.tip_charge_rises",
                    ("percent", UiText.Percent(task.chargeGrowth - 1f)), ("count", sim.UsesThisRun(task))));
            if (task.extraDrainPerSecond > 0f)
                tip.Append('\n').Append(GameText.Get("actions.tip_extra_drain", ("rate", UiText.Rate(task.extraDrainPerSecond))));

            // The small print.
            tip.Append('\n');
            if (!string.IsNullOrWhiteSpace(task.description))
                tip.Append('\n').Append(UiStyle.Aside(task.description.Trim()));
            if (task.skill != null)
                tip.Append('\n').Append(UiStyle.Aside(GameText.Get("actions.tip_speed",
                    ("skill", task.skill.DisplayName), ("speed", UiText.Rate(sim.SpeedMultiplierFor(task))))));
            string targets = XpTargets(sim, task);
            if (targets != null)
                tip.Append('\n').Append(UiStyle.Aside(GameText.Get("actions.tip_xp",
                    ("xp", UiText.Number(sim.XpRewardOf(task, room))), ("targets", targets))));
            tip.Append('\n').Append(UiStyle.Aside(RepeatRule(sim, task, to, room)));
            return tip.ToString();
        }

        /// <summary>
        /// The hover explanation as a layout (plan ui-036a): the full form, or for Pick up and Put down
        /// the compact one. Same facts as <see cref="Tip"/>, in rows instead of sentences.
        /// </summary>
        public static TipLayout Layout(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room)
        {
            var layout = new TipLayout { Title = NameOf(sim, task, to, room) };
            if (IsFloorAction(sim, task))
                return CompactLayout(sim, layout, task, room);

            layout.Kicker = KickerOf(sim, task, room);
            var price = sim.PriceOf(task, room, to);

            // Takes: the time now, and what it was before her skill sped it up.
            var takes = new TipLayout.Row { Label = GameText.Get("tips.row_takes"), Value = Details(sim, task, to, room), Meaning = TipMeaning.Time };
            if (task.skill != null && Mathf.Abs(sim.SpeedMultiplierFor(task) - 1f) > 0.005f)
                takes.Base = GameText.Get("tips.from_base", ("seconds", UiText.Number(price.Seconds)));
            layout.Rows.Add(takes);

            AddCostRows(layout, sim, task, price);
            AddNeedRows(layout, sim, task);
            string gives = Items(sim, task.gives);
            if (gives != null)
                layout.Rows.Add(new TipLayout.Row { Label = GameText.Get("tips.row_gives"), Value = gives });

            var targets = XpTargetList(sim, task);
            if (targets.Count > 0)
            {
                var trains = new TipLayout.Row { Label = GameText.Get("tips.row_trains"), Rule = GameText.Get("tips.mastery_rule") };
                string xp = UiText.Number(sim.XpRewardOf(task, room));
                foreach (string target in targets)
                    trains.Chips.Add(GameText.Get("tips.chip_xp", ("name", target), ("xp", xp)));
                layout.Rows.Add(trains);
            }
            if (task.skill != null)
                layout.Rows.Add(new TipLayout.Row
                {
                    Label = GameText.Get("tips.row_faster"),
                    Value = GameText.Get("tips.faster_value", ("skill", task.skill.DisplayName), ("speed", UiText.Rate(sim.SpeedMultiplierFor(task)))),
                    Unit = GameText.Get("tips.now"),
                });

            if (!string.IsNullOrWhiteSpace(task.description))
                layout.Description = task.description.Trim();
            layout.Strip = FooterOf(sim, task, to, room);
            return layout;
        }

        // Pick up and Put down (the verb itself or its per-item copy): the one-per-item verbs with nothing to teach.
        private static bool IsFloorAction(Simulation sim, TaskDefinition task) =>
            task == sim.PickUpVerb || task == sim.PutDownVerb || task.picksUp != null || task.putsDown != null;

        private static TipLayout CompactLayout(Simulation sim, TipLayout layout, TaskDefinition task, NodeDefinition room)
        {
            layout.Compact = true;
            var price = sim.PriceOf(task, room);
            string trains = XpTargets(sim, task);
            var row = new TipLayout.Row
            {
                Value = Details(sim, task, null, room),
                Meaning = TipMeaning.Time,
                Unit = GameText.Get("tips.compact_line",
                    ("cost", price.HasCost ? CostText(price) : GameText.Get("tips.compact_free")),
                    ("trains", trains != null ? GameText.Get("tips.compact_trains", ("targets", trains)) : GameText.Get("tips.compact_trains_nothing"))),
            };
            if (task.picksUp != null)
                row.Note = GameText.Get("tips.compact_pick_up", ("amount", sim.OnFloor(room, task.picksUp))); // any room's popover offers its own pile
            else if (task.putsDown != null)
                row.Note = GameText.Get("tips.compact_put_down", ("amount", sim.InPockets(task.putsDown))); // what's in containers stays there
            layout.Rows.Add(row);
            return layout;
        }

        /// <summary>"Search · The Mirror's Laboratory": the verb class, then the place (the panel makes it small caps).</summary>
        private static string KickerOf(Simulation sim, TaskDefinition task, NodeDefinition room)
        {
            string verb = task == sim.ExploreVerb ? GameText.Get("tips.kicker_explore") :
                          task == sim.TravelVerb ? GameText.Get("tips.kicker_travel") :
                          GameText.Get(KickerKeyOf(task.kind));
            return room == null ? verb : GameText.Get("tips.kicker", ("verb", verb), ("room", room.DisplayName));
        }

        // Spelt out, not built from the kind's name, so the text-file check can see every key the code asks for.
        private static string KickerKeyOf(TaskKind kind)
        {
            switch (kind)
            {
                case TaskKind.Search: return "tips.kicker_search";
                case TaskKind.Study: return "tips.kicker_study";
                case TaskKind.Gather: return "tips.kicker_gather";
                case TaskKind.Assist: return "tips.kicker_assist";
                case TaskKind.Take: return "tips.kicker_take";
                case TaskKind.Work: return "tips.kicker_work";
                case TaskKind.Instantiate: return "tips.kicker_instantiate";
                default: return "tips.kicker_other";
            }
        }

        private static void AddCostRows(TipLayout layout, Simulation sim, TaskDefinition task, ActionPrice price)
        {
            var notes = new List<string>();
            if (task.escalatingCharge > 0f && task.chargeGrowth > 1f)
                notes.Add(GameText.Get("actions.tip_charge_rises",
                    ("percent", UiText.Percent(task.chargeGrowth - 1f)), ("count", sim.UsesThisRun(task))));
            if (task.extraDrainPerSecond > 0f)
                notes.Add(GameText.Get("actions.tip_extra_drain", ("rate", UiText.Rate(task.extraDrainPerSecond))));
            string notesText = notes.Count > 0 ? string.Join("\n", notes) : null;

            if (price.HasCost)
            {
                var costs = new TipLayout.Row
                {
                    Label = GameText.Get("tips.row_costs"),
                    // Placeholder rule: the flat note shows on every price, because no skill reduces a fixed charge.
                    Note = GameText.Get("tips.flat_cost"),
                    Rule = notesText,
                };
                if (price.Costs.Count == 1 && price.Costs[0].source == CostSource.Vitality)
                {
                    costs.Value = UiText.Number(price.Costs[0].amount);
                    costs.Unit = GameText.Get("tips.unit_vitality");
                    costs.Meaning = TipMeaning.Vitality;
                }
                else
                {
                    costs.Value = CostText(price);
                }
                layout.Rows.Add(costs);
            }
            else if (notesText != null)
            {
                layout.Rows.Add(new TipLayout.Row { Label = GameText.Get("tips.row_also"), Rule = notesText });
            }

            if (task.drainTimes < 1f)
                layout.Rows.Add(new TipLayout.Row
                {
                    Label = GameText.Get("tips.row_also"),
                    Rule = GameText.Get("actions.tip_hold_off", ("times", UiText.Setting(task.drainTimes))),
                });
        }

        // Needs: one line for each thing needed (items as today, skill gates met or unmet); only the first carries the label.
        private static void AddNeedRows(TipLayout layout, Simulation sim, TaskDefinition task)
        {
            string label = GameText.Get("tips.row_needs");
            void Add(string value, TipMeaning meaning)
            {
                layout.Rows.Add(new TipLayout.Row { Label = label, Value = value, Meaning = meaning });
                label = "";
            }

            string items = Items(sim, task.needs);
            if (items != null)
                Add(items, TipMeaning.Plain);
            foreach (var gate in task.requiresSkills)
            {
                if (gate.skill == null)
                    continue;
                int has = sim.StrengthOf(gate.skill);
                Add(GameText.Get("tips.need_skill", ("skill", gate.skill.DisplayName), ("level", gate.level), ("has", has)),
                    has >= gate.level ? TipMeaning.Met : TipMeaning.Unmet);
            }
        }

        // The state, not a description: the repeat rule, with how far through it she is where it has a count.
        private static TipLayout.Footer FooterOf(Simulation sim, TaskDefinition task, NodeDefinition to, NodeDefinition room)
        {
            var footer = new TipLayout.Footer { Text = RepeatRule(sim, task, to, room) };
            if (sim.RepeatKindOf(task, to, room) == RepeatKind.Explore && sim.HasSomethingToExplore(room))
            {
                footer.Count = GameText.Get("tips.count", ("done", sim.SearchStepsDoneIn(room)), ("total", room.exploresToFill));
                footer.Progress = Mathf.Clamp01(sim.ExploredFraction(room));
            }
            return footer;
        }

        /// <summary>"Wayfinding and Endurance": what it trains, or null for nothing.</summary>
        private static string XpTargets(Simulation sim, TaskDefinition task)
        {
            var names = XpTargetList(sim, task);
            if (names.Count == 0)
                return null;
            return names.Count == 1 ? names[0] : GameText.Get("actions.and", ("a", names[0]), ("b", names[1]));
        }

        /// <summary>What it trains, one name each: its skill, then the stat it trains.</summary>
        private static List<string> XpTargetList(Simulation sim, TaskDefinition task)
        {
            var names = new List<string>();
            if (task.skill != null)
                names.Add(task.skill.DisplayName);
            var stat = sim.StatTrainedBy(task);
            if (stat != ClaraAttribute.None)
                names.Add(GameText.Attribute(stat));
            return names;
        }

        /// <summary>"1 Glimmer (has 3 of 10), 2 Focus", or null for none.</summary>
        private static string Items(Simulation sim, List<ResourceAmount> amounts)
        {
            var items = new List<string>();
            foreach (var amount in amounts)
            {
                if (amount.resource == null)
                    continue;
                int cap = sim.ResourceCapOf(amount.resource);
                string item = cap < int.MaxValue
                    ? GameText.Get("actions.item_held", ("amount", amount.amount), ("item", amount.resource.DisplayName),
                        ("held", sim.AmountOf(amount.resource)), ("max", cap))
                    : GameText.Get("actions.item", ("amount", amount.amount), ("item", amount.resource.DisplayName));
                items.Add(item);
            }
            return items.Count > 0 ? UiText.List(items) : null;
        }
    }
}
