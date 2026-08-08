using System;
using BomberLegends.Services.Save;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Run;

namespace BomberLegends.Gameplay.Run
{
    /// <summary>
    /// Moves a run between memory and the save.
    /// </summary>
    /// <remarks>
    /// Kept out of <see cref="GameRun"/>, which is engine-free and knows nothing about storage, and
    /// out of the save service, which knows nothing about runs. This is the only place the two
    /// shapes meet.
    /// </remarks>
    public static class RunPersistence
    {
        /// <summary>Reads whatever run was left unfinished.</summary>
        public static RunSnapshot Read(ISaveService? save)
        {
            var data = save?.Data;

            if (data == null || !data.HasRunInProgress)
            {
                return RunSnapshot.None;
            }

            var stored = data.RunItems;
            var items = new ItemId[stored.Length];

            for (var i = 0; i < stored.Length; i++)
            {
                items[i] = (ItemId)stored[i];
            }

            // Cast back through the same signed reading it was written with; JsonUtility has no
            // unsigned integers, and the bits are what matter.
            return new RunSnapshot(
                unchecked((uint)data.RunSeed),
                data.RunArenaIndex,
                data.RunHealth,
                items,
                unchecked((uint)data.RunOfferState));
        }

        /// <summary>Stores a run so it survives the session.</summary>
        public static void Write(ISaveService? save, in RunSnapshot snapshot)
        {
            if (save?.Data == null)
            {
                return;
            }

            if (!snapshot.HasProgress)
            {
                Clear(save);
                return;
            }

            var items = new int[snapshot.Held.Length];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = (int)snapshot.Held[i];
            }

            var data = save.Data;

            data.HasRunInProgress = true;
            data.RunSeed = unchecked((int)snapshot.Seed);
            data.RunArenaIndex = snapshot.ArenaIndex;
            data.RunHealth = snapshot.CarriedHealth;
            data.RunItems = items;
            data.RunOfferState = unchecked((int)snapshot.OfferState);

            save.MarkDirty();
        }

        /// <summary>Forgets the stored run, after a death or a fresh start.</summary>
        public static void Clear(ISaveService? save)
        {
            if (save?.Data == null || !save.Data.HasRunInProgress)
            {
                return;
            }

            save.Data.HasRunInProgress = false;
            save.Data.RunItems = Array.Empty<int>();

            save.MarkDirty();
        }
    }
}
