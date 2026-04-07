// ShipData.cs
// Runtime representation of a ship: its grid layout + derived combat stats.
// Enemy ships are created from CSV TextAssets; the player ship is persisted
// via JSON in Application.persistentDataPath.

using System.Collections.Generic;
using UnityEngine;
using SpaceGame.Core;

namespace SpaceGame.Ship
{
    /// <summary>
    /// A fully-described ship with a grid layout and derived stats.
    /// This is a plain C# class (not a MonoBehaviour/ScriptableObject) so it
    /// can be freely created, copied, and serialized at runtime.
    /// </summary>
    [System.Serializable]
    public class ShipData
    {
        // ── Identity ──────────────────────────────────────────────────────────
        public string ShipName   = "Unknown";
        public int    ThreatLevel = 1;       // 1–5, shown on the space map

        // ── Layout ────────────────────────────────────────────────────────────
        public ShipGrid Grid;

        // ── Derived stats (computed, not serialized) ──────────────────────────
        [System.NonSerialized] public int   MaxHP;
        [System.NonSerialized] public int   CurrentHP;
        [System.NonSerialized] public int   MaxShieldHP;
        [System.NonSerialized] public int   CurrentShieldHP;
        [System.NonSerialized] public int   DamagePerRound;
        [System.NonSerialized] public float EvasionChance;   // 0–1

        // ── Constructor ───────────────────────────────────────────────────────
        public ShipData(string name, ShipGrid grid, int threatLevel = 1)
        {
            ShipName    = name;
            Grid        = grid;
            ThreatLevel = threatLevel;
            RecalculateStats();
        }

        // ── Stats ─────────────────────────────────────────────────────────────
        /// <summary>Recompute all derived stats from the current grid.</summary>
        public void RecalculateStats()
        {
            if (Grid == null) return;
            MaxHP           = Grid.TotalHP();
            CurrentHP       = MaxHP;
            MaxShieldHP     = Grid.TotalShieldHP();
            CurrentShieldHP = MaxShieldHP;
            DamagePerRound  = Grid.TotalDamagePerRound();
            EvasionChance   = Grid.TotalEvasion();
        }

        /// <summary>True when CurrentHP has dropped to zero or below.</summary>
        public bool IsDestroyed => CurrentHP <= 0;

        // ── Factory: from CSV TextAsset ───────────────────────────────────────
        public static ShipData FromTextAsset(TextAsset asset, string shipName, int threatLevel = 1)
        {
            var grid = ShipGrid.FromTextAsset(asset);
            return new ShipData(shipName, grid, threatLevel);
        }

        // ── Factory: starter player ship (2x3 — engine row + gun row) ─────────
        public static ShipData CreateStarterPlayerShip()
        {
            // Layout:
            //   _,g,_    ← gun in centre
            //   e,h,e    ← engines on flanks, hull in centre
            var grid = new ShipGrid(2, 3);
            grid.Set(0, 1, ModuleType.Gun);
            grid.Set(1, 0, ModuleType.Engine);
            grid.Set(1, 1, ModuleType.Hull);
            grid.Set(1, 2, ModuleType.Engine);
            return new ShipData("Player Ship", grid, 0);
        }

        // ── Loot helpers ──────────────────────────────────────────────────────
        /// <summary>
        /// Returns a random subset of this ship's modules as loot.
        /// dropRate: probability each individual module drops (0–1).
        /// </summary>
        public List<ModuleType> RollLoot(float dropRate = 0.5f)
        {
            var all  = Grid.AllModules();
            var loot = new List<ModuleType>();
            foreach (var mod in all)
                if (Random.value <= dropRate)
                    loot.Add(mod);
            return loot;
        }

        // ── Summary string (shown in pre-fight UI) ────────────────────────────
        public string StatsSummary()
        {
            return $"HP: {MaxHP}  |  Shield: {MaxShieldHP}  |  " +
                   $"DMG/round: {DamagePerRound}  |  Evasion: {EvasionChance * 100f:F0}%\n" +
                   $"Size: {Grid.Rows}x{Grid.Cols}  |  Threat: {ThreatLevel}";
        }
    }
}
