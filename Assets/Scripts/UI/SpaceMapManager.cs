// SpaceMapManager.cs
// Manages the Space Map scene. Auto-finds all UI elements by name at Start()
// so no inspector wiring is required.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceGame.Core;
using SpaceGame.Ship;

namespace SpaceGame.UI
{
    public class SpaceMapManager : MonoBehaviour
    {
        [Header("Enemy Generation")]
        [SerializeField] private TextAsset[] _enemyCSVFiles;
        [SerializeField] private int         _enemyCount = 6;

        // ── Auto-found references ─────────────────────────────────────────────
        private RectTransform    _mapArea;
        private CanvasGroup      _preFightPanelCG;
        private TextMeshProUGUI  _preFightTitle;
        private TextMeshProUGUI  _preFightStats;
        private TextMeshProUGUI  _preFightLootHint;
        private ShipVisualizer   _preFightVisualizer;
        private Button           _fightButton;
        private Button           _cancelButton;
        private TextMeshProUGUI  _playerStatsLabel;
        private Button           _shipEditorButton;

        private ShipData _selectedEnemy;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            // Ensure GameManager exists even if Boot scene was skipped
            if (GameManager.Instance == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }

            // Find references BEFORE hiding any panels (inactive objects can't be found by GameObject.Find)
            FindReferences();

            // Hide panel after references are cached
            HidePanel(_preFightPanelCG);

            if (_fightButton  != null) _fightButton .onClick.AddListener(OnFightClicked);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(OnCancelClicked);
            _shipEditorButton?.onClick.AddListener(OnShipEditorClicked);

            if (GameManager.Instance.EnemyShips.Count == 0)
                GenerateEnemies();

            // Delay one frame so Unity layout system has computed RectTransform sizes
            StartCoroutine(SpawnAfterLayout());
            RefreshPlayerHUD();
        }

        private IEnumerator SpawnAfterLayout()
        {
            yield return null; // wait one frame
            DisableBackgroundRaycasts();
            SpawnEnemyButtons();
        }

        /// <summary>
        /// Non-interactive background/map panels must not block raycasts.
        /// Only Buttons and explicit interactive elements should have raycastTarget = true.
        /// </summary>
        private static void DisableBackgroundRaycasts()
        {
            foreach (var name in new[] { "Background", "MapArea", "PlayerHUD" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                // Disable on the panel itself and any non-Button Image children
                foreach (var img in go.GetComponentsInChildren<Image>(true))
                {
                    // Keep raycast only on actual Button images
                    if (img.GetComponent<Button>() == null)
                        img.raycastTarget = false;
                }
                // Also disable TMP labels (they don't need raycasts)
                foreach (var tmp in go.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                    tmp.raycastTarget = false;
            }
        }

        // ── Auto-find ─────────────────────────────────────────────────────────
        private void FindReferences()
        {
            _mapArea          = FindInScene<RectTransform>("MapArea");
            _playerStatsLabel = FindInScene<TextMeshProUGUI>("PlayerStatsLabel");
            _shipEditorButton = FindInScene<Button>("ShipEditorButton");

            var preFightPanelGO = FindInactiveGO("PreFightPanel");

            if (preFightPanelGO != null)
            {
                _preFightPanelCG = preFightPanelGO.GetComponent<CanvasGroup>();
                if (_preFightPanelCG == null) _preFightPanelCG = preFightPanelGO.AddComponent<CanvasGroup>();
                // Immediately block raycasts so it doesn't intercept clicks on the map
                _preFightPanelCG.alpha          = 0f;
                _preFightPanelCG.blocksRaycasts = false;
                _preFightPanelCG.interactable   = false;
                _preFightTitle      = FindInGO<TextMeshProUGUI>(preFightPanelGO, "PreFightTitle");
                _preFightStats      = FindInGO<TextMeshProUGUI>(preFightPanelGO, "PreFightStats");
                _preFightLootHint   = FindInGO<TextMeshProUGUI>(preFightPanelGO, "PreFightLootHint");
                _preFightVisualizer = FindInGO<ShipVisualizer>(preFightPanelGO,  "PreFightVisualizer");
                _fightButton        = FindInGO<Button>(preFightPanelGO,          "FightButton");
                _cancelButton       = FindInGO<Button>(preFightPanelGO,          "CancelButton");
            }
        }

        /// <summary>Finds a GameObject by name even if it is inactive.</summary>
        private static GameObject FindInactiveGO(string goName)
        {
            // Search all root objects and their full hierarchies including inactive
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindInactiveRecursive(root.transform, goName);
                if (found != null) return found.gameObject;
            }
            Debug.LogWarning($"[SpaceMap] Could not find inactive GameObject '{goName}'");
            return null;
        }

        private static Transform FindInactiveRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindInactiveRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        // ── Enemy generation ──────────────────────────────────────────────────
        private void GenerateEnemies()
        {
            var enemies = new List<ShipData>();
            for (int i = 0; i < _enemyCount; i++)
            {
                ShipData ship;
                if (_enemyCSVFiles != null && _enemyCSVFiles.Length > 0)
                {
                    var csv    = _enemyCSVFiles[Random.Range(0, _enemyCSVFiles.Length)];
                    int threat = Random.Range(1, 5);
                    ship = ShipData.FromTextAsset(csv, $"Enemy {i + 1}", threat);
                }
                else
                {
                    ship = GenerateProceduralEnemy(i);
                }
                enemies.Add(ship);
            }
            GameManager.Instance.SetEnemyShips(enemies);
        }

        private ShipData GenerateProceduralEnemy(int index)
        {
            int threat  = Random.Range(1, 4);
            int minSize = 2 + threat;
            int rows    = Random.Range(minSize, minSize + 3);
            int cols    = Random.Range(minSize, minSize + 3);

            var grid = new ShipGrid(rows, cols);

            ModuleType[] weapons  = { ModuleType.Gun, ModuleType.Cannon, ModuleType.LaserArray, ModuleType.MissileBay };
            ModuleType[] support  = { ModuleType.Engine, ModuleType.Hull, ModuleType.Shield, ModuleType.Reactor, ModuleType.Thruster };

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float roll = Random.value;
                    if      (roll < 0.12f) grid.Set(r, c, ModuleType.Empty);
                    else if (roll < 0.40f) grid.Set(r, c, weapons[Random.Range(0, weapons.Length)]);
                    else                   grid.Set(r, c, support[Random.Range(0, support.Length)]);
                }

            return new ShipData($"Enemy {index + 1}", grid, threat);
        }

        // ── Spawn buttons ─────────────────────────────────────────────────────
        private void SpawnEnemyButtons()
        {
            if (_mapArea == null) { Debug.LogError("[SpaceMap] MapArea not found!"); return; }

            // Clear old buttons
            for (int i = _mapArea.childCount - 1; i >= 0; i--)
                Destroy(_mapArea.GetChild(i).gameObject);

            // Use actual rect size, fall back to 800x600 if layout hasn't run
            float mapW = _mapArea.rect.width  > 10f ? _mapArea.rect.width  : 800f;
            float mapH = _mapArea.rect.height > 10f ? _mapArea.rect.height : 600f;

            foreach (var enemy in GameManager.Instance.EnemyShips)
            {
                // Build a button entirely in code — no prefab needed
                var go  = new GameObject(enemy.ShipName, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_mapArea, false);

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 65f);
                float hw = mapW / 2f - 55f;
                float hh = mapH / 2f - 40f;
                rt.anchoredPosition = new Vector2(
                    Random.Range(-hw, hw),
                    Random.Range(-hh, hh));

                var img = go.GetComponent<Image>();
                img.color = ThreatColor(enemy.ThreatLevel);

                // Label
                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(go.transform, false);
                var labelRT = labelGO.GetComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = new Vector2(4, 4);
                labelRT.offsetMax = new Vector2(-4, -4);
                var tmp = labelGO.GetComponent<TextMeshProUGUI>();
                tmp.text      = $"{enemy.ShipName}\n[{enemy.Grid.Rows}x{enemy.Grid.Cols}]  T{enemy.ThreatLevel}";
                tmp.fontSize  = 13;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;

                ShipData captured = enemy;
                go.GetComponent<Button>().onClick.AddListener(() => OnEnemyClicked(captured));
            }
        }

        // ── Pre-fight ─────────────────────────────────────────────────────────
        private void OnEnemyClicked(ShipData enemy)
        {
            _selectedEnemy = enemy;
            if (_preFightTitle    != null) _preFightTitle   .text = $"Intercept: {enemy.ShipName}";
            if (_preFightStats    != null) _preFightStats   .text = enemy.StatsSummary();
            if (_preFightLootHint != null) _preFightLootHint.text = "Potential loot: random modules (~50% drop chance each)";
            if (_preFightVisualizer != null) _preFightVisualizer.Render(enemy);
            ShowPanel(_preFightPanelCG);
        }

        private void OnFightClicked()
        {
            Debug.Log($"[SpaceMap] Fight clicked. Selected enemy: {(_selectedEnemy?.ShipName ?? "null")}");
            if (_selectedEnemy == null) return;
            GameManager.Instance.SelectEnemy(_selectedEnemy);
            GameManager.Instance.GoToFight();
        }

        private void OnCancelClicked()
        {
            _selectedEnemy = null;
            HidePanel(_preFightPanelCG);
        }

        private void OnShipEditorClicked() => GameManager.Instance.GoToEditor();

        private static void ShowPanel(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.alpha          = 1f;
            cg.blocksRaycasts = true;
            cg.interactable   = true;
            // Re-enable raycast on the background image
            var img = cg.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
        }

        private static void HidePanel(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;
            // Disable raycast on the background image so clicks pass through
            var img = cg.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            // Also disable on all children
            foreach (var childImg in cg.GetComponentsInChildren<Image>(true))
                childImg.raycastTarget = false;
        }

        // ── HUD ───────────────────────────────────────────────────────────────
        private void RefreshPlayerHUD()
        {
            if (_playerStatsLabel == null) return;
            var p = GameManager.Instance.PlayerShip;
            _playerStatsLabel.text =
                $"YOUR SHIP\n{p.StatsSummary()}\nInventory: {GameManager.Instance.Inventory.Count} modules";
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Color ThreatColor(int threat) => threat switch
        {
            1 => new Color(0.3f, 0.8f, 0.3f),
            2 => new Color(0.8f, 0.8f, 0.2f),
            3 => new Color(0.9f, 0.5f, 0.1f),
            4 => new Color(0.9f, 0.1f, 0.1f),
            _ => Color.white
        };

        // ── Scene search utilities ────────────────────────────────────────────
        private static T FindInScene<T>(string goName) where T : Component
        {
            var go = GameObject.Find(goName);
            if (go == null) { Debug.LogWarning($"[SpaceMap] Could not find GameObject '{goName}'"); return null; }
            return go.GetComponent<T>();
        }

        private static GameObject FindGOInScene(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null) Debug.LogWarning($"[SpaceMap] Could not find GameObject '{goName}'");
            return go;
        }

        private static T FindInGO<T>(GameObject parent, string childName) where T : Component
        {
            if (parent == null) return null;
            var t = parent.transform.Find(childName);
            if (t == null)
            {
                // Deep search
                var all = parent.GetComponentsInChildren<Transform>(true);
                foreach (var child in all)
                    if (child.name == childName) { t = child; break; }
            }
            if (t == null) { Debug.LogWarning($"[SpaceMap] Could not find child '{childName}' in '{parent.name}'"); return null; }
            return t.GetComponent<T>();
        }
    }
}
