// ShipGrid.cs
// Represents a ship's layout as a 2D array of ModuleTypes.
// Handles CSV parsing and serialization.
//
// CSV format:
//   Rows are newline-separated, columns are comma-separated.
//   Each cell is a single module code character (e, g, s, h, etc.)
//   Example 2x3 ship:
//     _,g,_
//     e,h,e
//
// Row 0 is the TOP of the ship (nose), last row is the BOTTOM (engines).

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SpaceGame.Core;

namespace SpaceGame.Ship
{
    [Serializable]
    public class ShipGrid
    {
        // ── Data ──────────────────────────────────────────────────────────────
        [SerializeField] private int _rows;
        [SerializeField] private int _cols;

        // Flat serializable array; index = row * _cols + col
        [SerializeField] private ModuleType[] _cells;

        // ── Properties ────────────────────────────────────────────────────────
        public int Rows => _rows;
        public int Cols => _cols;

        // ── Constructor ───────────────────────────────────────────────────────
        public ShipGrid(int rows, int cols)
        {
            _rows  = rows;
            _cols  = cols;
            _cells = new ModuleType[rows * cols];
        }

        // ── Accessors ─────────────────────────────────────────────────────────
        public ModuleType Get(int row, int col)
        {
            if (!InBounds(row, col)) return ModuleType.Empty;
            return _cells[row * _cols + col];
        }

        public void Set(int row, int col, ModuleType type)
        {
            if (!InBounds(row, col)) return;
            _cells[row * _cols + col] = type;
        }

        public bool InBounds(int row, int col)
            => row >= 0 && row < _rows && col >= 0 && col < _cols;

        // ── CSV Parsing ───────────────────────────────────────────────────────
        /// <summary>
        /// Parse a CSV text string into a new ShipGrid.
        /// Lines are rows; comma-separated tokens are columns.
        /// The grid size is determined by the content (non-uniform rows are padded with Empty).
        /// </summary>
        public static ShipGrid FromCSV(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new ShipGrid(1, 1);

            string[] lines = csv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Determine dimensions
            int rows = lines.Length;
            int cols = 0;
            var parsedRows = new List<string[]>(rows);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(',');
                parsedRows.Add(tokens);
                if (tokens.Length > cols) cols = tokens.Length;
            }

            if (cols == 0) cols = 1;

            var grid = new ShipGrid(rows, cols);
            for (int r = 0; r < rows; r++)
            {
                string[] tokens = parsedRows[r];
                for (int c = 0; c < cols; c++)
                {
                    string token = c < tokens.Length ? tokens[c] : "_";
                    grid.Set(r, c, ModuleTypeExtensions.FromCode(token));
                }
            }
            return grid;
        }

        /// <summary>
        /// Parse a CSV from a Unity TextAsset (file loaded via Resources or direct reference).
        /// </summary>
        public static ShipGrid FromTextAsset(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[ShipGrid] TextAsset is null.");
                return new ShipGrid(1, 1);
            }
            return FromCSV(asset.text);
        }

        // ── CSV Serialization ─────────────────────────────────────────────────
        /// <summary>Serialize this grid back to a CSV string.</summary>
        public string ToCSV()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    sb.Append(Get(r, c).ToCode());
                    if (c < _cols - 1) sb.Append(',');
                }
                if (r < _rows - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        // ── Stats Helpers ─────────────────────────────────────────────────────
        /// <summary>Count how many modules of a given type exist on this grid.</summary>
        public int CountOf(ModuleType type)
        {
            int count = 0;
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == type) count++;
            return count;
        }

        /// <summary>Total HP of all modules (sum of each module's MaxHP).</summary>
        public int TotalHP()
        {
            int hp = 0;
            foreach (var cell in _cells)
                hp += ModuleDefinition.Get(cell).MaxHP;
            return hp;
        }

        /// <summary>Total damage output per round from all weapon modules.</summary>
        public int TotalDamagePerRound()
        {
            int dmg = 0;
            foreach (var cell in _cells)
                dmg += ModuleDefinition.Get(cell).DamagePerRound;
            return dmg;
        }

        /// <summary>Evasion chance (0–1 clamped) from all engine/thruster modules.</summary>
        public float TotalEvasion()
        {
            float ev = 0f;
            foreach (var cell in _cells)
                ev += ModuleDefinition.Get(cell).EvasionBonus;
            return Mathf.Clamp01(ev);
        }

        /// <summary>Total shield HP from all shield modules.</summary>
        public int TotalShieldHP()
        {
            int shp = 0;
            foreach (var cell in _cells)
                shp += ModuleDefinition.Get(cell).ShieldHP;
            return shp;
        }

        /// <summary>Flat list of all non-Empty module types (used for loot drops).</summary>
        public List<ModuleType> AllModules()
        {
            var list = new List<ModuleType>();
            foreach (var cell in _cells)
                if (cell != ModuleType.Empty)
                    list.Add(cell);
            return list;
        }

        /// <summary>Deep copy of this grid.</summary>
        public ShipGrid Clone()
        {
            var clone = new ShipGrid(_rows, _cols);
            Array.Copy(_cells, clone._cells, _cells.Length);
            return clone;
        }
    }
}
