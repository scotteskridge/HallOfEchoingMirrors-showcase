using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>How well Clara knows a room, worded once for everywhere it shows (the room popover's header and the map's tags).</summary>
    public static class RoomKnowledgeText
    {
        /// <summary>By heart, or "not worked" / how many runs she has worked here and how many it takes.</summary>
        public static string ByHeart(Simulation sim, NodeDefinition room)
        {
            if (sim.IsKnownByHeart(room))
                return GameText.Get("popover.known_by_heart");
            int runs = sim.RunsWorkedIn(room);
            return runs == 0
                ? GameText.Get("popover.not_worked")
                : GameText.Get(runs == 1 ? "popover.runs_worked.one" : "popover.runs_worked.many",
                    ("runs", runs), ("needed", sim.Settings.byHeartRuns));
        }
    }
}
