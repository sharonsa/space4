// CombatManager.cs
// Auto-battle system.  Attach to a GameObject in the Combat scene.
//
// Flow:
//   1. OnEnable: pulls PlayerShip + SelectedEnemy from GameManager.
//   2. RunCombat() simulates rounds until one ship reaches 0 HP.
//   3. Writes result + loot to GameManager, then returns to the map.

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
        // ── Inspector references ──────────────────────────────────────────────
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _combatLogText;
        [SerializeField] private TextMeshProUGUI _playerStatsText;
        [SerializeField] private TextMeshProUGUI _enemyStatsText;
        [SerializeField] private GameObject      _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private Button          _continueButton;

        [Header("Timing")]
        [SerializeField] private float _roundDelay = 0.8f;  // seconds between rounds

        // ── Private state ─────────────────────────────────────────────────────
        private ShipData _player;
        private ShipData _enemy;
        private readonly List<string> _log = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            _resultPanel.SetActive(false);
            _continueButton.onClick.AddListener(OnContinueClicked);

            _player = GameManager.Instance.PlayerShip;
            _enemy  = GameManager.Instance.SelectedEnemy;

            if (_player == null || _enemy == null)
            {
                LogLine("ERROR: Missing ship data. Returning to map.");
                return;
            }

            // Re-initialise HP (fight starts fresh)
            _player.RecalculateStats();
            _enemy.RecalculateStats();

            UpdateStatsUI();
            StartCoroutine(RunCombat());
        }

        // ── Combat loop ───────────────────────────────────────────────────────
        private IEnumerator RunCombat()
        {
            LogLine($"=== COMBAT START ===");
            LogLine($"  {_player.ShipName}  vs  {_enemy.ShipName}");
            LogLine($"  Player — HP:{_player.CurrentHP}  DMG:{_player.DamagePerRound}/rd  Eva:{_player.EvasionChance*100:F0}%");
            LogLine($"  Enemy  — HP:{_enemy.CurrentHP}   DMG:{_enemy.DamagePerRound}/rd  Eva:{_enemy.EvasionChance*100:F0}%");
            LogLine("─────────────────────");

            int round = 1;
            while (!_player.IsDestroyed && !_enemy.IsDestroyed)
            {
                LogLine($"Round {round}:");
                SimulateRound();
                UpdateStatsUI();
                yield return new WaitForSeconds(_roundDelay);
                round++;

                // Safety cap to avoid infinite loops (e.g. both ships evasion-locked)
                if (round > 200)
                {
                    LogLine("Combat timed out — Draw!");
                    break;
                }
            }

            EndCombat();
        }

        private void SimulateRound()
        {
            // Player attacks enemy
            ApplyAttack(_player, _enemy, "Player", "Enemy");
            if (_enemy.IsDestroyed) return;

            // Enemy attacks player
            ApplyAttack(_enemy, _player, "Enemy", "Player");
        }

        private void ApplyAttack(ShipData attacker, ShipData defender, string attackerName, string defenderName)
        {
            // Check evasion
            if (Random.value < defender.EvasionChance)
            {
                LogLine($"  {attackerName} fires — {defenderName} evades!");
                return;
            }

            int rawDamage = attacker.DamagePerRound;

            // Shields absorb first
            if (defender.CurrentShieldHP > 0)
            {
                int shieldAbsorb = Mathf.Min(defender.CurrentShieldHP, rawDamage);
                defender.CurrentShieldHP -= shieldAbsorb;
                rawDamage -= shieldAbsorb;
                LogLine($"  {attackerName} fires — Shield absorbs {shieldAbsorb} dmg.");
            }

            // Remaining hits hull
            if (rawDamage > 0)
            {
                defender.CurrentHP -= rawDamage;
                defender.CurrentHP  = Mathf.Max(0, defender.CurrentHP);
                LogLine($"  {attackerName} fires — {defenderName} takes {rawDamage} dmg (HP: {defender.CurrentHP})");
            }
        }

        // ── End of combat ─────────────────────────────────────────────────────
        private void EndCombat()
        {
            bool playerWon = !_player.IsDestroyed;
            GameManager.Instance.LastCombatPlayerWon = playerWon;

            if (playerWon)
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
                LogLine("=== ENEMY WINS. ===");
                GameManager.Instance.LastCombatLoot = new List<ModuleType>();
                _resultText.text = "Defeated!\n\nYour ship was destroyed.";
            }

            GameManager.Instance.SaveGame();
            _resultPanel.SetActive(true);
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void LogLine(string line)
        {
            _log.Add(line);
            if (_combatLogText != null)
                _combatLogText.text = string.Join("\n", _log);
        }

        private void UpdateStatsUI()
        {
            if (_playerStatsText != null)
                _playerStatsText.text =
                    $"PLAYER\nHP: {_player.CurrentHP}/{_player.MaxHP}\n" +
                    $"Shield: {_player.CurrentShieldHP}/{_player.MaxShieldHP}";

            if (_enemyStatsText != null)
                _enemyStatsText.text =
                    $"ENEMY\nHP: {_enemy.CurrentHP}/{_enemy.MaxHP}\n" +
                    $"Shield: {_enemy.CurrentShieldHP}/{_enemy.MaxShieldHP}";
        }

        private string FormatLoot(List<ModuleType> loot)
        {
            if (loot == null || loot.Count == 0) return "(nothing dropped)";
            var counts = new Dictionary<ModuleType, int>();
            foreach (var m in loot)
            {
                if (!counts.ContainsKey(m)) counts[m] = 0;
                counts[m]++;
            }
            var lines = new List<string>();
            foreach (var kv in counts)
                lines.Add($"  {ModuleDefinition.Get(kv.Key).DisplayName} x{kv.Value}");
            return string.Join("\n", lines);
        }

        private void OnContinueClicked()
        {
            GameManager.Instance.GoToMap();
        }
    }
}
