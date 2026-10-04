using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// What an action costs, with everything that changes it: the rooms (a trip's two ends, the room
    /// explored), what she holds, and Attunement and Composure. Worked out in one place
    /// (Simulation.PriceOf), so what the screen shows is exactly what she's charged.
    /// </summary>
    public class ActionPrice
    {
        /// <summary>Its length at normal speed, in seconds (her skill then speeds it up).</summary>
        public float Seconds { get; }
        /// <summary>The rooms' and held items' combined time multiplier (1 = unchanged).</summary>
        public float TimeTimes { get; }
        /// <summary>
        /// Who pays how much: vitality or a pool. Empty when it costs nothing, or while costs are
        /// switched off. The pool is null for a hue she hasn't learnt yet.
        /// </summary>
        public List<(CostSource source, Pool pool, float amount)> Costs { get; }

        public bool HasCost => Costs.Count > 0;

        /// <summary>
        /// The vitality of the escalating charge (plan 030b), which is also an entry of <see cref="Costs"/> (see <see cref="ChargeIndex"/>);
        /// 0 when the task has none.
        /// </summary>
        public float EscalatingCharge { get; }

        /// <summary>Which entry of <see cref="Costs"/> is the escalating charge, or -1 when there is none.</summary>
        public int ChargeIndex { get; }

        public ActionPrice(float seconds, float timeTimes, List<(CostSource, Pool, float)> costs, float escalatingCharge = 0f, int chargeIndex = -1)
        {
            Seconds = seconds;
            TimeTimes = timeTimes;
            Costs = costs;
            EscalatingCharge = escalatingCharge;
            ChargeIndex = chargeIndex;
        }
    }
}
