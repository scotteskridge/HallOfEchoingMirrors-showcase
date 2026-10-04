using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The words under a room on the map and in its hover text (the map's, split out of MapView): how well
    /// she knows the room, its room speed, and on the planning screen the warnings ahead on the plan.
    /// </summary>
    public static class MapTagText
    {
        /// <summary>
        /// How well she knows a room, and (on the planning screen) how fast its clock runs now and after one more run.
        /// <paramref name="showSpeedLines"/> and <paramref name="showPlanMarks"/> are the planning screen's switches on the map.
        /// </summary>
        public static string TagText(Simulation sim, NodeDefinition node, MapStyle style, bool showSpeedLines, bool showPlanMarks)
        {
            string tag = RoomKnowledgeText.ByHeart(sim, node) + WarnMark(sim, node, showPlanMarks);
            if (!sim.IsKnownByHeart(node) || !showSpeedLines || !sim.RoomSpeedUnlocked)
                return tag;
            int runs = sim.RunsWorkedIn(node);
            float now = sim.RoomSpeed(node);
            float next = sim.Settings.RoomSpeedAfter(runs + 1);
            string line = next > now
                ? GameText.Get("map.speed_line", ("now", UiText.Number(now)), ("next", UiText.Number(next)))
                : GameText.Get("map.speed_full", ("now", UiText.Number(now)));
            return tag + "\n" + UiStyle.Sized(UiStyle.Colour(line, "#" + ColorUtility.ToHtmlStringRGB(style.speedLine)), style.speedLineSize);
        }

        /// <summary>
        /// On the planning screen: ⚠ after a room's tag when a stop here will be refused (the same mark the
        /// Queue column's cards wear).
        /// </summary>
        private static string WarnMark(Simulation sim, NodeDefinition node, bool showPlanMarks) =>
            showPlanMarks && sim.RoomWarnings(node).Count > 0 ? " " + WarningText.Mark : "";

        /// <summary>On the planning screen: a line under a room's tag when a stop here won't carry over into the next run.</summary>
        public static string NotCarriedLine(IReadOnlyList<QueueStop> stops, NodeDefinition node, bool showPlanMarks)
        {
            if (!showPlanMarks)
                return "";
            foreach (var stop in stops)
                if (stop.Room == node && StopCard.MarksNotCarried(stop))
                    return "\n" + WarningText.ReasonLine(GameText.Get("map.not_carried_mark"));
            return "";
        }

        /// <summary>
        /// The hover text of a room, built here and nowhere else: on the planning screen first why stops here
        /// will be refused; then, known by heart, how much faster its clock runs (once that is unlocked),
        /// otherwise why what she does here won't carry over.
        /// </summary>
        public static string RoomTip(Simulation sim, NodeDefinition node, bool showPlanMarks)
        {
            string tip = RoomSpeedTip(sim, node);
            var warnings = showPlanMarks ? sim.RoomWarnings(node) : null;
            if (warnings == null || warnings.Count == 0)
                return tip;
            var lines = new List<string>();
            foreach (var reason in warnings)
                lines.Add(WarningText.ReasonLine(reason));
            string reasons = GameText.Get("map.warning_tip", ("reasons", string.Join("\n", lines)));
            return tip == null ? reasons : reasons + "\n" + tip;
        }

        private static string RoomSpeedTip(Simulation sim, NodeDefinition node)
        {
            if (!sim.IsKnownByHeart(node))
                return GameText.Get("queue.not_carried_tip", ("needed", sim.Settings.byHeartRuns));
            float speed = sim.RoomSpeed(node);
            return sim.RoomSpeedUnlocked && speed > 1f
                ? GameText.Get("map.room_speed_tip", ("speed", UiText.Number(speed)), ("runs", sim.RunsWorkedIn(node).ToString()))
                : null;
        }
    }
}
