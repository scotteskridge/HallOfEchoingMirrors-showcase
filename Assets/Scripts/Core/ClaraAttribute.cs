using System;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Clara's stats (attributes). Levels reset every loop; mastery persists.
    /// (Named ClaraAttribute because C# already has a type called Attribute.)
    /// Save files will store these as numbers: never renumber, only add at the end.
    /// </summary>
    public enum ClaraAttribute
    {
        None = 0,
        /// <summary>Banks kept maximum vitality after each run, from what she lost; lets her use restoratives with less wasted.</summary>
        Endurance = 1,
        /// <summary>Reveals what's hidden: finds and ways that need Perception. Doesn't speed searching.</summary>
        Perception = 2,
        /// <summary>Reading, deciphering, translating Roland's notes.</summary>
        Scholarship = 3,
        /// <summary>Makes restoratives give back more (once woken by a switch); its hue-cost job is off while costs are.</summary>
        Attunement = 4,
        /// <summary>Holding steady: slows the drain's growth, softens a task's own extra drain and charged costs (the ring, pools); trains from carrying what costs her.</summary>
        Composure = 5,
    }

    /// <summary>
    /// The shape of the XP curve, shared by skills, stats and mastery: level 1 needs
    /// <see cref="FirstLevel"/>; each level after needs <see cref="Growth"/> times the one before,
    /// but at least <see cref="MinIncrease"/> more.
    /// </summary>
    public readonly struct XpCurve
    {
        public readonly float FirstLevel, Growth, MinIncrease;

        public XpCurve(float firstLevel, float growth, float minIncrease)
        {
            FirstLevel = firstLevel;
            Growth = growth;
            MinIncrease = minIncrease;
        }

        public static XpCurve From(LoopSettings s) =>
            new XpCurve(s.xpForFirstLevel, s.xpGrowthPerLevel, s.xpMinIncreasePerLevel);

        /// <summary>Mastery's curve: its own first level, growing the same way.</summary>
        public static XpCurve MasteryFrom(LoopSettings s) =>
            new XpCurve(s.masteryXpForFirstLevel, s.xpGrowthPerLevel, s.xpMinIncreasePerLevel);

        public float NextAfter(float needed) => Math.Max(needed * Growth, needed + MinIncrease);
    }

    /// <summary>The XP curve maths: each level needs more than the last.</summary>
    public static class AttributeMath
    {
        /// <summary>The stat an action of this kind trains (the Search verb trains Perception too).</summary>
        public static ClaraAttribute StatForKind(TaskKind kind) => kind switch
        {
            TaskKind.Search => ClaraAttribute.Perception,
            TaskKind.Study => ClaraAttribute.Scholarship,
            TaskKind.Instantiate => ClaraAttribute.Attunement,
            _ => ClaraAttribute.None,
        };

        const int MaxLevel = 999;

        /// <summary>Every real attribute (everything except None), in order.</summary>
        public static readonly ClaraAttribute[] All = Array.FindAll(
            (ClaraAttribute[])Enum.GetValues(typeof(ClaraAttribute)), a => a != ClaraAttribute.None);

        /// <summary>Walks the curve once: the level reached, and how far through the next one (0 to 1).</summary>
        private static (int level, float progress) Measure(float xp, XpCurve curve)
        {
            var (level, into, needed) = Breakdown(xp, curve);
            return (level, needed > 0f ? into / needed : 0f);
        }

        /// <summary>The level reached, the XP earned towards the next one, and what the next one needs.</summary>
        public static (int level, float into, float needed) Breakdown(float xp, XpCurve curve)
        {
            int level = 0;
            float needed = curve.FirstLevel;
            while (xp >= needed && level < MaxLevel && needed > 0f)
            {
                xp -= needed;
                level++;
                needed = curve.NextAfter(needed);
            }
            return (level, xp, needed);
        }

        public static int LevelFor(float xp, XpCurve curve) => Measure(xp, curve).level;

        /// <summary>Total XP needed to reach a level from zero.</summary>
        public static float XpForLevel(int level, XpCurve curve)
        {
            float total = 0f, needed = curve.FirstLevel;
            for (int i = 0; i < level; i++)
            {
                total += needed;
                needed = curve.NextAfter(needed);
            }
            return total;
        }

        /// <summary>How far through the current level, from 0 to 1.</summary>
        public static float ProgressToNext(float xp, XpCurve curve) => Measure(xp, curve).progress;

        /// <summary>The usual "each level adds X%" bonus: 1 + perLevel × level (1 = no change).</summary>
        public static float Bonus(float perLevel, int level) => 1f + perLevel * level;
    }
}
