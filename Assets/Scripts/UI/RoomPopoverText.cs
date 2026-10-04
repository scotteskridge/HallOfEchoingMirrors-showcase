using System.Collections.Generic;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The wording RoomPopover shows for its title, the route note under it, and the Play, Schedule and
    /// Carry tips (split out of RoomPopover). Everything is passed in, so it holds no state and never
    /// holds the Simulation, which loading replaces.
    /// </summary>
    public static class RoomPopoverText
    {
        /// <summary>The room's name, then any knowledge found here that has done its job ("Mirrors Found 10"). <paramref name="known"/> is refilled with that knowledge.</summary>
        public static string Title(Simulation sim, NodeDefinition room, ScheduleTarget target, List<(ResourceDefinition item, int amount)> known)
        {
            sim.DoneKnowledgeFoundIn(room, known);
            string note = RouteNote(sim, target);
            string heart = UiStyle.Aside(RoomKnowledgeText.ByHeart(sim, room));
            note = note == null ? heart : note + "\n" + heart;
            if (known.Count == 0)
                return room.DisplayName + "\n" + note;
            string list = null;
            foreach (var (item, amount) in known)
            {
                string one = GameText.Get("popover.known_item", ("item", item.DisplayName), ("amount", amount));
                list = list == null ? one : GameText.Get("popover.known_more", ("list", list), ("item", one));
            }
            string title = GameText.Get("popover.title_known", ("room", room.DisplayName), ("known", UiStyle.Aside(list)));
            return title + "\n" + note;
        }

        /// <summary>The line under the title: which stop Schedule joins, or how far it would walk her first. Null when neither.</summary>
        private static string RouteNote(Simulation sim, ScheduleTarget target)
        {
            if (!target.CanSchedule || sim.StartNode == null)
                return null;
            string note = target.OnRoute
                ? GameText.Get("popover.on_route", ("stop", target.StopNumber))
                : GameText.Get("popover.adds_walk", ("time", UiText.Clock(target.WalkSeconds)));
            return UiStyle.Aside(note);
        }

        private static bool CanPlayHere(Simulation sim, NodeDefinition room) => room == sim.PlannedNodeAfter(0);

        /// <param name="sim">May be null (no game yet).</param>
        public static string PlayTip(Simulation sim, NodeDefinition room, ScheduleTarget target)
        {
            if (sim != null && !sim.RunUnderWay)
                return GameText.Get("popover.play_between_runs_tip");
            if (sim == null || CanPlayHere(sim, room))
                return GameText.Get("actions.play_tip");
            return target.CanSchedule ? GameText.Get("popover.play_elsewhere") : target.Reason;
        }

        public static string ScheduleTip(ScheduleTarget target) =>
            target.CanSchedule ? GameText.Get("actions.schedule_tip") : target.Reason;

        /// <param name="sim">May be null (no game yet).</param>
        public static string CarryTip(Simulation sim, NodeDefinition room, ScheduleTarget target)
        {
            if (sim == null || CanPlayHere(sim, room))
                return GameText.Get("actions.carry_tip");
            return target.CanSchedule ? GameText.Get("popover.carry_elsewhere") : target.Reason;
        }
    }
}
