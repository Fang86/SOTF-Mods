using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using Mache;
using Warps;
using Il2CppInterop.Runtime;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using UniverseLib.UI.Models;
using Mache.UI;

namespace Warps.UI
{
    internal class WarpsMenu : MonoBehaviour
    {
        public static WarpsMenu Instance { get; private set; }

        internal static GameObject warpsListScrollview;
        internal static List<GameObject> warpElements = new List<GameObject>();

        private GameObject content;

        private void Start()
        {
            ConstructContent(gameObject);
        }

        internal void AddWarp(string name)
        {
            var warpListElement = UIFactory.CreateHorizontalGroup(warpsListScrollview, $"warp_{name}_row", true, false, true, true, spacing: 10, padding: new Vector4(10, 10, 10, 10), bgColor: Color.black);

            Text warpLabel = UIFactory.CreateLabel(warpListElement, $"warp_{name}_label", name, TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(warpLabel.gameObject, minWidth: 85, minHeight: 30, flexibleWidth: 0, preferredWidth: 350);

            var warpToButton = UIFactory.CreateButton(warpListElement, $"warp_to_{name}_button", "Warp", normalColor: new Color(0.251f, 0.251f, 0.251f));
            UIFactory.SetLayoutElement(warpToButton.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0, preferredWidth: 125);

            var warpDeleteButton = UIFactory.CreateButton(warpListElement, $"warp_delete_{name}_button", "Delete", normalColor: new Color(0.8f, 0.160f, 0.160f));
            UIFactory.SetLayoutElement(warpDeleteButton.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0, preferredWidth: 125);

            warpToButton.OnClick = () =>
            {
                WarpsComponent.DLog.LogMessage($"Warp to: {name}");
                if (WarpsComponent.IsInGame())
                {
                    WarpsComponent.WarpTo(name);
                }
            };

            ColorBlock warpCB = warpToButton.Component.colors;
            warpCB.pressedColor = new Color(0.251f, 0.251f, 0.251f);
            warpCB.selectedColor = new Color(0.251f, 0.251f, 0.251f);
            warpToButton.Component.colors = warpCB;

            warpDeleteButton.OnClick = () =>
            {
                WarpsComponent.DLog.LogMessage($"Delete warp: {name}");
                if (WarpsComponent.IsInGame() || true)
                {
                    WarpsComponent.DeleteWarp(name);
                    warpElements.Remove(warpListElement);
                    GameObject.Destroy(warpListElement);
                }
            };

            ColorBlock deleteCB = warpDeleteButton.Component.colors;
            deleteCB.pressedColor = new Color(1f, 0.2f, 0.2f);
            deleteCB.selectedColor = new Color(1f, 0.2f, 0.2f);
            warpDeleteButton.Component.colors = deleteCB;

            warpElements.Add(warpListElement);
        }

        internal void ConstructContent(GameObject root)
        {
            // Menu container //
            content = UIFactory.CreateVerticalGroup(root, "warps_col", true, false, true, true, spacing: 10, padding: new Vector4(10, 10, 10, 10));

            // New Warp //
            var new_warp_container = UIFactory.CreateHorizontalGroup(content, "new_warp_row", true, false, true, true, spacing: 10, padding: new Vector4(10, 10, 10, 10));

            Text newWarpLabel = UIFactory.CreateLabel(new_warp_container, "new_warp_label", "New Warp", TextAnchor.MiddleRight);
            UIFactory.SetLayoutElement(newWarpLabel.gameObject, minWidth: 85, minHeight: 30, flexibleWidth: 0);

            var newWarpInput = UIFactory.CreateInputField(new_warp_container, "new_warp_input", "Warp Name");
            UIFactory.SetLayoutElement(newWarpInput.GameObject, minHeight: 30, flexibleWidth: 9999, preferredWidth: 465);

            var newWarpButton = UIFactory.CreateButton(new_warp_container, "new_warp_button", "Create");
            UIFactory.SetLayoutElement(newWarpButton.GameObject, minHeight: 30, flexibleWidth: 9999, preferredWidth: 100);


            // Warps List //
            var warpListContainer = UIFactory.CreateHorizontalGroup(content, "warp_list_row", true, true, true, true, 2, new Vector4(3, 3, 3, 3), new Color(0.1f, 0.1f, 0.1f));

            var warpListView = UIFactory.CreateScrollView(warpListContainer, "warp_list_scrollview", out var scrollview, out _);
            UIFactory.SetLayoutElement(warpListView, minHeight: 400, flexibleWidth: 9999);
            warpsListScrollview = scrollview;
            UIFactory.CreateVerticalGroup(scrollview, "scrollview_vert_group", true, false, true, true, spacing: 10, bgColor: new Color(0.05f, 1.0f, 0.05f));

            
            // Event Handlers //
            newWarpButton.OnClick = () =>
            {
                if (WarpsComponent.IsInGame())
                {
                    if (!WarpsComponent.SetWarp(newWarpInput.Text))
                    {
                        AddWarp(newWarpInput.Text);
                    }
                }
            };

            string[] ops = GetOptions();
            if (ops.Length > 0)
            {
                foreach (string warp in ops)
                {
                    AddWarp(warp);
                }
            }
        }

        private static string[] GetOptions()
        {
            string[] lines = File.ReadAllLines($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Warps/Warps.txt");
            List<string> items = new List<string>();

            if (lines.Length > 0)
            {
                foreach (string line in lines)
                {
                    string[] comps = line.Split('=');
                    if (comps.Length == 2)
                    {
                        items.Add(comps[0]);
                    }
                }
            }

            return items.ToArray();
        }

        // Refresh dropdown options
        private static void Refresh(Dropdown drop)
        {
            drop.ClearOptions();

            string[] ops = GetOptions();
            if (ops.Length > 0)
            {
                var options = ops.ToList<string>();
                var il_options = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (var item in options)
                {
                    il_options.Add(item);
                }
                drop.AddOptions(il_options);
            }
        }

    }
}
