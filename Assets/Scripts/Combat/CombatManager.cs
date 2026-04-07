// CombatManager.cs
// Auto-battle system. Auto-finds all UI elements by name at Start()
// so no inspector wiring is required.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceGame.Core;
using SpaceGame.Ship;

namespace SpaceGame.Combat
{
    public class CombatManager : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _roundDelay = 0.8f;

        // ── Auto-found references ─────────────────────────────────────────────
        private TextMeshProUGUI _combatLogText;
        private TextMeshProUGUI _playerStatsText;
        private TextMeshProUGUI _enemyStatsText;
        private CanvasGroup     _resultPanelCG;
        private TextMeshProUGUI _resultText;
        private Button          _continueButton;

        // ── State ─────────────────────────────────────────────────────────────
        private ShipData          _player;
        private ShipData          _enemy;
        private readonly List<string> _log = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            if (GameManager.Instance == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }

            FindReferences();

            HidePanel(_resultPanelCG);
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);

            _player = GameManager.Instance.PlayerShip;
            _enemy  = GameManager.Instance.SelectedEnemy;

            if (_player == null || _enemy == null)
            {
                LogLine("ERROR: Missing ship data. Returning to map in 3s...");
                Invoke(nameof(ReturnToMap), 3f);
                return;
            }

            _player.RecalculateStats();
            _enemy .RecalculateStats();

            UpdateStatsUI();
            StartCoroutine(RunCombat());
        }

        // ── Auto-find ─────────────────────────────────────────────────────────
        private void FindReferences()
        {
            _combatLogText   = FindTMP("CombatLogText");
            _playerStatsText = FindTMP("PlayerStatsText");
            _enemyStatsText  = FindTMP("EnemyStatsText");

            var resultPanelGO = GameObject.Find("ResultPanel");
            if (resultPanelGO != null)
            {
                // Ensure it has a CanvasGroup for alpha-based show/hide
                _resultPanelCG = resultPanelGO.GetComponent<CanvasGroup>();
                if (_resultPanelCG == null) _resultPanelCG = resultPanelGO.AddComponent<CanvasGroup>();
                _resultText     = FindInGO<TextMeshProUGUI>(resultPanelGO, "ResultText");
                _continueButton = FindInGO<Button>(resultPanelGO, "ContinueButton");
            }
        }

        // ── Combat loop ───────────────────────────────────────────────────────
        private IEnumerator RunCombat()
        {
            LogLine("=== COMBAT START ===");
            LogLine($"  {_player.ShipName}  vs  {_enemy.ShipName}");
            LogLine($"  Player — HP:{_player.CurrentHP}  DMG:{_player.DamagePerRound}/rd  Eva:{_player.EvasionChance*100:F0}%");
            LogLine($"  Enemy  — HP:{_enemy.CurrentHP}  DMG:{_enemy.DamagePerRound}/rd  Eva:{_enemy.EvasionChance*100:F0}%");
            LogLine("─────────────────────");

            int round = 1;
            while (!_player.IsDestroyed && !_enemy.IsDestroyed)
            {
                LogLine($"Round {round}:");
                SimulateRound();
                UpdateStatsUI();
                yield return new WaitForSeconds(_roundDelay);
                round++;
                if (round > 200) { LogLine("Combat timed out — Draw!"); break; }
            }

            EndCombat();
        }

        private void SimulateRound()
        {
            ApplyAttack(_player, _enemy, "Player", "Enemy");
            if (_enemy.IsDestroyed) return;
            ApplyAttack(_enemy, _player, "Enemy", "Player");
        }

        private void ApplyAttack(ShipData attacker, ShipData defender, string aName, string dName)
        {
            if (Random.value < defender.EvasionChance)
            {
                LogLine($"  {aName} fires — {dName} evades!");
                return;
            }

            int dmg = attacker.DamagePerRound;

            if (defender.CurrentShieldHP > 0)
            {
                int absorb = Mathf.Min(defender.CurrentShieldHP, dmg);
                defender.CurrentShieldHP -= absorb;
                dmg -= absorb;
                LogLine($"  {aName} fires — Shield absorbs {absorb}.");
            }

            if (dmg > 0)
            {
                defender.CurrentHP = Mathf.Max(0, defender.CurrentHP - dmg);
                LogLine($"  {aName} fires — {dName} takes {dmg} dmg (HP:{defender.CurrentHP})");
            }
        }

        private void EndCombat()
        {
            bool won = !_player.IsDestroyed;
            GameManager.Instance.LastCombatPlayerWon = won;

            if (won)
            {
                LogLine("=== PLAYER WINS! ===");
                var loot = _enemy.RollLoot(0.5f);
                GameManager.Instance.LastCombatLoot = loot;
                GameManager.Instance.AddToInventory(loot);
                GameManager.Instance.RemoveDefeatedEnemy();
                _resultText.text = $"Victory!\n\nLoot gained:\n{FormatLoot(loot)}";
            }
            else
            {
                LogLine("=== DEFEATED. ===");
                GameManager.Instance.LastCombatLoot = new List<ModuleType>();
                _resultText.text = "Defeated!\nYour ship was destroyed.";
            }

            GameManager.Instance.SaveGame();
            ShowPanel(_resultPanelCG);
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void LogLine(string line)
        {
            _log.Add(line);
            // Keep last 30 lines to avoid overflow
            if (_log.Count > 30) _log.RemoveAt(0);
            if (_combatLogText != null)
                _combatLogText.text = string.Join("\n", _log);
        }

        private void UpdateStatsUI()
        {
            if (_playerStatsText != null)
                _playerStatsText.text =
                    $"PLAYER\nHP: {_player.CurrentHP}/{_player.MaxHP}\nShield: {_player.CurrentShieldHP}/{_player.MaxShieldHP}";
            if (_enemyStatsText != null)
                _enemyStatsText.text =
                    $"ENEMY\nHP: {_enemy.CurrentHP}/{_enemy.MaxHP}\nShield: {_enemy.CurrentShieldHP}/{_enemy.MaxShieldHP}";
        }

        private string FormatLoot(List<ModuleType> loot)
        {
            if (loot == null || loot.Count == 0) return "(nothing dropped)";
            var counts = new Dictionary<ModuleType, int>();
            foreach (var m in loot) { if (!counts.ContainsKey(m)) counts[m] = 0; counts[m]++; }
            var lines = new List<string>();
            foreach (var kv in counts)
                lines.Add($"  {ModuleDefinition.Get(kv.Key).DisplayName} x{kv.Value}");
            return string.Join("\n", lines);
        }

        private void OnContinueClicked() => GameManager.Instance.GoToMap();
        private void ReturnToMap()       => GameManager.Instance.GoToMap();

        private static void ShowPanel(CanvasGroup cg) { if (cg == null) return; cg.alpha = 1f; cg.blocksRaycasts = true;  cg.interactable = true;  }
        private static void HidePanel(CanvasGroup cg) { if (cg == null) return; cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }

        // ── Search utilities ──────────────────────────────────────────────────
        private static TextMeshProUGUI FindTMP(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
        }

        private static T FindInGO<T>(GameObject parent, string childName) where T : Component
        {
            if (parent == null) return null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t.GetComponent<T>();
            return null;
        }
    }
}
