// ShipVisualizer.cs
// Renders a ShipGrid as a grid of colored UI squares inside a RectTransform.
// Works in both the Space Map miniature preview and the Pre-Fight full view.
//
// Usage: place on any UI GameObject that has a RectTransform.
//        Call Render(shipData) to populate it.

using UnityEngine;
using UnityEngine.UI;
using SpaceGame.Core;
using SpaceGame.Ship;

namespace SpaceGame.UI
{
    public class ShipVisualizer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Tooltip("Size in pixels of each module cell.")]
        [SerializeField] private float _cellSize  = 32f;

        [Tooltip("Gap between cells in pixels.")]
        [SerializeField] private float _cellGap   = 2f;

        [Tooltip("If true, the visualizer auto-sizes its RectTransform to fit the grid.")]
        [SerializeField] private bool  _autoResize = true;

        // ── Runtime ───────────────────────────────────────────────────────────
        private ShipData _currentShip;

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Clear and re-render the grid for the given ship.</summary>
        public void Render(ShipData ship)
        {
            _currentShip = ship;
            Clear();
            if (ship == null || ship.Grid == null) return;
            BuildGrid(ship.Grid);
        }

        public void Clear()
        {
            // Destroy all child objects
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void BuildGrid(ShipGrid grid)
        {
            int rows = grid.Rows;
            int cols = grid.Cols;
            float step = _cellSize + _cellGap;

            if (_autoResize)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(cols * step - _cellGap, rows * step - _cellGap);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    ModuleType type  = grid.Get(r, c);
                    ModuleStats stat = ModuleDefinition.Get(type);

                    // Create cell GameObject
                    var cell = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image));
                    cell.transform.SetParent(transform, false);

                    var cellRT = cell.GetComponent<RectTransform>();
                    cellRT.anchorMin = new Vector2(0f, 1f);
                    cellRT.anchorMax = new Vector2(0f, 1f);
                    cellRT.pivot     = new Vector2(0f, 1f);
                    cellRT.sizeDelta = new Vector2(_cellSize, _cellSize);
                    cellRT.anchoredPosition = new Vector2(c * step, -r * step);

                    var img = cell.GetComponent<Image>();
                    img.color = stat.DisplayColor;

                    // Add a subtle outline for non-empty modules
                    if (type != ModuleType.Empty)
                        AddOutline(cell);

                    // Tooltip on hover (uses Unity's built-in EventTrigger would be complex;
                    // we store the type on the cell instead for future use)
                    cell.name = $"{stat.DisplayName}_{r}_{c}";
                }
            }
        }

        private static void AddOutline(GameObject cell)
        {
            var outline = cell.AddComponent<Outline>();
            outline.effectColor    = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
