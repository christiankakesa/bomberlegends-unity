using UnityEngine;

namespace BomberLegends.Gameplay.Run
{
    /// <summary>
    /// How each attempt at a run begins: on which seed, and on which arena.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A seed of zero means <b>a fresh one every attempt</b>. Rounds 1–3 of the playtest ran on a
    /// fixed seed, which handed every tester the same first board and the same first three items,
    /// and turned one row of the recording sheet into a fact about three cards rather than about a
    /// player. It also let the developer learn one board sequence by heart. Any other value is a
    /// fixed seed, replayable end to end, which is what tuning a specific board wants.
    /// </para>
    /// <para>
    /// The starting arena is a development aid in the same spirit as the starting items: a way to
    /// reach a state without playing up to it. A performance session on arena nine should cost ten
    /// minutes, not a twenty-five-minute climb that one death erases.
    /// </para>
    /// <para>
    /// Lives in the view layer because the fresh seed comes from the platform. Nothing in the
    /// simulation may draw a number it cannot reproduce, so the choice of seed is made out here and
    /// handed in.
    /// </para>
    /// </remarks>
    public sealed class RunStart
    {
        private readonly uint _fixedSeed;

        /// <summary>Describes how attempts begin.</summary>
        /// <param name="fixedSeed">The seed for every attempt, or zero for a fresh one each time.</param>
        /// <param name="startingArena">The arena each attempt begins on, counting from one.</param>
        public RunStart(uint fixedSeed, int startingArena)
        {
            _fixedSeed = fixedSeed;
            StartingArenaIndex = Mathf.Max(0, startingArena - 1);
        }

        /// <summary>Whether every attempt draws its own seed.</summary>
        public bool IsFresh => _fixedSeed == 0u;

        /// <summary>Which arena each attempt begins on, counting from zero.</summary>
        public int StartingArenaIndex { get; }

        /// <summary>Whether attempts begin anywhere but the first arena.</summary>
        public bool StartsDeep => StartingArenaIndex > 0;

        /// <summary>The seed the next attempt should run on.</summary>
        public uint NextSeed() => IsFresh ? FreshSeed() : _fixedSeed;

        /// <summary>
        /// A seed nobody chose.
        /// </summary>
        /// <remarks>
        /// Never zero, because zero is the request for a fresh seed and would read back as one; and
        /// never the same twice in a session, or two attempts in a row would replay each other.
        /// </remarks>
        private static uint FreshSeed()
        {
            var seed = unchecked((uint)Random.Range(int.MinValue, int.MaxValue));

            return seed == 0u ? 1u : seed;
        }
    }
}
