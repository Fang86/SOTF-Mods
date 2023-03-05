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
using UnityEngine.UI;
using TheForest.Utils;
using System.Collections.Generic;
using UniverseLib;
using UniverseLib.UI;
using Warps.UI;
using Mache;
using Il2CppInterop.Runtime;
using System.Linq;
using Mache.UI;

namespace Warps
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Warps : BasePlugin
    {
        public const string GUID = "fang86.Warps";
        public const string NAME = "Warps";
        public const string VERSION = "1.0.0";

        public static ManualLogSource DLog = new ManualLogSource("DLog");
        public static bool flying = false;

        public static ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Warps.cfg"), true);
        public static ConfigEntry<KeyCode> menu_key = configFile.Bind("General", "Menu key", KeyCode.K, "Hotkey to open the Warps menu");
        //public static ConfigEntry<float> window_x = configFile.Bind("General", "x", 0.0f, "Window x position");
        //public static ConfigEntry<float> window_y = configFile.Bind("General", "y", 0.0f, "Window y position");

        public override void Load()
        {
            BepInEx.Logging.Logger.Sources.Add(DLog);

            ClassInjector.RegisterTypeInIl2Cpp<WarpsComponent>();
            ClassInjector.RegisterTypeInIl2Cpp<WarpsMenu>();

            var go = new GameObject("WarpsMod") { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<WarpsComponent>();
            GameObject.DontDestroyOnLoad(go);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            //Instance = this;
            //AddComponent<WarpsComponent>();

            Log.LogInfo($"Loaded {NAME} {VERSION} by Fang86");
        }
    }

    public class WarpsComponent : MonoBehaviour
    {
        private static WarpsMenu Menu { get; set; }
        public static ManualLogSource DLog = Warps.DLog;
        
        private static string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Warps/Warps.txt";

        private void Start()
        {
            
            Mache.Mache.RegisterMod(() =>
            {
                return new ModDetails
                {
                    Id = Warps.GUID,
                    Version = Warps.VERSION,
                    Name = Warps.NAME,
                    Description = "Create custom warps to teleport around the world",
                    OnFinishedCreating = CreateMenu
                    //OnMenuShow = ShowMenu,
            };
            });
        }

        private void CreateMenu(GameObject parent)
        {
            parent.AddComponent<WarpsMenu>();
        }

        public static bool IsInGame()
        {
            return LocalPlayer.IsInWorld;
        }

        public static bool IsPaused()
        {
            return Sons.Gui.PauseMenu.IsActive;
        }

        // Modified from: https://stackoverflow.com/a/35496185
        private static void Overwrite(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        public static float[] GetWarp(string name)
        {
            string[] lines = File.ReadAllLines(path);

            if (lines.Length != 0)
            {
                foreach (string line in lines)
                {
                    string[] components = line.Split('=');

                    // Ensure line has name and pos
                    if (components.Length != 2) { continue; }

                    string warp_name = components[0];
                    string[] pos = components[1].Replace(" ", "").Split(',');
                    float x = float.Parse(pos[0]);
                    float y = float.Parse(pos[1]);
                    float z = float.Parse(pos[2]);

                    if (warp_name == name)
                    {
                        DLog.LogMessage($"Found {warp_name}! Position: {pos[0]}, {pos[1]}, {pos[2]}");
                        return new float[] { x, y, z };
                    }
                } 
            }

            return null;
        }

        public static Dictionary<String, String> GetWarps()
        {
            DLog.LogMessage("Getting all warps");
            string[] lines = File.ReadAllLines(path);
            Dictionary<String, String> warp_list = new Dictionary<String, String>();

            if (lines.Length != 0)
            {
                foreach (string line in lines)
                {
                    string[] components = line.Split('=');

                    // Ensure line has name and pos
                    if (components.Length != 2) { continue; }

                    string warp_name = components[0];
                    string pos = components[1];

                    warp_list.Add(warp_name, pos);

                    DLog.LogMessage($"{warp_name}: {pos}");
                }
            }

            return null;
        }

        // Set warp at current position
        // @Returns {bool} - overwritten or error
        public static bool SetWarp(string name)
        {
            if (name.Length == 0)
            {
                DLog.LogWarning("Warp name cannot be empty");
                return true;
            }
            else if (name.Length > 90)
            {
                DLog.LogWarning("Warp name cannot be longer than 90 characters");
                return true;
            }
            else if (name.Contains("="))
            {
                DLog.LogWarning("Warp names cannot contain \'=\'");
                return true;
            }

            Vector3 pos = LocalPlayer.GameObject.transform.position;
            string line_text = name + "=" + pos.x + ", " + pos.y + ", " + pos.z;
            string[] lines = File.ReadAllLines(path);

            //DLog.LogInfo($"Setting warp {name} at " + pos.x + ", " + pos.y + ", " + pos.z);

            if (lines.Length != 0)
            {
                // Search for warp
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] components = line.Split('=');

                    // Ensure line has name and pos
                    if (components.Length != 2) { continue; }

                    string warp_name = components[0];

                    if (warp_name == name)
                    {
                        // Already exists, overwrite
                        Overwrite(line_text, path, i);
                        return true;
                    }
                }
            }

            // Warp does not already exist
            File.AppendAllText(path, line_text + Environment.NewLine);
            return false;
                
        }

        public static bool DeleteWarp(string name)
        {
            string[] lines = File.ReadAllLines(path);

            if (lines.Length != 0)
            {
                // Search for warp
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] components = line.Split('=');

                    // Ensure line has name and pos
                    if (components.Length != 2) { continue; }

                    string warp_name = components[0];

                    if (warp_name == name)
                    {
                        //DLog.LogInfo($"Deleting warp {name}");
                        // Already exists => overwrite with empty line
                        Overwrite("", path, i);
                        CleanFile();
                        return true;
                    }
                }
                // Doesn't exist => no need to delete
            }

            return false;
        }

        private static void CleanFile()
        {
            DLog.LogInfo($"Cleaning {path}");

            string[] lines = File.ReadAllLines(path);
            List<string> clean_lines = new List<string>();


            if (lines.Length != 0)
            {
                // Search for warp
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] components = line.Split('=');

                    // Add lines that are valid (not whitespace, key=value format)
                    if (line != "\n" && !string.IsNullOrWhiteSpace(line) && components.Length == 2)
                    {
                        clean_lines.Add(line);
                    }
                }

                // Write cleaned lines back to file
                File.WriteAllLines(path, clean_lines.ToArray());
            }

            // Empty, no need for cleaning
        }

        public static void WarpTo(string name)
        {
            float[] pos = GetWarp(name);

            if (pos == null) // No warp found
            { 
                return; 
            } 
            else // Warp found => teleport
            { 
                LocalPlayer.TeleportTo(new Vector3(pos[0], pos[1], pos[2]), LocalPlayer.GameObject.transform.rotation); 
            } 
        }
    }
}
