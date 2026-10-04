using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The XP rules for one kind of thing Clara gets better at, stats or skills (the key): this
    /// run's XP gives its level (reset every run), and kept XP gives its mastery (never reset, no
    /// cap). Stats and skills follow the same rules, so they share this; a stat's mastery is
    /// deferred (<c>defersMastery</c>): the run's XP is held and settled into mastery when the run ends.
    /// It holds no state of its own, only the two XP tables it's given (the run's and the kept ones).
    /// </summary>
    public readonly struct XpTrack<T>
    {
        private readonly Dictionary<T, float> _runXp, _masteryXp;
        private readonly XpCurve _curve, _masteryCurve;
        private readonly float _masteryShare;
        private readonly bool _defersMastery;

        public XpTrack(Dictionary<T, float> runXp, Dictionary<T, float> masteryXp, XpCurve curve, XpCurve masteryCurve, float masteryShare, bool defersMastery = false)
        {
            _runXp = runXp;
            _masteryXp = masteryXp;
            _curve = curve;
            _masteryCurve = masteryCurve;
            _masteryShare = masteryShare;
            _defersMastery = defersMastery;
        }

        public float RunXp(T key) => _runXp.TryGetValue(key, out float xp) ? xp : 0f;
        public float MasteryXp(T key) => _masteryXp.TryGetValue(key, out float xp) ? xp : 0f;

        // ---------- This run's level ----------

        public int Level(T key) => AttributeMath.LevelFor(RunXp(key), _curve);
        public float LevelProgress(T key) => AttributeMath.ProgressToNext(RunXp(key), _curve);
        public (float into, float needed) TowardsNextLevel(T key) => TowardsNext(RunXp(key), _curve);

        // ---------- Mastery (kept) ----------

        public int Mastery(T key) => MasteryFor(MasteryXp(key), _masteryCurve);
        public float MasteryProgress(T key) => MasteryProgressFor(MasteryXp(key), _masteryCurve);
        public (float into, float needed) TowardsNextMastery(T key) => TowardsNext(MasteryXp(key), _masteryCurve);

        /// <summary>The mastery a given amount of XP reaches (e.g. as a run began).</summary>
        public static int MasteryFor(float xp, XpCurve curve) => AttributeMath.LevelFor(xp, curve);

        /// <summary>Progress towards the next mastery level (0 to 1).</summary>
        public static float MasteryProgressFor(float xp, XpCurve curve) => AttributeMath.ProgressToNext(xp, curve);

        // ---------- Earning XP ----------

        /// <summary>
        /// Adds XP to this run's level and (by the mastery share) to mastery, unless mastery is
        /// deferred. Returns the new level and the new mastery if either went up, or 0 for each that didn't.
        /// </summary>
        public (int newLevel, int newMastery) Gain(T key, float xp) => Gain(key, xp, xp);

        /// <summary>
        /// As above, with this run's XP and mastery's XP apart: a learning bonus (a stat helping its
        /// skills) boosts the run's level only, so it never feeds mastery.
        /// </summary>
        public (int newLevel, int newMastery) Gain(T key, float runXp, float masteryXp)
        {
            int levelBefore = Level(key), masteryBefore = Mastery(key);

            _runXp[key] = RunXp(key) + runXp;
            if (!_defersMastery)
                _masteryXp[key] = MasteryXp(key) + masteryXp * _masteryShare;

            int levelAfter = Level(key), masteryAfter = Mastery(key);
            return (levelAfter > levelBefore ? levelAfter : 0, masteryAfter > masteryBefore ? masteryAfter : 0);
        }

        /// <summary>
        /// Settles held XP into mastery at a run's end: <paramref name="xp"/> times the mastery share
        /// times <paramref name="share"/> (1 for a walk out, less for a collapse). Returns the new
        /// mastery if it went up, else 0.
        /// </summary>
        public int SettleMastery(T key, float xp, float share)
        {
            int before = Mastery(key);
            _masteryXp[key] = MasteryXp(key) + xp * _masteryShare * share;
            int after = Mastery(key);
            return after > before ? after : 0;
        }

        private static (float into, float needed) TowardsNext(float xp, XpCurve curve)
        {
            var (_, into, needed) = AttributeMath.Breakdown(xp, curve);
            return (into, needed);
        }
    }
}
