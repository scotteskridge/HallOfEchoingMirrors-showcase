namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One visit to a room in the queue's plan: where she'll be, and which queue entries happen
    /// there. A trip she'll actually make starts a stop (it's the stop's first entry); the first
    /// stop is where the queue starts from.
    /// </summary>
    public readonly struct QueueStop
    {
        public NodeDefinition Room { get; }
        /// <summary>1 for where the queue starts, then 2, 3... in order.</summary>
        public int Number { get; }
        /// <summary>The room was already a stop earlier in the queue (she's going back).</summary>
        public bool IsReturn { get; }
        /// <summary>The index in the queue of this stop's first entry, and how many entries it has.</summary>
        public int FirstEntry { get; }
        public int EntryCount { get; }
        /// <summary>The stop has entries and every one is carried from a room known by heart: the column folds it into one block.</summary>
        public bool ByHeart { get; }
        /// <summary>
        /// What she does here will carry into the next run's plan: this room and every one before it will be
        /// known by heart when the run ends (the carry stops at the first room that isn't).
        /// </summary>
        public bool CarriesOver { get; }

        public QueueStop(NodeDefinition room, int number, bool isReturn, int firstEntry, int entryCount, bool byHeart = false, bool carriesOver = false)
        {
            Room = room;
            Number = number;
            IsReturn = isReturn;
            FirstEntry = firstEntry;
            EntryCount = entryCount;
            ByHeart = byHeart;
            CarriesOver = carriesOver;
        }

        /// <summary>This stop with <see cref="CarriesOver"/> set (the route works it out once every stop is known).</summary>
        public QueueStop WithCarriesOver(bool carriesOver) =>
            new QueueStop(Room, Number, IsReturn, FirstEntry, EntryCount, ByHeart, carriesOver);
    }
}
