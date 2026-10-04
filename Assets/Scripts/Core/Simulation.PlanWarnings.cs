using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Warnings ahead on the plan (plan ui-024d): which queued entries will be refused when they're
    // reached. The walk and its caches live in PlanWarningsTracker (plan 052); these forward to it.
    public partial class Simulation
    {
        private readonly PlanWarningsTracker _planWarnings;

        /// <summary>
        /// Why each queue entry will be refused when it's reached, by queue index (null: no warning), in
        /// the words the run would use. Worked out again only after something that can change it.
        /// Placeholder rule (plan ui-024d): the walk warns only where it's sure. It starts from where the
        /// queue starts (between runs: the start room, her packed items and what she keeps; during a run:
        /// where she is, her pockets and every floor), and leaves the top entry of a run to the real check.
        /// Warned: a trip that can't be made; a locked task; a task not offered in the room she'd be in; a
        /// hue she hasn't learned; a second once-a-run task; a need for an item nothing can give her in time
        /// (not held, packed or on a floor, given by no earlier entry, and for an action, made by nothing in
        /// that room). Items used up by earlier entries aren't subtracted, and an entry sure to be refused
        /// gives nothing. Silent: stat gates, pools, vitality, full pockets or floors, pick-ups and
        /// put-downs, and everything after an entry that can flip a switch or a trip refused for an item.
        /// </summary>
        public IReadOnlyList<string> PlanWarnings() => _planWarnings.Warnings();

        /// <summary>
        /// The warnings of the entries at one room's stops, in queue order (empty when none): the same
        /// words as <see cref="PlanWarnings"/>, grouped the way the queue column groups stops (a trip
        /// belongs to the stop it starts: a trip the way can't make counts in the room she'd leave, one refused
        /// for a missing item in the room it arrives at).
        /// </summary>
        public IReadOnlyList<string> RoomWarnings(NodeDefinition room) => _planWarnings.RoomWarnings(room);

        /// <summary>How many queued entries <see cref="PlanWarnings"/> warns about.</summary>
        public int PlanWarningCount => _planWarnings.Count;

        private void MarkPlanWarningsStale() => _planWarnings.MarkStale();

        /// <summary>Whether the task really gives this item. Shared by the plan's warnings and supplying.</summary>
        internal static bool Gives(TaskDefinition task, ResourceDefinition item)
        {
            foreach (var give in task.gives)
                if (give.IsReal && give.resource == item)
                    return true;
            return false;
        }
    }
}
