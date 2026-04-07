// SceneCreator.cs  (Editor-only)
// Unity menu tool: SpaceGame > Create All Scenes
// Creates the three required scenes with basic UI scaffolding and
// adds them all to the Build Settings.
//
// Run this ONCE after importing the project.
// After running, open each scene and wire up the serialized references
// shown in each manager's inspector.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace SpaceGame.Editor
{
    public static class SceneCreator
    {
        private const string SCENES_PATH = "Assets/Scenes";

        [MenuItem("SpaceGame/Create All Scenes")]
        public static void CreateAllScenes()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(SCENES_PATH);

            CreateBootScene();
            CreateSpaceMapScene();
            CreateCombatScene();
            CreateShipEditorScene();

            AddScenesToBuildSettings();

            AssetDatabase.Refresh();
            Debug.Log("[SceneCreator] All scenes created and added to Build Settings.");
            EditorUtility.DisplayDialog("SpaceGame", "All scenes created!\n\nNext steps:\n" +
                "1. Open SpaceMap scene and assign references in SpaceMapManager.\n" +
                "2. Open Combat scene and assign references in CombatManager.\n" +
                "3. Open ShipEditor scene and assign references in ShipEditorManager.\n" +
                "4. Press Play from the Boot scene.", "OK");
        }

        // ── Boot scene ────────────────────────────────────────────────────────
        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<SpaceGame.Core.GameManager>();

            var bootGO = new GameObject("SceneBootstrapper");
            bootGO.AddComponent<SpaceGame.Core.SceneBootstrapper>();

            EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/Boot.unity");
        }

        // ── Space Map scene ───────────────────────────────────────────────────
        private static void CreateSpaceMapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f);

            // Canvas
            var canvasGO = CreateCanvas("Canvas");
            var canvas   = canvasGO.GetComponent<Canvas>();

            // Background (starfield placeholder — dark panel)
            var bg = CreatePanel(canvasGO.transform, "Background", new Color(0.03f, 0.03f, 0.12f));
            StretchFull(bg);

            // Map area
            var mapArea = CreatePanel(bg.transform, "MapArea", new Color(0f, 0f, 0f, 0f));
            StretchFull(mapArea);

            // Player HUD (top-left)
            var hud = CreatePanel(canvasGO.transform, "PlayerHUD", new Color(0f, 0f, 0f, 0.6f));
            var hudRT = hud.GetComponent<RectTransform>();
            hudRT.anchorMin = new Vector2(0f, 0.7f);
            hudRT.anchorMax = new Vector2(0.25f, 1f);
            hudRT.offsetMin = new Vector2(10, -10);
            hudRT.offsetMax = new Vector2(-10, 10);
            var hudLabel = CreateTMPLabel(hud.transform, "PlayerStatsLabel", "YOUR SHIP\n...", 14);

            // Ship editor button
            var editorBtn = CreateButton(hud.transform, "ShipEditorButton", "Edit Ship");
            var editorBtnRT = editorBtn.GetComponent<RectTransform>();
            editorBtnRT.anchorMin = new Vector2(0f, 0f);
            editorBtnRT.anchorMax = new Vector2(1f, 0.25f);

            // Pre-Fight panel (centered, hidden by default)
            var pfPanel = CreatePanel(canvasGO.transform, "PreFightPanel", new Color(0.05f, 0.05f, 0.15f, 0.95f));
            var pfRT    = pfPanel.GetComponent<RectTransform>();
            pfRT.anchorMin = new Vector2(0.2f, 0.1f);
            pfRT.anchorMax = new Vector2(0.8f, 0.9f);
            pfRT.offsetMin = Vector2.zero;
            pfRT.offsetMax = Vector2.zero;
            pfPanel.SetActive(false);

            var pfTitle    = CreateTMPLabel(pfPanel.transform, "PreFightTitle",    "Intercept: Enemy", 22);
            var pfStats    = CreateTMPLabel(pfPanel.transform, "PreFightStats",    "Stats...", 14);
            var pfLootHint = CreateTMPLabel(pfPanel.transform, "PreFightLootHint", "Loot...",  13);

            // Ship visualizer area
            var vizGO = new GameObject("PreFightVisualizer", typeof(RectTransform));
            vizGO.transform.SetParent(pfPanel.transform, false);
            vizGO.AddComponent<SpaceGame.UI.ShipVisualizer>();
            var vizRT = vizGO.GetComponent<RectTransform>();
            vizRT.anchorMin = new Vector2(0.1f, 0.35f);
            vizRT.anchorMax = new Vector2(0.9f, 0.75f);
            vizRT.offsetMin = Vector2.zero;
            vizRT.offsetMax = Vector2.zero;

            var fightBtn  = CreateButton(pfPanel.transform, "FightButton",  "FIGHT!");
            var cancelBtn = CreateButton(pfPanel.transform, "CancelButton", "Retreat");
            PositionButton(fightBtn,  new Vector2(0.1f, 0.02f), new Vector2(0.45f, 0.18f));
            PositionButton(cancelBtn, new Vector2(0.55f, 0.02f), new Vector2(0.9f, 0.18f));

            // Enemy button prefab (created as disabled child; SpaceMapManager will use it as prefab)
            // NOTE: In a real project this would be a proper prefab. For now we create a template.
            var enemyBtnTemplate = CreateButton(canvasGO.transform, "EnemyShipButtonTemplate", "Enemy\n[2x2] T1");
            enemyBtnTemplate.SetActive(false);
            var ebtRT = enemyBtnTemplate.GetComponent<RectTransform>();
            ebtRT.sizeDelta = new Vector2(80f, 60f);

            // SpaceMapManager
            var mgrGO = new GameObject("SpaceMapManager");
            var mgr   = mgrGO.AddComponent<SpaceGame.UI.SpaceMapManager>();

            // NOTE: Inspector references must be wired manually after scene creation,
            // as we cannot set serialized fields on MonoBehaviours from editor scripts easily.
            // The scene is created with all needed objects; drag-and-drop in inspector to connect.

            Debug.Log("[SceneCreator] SpaceMap scene created. Wire inspector references manually.");

            EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/SpaceMap.unity");
        }

        // ── Combat scene ──────────────────────────────────────────────────────
        private static void CreateCombatScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f);

            var canvasGO = CreateCanvas("Canvas");

            // Background
            var bg = CreatePanel(canvasGO.transform, "Background", new Color(0.03f, 0.03f, 0.1f));
            StretchFull(bg);

            // Player stats (top-left)
            var playerStats = CreatePanel(canvasGO.transform, "PlayerStats", new Color(0f, 0.2f, 0f, 0.8f));
            PositionPanel(playerStats, new Vector2(0f, 0.8f), new Vector2(0.2f, 1f));
            CreateTMPLabel(playerStats.transform, "PlayerStatsText", "PLAYER\nHP: 0/0\nShield: 0/0", 14);

            // Enemy stats (top-right)
            var enemyStats = CreatePanel(canvasGO.transform, "EnemyStats", new Color(0.2f, 0f, 0f, 0.8f));
            PositionPanel(enemyStats, new Vector2(0.8f, 0.8f), new Vector2(1f, 1f));
            CreateTMPLabel(enemyStats.transform, "EnemyStatsText", "ENEMY\nHP: 0/0\nShield: 0/0", 14);

            // Combat log (scrolling text in center)
            var logPanel = CreatePanel(canvasGO.transform, "CombatLogPanel", new Color(0f, 0f, 0f, 0.7f));
            PositionPanel(logPanel, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.78f));
            var logLabel = CreateTMPLabel(logPanel.transform, "CombatLogText", "Combat starting...", 13);
            logLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

            // Result panel (hidden)
            var resultPanel = CreatePanel(canvasGO.transform, "ResultPanel", new Color(0.05f, 0.05f, 0.15f, 0.97f));
            PositionPanel(resultPanel, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            resultPanel.SetActive(false);
            CreateTMPLabel(resultPanel.transform, "ResultText", "Result", 24);
            var contBtn = CreateButton(resultPanel.transform, "ContinueButton", "Continue");
            PositionButton(contBtn, new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.25f));

            // CombatManager
            var mgrGO = new GameObject("CombatManager");
            mgrGO.AddComponent<SpaceGame.Combat.CombatManager>();

            EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/Combat.unity");
        }

        // ── Ship Editor scene ─────────────────────────────────────────────────
        private static void CreateShipEditorScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f);

            var canvasGO = CreateCanvas("Canvas");

            var bg = CreatePanel(canvasGO.transform, "Background", new Color(0.03f, 0.03f, 0.1f));
            StretchFull(bg);

            // Title
            var titleLabel = CreateTMPLabel(canvasGO.transform, "Title", "SHIP EDITOR", 28);
            PositionPanel(titleLabel, new Vector2(0.2f, 0.9f), new Vector2(0.8f, 1f));

            // Grid area (right 70%)
            var gridPanel = CreatePanel(canvasGO.transform, "GridPanel", new Color(0f, 0f, 0f, 0.4f));
            PositionPanel(gridPanel, new Vector2(0.28f, 0.08f), new Vector2(0.98f, 0.88f));
            var gridParent = new GameObject("GridParent", typeof(RectTransform));
            gridParent.transform.SetParent(gridPanel.transform, false);
            StretchFull(gridParent);

            // Inventory (left 25%)
            var invPanel = CreatePanel(canvasGO.transform, "InventoryPanel", new Color(0f, 0f, 0.2f, 0.7f));
            PositionPanel(invPanel, new Vector2(0.01f, 0.08f), new Vector2(0.27f, 0.88f));
            CreateTMPLabel(invPanel.transform, "InventoryTitle", "INVENTORY", 16);
            var invScroll = new GameObject("InventoryScrollContent", typeof(RectTransform));
            invScroll.transform.SetParent(invPanel.transform, false);
            StretchFull(invScroll);

            // Buttons (bottom)
            var confirmBtn = CreateButton(canvasGO.transform, "ConfirmButton", "Confirm Layout");
            var backBtn    = CreateButton(canvasGO.transform, "BackButton",    "Back to Map");
            PositionButton(confirmBtn, new Vector2(0.3f, 0.01f), new Vector2(0.65f, 0.08f));
            PositionButton(backBtn,   new Vector2(0.67f, 0.01f), new Vector2(0.98f, 0.08f));

            var statusLabel = CreateTMPLabel(canvasGO.transform, "StatusText", "", 13);
            PositionPanel(statusLabel, new Vector2(0f, 0.01f), new Vector2(0.28f, 0.08f));

            // Inventory slot prefab template (disabled)
            var slotTemplate = new GameObject("InventorySlotTemplate", typeof(RectTransform), typeof(Image));
            slotTemplate.transform.SetParent(canvasGO.transform, false);
            slotTemplate.SetActive(false);
            CreateTMPLabel(slotTemplate.transform, "SlotLabel", "Module x1", 11);

            // ShipEditorManager
            var mgrGO = new GameObject("ShipEditorManager");
            mgrGO.AddComponent<SpaceGame.UI.ShipEditorManager>();

            EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/ShipEditor.unity");
        }

        // ── Build Settings ────────────────────────────────────────────────────
        private static void AddScenesToBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{SCENES_PATH}/Boot.unity",       true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/SpaceMap.unity",   true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Combat.unity",     true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/ShipEditor.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        private static GameObject CreateCanvas(string name)
        {
            var go     = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.6f);
            CreateTMPLabel(go.transform, "Label", label, 16);
            return go;
        }

        private static GameObject CreateTMPLabel(Transform parent, string name, string text, int fontSize)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            StretchFull(go);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionPanel(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionButton(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
            => PositionPanel(go, anchorMin, anchorMax);
    }
}
#endif
