using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            InitializePagination(assembly);
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

    private static void InitializePagination(Assembly assembly)
    {
        // Keep paging hooks independent: an unsupported AAA version must not disable masking.
        Harmony harmony = new(VeiledRecipesPlugin.ModGUID + ".AAA.Paging");
        try
        {
            Type paginator = assembly.GetType(Namespace + "Patches.PaginatorPatches+InventoryGuiUpdateRecipeListPatch", throwOnError: true)!;
            Type[] parameters = { typeof(InventoryGui), typeof(List<Recipe>).MakeByRefType() };
            MethodInfo prefix = AccessTools.DeclaredMethod(paginator, "Prefix", parameters) ?? throw new MissingMethodException(paginator.FullName, "Prefix");
            MethodInfo postfix = AccessTools.DeclaredMethod(paginator, "Postfix", parameters) ?? throw new MissingMethodException(paginator.FullName, "Postfix");
            HarmonyMethod transpiler = new(typeof(VeiledRecipeAaaCraftingCompat), nameof(GroupBeforePagination));
            harmony.Patch(prefix, transpiler: transpiler);
            harmony.Patch(postfix, transpiler: transpiler);
            VeiledRecipesPlugin.PluginLogger.LogInfo("AAA Crafting grouping before pagination enabled.");
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            VeiledRecipesPlugin.PluginLogger.LogWarning($"AAA Crafting full-list grouping could not be enabled; keeping page-local grouping: {ex.Message}");
        }
    }

    private static IEnumerable<CodeInstruction> GroupBeforePagination(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        bool craft = __originalMethod.Name == "Prefix";
        Type entryType = craft ? typeof(Recipe) : typeof(InventoryGui.RecipeDataPair);
        MethodInfo replacement = AccessTools.Method(typeof(VeiledRecipeAaaCraftingCompat), craft ? nameof(SkipCraftRecipes) : nameof(SkipRecipePairs));
        List<CodeInstruction> codes = instructions.ToList();
        int replacements = 0;
        foreach (CodeInstruction code in codes)
        {
            if (code.opcode != OpCodes.Call || code.operand is not MethodInfo method ||
                method.DeclaringType != typeof(Enumerable) || method.Name != nameof(Enumerable.Skip) ||
                !method.IsGenericMethod || method.GetGenericArguments()[0] != entryType ||
                method.GetParameters().Length != 2 || method.GetParameters()[1].ParameterType != typeof(int))
            {
                continue;
            }

            code.operand = replacement;
            replacements++;
        }

        // Crafting has fresh-list and cache-reuse paths; upgrades have one pair-based path.
        int expected = craft ? 2 : 1;
        if (replacements != expected)
        {
            throw new InvalidOperationException($"Unsupported AAA {__originalMethod.Name} pagination: expected {expected} Skip calls, found {replacements}.");
        }
        return codes;
    }

    private static IEnumerable<Recipe> SkipCraftRecipes(IEnumerable<Recipe> recipes, int count)
    {
        return SkipGrouped(recipes, count, (player, recipe) => VeiledRecipeState.IsUnknownRecipePreview(player, recipe));
    }

    private static IEnumerable<InventoryGui.RecipeDataPair> SkipRecipePairs(IEnumerable<InventoryGui.RecipeDataPair> pairs, int count)
    {
        return SkipGrouped(pairs, count, (player, pair) =>
            VeiledRecipeState.GetRecipeVisibilityState(player, pair.Recipe, pair.ItemData) == VeiledRecipeVisibilityState.UnknownPreview);
    }

    private static IEnumerable<T> SkipGrouped<T>(IEnumerable<T> source, int count, Func<Player, T, bool> isUnknown)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || !VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes)
        {
            return source.Skip(count);
        }

        // Re-evaluate knowledge for every page, without changing AAA's cached sort/filter result.
        List<T> known = new();
        List<T> unknown = new();
        foreach (T entry in source)
        {
            (isUnknown(player, entry) ? unknown : known).Add(entry);
        }
        known.AddRange(unknown);
        return known.Skip(count);
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
