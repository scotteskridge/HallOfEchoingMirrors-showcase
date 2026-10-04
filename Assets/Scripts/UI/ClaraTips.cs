using System.Collections.Generic;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// What Clara's stats and skills do, in words: the hover pop-ups and the short "×1.16 speed"
    /// notes. Shared by every panel that shows them, so they always explain things the same way.
    /// </summary>
    public static class ClaraTips
    {
        // ---------- Stats ----------

        /// <summary>
        /// A stat's pop-up: its name and what it does now, a table of this run's level, mastery and
        /// the total (its strength, and what that does), then what it does in small print.
        /// </summary>
        public static string StatTip(Simulation sim, ClaraAttribute attribute)
        {
            if (sim == null)
                return null;
            string effect = StatEffect(sim, attribute);

            var table = new TipTable(GameText.Get("tips.col_level"), GameText.Get("tips.col_xp"));
            table.AddRow(GameText.Get("tips.row_run"),
                new[] { sim.LevelOf(attribute).ToString(), XpCell(sim.XpTowardsNext(attribute)) });
            table.AddRow(GameText.Get("tips.row_mastery"),
                new[] { sim.MasteryOf(attribute).ToString(), XpCell(sim.MasteryXpTowardsNext(attribute)) });
            table.AddRow(GameText.Get("tips.row_total"), new[] { sim.StrengthOf(attribute).ToString() }, effect);

            string smallPrint = WhatItDoes(sim, attribute) + LearnsFaster(sim, attribute) +
                                "\n" + GameText.Get("tips.stat_rule") +
                                "\n" + GameText.Get("tips.xp_rule");
            return GameText.Get("tips.stat_heading", ("name", UiStyle.Heading(GameText.Attribute(attribute))), ("effect", effect)) +
                   "\n\n" + table + "\n\n" + UiStyle.Aside(smallPrint);
        }

        private static string WhatItDoes(Simulation sim, ClaraAttribute attribute) => attribute switch
        {
            ClaraAttribute.Endurance => GameText.Get("tips.endurance",
                ("share", UiText.Percent(sim.Settings.enduranceBankShare)),
                ("per_level", UiText.Percent(sim.Settings.enduranceBankPerLevel)),
                ("overflow", UiText.Setting(sim.Settings.enduranceOverflowPerLevel)),
                ("vitality", UiText.Number(sim.Persistent.KeptVitality)),
                ("waste", UiText.Number(sim.RestoreOverflow))),
            ClaraAttribute.Perception => GameText.Get("tips.perception"),
            ClaraAttribute.Scholarship => GameText.Get("tips.scholarship",
                ("every", sim.Settings.scholarshipLevelsPerExtraStudy), ("now", sim.StudyBonus)),
            ClaraAttribute.Attunement => GameText.Get("tips.attunement",
                    ("per_level", UiText.Percent(sim.Settings.attunementRestorePerLevel)),
                    ("now", UiText.Rate(sim.AttunementRestoreMultiplier))) +
                (sim.IsAwake(attribute) ? "" : " " + GameText.Get("tips.attunement_asleep")) +
                (sim.Settings.chargeActionCosts ? "" : " " + GameText.Get("tips.costs_off")),
            ClaraAttribute.Composure => GameText.Get("tips.composure",
                ("per_level", UiText.Setting(sim.Settings.composureDrainGrowthPerLevel)),
                ("now", UiText.Rate(sim.ComposureGrowthMultiplier)),
                ("extra", UiText.Rate(sim.ComposureExtraDrainMultiplier))),
            _ => "",
        };

        /// <summary>"Wayfinding and Gathering learn 5% faster a level", or "" if no known skill learns with it.</summary>
        private static string LearnsFaster(Simulation sim, ClaraAttribute attribute)
        {
            var names = new List<string>();
            foreach (var skill in sim.SkillsLearningFasterWith(attribute))
                names.Add(skill.DisplayName);
            return names.Count == 0 ? "" : "\n" + GameText.Get("tips.learns_faster",
                ("skills", UiText.List(names)), ("percent", UiText.Percent(sim.Settings.statSkillXpPerLevel)));
        }

        /// <summary>
        /// A stat's chip in the row under the vitality bar: its name and what it's doing now, e.g.
        /// "Endurance +12 vitality kept" (the banked vitality, not the level).
        /// </summary>
        public static string StatBrief(Simulation sim, ClaraAttribute attribute) =>
            GameText.Get("stats.brief", ("name", GameText.Attribute(attribute)), ("effect", StatEffect(sim, attribute)));

        /// <summary>What a stat is doing now, without its name, e.g. "+12 vitality kept" or "×1.30 restoring".</summary>
        public static string StatEffect(Simulation sim, ClaraAttribute attribute)
        {
            switch (attribute)
            {
                case ClaraAttribute.Endurance:
                    return GameText.Get("stats.effect_endurance", ("value", UiText.Number(sim.Persistent.KeptVitality)));
                case ClaraAttribute.Perception:
                    return GameText.Get("stats.effect_perception");
                case ClaraAttribute.Scholarship:
                    return GameText.Get("stats.effect_scholarship", ("value", sim.StudyBonus));
                case ClaraAttribute.Attunement:
                    return sim.IsAwake(attribute)
                        ? GameText.Get("stats.effect_attunement", ("value", UiText.Rate(sim.AttunementRestoreMultiplier)))
                        : GameText.Get("stats.effect_attunement_asleep");
                case ClaraAttribute.Composure:
                    return GameText.Get("stats.effect_composure", ("value", UiText.Rate(sim.ComposureGrowthMultiplier)));
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(attribute), attribute, "ClaraTips: no effect line for this stat. Add one.");
            }
        }

        // The table's XP column: "40 / 120".
        private static string XpCell((float into, float needed) xp) =>
            GameText.Get("tips.xp", ("xp", UiText.Number(xp.into)), ("needed", UiText.Number(xp.needed)));

        // ---------- Skills ----------

        /// <summary>A skill's chip in the row under the vitality bar, e.g. "Wayfinding ×2.10" (every skill is a speed).</summary>
        public static string SkillBrief(Simulation sim, SkillDefinition skill) =>
            GameText.Get("stats.brief_skill", ("name", skill.DisplayName), ("value", UiText.Rate(sim.SpeedMultiplierFor(skill))));

        /// <summary>
        /// A skill's pop-up: its name and speed, a table of this run's level, mastery and the total
        /// speed (each part's share of it), then what it speeds up in small print.
        /// </summary>
        public static string SkillTip(Simulation sim, SkillDefinition skill)
        {
            if (sim == null || skill == null)
                return null;

            var tasks = new List<string>();
            foreach (var task in sim.TasksUsing(skill))
                tasks.Add(task.displayName);
            var settings = sim.Settings;

            var table = new TipTable(GameText.Get("tips.col_level"), GameText.Get("tips.col_xp"), GameText.Get("tips.col_speed"));
            table.AddRow(GameText.Get("tips.row_run"),
                new[] { sim.LevelOf(skill).ToString(), XpCell(sim.XpTowardsNext(skill)), SpeedCell(sim.LevelSpeedFor(skill)) });
            table.AddRow(GameText.Get("tips.row_mastery"),
                new[] { sim.MasteryOf(skill).ToString(), XpCell(sim.MasteryXpTowardsNext(skill)),
                    SpeedCell(sim.MasterySpeedFor(skill)) });
            table.AddRow(GameText.Get("tips.row_total"), new[] { "", "", SpeedCell(sim.SpeedMultiplierFor(skill)) });

            string smallPrint = GameText.Get("tips.skill_uses", ("tasks", UiText.List(tasks))) +
                                "\n" + GameText.Get("tips.skill_rule",
                                    ("per_level", UiText.Setting(settings.skillSpeedPerLevel)),
                                    ("per_mastery", UiText.Setting(settings.skillMasterySpeedPerLevel)));
            return GameText.Get("tips.skill_heading", ("name", UiStyle.Heading(skill.DisplayName)), ("speed", UiText.Rate(sim.SpeedMultiplierFor(skill)))) +
                   "\n\n" + table + "\n\n" + UiStyle.Aside(smallPrint);
        }

        private static string SpeedCell(float speed) => GameText.Get("tips.speed_cell", ("speed", UiText.Rate(speed)));
    }
}
