// MonoBehaviour lifecycle bridge for BepInEx 5 Mono — added 2026 by AZander48.
// Based on BepInEx.ConfigurationManager (LGPL v3) by ManlyMarco / MarC0.
#if Mono
using UnityEngine;

namespace ConfigurationManager
{
    /// <summary>
    /// Routes Unity lifecycle to ConfigurationManager on BepInEx 5 Mono.
    /// Needed for games where BaseUnityPlugin Update/OnGUI do not run reliably.
    /// </summary>
    internal sealed class ConfigurationManagerMonoBehaviour : MonoBehaviour
    {
        internal ConfigurationManager Plugin;

        public void Init(ConfigurationManager plugin)
        {
            Plugin = plugin;
        }

        private void Start() => Plugin.PluginStart();

        private void Update() => Plugin.PluginUpdate();

        private void LateUpdate() => Plugin.PluginLateUpdate();

        private void OnGUI() => Plugin.PluginOnGUI();

        private void OnDestroy() => Utilities.InputSystemHelper.UnregisterHotkeyChecker();
    }
}
#endif
