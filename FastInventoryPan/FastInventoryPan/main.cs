using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace FastInventoryPan
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class FastInventoryPan : BasePlugin
    {
        private const string GUID = "fang86.FasterInventoryPanning";
        private const string NAME = "Faster Inventory Panning";
        private const string VERSION = "1.1.0";
        private const string author = "Fang86";

        public static ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "FasterInventoryPanning.cfg"), true);
        public static ConfigEntry<float> pan_speed = configFile.Bind("General", "Speed", 2.0f, "Camera panning speed - Recommended range: 1.0 - 2.5");
        public static ConfigEntry<float> cursor_speed = configFile.Bind("General", "Cursor Speed", 1.0f, "Cursor speed - Affects mouse and gamepad.");
        public static ConfigEntry<int> fov = configFile.Bind("General", "FOV", 85, "Inventory camera FOV - Note: A high FOV may lead to undesirable results. Recommended range: 70 - 85");

        public static ManualLogSource DLog = new ManualLogSource("DLog");
        public override void Load()
        {
            BepInEx.Logging.Logger.Sources.Add(DLog);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            Log.LogInfo($"Loaded Faster Inventory Panning by Fang86");
        }


        [HarmonyPatch(typeof(Sons.Inventory.InventoryCameraController), "Start")]
        public static class CameraPatcher
        {
            private static void Postfix(ref Sons.Inventory.InventoryCameraController __instance)
            {
                __instance._panSmoothTime = 1.5f / FastInventoryPan.pan_speed.Value;
                __instance._defaultFieldOfView = FastInventoryPan.fov.Value;
            }
        }

        [HarmonyPatch(typeof(Sons.Inventory.InventoryCursorController), "OnEnable")]
        public static class CursorPatcher
        {
            private static void Postfix(ref Sons.Inventory.InventoryCursorController __instance)
            {
                __instance._cursorMouseSpeed = FastInventoryPan.cursor_speed.Value * 0.0008f;
                __instance._cursorGamepadSpeed = FastInventoryPan.cursor_speed.Value * 0.003f;
            }
        }

    }
}