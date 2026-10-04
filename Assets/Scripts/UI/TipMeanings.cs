namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Maps a <see cref="TipMeaning"/> to its colour role and second cue, in one place so no tooltip
    /// builder can forget the cue. (✓ and ○ come from the Inter symbols fallback font.)
    /// </summary>
    public static class TipMeanings
    {
        public static ColourRole RoleOf(TipMeaning meaning)
        {
            switch (meaning)
            {
                case TipMeaning.Time: return ColourRole.TipTime;
                case TipMeaning.Vitality: return ColourRole.TipVitality;
                case TipMeaning.Met: return ColourRole.TipMet;
                case TipMeaning.Unmet: return ColourRole.TipUnmet;
                default: return ColourRole.TextMain;
            }
        }

        /// <summary>Times and vitality are the numbers she decides by: shown large, with their unit in small print beside them.</summary>
        public static bool IsBig(TipMeaning meaning) => meaning == TipMeaning.Time || meaning == TipMeaning.Vitality;

        /// <summary>The mark shown before the value: ✓ for met, ○ for unmet, nothing for the rest.</summary>
        public static string CueOf(TipMeaning meaning)
        {
            switch (meaning)
            {
                case TipMeaning.Met: return "✓ ";
                case TipMeaning.Unmet: return "○ ";
                default: return "";
            }
        }
    }
}
