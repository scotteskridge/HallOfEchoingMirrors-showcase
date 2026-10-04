namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// What kind of text a label is, which sets its font and size (the UiFonts asset). Every text
    /// in the UI has one (FontRole). New roles go at the end: labels store the number.
    /// </summary>
    public enum TextRole
    {
        /// <summary>Tooltips, buttons, chips, numbers: the everyday sans (Inter).</summary>
        Body,
        /// <summary>The game's title on the menu (Boecklins Universe).</summary>
        Title,
        /// <summary>A panel's or pop-up's heading (the serif).</summary>
        Heading,
        /// <summary>Room and action names (the serif).</summary>
        Name,
        /// <summary>Story in Clara's voice: the feed's lines, story cards and the story pop-up (the serif).</summary>
        Story,
        /// <summary>Small print: details, side notes, section labels (the sans).</summary>
        Small,
    }
}
