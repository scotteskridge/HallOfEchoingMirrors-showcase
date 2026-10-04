using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Paying for tasks: what one costs (its price, time and modifiers), spending it as it runs, and finishing it.
    public partial class Simulation
    {
        // ---------- Paying for tasks ----------

        /// <summary>
        /// Whether this task's cost is charged: always while action costs are on (LoopSettings),
        /// otherwise only for tasks marked Always Charged (e.g. the chase). The rest only cost time.
        /// </summary>
        public bool Charges(TaskDefinition task) =>
            task != null && (_settings.chargeActionCosts || task.alwaysCharged);

        internal float TotalCostOf(TaskDefinition task) => Charges(task) && task.HasCost ? task.cost : 0f;

        /// <summary>
        /// How many times she has finished this task this run (an interrupted go doesn't count). 0 between
        /// runs: the plan being made is for the next run, which starts from none.
        /// </summary>
        public int UsesThisRun(TaskDefinition task)
        {
            if (Loop.IsOver)
                return 0;
            int uses = 0;
            foreach (var step in Loop.Steps)
                if (step.Task == task)
                    uses++;
            return uses;
        }

        /// <summary>
        /// The vitality the next go at this task costs on top of its ordinary cost: its Escalating
        /// Charge, grown once for every go already finished this run (plan 030b). 0 for most tasks.
        /// </summary>
        public float EscalatingChargeOf(TaskDefinition task) => EscalatingChargeAfter(task, UsesThisRun(task));

        /// <summary>The escalating charge of the go that comes after <paramref name="uses"/> finished ones (for projecting a plan).</summary>
        public float EscalatingChargeAfter(TaskDefinition task, int uses) =>
            EscalatingChargeAfter(task.escalatingCharge, task.chargeGrowth, uses);

        /// <summary>
        /// The one formula for an escalating charge, with no simulation needed (the Balance Sheet uses it
        /// too): <paramref name="baseCharge"/> grown by <paramref name="growth"/> once per go already
        /// finished. 0 when the task has no charge.
        /// </summary>
        public static float EscalatingChargeAfter(float baseCharge, float growth, int uses) =>
            baseCharge > 0f ? baseCharge * CoreMath.Pow(growth, uses) : 0f;

        /// <summary>
        /// What each pool actually pays for a task this run, after Attunement and Composure. An
        /// All Pools share is split evenly across this run's pools. The pool is null if that hue
        /// is still locked. <paramref name="costTimes"/> is a room's modifier (1 for an ordinary room).
        /// </summary>
        private List<(CostSource source, Pool pool, float amount)> EffectiveCosts(TaskDefinition task, float costTimes)
        {
            var costs = new List<(CostSource, Pool, float)>();
            float hueMultiplier = HueCostMultiplier;
            foreach (var (source, amount) in task.CostBreakdown(TotalCostOf(task) * costTimes))
            {
                if (source == CostSource.Vitality)
                    costs.Add((source, Loop.Vitality, amount));
                else if (source == CostSource.AllPools && Loop.Pools.Count == 0)
                    costs.Add((source, Loop.Vitality, amount)); // no pools yet: the hall eats her directly
                else if (source == CostSource.AllPools)
                    foreach (var pool in Loop.Pools)
                        costs.Add((source, pool, amount / Loop.Pools.Count * hueMultiplier));
                else
                    costs.Add((source, Loop.FindPool(source.ToHue()), amount * hueMultiplier));
            }
            return costs;
        }

        /// <summary>
        /// Spends each part of the task's cost in proportion to the work done this tick, so a faster
        /// task spends the same total, just sooner. The final tick spends whatever is left, so every
        /// total comes out exact. Returns what vitality must take: its own share, plus any shortfall
        /// from pools that have run dry.
        /// </summary>
        private float SpendCurrentTaskCost(float work, bool finalTick)
        {
            float toVitality = 0f;

            foreach (var cost in Loop.CurrentTaskCosts)
            {
                if (cost.Remaining <= 0f)
                    continue;

                float amount = finalTick ? cost.Remaining : MathF.Min(cost.PerWork * work, cost.Remaining);
                cost.Remaining -= amount;
                // What escalating charges have cost her this run, for the Summary: a trip's apart from the rest.
                if (cost.IsCharge && Loop.CurrentDestination != null)
                    Loop.MoveVitalityPaid += amount;
                else if (cost.IsCharge && Loop.CurrentTask != null)
                    Loop.TaskChargePaid[Loop.CurrentTask] = (Loop.TaskChargePaid.TryGetValue(Loop.CurrentTask, out float paid) ? paid : 0f) + amount;

                toVitality += cost.Pool == Loop.Vitality ? amount : DrainAndReport(cost.Pool, amount);
            }
            return toVitality;
        }

        // ---------- Pricing ----------

        /// <summary>
        /// What a task costs if done at <paramref name="at"/> (for a trip: from there to
        /// <paramref name="destination"/>), with everything that changes it. Used both to charge her
        /// and to show prices, so the two always agree.
        /// </summary>
        public ActionPrice PriceOf(TaskDefinition task, NodeDefinition at, NodeDefinition destination = null)
        {
            // The common verbs are changed by the rooms: a trip by both ends, exploring by the room she's in.
            var (costTimes, timeTimes) =
                destination != null ? TripModifier(at, destination) :
                task == ExploreVerb && at != null ? (at.exploring.cost, at.exploring.time) :
                (1f, 1f);
            // And by what she holds (the bench cleared, what she noticed about him).
            var (heldCost, heldTime) = HeldModifierFor(task);
            costTimes *= heldCost;
            timeTimes *= heldTime;
            // A search is one go at the whole bar that's left: each step's time and cost, for each step.
            float steps = destination == null && SearchesWhatIsLeft(task, at) ? SearchStepsLeft(at) : 1f;
            var costs = EffectiveCosts(task, costTimes * steps);
            // The escalating charge is always paid, by vitality alone: no pool or stat softens it.
            float charge = EscalatingChargeOf(task) * costTimes;
            int chargeIndex = -1;
            if (charge > 0f)
            {
                chargeIndex = costs.Count;
                costs.Add((CostSource.Vitality, Loop.Vitality, charge));
            }
            return new ActionPrice(BaseSecondsOf(task, at) * timeTimes * steps, timeTimes, costs, charge, chargeIndex);
        }

        /// <summary>
        /// A task's base time at normal speed, before what the room modifies or she holds: the cost
        /// curve (family × standard trip × room step ^ depth × its own multiplier). <paramref name="room"/>
        /// is where it's done (a trip: the room she leaves); null, with no rooms, counts as depth 0.
        /// </summary>
        public float BaseSecondsOf(TaskDefinition task, NodeDefinition room)
        {
            if (!HasFamily(task))
                return 0f;
            return CostCurve.BaseSeconds(task.family.durationCoefficient, _settings.standardTripSeconds, _settings.roomStep,
                room != null ? room.depth : 0, task.durationMultiplier);
        }

        /// <summary>
        /// Where the game hears about a content mistake (a task with no cost family): the game sends it to the
        /// Console; tests collect it. Core has no logger of its own. Null means nobody is listening, and
        /// the rule that found the mistake carries on all the same. The second argument is the broken asset,
        /// so clicking the Console line selects it.
        /// </summary>
        public Action<string, UnityEngine.Object> ContentProblem { get; set; }

        // A task with no family has no time, so there's nothing to price: a content mistake, reported (once
        // per task, not every frame it's drawn) and handled gracefully. The fallback is the one the rules
        // already had: it costs 0 seconds here, CantStartAtAll refuses it with reasons.no_cost_family (nothing
        // queued, no time spent, the player is told why), and a queued one is skipped with the same reason.
        private readonly HashSet<TaskDefinition> _reportedWithoutFamily = new HashSet<TaskDefinition>();

        private bool HasFamily(TaskDefinition task)
        {
            if (task.family != null)
                return true;
            // Only counts as reported once someone is listening: on a load the reporter is wired after the
            // save is restored, and a report spent on nobody would hide the mistake for the whole session.
            if (ContentProblem != null && _reportedWithoutFamily.Add(task))
                ContentProblem($"Task \"{task.displayName}\" has no cost family, so it has no time and can't be done. Give it a Family in its Inspector or the Balance Sheet.", task);
            return false;
        }

        /// <summary>
        /// How long one go of a task really takes at her speed now (its skill), where PriceOf gives
        /// the time at normal speed. Counted in whole ticks, as it runs: a task finishes at most once
        /// a tick, so nothing is quicker than one tick.
        /// </summary>
        public float SecondsAtSpeedNow(TaskDefinition task, NodeDefinition at, NodeDefinition destination = null)
        {
            int work = destination == null && SearchesWhatIsLeft(task, at)
                ? Math.Max(1, (int)MathF.Round(SearchWorkLeft(at)))
                : WorkNeededFor(task, at, PriceOf(task, at, destination).TimeTimes);
            int ticks = Math.Max(1, (int)MathF.Ceiling(work / SpeedMultiplierFor(task) - WorkEpsilon));
            return ticks / (float)_ticksPerSecond;
        }

        // How much work a task is: its duration in ticks at normal speed. Speed (from its skill, and
        // meta currencies later) changes how fast the work gets done, tick by tick.
        private int WorkNeededFor(TaskDefinition task, NodeDefinition room, float timeTimes) =>
            Math.Max(1, (int)MathF.Round(BaseSecondsOf(task, room) * timeTimes * _ticksPerSecond));

        /// <summary>
        /// How what she holds changes a task: each modifier applies once per one held, e.g. two
        /// of a ×0.85 item make it ×0.72. (1, 1) when nothing she holds matters.
        /// </summary>
        public (float cost, float time) HeldModifierFor(TaskDefinition task) => HeldModifierFor(task, atPlanStart: false);

        /// <summary>As above; with <paramref name="atPlanStart"/>, what she holds where the queue starts (see <see cref="HeldWhenPlanStarts"/>).</summary>
        private (float cost, float time) HeldModifierFor(TaskDefinition task, bool atPlanStart)
        {
            float cost = 1f, time = 1f;
            foreach (var modifier in task.easierWith)
            {
                if (modifier.whileHolding == null)
                    continue;
                int held = atPlanStart ? HeldWhenPlanStarts(modifier.whileHolding) : AmountOf(modifier.whileHolding);
                if (held <= 0)
                    continue;
                cost *= CoreMath.Pow(modifier.costEach, held);
                time *= CoreMath.Pow(modifier.timeEach, held);
            }
            return (cost, time);
        }

        // ---------- Finishing tasks ----------

        private void CompleteCurrentTask()
        {
            var task = Loop.CurrentTask;
            var entry = Loop.RunningEntry;
            var arrivedAt = Loop.CurrentDestination;
            ClearCurrentTask();

            Loop.CompletedTasks.Add(task);
            Loop.Steps.Add(new CompletedStep(task, Loop.CurrentNode, arrivedAt, entry?.SuppliesFor != null));
            if (entry != null)
            {
                entry.TimesDone++;
                if (entry.TimesLeft.HasValue)
                    entry.TimesLeft--;
            }
            TaskCompleted?.Invoke(task);
            if (arrivedAt != null)
            {
                Loop.CurrentNode = arrivedAt;
                bool firstEntryThisRun = EnterRoom(arrivedAt);
                Arrived?.Invoke(arrivedAt, firstEntryThisRun);
            }
            if (task.picksUp != null && HasPlaces)
                PickUp(task.picksUp);
            if (task.putsDown != null && HasPlaces)
                PutDownFromPockets(task.putsDown);

            // After TaskCompleted, so the log reads "Search done." before "+1 Mirrors found".
            // Scholarship: a study gives more of what it gives, every few levels.
            // What it takes goes first, so the pocket it frees is there for what it makes.
            int extra = task.kind == TaskKind.Study ? StudyBonus : 0;
            foreach (var take in task.takes)
                UseUp(take.resource, take.amount);
            bool makes = MakesThings(task);
            foreach (var give in task.gives)
                Grant(give.resource, give.amount + extra, makeWay: makes);
            HoldOffTheDarkness(task);
            CheckSwitches(SwitchTrigger.TasksCompletedInOneRun);

            // After what it gives, so "she can hold no more" counts this completion.
            if (entry != null && IsDone(entry) && Loop.Queue.Entries.Remove(entry))
                QueueChanged?.Invoke();
            if (task.singleAction && !Loop.IsOver)
                SingleActionDone?.Invoke(task);

            // An exit: she walks out, and keeps what she carries.
            if (task.walksOut && !Loop.IsOver)
                EndLoop(LoopEndReason.WalkedOut);
        }
    }
}
