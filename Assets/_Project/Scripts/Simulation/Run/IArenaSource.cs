using BomberLegends.Core;
using BomberLegends.Simulation.Board;

namespace BomberLegends.Simulation.Run
{
    /// <summary>
    /// Supplies the arena a run should play next.
    /// </summary>
    /// <remarks>
    /// Exists so a run does not care whether its levels were authored by hand or rolled from a seed.
    /// Both are useful: generated arenas give a run variety, and a fixed list is how a specific
    /// layout gets played over and over while something is being tuned.
    /// </remarks>
    public interface IArenaSource
    {
        /// <summary>Produces the arena for a position in the run.</summary>
        /// <param name="random">
        /// A generator seeded for this arena alone, so what the board looks like cannot shift
        /// because something elsewhere in the run happened to roll a different number of times.
        /// </param>
        LevelLayout Create(int arenaIndex, ref DeterministicRandom random);
    }

    /// <summary>Plays a fixed list of hand-authored arenas in order, repeating from the start.</summary>
    public sealed class AuthoredArenaSource : IArenaSource
    {
        private readonly LevelLayout[] _arenas;

        /// <summary>Creates a source over the given arenas.</summary>
        /// <exception cref="System.ArgumentException">No arenas were supplied.</exception>
        public AuthoredArenaSource(LevelLayout[] arenas)
        {
            if (arenas == null || arenas.Length == 0)
            {
                throw new System.ArgumentException("A run needs at least one arena.", nameof(arenas));
            }

            _arenas = arenas;
        }

        /// <inheritdoc />
        public LevelLayout Create(int arenaIndex, ref DeterministicRandom random) =>
            _arenas[arenaIndex % _arenas.Length];
    }

    /// <summary>Rolls a fresh arena for every stage of the run.</summary>
    public sealed class GeneratedArenaSource : IArenaSource
    {
        private readonly ArenaSettings _settings;

        /// <summary>Creates a source generating within the given bounds.</summary>
        public GeneratedArenaSource(in ArenaSettings settings)
        {
            settings.Validate();
            _settings = settings;
        }

        /// <inheritdoc />
        public LevelLayout Create(int arenaIndex, ref DeterministicRandom random) =>
            ArenaGenerator.Generate(arenaIndex, _settings, ref random);
    }
}
