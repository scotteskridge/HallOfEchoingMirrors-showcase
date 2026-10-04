namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One line of what Clara carries (Simulation.CarriedItems): items in her pockets, an object
    /// carried outside them (a satchel), a container she holds, or items inside that container
    /// (which then name it).
    /// </summary>
    public readonly struct CarriedEntry
    {
        public readonly ResourceDefinition Item;
        public readonly int Amount;
        /// <summary>The container these are in, or null otherwise (her pockets, a satchel, a container itself).</summary>
        public readonly ResourceDefinition Container;

        public CarriedEntry(ResourceDefinition item, int amount, ResourceDefinition container)
        {
            Item = item;
            Amount = amount;
            Container = container;
        }

        /// <summary>Whether this line is a container she holds, rather than something carried in pockets or in one.</summary>
        public bool IsContainer => Container == null && Item != null && Item.IsContainer;
    }
}
