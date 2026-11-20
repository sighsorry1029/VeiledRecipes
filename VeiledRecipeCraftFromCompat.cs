using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using TMPro;
using UnityEngine;

namespace VeiledRecipes;

internal static class VeiledRecipeCraftFromCompat
{
    internal const string PluginGuid = "sighsorry.CraftFrom";
    private delegate bool RecipeQuote(Player player, Recipe recipe, ItemDrop.ItemData? upgrade, int quality,
        int multiplier, int index, out int available, out int needed, out bool unselected);
    private delegate bool PieceQuote(Player player, Piece piece, int index, out int available, out int needed, out bool affordable);
    private static RecipeQuote? recipeQuote;
    private static PieceQuote? pieceQuote;
    private static readonly ConditionalWeakTable<Transform, TMP_Text> Labels = new();
    private static readonly ConditionalWeakTable<Transform, TMP_Text>.CreateValueCallback FindLabel =
        root => VeiledRecipeRequirementUi.FindComponent<TMP_Text>(root, VeiledRecipeConstants.RequirementAmountChild)!;

    internal static void Initialize()
    {
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var plugin) || !plugin.Instance) return;
        BindApi(plugin.Instance.GetType().Assembly.GetType("CraftFrom.API"));
    }

    internal static void BindApi(Type? api)
    {
        recipeQuote = Bind<RecipeQuote>(api, "TryGetRecipeRequirement");
        pieceQuote = Bind<PieceQuote>(api, "TryGetBuildingRequirement");
    }

    private static T? Bind<T>(Type? api, string name) where T : Delegate
    {
        if (api == null) return null;
        try
        {
            var signature = typeof(T).GetMethod("Invoke")!.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = api.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, signature, null);
            return method?.ReturnType == typeof(bool) ? (T)method.CreateDelegate(typeof(T)) : null;
        }
        catch (Exception error)
        {
            VeiledRecipesPlugin.PluginLogger.LogWarning($"CraftFrom {name} could not be bound: {error.Message}");
            return null;
        }
    }

    internal static void Shutdown()
    {
        recipeQuote = null;
        pieceQuote = null;
    }

    internal static void ShowRecipe(Transform root, Player player, Recipe recipe, ItemDrop.ItemData? upgrade,
        int quality, int multiplier, int index)
    {
        if (recipeQuote == null) return;
        try
        {
            if (recipeQuote(player, recipe, upgrade, quality, multiplier, index, out int available, out int needed, out bool unselected))
                Display(root, available, needed, available >= needed, unselected);
        }
        catch (Exception error)
        {
            recipeQuote = null;
            VeiledRecipesPlugin.PluginLogger.LogWarning($"CraftFrom recipe preview stopped: {error.Message}");
        }
    }

    internal static void ShowPiece(Transform root, Player player, Piece piece, int index)
    {
        if (pieceQuote == null) return;
        try
        {
            if (pieceQuote(player, piece, index, out int available, out int needed, out bool affordable))
                Display(root, available, needed, affordable, false);
        }
        catch (Exception error)
        {
            pieceQuote = null;
            VeiledRecipesPlugin.PluginLogger.LogWarning($"CraftFrom building preview stopped: {error.Message}");
        }
    }

    private static void Display(Transform root, int available, int needed, bool affordable, bool unselected)
    {
        var label = Labels.GetValue(root, FindLabel);
        if (!label)
        {
            Labels.Remove(root);
            return;
        }
        // TMP's allocation-free numeric formatter takes floats.
        if (available <= 1 << 24 && needed <= 1 << 24) label.SetText("{0}/{1}", available, needed);
        else label.text = $"{available}/{needed}";
        label.color = unselected ? Color.gray : !affordable && Mathf.Sin(Time.time * 10f) > 0f ? Color.red : Color.white;
    }
}
