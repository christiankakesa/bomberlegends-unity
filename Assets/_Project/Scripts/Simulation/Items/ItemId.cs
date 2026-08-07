namespace BomberLegends.Simulation.Items
{
    /// <summary>Every passive item that can be carried.</summary>
    /// <remarks>
    /// The identity of an item and nothing else. What it <i>does</i> is data, held in
    /// <see cref="ItemCatalog"/>, so adding an item is an entry in a table rather than a branch in
    /// a system.
    /// </remarks>
    public enum ItemId : byte
    {
        /// <summary>An empty inventory slot.</summary>
        None = 0,

        /// <summary>The skillshot sets off bombs it flies over.</summary>
        Overcharge = 1,

        /// <summary>The dash injures enemies it passes through.</summary>
        Momentum = 2,

        /// <summary>Every skill travels half again as far and fast.</summary>
        KineticCore = 3
    }
}
