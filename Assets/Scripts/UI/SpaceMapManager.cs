// SpaceMapManager.cs
// Manages the Space Map scene:
//   - Generates (or re-uses) the list of enemy ships from GameManager.
//   - Spawns a clickable button for each enemy ship on the map.
//   - When an enemy is clicked, opens the Pre-Fight panel.
//
// Attach to a GameObject in the SpaceMap scene.
// Requires: GameManager (auto-persistent), and UI panels assigned in inspector.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceGame.Core;
using SpaceGame.Ship;
using SpaceGame.UI;

namespace SpaceGame.UI
{
    public class SpaceMapManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Map Area")]
        [SerializeField] private RectTransform _mapArea;          // The starfield panel ships are placed on

        [Header("Enemy Ship Prefab")]
        [Tooltip("A Button prefab that represents an enemy on the map. " +
                 "Must have a TextMeshProUGUI child named 'Label' and an Image.")]
        [SerializeField] private GameObject _enemyButtonPrefab;

        [Header("Pre-Fight Panel")]
        [SerializeField] private GameObject      _preFightPanel;
        [SerializeField] private TextMeshProUGUI _preFightTitle;
        [SerializeField] private TextMeshProUGUI _preFightStats;
        [SerializeField] private TextMeshProUGUI _preFightLootHint;
        [SerializeField] private ShipVisualizer  _preFightVisualizer;
        [SerializeField] private Button          _fightButton;
        [SerializeField] private Button          _cancelButton;

        [Header("Player HUD")]
        [SerializeField] private TextMeshProUGUI _playerStatsLabel;
        [SerializeField] private Button          _shipEditorButton;

        [Header("Enemy Generation")]
        [SerializeField] private TextAsset[]     _enemyCSVFiles;   // Assign in inspector
        [SerializeField] private int             _enemyCount = 6;

        // ── Private state ─────────────────────────────────────────────────────
        private ShipData _selectedEnemy;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            _preFightPanel.SetActive(false);

            _fightButton .onClick.AddListener(OnFightClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
            _shipEditorButton?.onClick.AddListener(OnShipEditorClicked);

            // Generate enemies only on a fresh map (list is empty)
            if (GameManager.Instance.EnemyShips.Count == 0)
                GenerateEnemies();

            SpawnEnemyButtons();
            RefreshPlayerHUD();
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
                    // Pick a random CSV template
                    var csv = _enemyCSVFiles[Random.Range(0, _enemyCSVFiles.Length)];
                    int threat = Random.Range(1, 5);
                    ship = ShipData.FromTextAsset(csv, $"Enemy {i + 1}", threat);
                }
                else
                {
                    // Procedurally generate a random ship if no CSV files are assigned
                    ship = GenerateProceduralEnemy(i);
                }
                enemies.Add(ship);
            }

            GameManager.Instance.SetEnemyShips(enemies);
        }

        private ShipData GenerateProceduralEnemy(int index)
        {
            int threat  = Random.Range(1, 5);
            int minSize = 2 + threat;
            int maxSize = minSize + 3;
            int rows    = Random.Range(minSize, maxSize + 1);
            int cols    = Random.Range(minSize, maxSize + 1);

            var grid = new ShipGrid(rows, cols);

            ModuleType[] weaponTypes  = { ModuleType.Gun, ModuleType.Cannon, ModuleType.LaserArray, ModuleType.MissileBay };
            ModuleType[] supportTypes = { ModuleType.Engine, ModuleType.Hull, ModuleType.Shield, ModuleType.Reactor, ModuleType.Thruster };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float roll = Random.value;
                    if (roll < 0.15f)
                        grid.Set(r, c, ModuleType.Empty);
                    else if (roll < 0.40f)
                        grid.Set(r, c, weaponTypes[Random.Range(0, weaponTypes.Length)]);
                    else
                        grid.Set(r, c, supportTypes[Random.Range(0, supportTypes.Length)]);
                }
            }

            return new ShipData($"Enemy {index + 1}", grid, threat);
        }

        // ── Spawn buttons ─────────────────────────────────────────────────────
        private void SpawnEnemyButtons()
        {
            // Clear any old buttons
            for (int i = _mapArea.childCount - 1; i >= 0; i--)
                Destroy(_mapArea.GetChild(i).gameObject);

            foreach (var enemy in GameManager.Instance.EnemyShips)
            {
                var go  = Instantiate(_enemyButtonPrefab, _mapArea);
                var btn = go.GetComponent<Button>();

                // Random position on the map
                var rt = go.GetComponent<RectTransform>();
                float x = Random.Range(-_mapArea.rect.width  / 2f + 60f, _mapArea.rect.width  / 2f - 60f);
                float y = Random.Range(-_mapArea.rect.height / 2f + 60f, _mapArea.rect.height / 2f - 60f);
                rt.anchoredPosition = new Vector2(x, y);

                // Label
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{enemy.ShipName}\n[{enemy.Grid.Rows}x{enemy.Grid.Cols}]  T{enemy.ThreatLevel}";

                // Threat colour tint
                var img = go.GetComponent<Image>();
                if (img != null)
                    img.color = ThreatColor(enemy.ThreatLevel);

                // Capture for lambda
                ShipData captured = enemy;
                btn.onClick.AddListener(() => OnEnemyClicked(captured));
            }
        }

        // ── Enemy clicked ─────────────────────────────────────────────────────
        private void OnEnemyClicked(ShipData enemy)
        {
            _selectedEnemy = enemy;

            _preFightTitle.text    = $"Intercept: {enemy.ShipName}";
            _preFightStats.text    = enemy.StatsSummary();
            _preFightLootHint.text = "Potential loot: random modules from enemy ship\n(~50% drop chance per module)";
            _preFightVisualizer.Render(enemy);

            _preFightPanel.SetActive(true);
        }

        private void OnFightClicked()
        {
            if (_selectedEnemy == null) return;
            GameManager.Instance.SelectEnemy(_selectedEnemy);
            GameManager.Instance.GoToFight();
        }

        private void OnCancelClicked()
        {
            _selectedEnemy = null;
            _preFightPanel.SetActive(false);
        }

        private void OnShipEditorClicked()
        {
            GameManager.Instance.GoToEditor();
        }

        // ── HUD ───────────────────────────────────────────────────────────────
        private void RefreshPlayerHUD()
        {
            if (_playerStatsLabel == null) return;
            var p = GameManager.Instance.PlayerShip;
            _playerStatsLabel.text =
                $"YOUR SHIP\n{p.StatsSummary()}\nInventory modules: {GameManager.Instance.Inventory.Count}";
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Color ThreatColor(int threat)
        {
            return threat switch
            {
                1 => new Color(0.3f, 0.8f, 0.3f),  // green  — easy
                2 => new Color(0.8f, 0.8f, 0.2f),  // yellow — moderate
                3 => new Color(0.9f, 0.5f, 0.1f),  // orange — hard
                4 => new Color(0.9f, 0.1f, 0.1f),  // red    — very hard
                _ => Color.white
            };
        }
    }
}
