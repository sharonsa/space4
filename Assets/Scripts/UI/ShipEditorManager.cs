// ShipEditorManager.cs
// Ship editor scene. Auto-finds all UI elements by name at Start()
// so no inspector wiring is required.

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
            _canvasRT = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_canvas == null) return;
            _dragProxy = new GameObject("DragProxy", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _dragProxy.transform.SetParent(_canvas.transform, false);
            _dragProxy.transform.SetAsLastSibling();

            _dragProxy.GetComponent<Image>().color = ModuleDefinition.Get(Module).DisplayColor;
            _dragProxy.GetComponent<CanvasGroup>().blocksRaycasts = false;
            _dragProxy.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);

            UpdateProxyPosition(e);
        }

        public void OnDrag(PointerEventData e) => UpdateProxyPosition(e);

        public void OnEndDrag(PointerEventData e)
        {
            if (_dragProxy != null) { Destroy(_dragProxy); _dragProxy = null; }
        }

        private void UpdateProxyPosition(PointerEventData e)
        {
            if (_dragProxy == null || _canvasRT == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, e.position, e.pressEventCamera, out var local);
            _dragProxy.GetComponent<RectTransform>().anchoredPosition = local;
        }
    }

    // ── Grid cell (drop target) ───────────────────────────────────────────────
    public class GridCell : MonoBehaviour, IDropHandler, IPointerClickHandler
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
            if (_image) _image.color = ModuleDefinition.Get(type).DisplayColor;
        }

        public void OnDrop(PointerEventData e)
        {
            var slot = e.pointerDrag?.GetComponent<InventorySlot>();
            if (slot != null) Editor.PlaceModule(slot.Module, Row, Col);
        }

        // Right-click to remove module back to inventory
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right && CurrentModule != ModuleType.Empty)
                Editor.RemoveModule(Row, Col);
        }
    }

    // ── Ship Editor Manager ───────────────────────────────────────────────────
    public class ShipEditorManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int   _maxRows = 10;
        [SerializeField] private int   _maxCols = 10;
        [SerializeField] private float _cellSize = 44f;
        [SerializeField] private float _cellGap  = 3f;

        // ── Auto-found references ─────────────────────────────────────────────
        private Transform        _gridParent;
        private Transform        _inventoryParent;
        private Button           _confirmButton;
        private Button           _backButton;
        private TextMeshProUGUI  _statusText;

        // ── State ─────────────────────────────────────────────────────────────
        private ModuleType[,]    _editorGrid;
        private GridCell[,]      _cellObjects;
        private List<ModuleType> _inventory;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            FindReferences();

            _editorGrid  = new ModuleType[_maxRows, _maxCols];
            _cellObjects = new GridCell[_maxRows, _maxCols];
            _inventory   = new List<ModuleType>(GameManager.Instance.Inventory);

            BuildGridUI();
            LoadCurrentShipIntoGrid();
            RefreshInventoryUI();

            _confirmButton.onClick.AddListener(OnConfirm);
            _backButton   .onClick.AddListener(OnBack);
        }

        // ── Auto-find ─────────────────────────────────────────────────────────
        private void FindReferences()
        {
            _gridParent      = FindGO("GridParent")?.transform;
            _inventoryParent = FindGO("InventoryScrollContent")?.transform;
            _confirmButton   = FindGO("ConfirmButton")?.GetComponent<Button>();
            _backButton      = FindGO("BackButton")?.GetComponent<Button>();
            _statusText      = FindGO("StatusText")?.GetComponent<TextMeshProUGUI>();
        }

        // ── Grid UI ───────────────────────────────────────────────────────────
        private void BuildGridUI()
        {
            if (_gridParent == null) return;
            float step = _cellSize + _cellGap;

            for (int r = 0; r < _maxRows; r++)
            {
                for (int c = 0; c < _maxCols; c++)
                {
                    var go = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(GridCell), typeof(CanvasGroup));
                    go.transform.SetParent(_gridParent, false);

                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin        = new Vector2(0f, 1f);
                    rt.anchorMax        = new Vector2(0f, 1f);
                    rt.pivot            = new Vector2(0f, 1f);
                    rt.sizeDelta        = new Vector2(_cellSize, _cellSize);
                    rt.anchoredPosition = new Vector2(c * step, -r * step);

                    var cell = go.GetComponent<GridCell>();
                    cell.Row    = r;
                    cell.Col    = c;
                    cell.Editor = this;
                    cell.SetModule(ModuleType.Empty);

                    _cellObjects[r, c] = cell;
                    _editorGrid[r, c]  = ModuleType.Empty;
                }
            }
        }

        private void LoadCurrentShipIntoGrid()
        {
            var g    = GameManager.Instance.PlayerShip.Grid;
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
            if (_inventoryParent == null) return;

            for (int i = _inventoryParent.childCount - 1; i >= 0; i--)
                Destroy(_inventoryParent.GetChild(i).gameObject);

            var counts = new Dictionary<ModuleType, int>();
            foreach (var m in _inventory)
            {
                if (!counts.ContainsKey(m)) counts[m] = 0;
                counts[m]++;
            }

            foreach (var kv in counts)
            {
                var go   = new GameObject(kv.Key.ToString(), typeof(RectTransform), typeof(Image), typeof(InventorySlot), typeof(CanvasGroup));
                go.transform.SetParent(_inventoryParent, false);

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(120f, 50f);

                go.GetComponent<Image>().color = ModuleDefinition.Get(kv.Key).DisplayColor;

                var slot = go.GetComponent<InventorySlot>();
                slot.Module = kv.Key;

                // Label
                var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lblGO.transform.SetParent(go.transform, false);
                var lblRT = lblGO.GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = new Vector2(4, 2);
                lblRT.offsetMax = new Vector2(-4, -2);
                var tmp = lblGO.GetComponent<TextMeshProUGUI>();
                tmp.text      = $"{ModuleDefinition.Get(kv.Key).DisplayName}\nx{kv.Value}";
                tmp.fontSize  = 12;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        // ── Called by GridCell ────────────────────────────────────────────────
        public void PlaceModule(ModuleType module, int row, int col)
        {
            if (!_inventory.Contains(module))
            {
                SetStatus("You don't have that module!");
                return;
            }

            // Return existing module to inventory
            ModuleType existing = _editorGrid[row, col];
            if (existing != ModuleType.Empty) _inventory.Add(existing);

            _inventory.Remove(module);
            _editorGrid[row, col] = module;
            _cellObjects[row, col].SetModule(module);

            RefreshInventoryUI();
            SetStatus($"Placed {ModuleDefinition.Get(module).DisplayName} at [{row},{col}]");
        }

        public void RemoveModule(int row, int col)
        {
            ModuleType existing = _editorGrid[row, col];
            if (existing == ModuleType.Empty) return;
            _inventory.Add(existing);
            _editorGrid[row, col] = ModuleType.Empty;
            _cellObjects[row, col].SetModule(ModuleType.Empty);
            RefreshInventoryUI();
            SetStatus($"Removed {ModuleDefinition.Get(existing).DisplayName}");
        }

        // ── Confirm / Back ────────────────────────────────────────────────────
        private void OnConfirm()
        {
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

            if (!hasAny) { SetStatus("Place at least one module!"); return; }

            int rows = maxR - minR + 1;
            int cols = maxC - minC + 1;
            var newGrid = new ShipGrid(rows, cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    newGrid.Set(r, c, _editorGrid[minR + r, minC + c]);

            GameManager.Instance.Inventory.Clear();
            GameManager.Instance.Inventory.AddRange(_inventory);
            GameManager.Instance.ApplyNewPlayerGrid(newGrid);
            GameManager.Instance.SaveGame();
            GameManager.Instance.GoToMap();
        }

        private void OnBack() => GameManager.Instance.GoToMap();

        private void SetStatus(string msg) { if (_statusText) _statusText.text = msg; }

        // ── Utilities ─────────────────────────────────────────────────────────
        private static GameObject FindGO(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) Debug.LogWarning($"[ShipEditor] Could not find '{name}'");
            return go;
        }
    }
}
