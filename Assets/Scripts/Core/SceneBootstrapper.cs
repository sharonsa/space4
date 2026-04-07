// SceneBootstrapper.cs
// Place this on a GameObject in the very first scene (SampleScene or a Boot scene).
// It ensures GameManager exists, then loads the Space Map.
// If a save exists it loads it, otherwise starts a fresh game.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceGame.Core
{
    public class SceneBootstrapper : MonoBehaviour
    {
        private void Start()
        {
            // GameManager will have created itself via Awake if it didn't exist yet.
            // Try to load a previous save.
            bool hasSave = GameManager.Instance.LoadGame();
            if (!hasSave)
                Debug.Log("[Boot] No save found — starting fresh game.");

            // Go to the space map
            GameManager.Instance.GoToMap();
        }
    }
}
