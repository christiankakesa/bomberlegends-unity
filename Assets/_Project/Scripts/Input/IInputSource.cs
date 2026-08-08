using BomberLegends.Simulation;

namespace BomberLegends.Input
{
    /// <summary>
    /// Produces one tick of player intent.
    /// </summary>
    /// <remarks>
    /// Every way of controlling the game funnels through this: touch, keyboard, gamepad, a recorded
    /// replay, and eventually a remote player. Because the simulation consumes nothing but the
    /// resulting <see cref="PlayerIntent"/>, none of those need any gameplay code of their own.
    /// </remarks>
    public interface IInputSource
    {
        /// <summary>
        /// Which family of devices this reads.
        /// </summary>
        /// <remarks>
        /// Declared so a composite can hand control to whichever device the player last actually
        /// used. Without it, sources that are always readable — a mouse always has a position —
        /// drown out ones that are only sometimes touched.
        /// </remarks>
        ControlScheme Scheme { get; }

        /// <summary>Samples the control surface for the given simulation tick.</summary>
        PlayerIntent Sample(int tick);
    }
}
