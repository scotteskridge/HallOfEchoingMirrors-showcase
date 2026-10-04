namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// A colour that's set once and doesn't change at runtime: a panel, row, button face, bar fill,
    /// or piece of text. Every Graphic tagged with one (ColourRoleTag) takes its colour from the
    /// UiColours asset. State-dependent colour pairs (hover/pressed/locked, dragging) and rich-text
    /// colours stay in UiStyle: they're not roles. New roles go at the end: labels store the number.
    /// </summary>
    public enum ColourRole
    {
        PanelBackground,
        RowBackground,
        BandBackground,
        DrawerShade,
        /// <summary>Unused: no Graphic has this colour today (Step 88's "behind the map" case never matched anything distinct from BandBackground). Reserved.</summary>
        MapBackground,
        PopoverBackground,
        /// <summary>Unused: StopCard.cs sets this at runtime from UiStyle.StopCard/StopCardCurrent, a state pair — not a role Apply UI Colours can reach.</summary>
        StopCard,
        ChipBackground,
        ChipBackgroundFaint,
        TipBackground,
        NoticeBackground,
        Track,
        /// <summary>Unused: SlideDrawer.cs sets this at runtime from UiStyle.BookmarkTab/BookmarkTabOpen, a state pair — not a role Apply UI Colours can reach.</summary>
        BookmarkTab,
        DragGhost,
        ButtonFace,
        Accent,
        AccentText,
        TextMain,
        TextSecondary,
        PageTint,
        VitalityFill,
        MasteryBar,
        LevelBar,
        QueueProgress,
        /// <summary>Unused: the room popover's searched bar takes its colour from MapStyle (RoomPopover.cs), not this asset.</summary>
        SearchedBar,
        DropLine,
        /// <summary>A tooltip's times ("1.1s"): the amber of a milestone.</summary>
        TipTime,
        /// <summary>A tooltip's vitality costs.</summary>
        TipVitality,
        /// <summary>A tooltip's met conditions and "kept" notes.</summary>
        TipMet,
        /// <summary>A tooltip's unmet conditions.</summary>
        TipUnmet,
        /// <summary>A tooltip's footer strip, darker than its body.</summary>
        TipFooter,
        /// <summary>A tooltip's frame and dividers.</summary>
        TipBorder,
    }
}
