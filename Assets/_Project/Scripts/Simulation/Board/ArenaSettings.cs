using System;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// The bounds a generated arena is rolled within.
    /// </summary>
    /// <remarks>
    /// Plain data with no engine types, so generation is testable in milliseconds without a scene.
    /// Difficulty is expressed as growth per arena rather than as a curve, because a curve is one
    /// more thing to tune before anyone has confirmed the base numbers are right.
    /// </remarks>
    public readonly struct ArenaSettings
    {
        /// <summary>Creates settings.</summary>
        public ArenaSettings(
            int baseWidth = 21,
            int baseHeight = 15,
            int growthPerArena = 2,
            int maxWidth = 31,
            int maxHeight = 21,
            int baseEnemies = 3,
            int enemiesPerArena = 1,
            int maxEnemies = 12,
            int destructiblePercent = 55,
            int blockClusterSize = 3,
            int minEnemyDistance = 9,
            int safePocketRadius = 2)
        {
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            GrowthPerArena = growthPerArena;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            BaseEnemies = baseEnemies;
            EnemiesPerArena = enemiesPerArena;
            MaxEnemies = maxEnemies;
            DestructiblePercent = destructiblePercent;
            BlockClusterSize = blockClusterSize;
            MinEnemyDistance = minEnemyDistance;
            SafePocketRadius = safePocketRadius;
        }

        /// <summary>Width of the first arena, including its border.</summary>
        public int BaseWidth { get; }

        /// <summary>Height of the first arena, including its border.</summary>
        public int BaseHeight { get; }

        /// <summary>Tiles added to each dimension per arena cleared.</summary>
        public int GrowthPerArena { get; }

        /// <summary>Widest an arena may become.</summary>
        public int MaxWidth { get; }

        /// <summary>Tallest an arena may become.</summary>
        public int MaxHeight { get; }

        /// <summary>Enemies in the first arena.</summary>
        public int BaseEnemies { get; }

        /// <summary>Enemies added per arena cleared.</summary>
        public int EnemiesPerArena { get; }

        /// <summary>The most enemies an arena may hold.</summary>
        public int MaxEnemies { get; }

        /// <summary>Share of free tiles, in percent, that become destructible blocks.</summary>
        public int DestructiblePercent { get; }

        /// <summary>
        /// How many tiles a run of destructible blocks grows to before a new one is started.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Density is set by <see cref="DestructiblePercent"/>; this decides how that budget is
        /// distributed. At 1 the blocks land independently of one another, which is the failure
        /// this exists to fix: uncorrelated blocks are salt and pepper, and they chop the maze into
        /// segments shorter than a blast. A bomb then fills a whole segment, so an enemy caught in
        /// it has nowhere to go and the kill was decided by the layout rather than by the player.
        /// </para>
        /// <para>
        /// Growing the same budget into runs leaves the floor between them open. The corridors get
        /// longer, an enemy has somewhere to run to, and a blast arm meets a run of blocks instead
        /// of empty floor — which is also what makes a bomb look like it breaks more than one.
        /// </para>
        /// </remarks>
        public int BlockClusterSize { get; }

        /// <summary>
        /// How far from spawn an enemy must start.
        /// </summary>
        /// <remarks>
        /// Being hit before the arena has been read is not difficulty, it is a bad first second.
        /// Kept above <see cref="SimulationConfig.EnemyAggroRadius"/> on purpose, so no Sentinel is
        /// awake when a sector begins and the player always gets a moment to look around.
        /// </remarks>
        public int MinEnemyDistance { get; }

        /// <summary>
        /// How many tiles around spawn are guaranteed clear.
        /// </summary>
        /// <remarks>
        /// The player must be able to place a bomb and leave on the very first move. Generating a
        /// spawn with one exit is a death sentence disguised as a layout.
        /// </remarks>
        public int SafePocketRadius { get; }

        /// <summary>
        /// Default bounds for the vertical slice.
        /// </summary>
        /// <remarks>
        /// Spelled out rather than written as <c>new ArenaSettings()</c>. A struct always offers an
        /// implicit parameterless constructor that zeroes every field and ignores the defaults
        /// declared on the real one, so the shorter form silently produces an arena of size zero.
        /// <see cref="Validate"/> catches it, but only once something tries to generate.
        /// </remarks>
        public static ArenaSettings Default => new ArenaSettings(
            baseWidth: 21,
            baseHeight: 15,
            growthPerArena: 2,
            maxWidth: 31,
            maxHeight: 21,
            baseEnemies: 3,
            enemiesPerArena: 1,
            maxEnemies: 12,
            destructiblePercent: 55,
            blockClusterSize: 3,
            minEnemyDistance: 9,
            safePocketRadius: 2);

        /// <summary>Throws if any value would produce an unplayable arena.</summary>
        /// <exception cref="ArgumentException">A value is outside its usable range.</exception>
        public void Validate()
        {
            if (BaseWidth < 9 || BaseHeight < 9)
            {
                throw new ArgumentException(
                    "An arena must be at least nine tiles across, or the safe pocket fills it.");
            }

            if (MaxWidth < BaseWidth || MaxHeight < BaseHeight)
            {
                throw new ArgumentException("An arena may not be capped below its starting size.");
            }

            if (GrowthPerArena < 0)
            {
                throw new ArgumentException("Arenas must not shrink as a run progresses.");
            }

            if (BaseEnemies <= 0 || MaxEnemies < BaseEnemies)
            {
                throw new ArgumentException("An arena needs at least one enemy to be clearable.");
            }

            if (MaxEnemies > LevelLayout.MaxEnemySpawns)
            {
                throw new ArgumentException(
                    $"A layout holds at most {LevelLayout.MaxEnemySpawns} enemies.");
            }

            if (DestructiblePercent < 0 || DestructiblePercent > 100)
            {
                throw new ArgumentException("Destructible density must be a percentage.");
            }

            if (BlockClusterSize < 1)
            {
                throw new ArgumentException("A run of blocks is at least one block long.");
            }

            if (MinEnemyDistance < 1 || SafePocketRadius < 1)
            {
                throw new ArgumentException("Spawn must be given room and distance.");
            }
        }
    }
}
