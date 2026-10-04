using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How a queue entry reads on screen, wherever it's shown (the ribbon, the queue drawer's stop
    /// cards): its progress, its line of details, and its tooltip.
    /// </summary>
    public static class QueueEntryText
    {
        public static bool IsRunning(Simulation sim, QueueEntry entry) =>
            entry == sim.Loop.RunningEntry && !sim.Loop.IsOver;

        /// <summary>How far through it is (0 to 1): the running one, or one Play pushed down (it carries on from there).</summary>
        public static float Progress(Simulation sim, QueueEntry entry) =>
            IsRunning(sim, entry) ? sim.Loop.CurrentTaskProgress : entry.SavedFraction;

        /// <summary>"4.2s left", or "waiting, 40% done".</summary>
        public static string TimeLeft(Simulation sim, QueueEntry entry)
        {
            if (IsRunning(sim, entry))
                return GameText.Get("queue.left", ("seconds", UiText.Countdown(sim.CurrentTimeLeftSeconds)));
            return entry.SavedWork > 0f
                ? GameText.Get("queue.waiting", ("percent", UiText.Percent(Progress(sim, entry))))
                : GameText.Get("queue.next");
        }

        /// <summary>"4.2s left · done 3 times", "waiting, 40% done", or how long it repeats; <paramref name="tripCharge"/> is the entry's entry in <see cref="Simulation.PlannedTripCharges"/>.</summary>
        public static string Details(Simulation sim, QueueEntry entry, float tripCharge)
        {
            string detail = TimeLeft(sim, entry);
            if (entry.CarryOnly)
                detail += " " + UiStyle.Aside(GameText.Get("queue.carry_only"));
            if (entry.SuppliesFor != null)
                detail += " " + UiStyle.Aside(GameText.Get("queue.supplying",
                    ("item", entry.Supplies.DisplayName), ("task", entry.SuppliesFor.Task.displayName)));
            if (entry.TimesDone > 0)
                detail += " " + GameText.Get("queue.done_times", ("count", entry.TimesDone));
            if (entry.TimesLeft.HasValue)
                detail += " " + GameText.Get("queue.times_left", ("count", entry.TimesLeft.Value));
            // A trip shows what it will charge: the growing cost of every move this run, counting the moves queued before it.
            if (tripCharge > 0f)
                detail += " " + UiStyle.Aside(GameText.Get("queue.trip_charge", ("cost", GameText.Get("costs.part",
                    ("amount", UiText.Number(tripCharge)), ("source", sim.Loop.Vitality.Name)))));
            return detail;
        }

        /// <summary>The hover text for the entry at <paramref name="index"/>: its name, how long it repeats, how the queue works.</summary>
        public static string Tip(Simulation sim, int index, float tripCharge)
        {
            if (sim == null || index < 0 || index >= sim.Queue.Count)
                return null;
            var entry = sim.Queue.Entries[index];
            var room = sim.HasPlaces ? sim.PlannedNodeAfter(index) : null;
            // The charge shown on a trip's row is projected from what she holds now, not what she'll pick up on the way.
            string chargeNote = tripCharge > 0f ? UiStyle.Aside(GameText.Get("queue.trip_charge_tip")) + "\n" : "";
            return UiStyle.Heading(ActionText.NameOf(sim, index, entry)) + "\n" +
                   ActionText.RepeatRule(sim, entry.Task, entry.Destination, room) + "\n" +
                   chargeNote +
                   UiStyle.Aside(GameText.Get("queue.row_tip"));
        }

        /// <summary>Sends the entry to the top of the queue (Play: she does it now). Nothing happens if it has left the queue since the click was drawn.</summary>
        public static void ToTop(Simulation sim, QueueEntry entry)
        {
            if (sim == null || entry == null || !sim.Queue.Entries.Contains(entry))
                return;
            if (entry.Destination != null)
                sim.PlayTripNow(entry.Destination);
            else
                sim.PlayNow(entry.Task);
        }
    }
}
