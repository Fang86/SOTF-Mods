using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using HarmonyLib;
using UnityEngine;
using TheForest.Utils;

namespace Flight
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Flight : BasePlugin
    {
        private const string GUID = "fang86.Flight";
        private const string NAME = "Flight";
        private const string VERSION = "1.0.2";
        
        public static ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Flight.cfg"), true);
        public static ConfigEntry<KeyCode> toggle_key = configFile.Bind("Hotkeys", "Toggle", KeyCode.J, "Flight mode toggle key");
        public static ConfigEntry<KeyCode> up_key = configFile.Bind("Hotkeys", "Ascend", KeyCode.Space, "Ascend key");
        public static ConfigEntry<KeyCode> down_key = configFile.Bind("Hotkeys", "Descend", KeyCode.LeftControl, "Descend key");
        public static ConfigEntry<KeyCode> accel_key = configFile.Bind("Hotkeys", "Accelerate", KeyCode.LeftShift, "Acceleration key - Multiplies your flight speed while held");
        public static ConfigEntry<float> speed = configFile.Bind("General", "Speed", 1.0f, "Base flight speed - Affects all directions");
        public static ConfigEntry<float> accel_speed = configFile.Bind("General", "Acceleration speed", 2.0f, "Acceleration speed - Multiplies speed by this value while holding acceleration key");
        public static ConfigEntry<float> grace_period = configFile.Bind("General", "Grace period", 3.0f, "GodMode grace period (seconds) after disabling flight mode");

        public static ManualLogSource DLog = new ManualLogSource("DLog");
        public static bool flying = false;

        public override void Load()
        {
            BepInEx.Logging.Logger.Sources.Add(DLog);

            ClassInjector.RegisterTypeInIl2Cpp<CustomComponent>();
            var go = new GameObject("FlightMod") { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<CustomComponent>();
            GameObject.DontDestroyOnLoad(go);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            Log.LogInfo($"Loaded Flight {VERSION} by Fang86");
        }

        public class CustomComponent : MonoBehaviour
        {
            public static GameObject player;
            public static Vector3 last_pos;
            public static float rot;
            public static float speed = Flight.speed.Value;
            public static int godmode_delay = (int)(grace_period.Value * 50.0f);
            public static int godmode_timer = 0;

            void Update()
            {
                // Mod logic - In game
                if (IsInGame())
                {
                    // Get player
                    player = GameObject.FindWithTag("Player");
                    FirstPersonCharacter fpc = player.GetComponent<FirstPersonCharacter>();

                    // Prevent shake
                    fpc.Grounded = true;

                    // Fall prevention
                    if (flying) { player.transform.position = last_pos; }

                    // Toggle flight
                    if (Input.GetKeyDown(toggle_key.Value) && !IsPaused()) 
                    { 
                        flying = !flying; 
                        Cheats.GodMode = true;
                        godmode_timer = 0;
                    }

                    if (flying && !IsPaused())
                    {
                        // Rotation radians
                        rot = Mathf.Deg2Rad * player.transform.localEulerAngles.y;

                        // Fixed speed
                        speed = 60 * Time.deltaTime;

                        // Accelerate
                        if (Input.GetKey(accel_key.Value))
                        {
                            speed = accel_speed.Value * Flight.speed.Value;
                        }
                        else
                        {
                            speed = Flight.speed.Value;
                        }

                        // Up/Down
                        if (Input.GetKey(up_key.Value))
                        {
                            player.transform.position = player.transform.position + new Vector3(0, speed, 0);
                        }
                        else if (Input.GetKey(down_key.Value))
                        {
                            player.transform.position = player.transform.position + new Vector3(0, -speed, 0);
                        }

                        // Left/Right
                        if (Input.GetKey(KeyCode.A))
                        {
                            player.transform.position = player.transform.position + new Vector3(-speed * Mathf.Cos(rot), 0, speed * Mathf.Sin(rot));
                        }
                        else if (Input.GetKey(KeyCode.D))
                        {
                            player.transform.position = player.transform.position + new Vector3(speed * Mathf.Cos(rot), 0, -speed * Mathf.Sin(rot));
                        }

                        // Forward/Back
                        if (Input.GetKey(KeyCode.W))
                        {
                            player.transform.position = player.transform.position + new Vector3(speed * Mathf.Sin(rot), 0, speed * Mathf.Cos(rot));
                        }
                        else if (Input.GetKey(KeyCode.S))
                        {
                            player.transform.position = player.transform.position + new Vector3(-speed * Mathf.Sin(rot), 0, -speed * Mathf.Cos(rot));
                        }
                    }

                    // Save position for fall prevention
                    last_pos = player.transform.position;
                }
            }

            void FixedUpdate()
            {
                // GodMode grace period
                if (!flying && Cheats.GodMode)
                {
                    // Count until grace period ends, then turn off GodMode
                    if (godmode_timer < godmode_delay) { godmode_timer += 1; }
                    else { Cheats.GodMode = false; }
                }
            }

            private static bool IsInGame()
            {
                // Source: f. in SOTF discord
                return LocalPlayer.IsInWorld;
            }

            private static bool IsPaused()
            {
                return Sons.Gui.PauseMenu.IsActive;
            }
        }
    }
    
    /*
    [HarmonyPatch(typeof(Sons.Player.CameraShakeController), "IsCameraShakeAllowed")]
    public static class Patcher
    {

        private static bool Prefix(ref bool __result)
        {
            if (Flight.flying)
            {
                return false;
            }
            return __result;
        }
    }
    */
}
