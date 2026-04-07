// GameManager.cs
// Singleton that persists across scenes.
// Owns: player ship data, player inventory, the list of enemy ships on the map,
// and the currently-selected enemy (for the pre-fight screen and combat).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpaceGame.Ship;
using SpaceGame.Core;

namespace SpaceGame.Core
{
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Scene name constants ──────────────────────────────────────────────
        public const string SCENE_MAP    = "SpaceMap";
        public const string SCENE_FIGHT  = "Combat";
        public const string SCENE_EDITOR = "ShipEditor";

        // ── Player state ──────────────────────────────────────────────────────
        public ShipData PlayerShip { get; private set; }

        /// <summary>Modules the player has collected but not yet placed on their ship.</summary>
        public List<ModuleType> Inventory { get; private set; } = new();

        // ── Map state ─────────────────────────────────────────────────────────
        /// <summary>All enemy ships currently on the space map.</summary>
        public List<ShipData> EnemyShips { get; private set; } = new();

        /// <summary>The enemy the player clicked on — set before loading the fight scene.</summary>
        public ShipData SelectedEnemy { get; private set; }

        // ── Combat result (written by CombatManager, read by loot screen) ─────
        public bool   LastCombatPlayerWon { get; set; }
        public List<ModuleType> LastCombatLoot { get; set; } = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitNewGame();
        }

        // ── Initialisation ────────────────────────────────────────────────────
        private void InitNewGame()
        {
            PlayerShip = ShipData.CreateStarterPlayerShip();
            Inventory.Clear();
            EnemyShips.Clear();
        }

        // ── Enemy management ──────────────────────────────────────────────────
        public void SetEnemyShips(List<ShipData> enemies)
        {
            EnemyShips = enemies;
        }

        public void SelectEnemy(ShipData enemy)
        {
            SelectedEnemy = enemy;
        }

        public void RemoveDefeatedEnemy()
        {
            if (SelectedEnemy != null)
                EnemyShips.Remove(SelectedEnemy);
            SelectedEnemy = null;
        }

        // ── Inventory ─────────────────────────────────────────────────────────
        public void AddToInventory(IEnumerable<ModuleType> modules)
        {
            Inventory.AddRange(modules);
        }

        public bool RemoveFromInventory(ModuleType module)
        {
            return Inventory.Remove(module);
        }

        /// <summary>
        /// Replace the player's ship grid with a new one built in the ShipEditor.
        /// Removes the placed modules from inventory.
        /// </summary>
        public void ApplyNewPlayerGrid(ShipGrid newGrid)
        {
            PlayerShip = new ShipData("Player Ship", newGrid, 0);
        }

        // ── Scene transitions ─────────────────────────────────────────────────
        public void GoToMap()    => SceneManager.LoadScene(SCENE_MAP);
        public void GoToFight()  => SceneManager.LoadScene(SCENE_FIGHT);
        public void GoToEditor() => SceneManager.LoadScene(SCENE_EDITOR);

        // ── Save / Load (simple JSON to persistent storage) ───────────────────
        private const string SAVE_KEY = "SpaceGame_Save";

        [System.Serializable]
        private class SaveData
        {
            public string      PlayerGridCSV;
            public string[]    InventoryModules;
        }

        public void SaveGame()
        {
            var save = new SaveData
            {
                PlayerGridCSV    = PlayerShip.Grid.ToCSV(),
                InventoryModules = new string[Inventory.Count]
            };
            for (int i = 0; i < Inventory.Count; i++)
                save.InventoryModules[i] = Inventory[i].ToCode();

            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
            Debug.Log("[GameManager] Game saved.");
        }

        public bool LoadGame()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;

            var save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));
            var grid = ShipGrid.FromCSV(save.PlayerGridCSV);
            PlayerShip = new ShipData("Player Ship", grid, 0);

            Inventory.Clear();
            foreach (var code in save.InventoryModules)
                Inventory.Add(ModuleTypeExtensions.FromCode(code));

            Debug.Log("[GameManager] Game loaded.");
            return true;
        }
    }
}
