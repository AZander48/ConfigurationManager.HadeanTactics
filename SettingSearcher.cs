using System;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace ConfigurationManager
{
    internal static class SettingSearcher
    {
        private static readonly ICollection<string> _updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        /// <summary>
        /// Search for all instances of BaseUnityPlugin loaded by chainloader or other means.
        /// </summary>
        public static BaseUnityPlugin[] FindPlugins()
        {
            var plugins = new List<BaseUnityPlugin>();

            try
            {
                foreach (var kvp in Chainloader.PluginInfos)
                {
                    var instance = kvp.Value != null ? kvp.Value.Instance : null;
                    // Unity overloads == for destroyed objects. Plugin MonoBehaviours can become
                    // "Unity null" after scene loads while the managed ConfigFile is still valid.
                    if (ReferenceEquals(instance, null))
                        continue;

                    if (!plugins.Contains(instance))
                        plugins.Add(instance);
                }
            }
            catch (Exception ex)
            {
                ConfigurationManager.Logger.LogError("Failed to read Chainloader.PluginInfos: " + ex);
            }

            try
            {
                var found = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin));
                if (found != null)
                {
                    foreach (var obj in found)
                    {
                        var plugin = obj as BaseUnityPlugin;
                        if (!ReferenceEquals(plugin, null) && !plugins.Contains(plugin))
                            plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigurationManager.Logger.LogWarning("FindObjectsOfType(BaseUnityPlugin) failed; using Chainloader list only. " + ex.Message);
            }

            return plugins.ToArray();
        }

        public static void CollectSettings(out IEnumerable<SettingEntryBase> results, out List<string> modsWithoutSettings, bool showDebug)
        {
            modsWithoutSettings = new List<string>();

            try
            {
                results = GetBepInExCoreConfig();
            }
            catch (Exception ex)
            {
                results = Enumerable.Empty<SettingEntryBase>();
                ConfigurationManager.Logger.LogError(ex);
            }

            foreach (var plugin in FindPlugins())
            {
                try
                {
                    var type = plugin.GetType();

                    var pluginInfo = plugin.Info.Metadata;
                    var pluginName = pluginInfo != null ? pluginInfo.Name : type.FullName;

                    if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                            .Any(x => !x.Browsable))
                    {
                        modsWithoutSettings.Add(pluginName);
                        continue;
                    }

                    var detected = new List<SettingEntryBase>();

                    detected.AddRange(GetPluginConfig(plugin).Cast<SettingEntryBase>());

                    detected.RemoveAll(x => x.Browsable == false);

                    if (detected.Count == 0)
                        modsWithoutSettings.Add(pluginName);

                    if (showDebug)
                    {
                        var enabledProp = type.GetProperty("enabled");
                        if (enabledProp != null &&
                            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .Any(x => _updateMethodNames.Contains(x.Name)))
                        {
                            var enabledSetting = new PropertySettingEntry(plugin, enabledProp, plugin);
                            enabledSetting.DispName = "!Allow plugin to run on every frame";
                            enabledSetting.Description = "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                            enabledSetting.IsAdvanced = true;
                            detected.Add(enabledSetting);
                        }
                    }

                    if (detected.Count > 0)
                        results = results.Concat(detected);
                }
                catch (Exception ex)
                {
                    string pluginName;
                    try { pluginName = plugin.Info.Metadata.Name; }
                    catch { pluginName = plugin.GetType().FullName; }

                    ConfigurationManager.Logger.LogError("Failed to collect settings of the following plugin: " + pluginName);
                    ConfigurationManager.Logger.LogError(ex);
                }
            }
        }

        private static IEnumerable<SettingEntryBase> GetBepInExCoreConfig()
        {
            var coreConfigProp = typeof(ConfigFile).GetProperty("CoreConfig", BindingFlags.Static | BindingFlags.NonPublic);
            if (coreConfigProp == null) throw new ArgumentNullException(nameof(coreConfigProp));

            var coreConfig = (ConfigFile)coreConfigProp.GetValue(null, null);
            var bepinMeta = new BepInPlugin("BepInEx", "BepInEx", typeof(BepInEx.Bootstrap.Chainloader).Assembly.GetName().Version.ToString());

            return coreConfig.Select(kvp => (SettingEntryBase)new ConfigSettingEntry(kvp.Value, null) { IsAdvanced = true, PluginInfo = bepinMeta });
        }

        private static IEnumerable<ConfigSettingEntry> GetPluginConfig(BaseUnityPlugin plugin)
        {
            return plugin.Config.Select(kvp => new ConfigSettingEntry(kvp.Value, plugin));
        }
    }
}
