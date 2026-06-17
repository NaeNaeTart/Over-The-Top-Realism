using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using OverTheTopRealism.Features;
using System.Reflection;

namespace OverTheTopRealism
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInProcess("CasualtiesUnknown.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        private Harmony _harmony = null!;
        internal static ModConfig Cfg { get; private set; } = null!;

        private void Awake()
        {
            Instance = this;
            Logger   = base.Logger;

            // Bind configuration settings
            Cfg = new ModConfig(Config);

            // Apply all Harmony patches
            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo($"[{PluginInfo.NAME} v{PluginInfo.VERSION}] Loaded successfully! Extreme physiological systems active.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
