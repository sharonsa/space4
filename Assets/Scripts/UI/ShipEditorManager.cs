// ShipEditorManager.cs
// Ship editor scene: the player arranges modules from their inventory onto a grid.
//
// Layout:
//   Left panel  — inventory list of collected modules (drag source)
//   Right panel — editable ship grid (drop target)
//   Confirm button — applies the new grid to PlayerShip and returns to the map
//
// Drag-and-drop is implemented with Unity's IBeginDragHandler / IDropHandler UI events.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using SpaceGame.Core;
using SpaceGame.Ship;

namespace SpaceGame.UI
{
    // ── Inventory slot (draggable) ────────────────────────────────────────────
    public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ModuleType Module;

        private GameObject    _dragProxy;
        private Canvas        _canvas;
        private RectTransform _canvasRT;

        private void Awake()
        {
            _canvas   = GetComponentInParent<Canvas>();
            _canvasRT = _canvas.GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            // Create a floating proxy image during the drag
            _dragProxy = new GameObject("DragProxy", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _dragProxy.transform.SetParent(_canvas.transform, false);
            _dragProxy.transform.SetAsLastSibling();

            var img  = _dragProxy.GetComponent<Image>();
            img.color = ModuleDefinition.Get(Module).DisplayColor;

            var cg = _dragProxy.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;   // let events pass through to drop targets

            var rt = _dragProxy.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 40f);
            UpdateProxyPosition(e);
        }

        public void OnDrag(PointerEventData e)        => UpdateProxyPosition(e);

        public void OnEndDrag(PointerEventData e)
        {
            if (_dragProxy != null) Destroy(_dragProxy);
        }

        private void UpdateProxyPosition(PointerEventData e)
        {
            if (_dragProxy == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, e.position, e.pressEventCamera, out var local);
            _dragProxy.GetComponent<RectTransform>().anchoredPosition = local;
        }
    }

    // ── Grid cell (drop target) ───────────────────────────────────────────────
    public class GridCell : MonoBehaviour, IDropHandler
    {
        public int Row;
        public int Col;
        public ShipEditorManager Editor;
        public ModuleType CurrentModule = ModuleType.Empty;

        private Image _image;

        private void Awake() => _image = GetComponent<Image>();

        public void SetModule(ModuleType type)
        {
            CurrentModule = type;
            _image.color  = ModuleDefinition.Get(type).DisplayColor;
        }

        public void OnDrop(PointerEventData e)
        {
            var slot = e.pointerDrag?.GetComponent<InventorySlot>();
            if (slot == null) return;
            Editor.PlaceModule(slot.Module, Row, Col, slot);
        }
    }

    // ── Ship Editor Manager ───────────────────────────────────────────────────
    public class ShipEditorManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Grid")]
        [SerializeField] private Transform _gridParent;      // parent for grid cell objects
        [SerializeField] private int       _maxRows = 10;
        [SerializeField] private int       _maxCols = 10;
        [SerializeField] private float     _cellSize = 48f;
        [SerializeField] private float     _cellGap  = 4f;

        [Header("Inventory")]
        [SerializeField] private Transform _inventoryParent; // scroll view content
        [SerializeField] private GameObject _inventorySlotPrefab; // prefab with InventorySlot + Image + TMP label

        [Header("Buttons")]
        [SerializeField] private Button          _confirmButton;
        [SerializeField] private Button          _backButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        // ── Private state ─────────────────────────────────────────────────────
        private ModuleType[,]     _editorGrid;   // what the player has placed
        private GridCell[,]       _cellObjects;
        private List<ModuleType>  _inventory;    // working copy of player inventory

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            _editorGrid  = new ModuleType[_maxRows, _maxCols];
            _cellObjects = new GridCell[_maxRows, _maxCols];
            _inventory   = new List<ModuleType>(GameManager.Instance.Inventory);

            BuildGridUI();
            LoadCurrentShipIntoGrid();
            RefreshInventoryUI();

            _confirmButton.onClick.AddListener(OnConfirm);
            _backButton   .onClick.AddListener(OnBack);
        }

        // ── Grid UI ───────────────────────────────────────────────────────────
        private void BuildGridUI()
        {
            float step = _cellSize + _cellGap;
            for (int r = 0; r < _maxRows; r++)
            {
                for (int c = 0; c < _maxCols; c++)
                {
                    var go   = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(GridCell));
                    go.transform.SetParent(_gridParent, false);

                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot     = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(_cellSize, _cellSize);
                    rt.anchoredPosition = new Vector2(c * step, -r * step);

                    var cell = go.GetComponent<GridCell>();
                    cell.Row    = r;
                    cell.Col    = c;
                    cell.Editor = this;
                    cell.SetModule(ModuleType.Empty);

                    // Add DropHandler component
                    go.AddComponent<GraphicRaycaster>(); // won't work on non-canvas; handled via GridCell.OnDrop

                    _cellObjects[r, c] = cell;
                    _editorGrid[r, c]  = ModuleType.Empty;
                }
            }
        }

        private void LoadCurrentShipIntoGrid()
        {
            var g = GameManager.Instance.PlayerShip.Grid;
            int rows = Mathf.Min(g.Rows, _maxRows);
            int cols = Mathf.Min(g.Cols, _maxCols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    _editorGrid[r, c] = g.Get(r, c);
                    _cellObjects[r, c].SetModule(_editorGrid[r, c]);
                }
        }

        // ── Inventory UI ──────────────────────────────────────────────────────
        private void RefreshInventoryUI()
        {
            // Clear existing slots
            for (int i = _inventoryParent.childCount - 1; i >= 0; i--)
                Destroy(_inventoryParent.GetChild(i).gameObject);

            // Count each type
            var counts = new Dictionary<ModuleType, int>();
            foreach (var m in _inventory)
            {
                if (!counts.ContainsKey(m)) counts[m] = 0;
                counts[m]++;
            }

            foreach (var kv in counts)
            {
                var go   = Instantiate(_inventorySlotPrefab, _inventoryParent);
                var slot = go.AddComponent<InventorySlot>();
                slot.Module = kv.Key;

                var img  = go.GetComponent<Image>();
                if (img) img.color = ModuleDefinition.Get(kv.Key).DisplayColor;

                var lbl  = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl) lbl.text = $"{ModuleDefinition.Get(kv.Key).DisplayName}\nx{kv.Value}";
            }
        }

        // ── Called by GridCell.OnDrop ─────────────────────────────────────────
        public void PlaceModule(ModuleType module, int row, int col, InventorySlot source)
        {
            // Check if player actually has this module in inventory
            if (!_inventory.Contains(module))
            {
                _statusText.text = "You don't have that module!";
                return;
            }

            // If a module was already in that cell, return it to inventory
            ModuleType existing = _editorGrid[row, col];
            if (existing != ModuleType.Empty)
                _inventory.Add(existing);

            // Place new module
            _inventory.Remove(module);
            _editorGrid[row, col] = module;
            _cellObjects[row, col].SetModule(module);

            RefreshInventoryUI();
            _statusText.text = $"Placed {ModuleDefinition.Get(module).DisplayName} at [{row},{col}]";
        }

        // ── Confirm / Back ────────────────────────────────────────────────────
        private void OnConfirm()
        {
            // Find bounding box of non-empty cells
            int minR = _maxRows, maxR = 0, minC = _maxCols, maxC = 0;
            bool hasAny = false;
            for (int r = 0; r < _maxRows; r++)
                for (int c = 0; c < _maxCols; c++)
                    if (_editorGrid[r, c] != ModuleType.Empty)
                    {
                        if (r < minR) minR = r; if (r > maxR) maxR = r;
                        if (c < minC) minC = c; if (c > maxC) maxC = c;
                        hasAny = true;
                    }

            if (!hasAny)
            {
                _statusText.text = "Place at least one module!";
                return;
            }

            int rows = maxR - minR + 1;
            int cols = maxC - minC + 1;
            var newGrid = new ShipGrid(rows, cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    newGrid.Set(r, c, _editorGrid[minR + r, minC + c]);

            // Sync inventory back: anything still in _inventory replaces the GameManager copy
            GameManager.Instance.Inventory.Clear();
            GameManager.Instance.Inventory.AddRange(_inventory);

            GameManager.Instance.ApplyNewPlayerGrid(newGrid);
            GameManager.Instance.SaveGame();
            GameManager.Instance.GoToMap();
        }

        private void OnBack()
        {
            GameManager.Instance.GoToMap();
        }
    }
}
