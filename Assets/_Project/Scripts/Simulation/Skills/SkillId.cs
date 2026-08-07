namespace BomberLegends.Simulation.Skills
{
    /// <summary>Which behaviour a loadout slot runs.</summary>
    /// <remarks>
    /// The only part of a skill that is not a number. Everything else about a skill — how long it
    /// recharges, how hard it hits, how fast and how far it goes — is tuning held in
    /// <see cref="SkillSlot"/>, which is what lets items reshape a skill without new code.
    /// </remarks>
    public enum SkillId : byte
    {
        /// <summary>An empty slot.</summary>
        None = 0,

        /// <summary>A short burst of speed in the direction of travel.</summary>
        Dash = 1,

        /// <summary>An aimed projectile that damages the first enemy it touches.</summary>
        Skillshot = 2
    }
}
