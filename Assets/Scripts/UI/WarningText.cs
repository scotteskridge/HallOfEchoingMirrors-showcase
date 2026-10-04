using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How a warning ahead on the plan reads, wherever it's shown (the Queue column's cards and rows, the map's
    /// room tags): the ⚠ mark and the orange reason lines, so every place words them the same way.
    /// </summary>
    public static class WarningText
    {
        /// <summary>The ⚠, in the warning colour.</summary>
        public static string Mark => UiStyle.Colour(GameText.Get("queue.warning_mark"), UiStyle.Warning);

        /// <summary>A reason on its own line, in the warning colour.</summary>
        public static string ReasonLine(string reason) => UiStyle.Colour(reason, UiStyle.Warning);

        /// <summary>"⚠ reason", all in the warning colour (a row's tooltip).</summary>
        public static string MarkedReason(string reason) =>
            UiStyle.Colour(GameText.Get("queue.warning_mark") + " " + reason, UiStyle.Warning);

        /// <summary>"Light a candle: needs 1 Candle", all in the warning colour (a folded block lists its refused actions this way).</summary>
        public static string ActionWithReason(string action, string reason) =>
            UiStyle.Colour(GameText.Get("queue.action_with_reason", ("action", action), ("reason", reason)), UiStyle.Warning);
    }
}
