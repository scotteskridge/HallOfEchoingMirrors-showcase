using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Clara's stats (attributes) and skills: XP and levels (per run), mastery (kept), and what
    // each one does. The XP rules themselves are XpTrack's; all numbers are in LoopSettings.
    public partial class Simulation
    {
        private XpCurve Curve => XpCurve.From(_settings);
        private XpCurve MasteryCurve => XpCurve.MasteryFrom(_settings);

        /// <summary>The balance numbers, for screens that explain them (hover pop-ups). Read only.</summary>
        public LoopSettings Settings => _settings;

        // The same XP rules for stats and for skills (see XpTrack): this run's XP and the kept mastery XP.
        // A stat's mastery is held back and settled when the run ends (SettleStatMastery); a skill's is live.
        private XpTrack<ClaraAttribute> Stats =>
            new XpTrack<ClaraAttribute>(Loop.AttributeXp, Persistent.AttributeMasteryXp, Curve, MasteryCurve, _settings.masteryShare, defersMastery: true);
        private XpTrack<SkillDefinition> Skills =>
            new XpTrack<SkillDefinition>(Loop.SkillXp, Persistent.SkillMasteryXp, Curve, MasteryCurve, _settings.masteryShare);

        // ---------- Stats ----------

        /// <summary>This run's level. Resets every loop.</summary>
        public int LevelOf(ClaraAttribute attribute) => Stats.Level(attribute);
        public float LevelProgressOf(ClaraAttribute attribute) => Stats.LevelProgress(attribute);
        /// <summary>XP earned towards the next level this run, and what that level needs.</summary>
        public (float into, float needed) XpTowardsNext(ClaraAttribute attribute) => Stats.TowardsNextLevel(attribute);

        /// <summary>
        /// How strong a stat is for what it does: its level this run plus its mastery, so what she
        /// keeps between runs is a head start and each run adds to it.
        /// </summary>
        public int StrengthOf(ClaraAttribute attribute) =>
            attribute == ClaraAttribute.None ? 0 : LevelOf(attribute) + MasteryOf(attribute);

        /// <summary>
        /// The strength a stat's effects use: <see cref="StrengthOf"/>, or 0 while the stat is asleep.
        /// The one choke point, so a sleeping stat does nothing (its skills' learning bonus included)
        /// however many places use it. Gates and switches read the plain strength: it still levels.
        /// </summary>
        public int EffectStrengthOf(ClaraAttribute attribute) =>
            IsAwake(attribute) ? StrengthOf(attribute) : 0;

        /// <summary>Whether a stat was awake as this run began: asleep, unless a switch flipped in an earlier run wakes it.</summary>
        private bool WasAwakeAtRunStart(ClaraAttribute attribute)
        {
            if (!_settings.asleepAttributes.Contains(attribute))
                return true;
            foreach (var @switch in Persistent.FlippedSwitches)
                if (@switch.wakesAttributes.Contains(attribute) && !Loop.SwitchesFlipped.Contains(@switch))
                    return true;
            return false;
        }

        /// <summary>
        /// Whether a stat's effects are on: any stat, unless LoopSettings start it asleep and no
        /// flipped switch wakes it. Worked out from the flipped switches, so nothing extra is saved.
        /// </summary>
        public bool IsAwake(ClaraAttribute attribute)
        {
            if (!_settings.asleepAttributes.Contains(attribute))
                return true;
            foreach (var @switch in Persistent.FlippedSwitches)
                if (@switch.wakesAttributes.Contains(attribute))
                    return true;
            return false;
        }

        /// <summary>Permanent mastery. Never resets, no cap; a run's stat XP joins it when the run ends.</summary>
        public int MasteryOf(ClaraAttribute attribute) => Stats.Mastery(attribute);
        public float MasteryProgressOf(ClaraAttribute attribute) => Stats.MasteryProgress(attribute);
        /// <summary>Mastery XP towards the next mastery level, and what it needs.</summary>
        public (float into, float needed) MasteryXpTowardsNext(ClaraAttribute attribute) => Stats.TowardsNextMastery(attribute);

        /// <summary>
        /// The stat an action trains, by its theme: searching trains Perception, studying
        /// Scholarship, instantiating Attunement; a task can name its stat instead (Trains Attribute).
        /// Endurance and Composure train on their own (TrainEnduranceAndComposure); anything else
        /// trains its skill only.
        /// </summary>
        public ClaraAttribute StatTrainedBy(TaskDefinition task) => StatTrainedBy(task, ExploreVerb);

        /// <summary>The same rule without a simulation (the Balance Sheet uses it): <paramref name="exploreVerb"/> is the common Search verb, which trains Perception.</summary>
        public static ClaraAttribute StatTrainedBy(TaskDefinition task, TaskDefinition exploreVerb)
        {
            if (task == null)
                return ClaraAttribute.None;
            if (task.trainsAttribute != ClaraAttribute.None)
                return task.trainsAttribute;
            if (task == exploreVerb)
                return ClaraAttribute.Perception;
            return AttributeMath.StatForKind(task.kind);
        }

        /// <summary>
        /// Every tick: Endurance learns from what she loses (<paramref name="vitalityLost"/>, every source),
        /// Composure from time in the hall (the hall noticing her), and more while she carries something that costs her.
        /// </summary>
        private void TrainEnduranceAndComposure(float vitalityLost)
        {
            GainXp(ClaraAttribute.Endurance, vitalityLost * _settings.enduranceXpPerVitalityLost);
            float composure = _settings.composureXpPerSecond +
                              (CarryCostPerSecond() > 0f ? _settings.composureXpPerSecondCarrying : 0f);
            GainXp(ClaraAttribute.Composure, composure / _ticksPerSecond);
        }

        // An asleep stat earns nothing: until a switch wakes it, it doesn't exist for Clara yet.
        private void GainXp(ClaraAttribute attribute, float xp)
        {
            if (attribute == ClaraAttribute.None || xp <= 0f || !IsAwake(attribute))
                return;
            var (level, _) = Stats.Gain(attribute, xp);
            if (level > 0)
                AttributeLevelledUp?.Invoke(attribute, level);
            if (level > 0)
            {
                // Its strength (level + mastery) rose: switches and hidden ways may now be reached.
                CheckSwitches(SwitchTrigger.AttributeLevelReached);
                if (attribute == ClaraAttribute.Perception)
                    FindWaysNowSeen();
            }
        }

        // ---------- Skills ----------

        /// <summary>This run's skill level. Resets every loop.</summary>
        public int LevelOf(SkillDefinition skill) => Skills.Level(skill);
        public float LevelProgressOf(SkillDefinition skill) => Skills.LevelProgress(skill);
        public (float into, float needed) XpTowardsNext(SkillDefinition skill) => Skills.TowardsNextLevel(skill);

        /// <summary>How strong a skill is for a gate (Crafting 8): its level this run plus its mastery, as for stats.</summary>
        public int StrengthOf(SkillDefinition skill) => skill == null ? 0 : LevelOf(skill) + MasteryOf(skill);

        /// <summary>The skill's permanent mastery. Never resets, no cap.</summary>
        public int MasteryOf(SkillDefinition skill) => Skills.Mastery(skill);
        public float MasteryProgressOf(SkillDefinition skill) => Skills.MasteryProgress(skill);
        public (float into, float needed) MasteryXpTowardsNext(SkillDefinition skill) => Skills.TowardsNextMastery(skill);

        private void GainXp(SkillDefinition skill, float xp)
        {
            if (skill == null || xp <= 0f)
                return;
            // The stat's learning bonus raises this run's level only: boosting mastery too would double-count it.
            var (level, mastery) = Skills.Gain(skill, xp * SkillXpMultiplierFor(skill), xp);
            if (mastery > 0)
                SkillMasteryGained?.Invoke(skill, mastery);
            if (level > 0)
                SkillLevelledUp?.Invoke(skill, level);
        }

        /// <summary>One tick of the running action's time goes to its skill (or to "other" with none), beside its XP.</summary>
        private void TallyTime(SkillDefinition skill)
        {
            if (skill == null)
            {
                Loop.OtherTicks++;
                return;
            }
            Loop.SkillTicks[skill] = (Loop.SkillTicks.TryGetValue(skill, out long ticks) ? ticks : 0L) + 1;
        }

        /// <summary>How much faster a skill learns thanks to its stat (1 = no help).</summary>
        public float SkillXpMultiplierFor(SkillDefinition skill) =>
            skill == null ? 1f : 1f + _settings.statSkillXpPerLevel * EffectStrengthOf(skill.learnsFasterWith);

        /// <summary>The known skills that learn faster with this stat, e.g. Endurance: Wayfinding and Gathering.</summary>
        public List<SkillDefinition> SkillsLearningFasterWith(ClaraAttribute attribute)
        {
            var result = new List<SkillDefinition>();
            foreach (var skill in KnownSkills())
                if (skill.learnsFasterWith == attribute)
                    result.Add(skill);
            return result;
        }

        /// <summary>
        /// Skills worth showing, in Game Content order: one she has trained (any XP, so it stays after its
        /// tasks lock), or one an unlocked task she can see uses (a task a room's search hides counts once found).
        /// </summary>
        public List<SkillDefinition> KnownSkills()
        {
            var known = new List<SkillDefinition>();
            if (_content == null)
                return known;
            foreach (var skill in _content.skills)
            {
                if (skill == null)
                    continue;
                if (Skills.MasteryXp(skill) > 0f || Skills.RunXp(skill) > 0f)
                    known.Add(skill);
                else
                    foreach (var task in TasksUsing(skill))
                        if (IsSeenSomewhere(task))
                        {
                            known.Add(skill);
                            break;
                        }
            }
            return known;
        }

        // Not waiting on a search: listed plainly in a room (or in none), or found in a room that hides it.
        private bool IsSeenSomewhere(TaskDefinition task)
        {
            bool hidden = false;
            foreach (var node in AllNodes)
            {
                if (node == null || !node.Lists(task))
                    continue;
                if (node.tasks.Contains(task) || IsFoundHere(task, node))
                    return true;
                hidden = true;
            }
            return !hidden;
        }

        /// <summary>The unlocked tasks (the common verbs included) that train this skill and are sped up by it.</summary>
        public List<TaskDefinition> TasksUsing(SkillDefinition skill)
        {
            var found = new List<TaskDefinition>();
            if (_content == null || skill == null)
                return found;

            void Consider(TaskDefinition task)
            {
                if (UsesSkill(task, skill) && !found.Contains(task))
                    found.Add(task);
            }
            Consider(_content.travelVerb);
            Consider(_content.exploreVerb);
            Consider(_content.pickUpVerb);
            Consider(_content.putDownVerb);
            foreach (var task in _content.tasks)
                Consider(task);
            foreach (var node in _content.PlayableNodes)
                if (node != null)
                    foreach (var task in node.tasks)
                        Consider(task);
            return found;
        }

        private bool UsesSkill(TaskDefinition task, SkillDefinition skill) =>
            task != null && task.skill == skill && IsUnlocked(task);

        // For the run report: mastery as it stood when the run began, from that XP.
        internal int MasteryFor(float xp) => XpTrack<ClaraAttribute>.MasteryFor(xp, MasteryCurve);
        internal float MasteryProgressFor(float xp) => XpTrack<ClaraAttribute>.MasteryProgressFor(xp, MasteryCurve);

        /// <summary>
        /// XP for finishing a task done in <paramref name="room"/>: its own reward, or the usual amount
        /// from LoopSettings (per second of its base time there, times its XP multiplier).
        /// </summary>
        public float XpRewardOf(TaskDefinition task, NodeDefinition room)
        {
            if (task == null)
                return 0f;
            if (task.xpReward > 0f)
                return task.xpReward;
            // Its base time, not how fast she does it now: faster work means XP sooner, not less of it.
            // A longer task (a ×2 duration) pays more by itself. (Less a hair before rounding up, so 0.2 × 10 counts as 2, not 3.)
            return MathF.Ceiling(_settings.xpPerSecondOfTask * BaseSecondsOf(task, room) * task.xpMultiplier - 0.0001f);
        }

        // ---------- What they do ----------

        /// <summary>How fast the running task is going right now (1 = normal speed), for display.</summary>
        public float CurrentSpeed => Loop.CurrentTask != null ? SpeedMultiplierFor(Loop.CurrentTask) : 1f;

        /// <summary>
        /// A task's skill makes it faster: ×1.05 per skill level (this run) times ×1.01 per mastery
        /// level (kept). 1 = normal speed, e.g. a task with no skill.
        /// </summary>
        public float SpeedMultiplierFor(TaskDefinition task) => task != null ? SpeedMultiplierFor(task.skill) : 1f;

        /// <summary>How much faster this skill makes its tasks right now (1 = normal speed).</summary>
        public float SpeedMultiplierFor(SkillDefinition skill) => LevelSpeedFor(skill) * MasterySpeedFor(skill);

        /// <summary>The part of a skill's speed that comes from this run's levels (1 = none).</summary>
        public float LevelSpeedFor(SkillDefinition skill) =>
            skill == null ? 1f : CoreMath.Pow(_settings.skillSpeedPerLevel, LevelOf(skill));

        /// <summary>The part of a skill's speed that comes from its mastery, kept between runs (1 = none).</summary>
        public float MasterySpeedFor(SkillDefinition skill) =>
            skill == null ? 1f : _settings.MasterySpeedAt(MasteryOf(skill));

        /// <summary>
        /// Vitality lost per second right now: the base drain, grown smoothly for every minute of
        /// the run (more slowly with Composure), and lowered by anything that held off the darkness,
        /// plus the running task's flat extra (<see cref="ExtraDrainNow"/>).
        /// </summary>
        public float VitalityDrainPerSecond =>
            DrainPerSecondAt(_settings.vitalityDrainPerSecond, DrainGrowthMultiplier, DrainHeldOffNow, ExtraDrainNow);

        /// <summary>
        /// The one formula for the drain, with no simulation needed (the Balance Sheet uses it too): the
        /// base drain times how much it has grown (<see cref="DrainGrowthAfter"/>) times what held it off,
        /// plus the running task's flat <paramref name="extraDrain"/> (added last, after growth and candles).
        /// </summary>
        public static float DrainPerSecondAt(float baseDrain, float growth, float heldOff, float extraDrain) =>
            baseDrain * growth * heldOff + extraDrain;

        /// <summary>As above for a point in a run: the settings' base drain grown at their rate (Composure not counted) for <paramref name="secondsIntoRun"/>.</summary>
        public static float DrainPerSecondAt(LoopSettings settings, float secondsIntoRun, float heldOff, float extraDrain) =>
            DrainPerSecondAt(settings.vitalityDrainPerSecond, DrainGrowthAfter(settings.drainGrowthPerMinute, secondsIntoRun / 60f), heldOff, extraDrain);

        /// <summary>How much the drain has grown after <paramref name="minutes"/> at <paramref name="growthPerMinute"/> (0.25 = 25% a minute, compounding): 1 at the start.</summary>
        public static float DrainGrowthAfter(float growthPerMinute, float minutes) =>
            CoreMath.Pow(1f + growthPerMinute, minutes);

        /// <summary>
        /// The running task's own extra drain per second (0 when nothing runs); added flat, after candles
        /// and growth, and softened by Composure like the drain's growth.
        /// </summary>
        public float ExtraDrainNow => Loop.CurrentTask != null
            ? Loop.CurrentTask.extraDrainPerSecond * ComposureExtraDrainMultiplier
            : 0f;

        /// <summary>What Composure does to a task's own extra drain (1 = nothing; 0.9 = 10% less).</summary>
        public float ComposureExtraDrainMultiplier =>
            CoreMath.Pow(1f - _settings.composureExtraDrainPerLevel, EffectStrengthOf(ClaraAttribute.Composure));

        /// <summary>How much the drain has grown this run so far (1 at the start, 1.25 after a minute...).</summary>
        public float DrainGrowthMultiplier => Loop.DrainGrown;

        /// <summary>How fast the drain grows now, per minute (0.25 = 25%), slowed by Composure.</summary>
        public float DrainGrowthPerMinute => _settings.drainGrowthPerMinute * ComposureGrowthMultiplier;

        /// <summary>What Composure does to the drain's growth (1 = nothing; 0.94 = 6% slower).</summary>
        public float ComposureGrowthMultiplier =>
            CoreMath.Pow(_settings.composureDrainGrowthPerLevel, EffectStrengthOf(ClaraAttribute.Composure));

        /// <summary>One tick's growth of the drain, at the rate of the moment.</summary>
        private void GrowTheDrain() =>
            Loop.DrainGrown *= DrainGrowthAfter(DrainGrowthPerMinute, 1f / (60f * _ticksPerSecond));

        /// <summary>
        /// Placeholder rule: at a run's end, this run's stat XP joins mastery: all of it for a walk
        /// out, <see cref="LoopSettings.collapseStatXpShare"/> of it for a collapse or an early end.
        /// (Skill mastery isn't held back: it rises as it's earned.)
        /// </summary>
        private void SettleStatMastery(LoopEndReason reason)
        {
            float share = reason == LoopEndReason.WalkedOut ? 1f : _settings.collapseStatXpShare;
            bool anyRose = false;
            foreach (var entry in new List<KeyValuePair<ClaraAttribute, float>>(Loop.AttributeXp))
            {
                int mastery = Stats.SettleMastery(entry.Key, entry.Value, share);
                if (mastery > 0)
                {
                    anyRose = true;
                    AttributeMasteryGained?.Invoke(entry.Key, mastery);
                }
            }
            // Strength (level + mastery) rose, as it did when mastery was live: a switch waiting on it may flip now.
            if (anyRose)
                CheckSwitches(SwitchTrigger.AttributeLevelReached);
        }

        /// <summary>Her vitality's maximum at the start of a run: the base plus what Endurance has banked (kept). Fixed for the run.</summary>
        public float MaxVitality => _settings.vitalityMax + Persistent.KeptVitality;

        /// <summary>
        /// Every loss of vitality counts here, whatever takes it (the drain, a task's extra drain,
        /// charges, costs, the ring). One counter feeds the bank and Endurance's XP.
        /// </summary>
        private void NoteVitalityLost(float amount) => Loop.VitalityLostThisRun += amount;

        /// <summary>
        /// Placeholder rule: at the end of any run, a share of the vitality she lost goes into the
        /// kept maximum, bigger with Endurance now. Losing more (long runs, restoratives) banks
        /// more; a quick death banks little. All numbers are in LoopSettings.
        /// </summary>
        private float BankVitality()
        {
            // No Endurance yet (Act I): nothing is banked, and max vitality stays the base. A switch flipped
            // this very run (the first walk out wakes it) doesn't count: it was asleep for the whole run.
            if (!WasAwakeAtRunStart(ClaraAttribute.Endurance))
                return 0f;
            float share = _settings.enduranceBankShare *
                          (1f + _settings.enduranceBankPerLevel * EffectStrengthOf(ClaraAttribute.Endurance));
            float gained = Loop.VitalityLostThisRun * share;
            Persistent.KeptVitality += gained;
            return gained;
        }

        /// <summary>How much of a restoration item Endurance lets her waste and still use it.</summary>
        public float RestoreOverflow =>
            _settings.enduranceOverflowPerLevel * EffectStrengthOf(ClaraAttribute.Endurance);

        /// <summary>Extra of each thing a study action gives, from Scholarship (0 until enough levels).</summary>
        public int StudyBonus =>
            _settings.scholarshipLevelsPerExtraStudy > 0
                ? EffectStrengthOf(ClaraAttribute.Scholarship) / _settings.scholarshipLevelsPerExtraStudy
                : 0;

        /// <summary>Game time this run has lasted so far (time paused doesn't count).</summary>
        public float SecondsThisRun => Loop.TicksElapsed / (float)_ticksPerSecond;

        /// <summary>Composure makes carried items (the ring) drain less, strongly.</summary>
        public float CarryCostMultiplier =>
            1f / AttributeMath.Bonus(_settings.composureCarryCostPerLevel, EffectStrengthOf(ClaraAttribute.Composure));

        /// <summary>
        /// Attunement and Composure both make tasks take less from the hue pools (tuned separately).
        /// Vitality costs are unaffected.
        /// </summary>
        public float HueCostMultiplier =>
            AttunementCostMultiplier / AttributeMath.Bonus(_settings.composureActionCostPerLevel, EffectStrengthOf(ClaraAttribute.Composure));

        /// <summary>What Attunement does to a restorative: how much more vitality it gives back (1 = nothing; asleep = 1).</summary>
        public float AttunementRestoreMultiplier =>
            AttributeMath.Bonus(_settings.attunementRestorePerLevel, EffectStrengthOf(ClaraAttribute.Attunement));

        /// <summary>What Attunement alone does to hue costs (1 = nothing).</summary>
        public float AttunementCostMultiplier =>
            1f / AttributeMath.Bonus(_settings.attunementPerLevel, EffectStrengthOf(ClaraAttribute.Attunement));
    }
}
