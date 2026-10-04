using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's tasks (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Cost curve ----------

        /// <summary>A task's base time at a room depth, from its family (0 with no family or settings).</summary>
        private float SecondsAt(TaskDefinition task, int depth) =>
            task == null || task.family == null || _settings == null
                ? 0f
                : CostCurve.BaseSeconds(task.family.durationCoefficient, _settings.standardTripSeconds, _settings.roomStep, depth, task.durationMultiplier);

        /// <summary>The shallowest and deepest rooms it's done in: its own room(s), every room for the common verbs, else depth 0.</summary>
        private (int shallowest, int deepest) DepthsOf(TaskDefinition task)
        {
            bool common = task == _content.travelVerb || task == _content.exploreVerb || task == _content.pickUpVerb || task == _content.putDownVerb;
            int low = int.MaxValue, high = 0;
            foreach (var node in _content.nodes)
                if (node != null && (common || node.Lists(task)))
                {
                    low = Mathf.Min(low, node.depth);
                    high = Mathf.Max(high, node.depth);
                }
            return (low == int.MaxValue ? 0 : low, high);
        }

        /// <summary>Its time in the shallowest room it's done in (the figure the rough "one run of it" sums use).</summary>
        private float SecondsOf(TaskDefinition task) => SecondsAt(task, DepthsOf(task).shallowest);

        private string SecondsText(TaskDefinition task)
        {
            if (task.family == null)
                return "no family!";
            var (low, high) = DepthsOf(task);
            return low == high ? SecondsAt(task, low).ToString("0.##") : $"{SecondsAt(task, low):0.##} - {SecondsAt(task, high):0.##}";
        }

        private void DrawFamilies()
        {
            if (!Section(ref _showFamilies, "Cost families (a task's kind of work: its share of the standard trip)") || _settings == null)
                return;

            Header(("Name", 170), ("Coefficient", 80), ("Seconds at depth 0", 120), ("Tasks using it", 90), ("", 50));
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CostFamily)))
            {
                var family = AssetDatabase.LoadAssetAtPath<CostFamily>(AssetDatabase.GUIDToAssetPath(guid));
                if (family == null)
                    continue;
                int uses = 0;
                foreach (var task in _content.tasks)
                    if (task != null && task.family == family)
                        uses++;
                var so = new SerializedObject(family);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Field(so, "durationCoefficient", 80);
                so.ApplyModifiedProperties(); // before the summary, so it shows the new number
                Label((family.durationCoefficient * _settings.standardTripSeconds).ToString("0.##"), 120,
                    "Coefficient × Standard Trip Seconds: one task of this family in the first room, before its own Time ×.");
                Label(uses.ToString(), 90, "Tasks listed in Game Content (not counting the common verbs).");
                SelectButton(family);
                EditorGUILayout.EndHorizontal();
            }
        }

        // ---------- Tasks ----------

        private void DrawTasks()
        {
            if (!Section(ref _showTasks, "Tasks") || _content == null)
                return;

            Header(("Name", 170), ("Where", 150), ("Kind", 80), ("Starts", 45), ("Single", 45), ("Once", 45),
                ("Walks out", 60), ("Family", 130), ("Time ×", 55), ("XP ×", 50), ("Seconds (in its room)", 110), ("Cost", 60), ("Always", 50), ("Drain +/s", 60), ("Charge", 55), ("Charge ×", 65), ("Paid by (source, %)", 260), ("Per sec", 55),
                ("One run of it", 100), ("Skill", 110), ("Trains", 90), ("Stat (by kind)", 100),
                ("XP (0 = by time)", 90), ("Drain ×", 60), ("Needs (item: amount)", 260), ("Gives", 220), ("Takes", 180),
                ("Needs stat level", 170), ("Needs skill level", 170), ("Easier with (item: cost× time×)", 280), ("", 50));

            // The common verbs first: they are the base for every trip, search, pick-up and put-down.
            var rows = new List<TaskDefinition>();
            if (_content.travelVerb != null)
                rows.Add(_content.travelVerb);
            foreach (var verb in new[] { _content.exploreVerb, _content.pickUpVerb, _content.putDownVerb })
                if (verb != null && !rows.Contains(verb))
                    rows.Add(verb);
            foreach (var task in _content.tasks)
                if (!rows.Contains(task))
                    rows.Add(task);

            foreach (var task in rows)
            {
                if (task == null)
                    continue;

                var so = new SerializedObject(task);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Label(WhereOf(task), 150);
                Field(so, "kind", 80);
                Field(so, "startsUnlocked", 45);
                Field(so, "singleAction", 45);
                Field(so, "oncePerRun", 45);
                Field(so, "walksOut", 60);
                Field(so, "family", 130);
                Field(so, "durationMultiplier", 55);
                Field(so, "xpMultiplier", 50);
                Field(so, "cost", 60);
                Field(so, "alwaysCharged", 50);
                Field(so, "extraDrainPerSecond", 60);
                Field(so, "escalatingCharge", 55);
                Field(so, "chargeGrowth", 65);
                so.ApplyModifiedProperties(); // before the summaries, so they show the new numbers

                Label(SecondsText(task), 110, "Its base time from the cost curve (family × standard trip × room step ^ the depth of the room it's done in × Time ×). " +
                    "A range where it's done in rooms of different depths. Read-only: change the family, Time × or a room's Depth.");
                CostShareFields(so, task, 260);
                Label(!task.HasCost ? "free" : Charged(task) ? $"{task.cost / SecondsOf(task):0.##}" : "off", 55,
                    "Cost per second while it runs. \"off\": it has a cost, but costs are switched off and it isn't Always Charged.");
                Label(OneRunOf(task), 100,
                    "How many times Clara could finish only this task in one run, and how long until she's spent.");
                Field(so, "skill", 110);
                Field(so, "trainsAttribute", 90);
                Label(StatOf(task), 100, "The stat its XP trains: the one in Trains, or else the one its kind trains (Search: Perception, " +
                    "Study: Scholarship, Instantiate: Attunement). Endurance and Composure train on their own (LoopSettings).");
                Field(so, "xpReward", 90);
                Field(so, "drainTimes", 60);
                AmountFields(so, "needs", task.needs, 260);
                AmountFields(so, "gives", task.gives, 220);
                AmountFields(so, "takes", task.takes, 180);
                StatLevelFields(so, task, 170);
                SkillLevelFields(so, task, 170);
                EasierWithFields(so, task, 280);
                SelectButton(task);
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>The nodes that list this task, "anywhere", or "every way" for the Travel verb.</summary>
        private string WhereOf(TaskDefinition task)
        {
            if (task == _content.travelVerb)
                return "every way (the Travel verb)";
            if (task == _content.exploreVerb)
                return "rooms with something to search";
            if (task == _content.pickUpVerb)
                return "wherever an object lies (every object)";
            if (task == _content.putDownVerb)
                return "where she is (every object)";
            _text.Clear();
            foreach (var node in _content.nodes)
                if (node != null && node.Lists(task))
                    _text.Append(_text.Length > 0 ? ", " : "").Append(node.DisplayName);
            return _text.Length > 0 ? _text.ToString() : (_content.startNode != null ? "anywhere" : "-");
        }

        private string StatOf(TaskDefinition task)
        {
            var stat = Simulation.StatTrainedBy(task, _content.exploreVerb);
            return stat == ClaraAttribute.None ? "-" : stat.ToString();
        }

        /// <summary>Whether this task's cost is charged: costs are on, or it's Always Charged (the chase).</summary>
        private bool Charged(TaskDefinition task) =>
            _settings != null && (_settings.chargeActionCosts || task.alwaysCharged);

        /// <summary>
        /// This one task on repeat: how many she'd finish before she's spent, and when. Vitality
        /// goes to the passive drain (growing each minute) and, if the task's cost is charged, to
        /// whatever of its cost the pools it uses can't cover, plus its own extra drain and escalating
        /// charge (Core's formulas). Worked out a tenth of a second at a time. Approximations: no
        /// Composure, no room modifiers (the shallowest room's time), pools assumed full at the start.
        /// </summary>
        private string OneRunOf(TaskDefinition task)
        {
            if (_settings == null || SecondsOf(task) <= 0f)
                return "-";

            float poolsCanCover = 0f;
            float costPerSecond = Charged(task) && task.HasCost ? task.cost / SecondsOf(task) : 0f;
            if (costPerSecond > 0f)
                foreach (var (source, _) in task.CostBreakdown(task.cost))
                {
                    if (source == CostSource.Vitality)
                        continue;
                    var pool = _settings.pools.Find(p => p.hue == source.ToHue());
                    if (pool != null)
                        poolsCanCover += pool.max;
                }

            const float step = 0.1f, giveUpAfter = 3600f;
            float goSeconds = SecondsOf(task);
            float vitality = _settings.vitalityMax, spentOnCost = 0f, seconds = 0f;
            float heldOff = 1f, nextDone = goSeconds; // its own Drain × after each go
            int finishedGoes = 0;
            while (vitality > 0f && seconds < giveUpAfter)
            {
                if (seconds >= nextDone)
                {
                    heldOff *= task.drainTimes;
                    nextDone += goSeconds;
                    finishedGoes++;
                }
                // The same formulas the game uses (Composure not counted): the drain with the task's own
                // flat extra, and the escalating charge of this go, paid by vitality alone as the go runs.
                float drain = Simulation.DrainPerSecondAt(_settings, seconds, heldOff, task.extraDrainPerSecond);
                float charge = Simulation.EscalatingChargeAfter(task.escalatingCharge, task.chargeGrowth, finishedGoes) / goSeconds * step;
                float cost = costPerSecond * step;
                float fromVitality = Mathf.Max(0f, spentOnCost + cost - poolsCanCover) - Mathf.Max(0f, spentOnCost - poolsCanCover);
                spentOnCost += cost;
                vitality -= drain * step + fromVitality + charge;
                seconds += step;
            }
            if (seconds >= giveUpAfter)
                return "never ends";

            int finished = Mathf.FloorToInt(seconds / SecondsOf(task) + 0.0001f);
            return $"x{finished}  ({HallOfEchoingMirrors.UI.UiText.Clock(seconds)})";
        }

        private static string HoldsOf(ResourceDefinition item)
        {
            var names = new List<string>();
            foreach (var held in item.holds)
                if (held != null)
                    names.Add(held.DisplayName);
            return names.Count > 0 ? string.Join(", ", names) : "-";
        }

        // Its own builder, since it's used in the middle of other descriptions.
        private static string Amounts(List<ResourceAmount> amounts)
        {
            var text = new StringBuilder();
            foreach (var entry in amounts)
                if (entry.resource != null)
                    text.Append(text.Length > 0 ? ", " : "").Append($"{entry.amount} {entry.resource.DisplayName}");
            return text.Length > 0 ? text.ToString() : "-";
        }
    }
}
