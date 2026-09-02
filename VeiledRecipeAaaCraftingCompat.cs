using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace VeiledRecipes;

internal static class VeiledRecipeAaaCraftingCompat
{
    internal const string PluginGuid = "Azumatt.AzuAntiArthriticCrafting";
    private const string Namespace = "AzuAntiArthriticCrafting.";
    private static bool _loaded;
    private static PropertyInfo? _trackedRecipe;
    private static FieldInfo? _trackedPanel;
    private static readonly HashSet<GameObject> HiddenTrackedPanels = new();
    private static GameObject? _tooltipRoot;
    private static TMP_Text? _tooltipTopic;
    private static TMP_Text? _tooltipText;
    private static bool _tooltipMasked;

    internal static void Initialize()
    {
        if (_loaded || !Chainloader.PluginInfos.TryGetValue(PluginGuid, out var plugin) || plugin.Instance == null)
        {
            return;
        }

        Harmony harmony = new(VeiledRecipesPlugin.ModGUID + ".AAA");
        try
        {
            Assembly assembly = plugin.Instance.GetType().Assembly;
            Type tooltip = assembly.GetType(Namespace + "Handlers.RecipeHoverTooltip", throwOnError: true)!;
            Type tracker = assembly.GetType(Namespace + "RecipeTracking.RecipeTrackerUI", throwOnError: true)!;
            Type entry = assembly.GetType(Namespace + "RecipeTracking.RecipeUI", throwOnError: true)!;
            _trackedRecipe = entry.GetProperty("Recipe") ?? throw new MissingMemberException(entry.FullName, "Recipe");
            _trackedPanel = entry.GetField("recipeStub") ?? throw new MissingFieldException(entry.FullName, "recipeStub");
            if (_trackedRecipe.PropertyType != typeof(Recipe) || _trackedPanel.FieldType != typeof(GameObject))
            {
                throw new InvalidOperationException("Unsupported recipe tracker fields.");
            }

            Patch(harmony, tooltip, "UpdateTextElements", Type.EmptyTypes, nameof(TooltipPostfix));
            Patch(harmony, tooltip, "LateUpdate", Type.EmptyTypes, nameof(TooltipLateUpdatePostfix));
            Patch(harmony, tracker, "AddSelectedRecipe", new[] { typeof(Recipe) }, nameof(AddTrackedRecipePrefix), prefix: true);
            Patch(harmony, tracker, "ToggleUI", Type.EmptyTypes, nameof(TrackerPostfix));
            _loaded = true;
            VeiledRecipesPlugin.PluginLogger.LogInfo("AAA Crafting compatibility enabled.");
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            VeiledRecipesPlugin.PluginLogger.LogWarning($"AAA Crafting compatibility could not be enabled: {ex.Message}");
        }
    }

    private static void Patch(Harmony harmony, Type type, string method, Type[] parameters, string handler, bool prefix = false)
    {
        MethodInfo target = AccessTools.DeclaredMethod(type, method, parameters) ?? throw new MissingMethodException(type.FullName, method);
        HarmonyMethod patch = new(typeof(VeiledRecipeAaaCraftingCompat), handler)
        {
            after = new[] { PluginGuid },
            priority = Priority.Last
        };
        harmony.Patch(target, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
    }

    internal static void MaskRecipeList(InventoryGui gui, Player player)
    {
        if (!_loaded)
        {
            return;
        }

        // AAA rewrites names and output counts after AddRecipeToList has finished.
        foreach (InventoryGui.RecipeDataPair pair in gui.m_availableRecipes)
        {
            if (pair.InterfaceElement != null && VeiledRecipeState.ShouldMaskRecipe(player, pair.Recipe, pair.ItemData))
            {
                InventoryGuiAddRecipeToListPatch.MaskRecipeListElement(pair.InterfaceElement, pair.Recipe);
            }
        }
    }

    private static bool ShouldMaskTooltip(Component tooltip)
    {
        InventoryGui? gui = InventoryGui.instance;
        Player? player = Player.m_localPlayer;
        if (tooltip == null || gui == null || player == null)
        {
            return false;
        }

        // Resolve the actual recipe pair, not a prefab name or an unrelated inventory item.
        foreach (InventoryGui.RecipeDataPair pair in gui.m_availableRecipes)
        {
            if (pair.InterfaceElement == tooltip.gameObject)
            {
                return VeiledRecipeState.ShouldMaskRecipe(player, pair.Recipe, pair.ItemData);
            }
        }

        return false;
    }

    private static void TooltipPostfix(Component __instance, GameObject ___m_tooltip, string ___m_topic, string ___m_text)
    {
        UpdateTooltip(__instance, ___m_tooltip, ___m_topic, ___m_text);
    }

    private static void TooltipLateUpdatePostfix(Component __instance, Component ___m_current, GameObject ___m_tooltip, string ___m_topic, string ___m_text)
    {
        // Only the active tooltip needs rechecking when knowledge or admin bypass changes.
        if (__instance == ___m_current)
        {
            UpdateTooltip(__instance, ___m_tooltip, ___m_topic, ___m_text);
        }
    }

    private static void UpdateTooltip(Component owner, GameObject root, string? topic, string? text)
    {
        if (root == null)
        {
            return;
        }

        if (_tooltipRoot != root)
        {
            _tooltipRoot = root;
            _tooltipTopic = Utils.FindChild(root.transform, "Topic")?.GetComponent<TMP_Text>();
            _tooltipText = Utils.FindChild(root.transform, "Text")?.GetComponent<TMP_Text>();
            _tooltipMasked = false;
        }

        bool mask = ShouldMaskTooltip(owner);
        if (!mask && !_tooltipMasked)
        {
            return;
        }

        // Leave AAA's source strings intact so an open tooltip can be restored after unlock.
        if (_tooltipTopic != null)
        {
            _tooltipTopic.text = mask ? VeiledRecipeState.UnknownNameText : Localization.instance.Localize(topic ?? "");
        }
        if (_tooltipText != null)
        {
            _tooltipText.text = mask ? VeiledRecipeState.UnknownDescriptionText : Localization.instance.Localize(text ?? "");
        }
        _tooltipMasked = mask;
    }

    private static bool AddTrackedRecipePrefix(Recipe recipe)
    {
        Player? player = Player.m_localPlayer;
        InventoryGui? gui = InventoryGui.instance;
        ItemDrop.ItemData? item = gui != null && gui.m_selectedRecipe.Recipe == recipe ? gui.m_selectedRecipe.ItemData : null;
        if (player == null || !VeiledRecipeState.ShouldMaskRecipe(player, recipe, item))
        {
            return true;
        }

        player.Message(MessageHud.MessageType.Center, VeiledRecipeState.UnknownDescriptionText);
        return false;
    }

    private static void TrackerPostfix(IEnumerable ___RecipeUIs)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || _trackedRecipe == null || _trackedPanel == null)
        {
            return;
        }

        HiddenTrackedPanels.RemoveWhere(panel => panel == null);
        foreach (object entry in ___RecipeUIs)
        {
            if (entry is not Component component || component == null ||
                _trackedRecipe.GetValue(entry) is not Recipe recipe ||
                _trackedPanel.GetValue(entry) is not GameObject panel || panel == null)
            {
                continue;
            }

            // A tracker has no upgrade target. Only discoverable crafting recipes need hiding.
            if (VeiledRecipeState.RequiresRecipeKnowledge(player, recipe))
            {
                if (panel.activeSelf)
                {
                    HiddenTrackedPanels.Add(panel);
                    panel.SetActive(false);
                }
            }
            else if (HiddenTrackedPanels.Remove(panel))
            {
                panel.SetActive(true);
            }
        }
    }
}
