using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace SecretRecipes;

internal static class SecretRecipeState
{
    private const string StationInteractionPrefix = "SecretRecipes.InteractedStation.";
    private const string UnknownNameFallback = "???";
    private const string UnknownDescriptionFallback = "Not enough info";
    private const string UnknownRequirementFallback = "?";
    private static readonly char[] PrefabBlacklistSeparators = [',', ';', '|', '\n', '\r'];
    private static string _recipePreviewPrefabBlacklistRaw = "";
    private static string _piecePreviewPrefabBlacklistRaw = "";
    private static HashSet<string> _recipePreviewPrefabBlacklist = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> _piecePreviewPrefabBlacklist = new(StringComparer.OrdinalIgnoreCase);

    internal static bool ShowUnknownCraftingRecipes => IsOn(SecretRecipesPlugin.ShowUnknownCraftingRecipes);

    internal static bool ShowUnknownBuildPieces => IsOn(SecretRecipesPlugin.ShowUnknownBuildPieces);

    internal static bool RequireStationLevelForUnknownCraftingRecipes => IsOn(SecretRecipesPlugin.RequireStationLevelForUnknownCraftingRecipes);

    internal static bool RequireStationInteractionForUnlock => IsOn(SecretRecipesPlugin.RequireStationInteractionForUnlock);

    internal static string UnknownNameText => SafeText(SecretRecipesPlugin.UnknownNameText, UnknownNameFallback);

    internal static string UnknownDescriptionText => SafeText(SecretRecipesPlugin.UnknownDescriptionText, UnknownDescriptionFallback);

    internal static string UnknownRequirementText => SafeText(SecretRecipesPlugin.UnknownRequirementText, UnknownRequirementFallback);

    internal static void RecordStationInteraction(Player player, CraftingStation station)
    {
        if (player == null || station == null)
        {
            return;
        }

        int level = Math.Max(1, station.GetLevel());
        string key = StationInteractionKey(station.m_name);
        if (player.m_customData.TryGetValue(key, out string existing) &&
            int.TryParse(existing, NumberStyles.Integer, CultureInfo.InvariantCulture, out int knownLevel) &&
            knownLevel >= level)
        {
            return;
        }

        player.m_customData[key] = level.ToString(CultureInfo.InvariantCulture);

        try
        {
            player.UpdateKnownRecipesList();
        }
        catch (Exception ex)
        {
            SecretRecipesPlugin.PluginLogger.LogDebug($"Could not refresh known recipes after station interaction: {ex.Message}");
        }
    }

    internal static bool IsRecipeActuallyKnown(Player player, Recipe recipe)
    {
        if (player == null || recipe == null || recipe.m_item == null)
        {
            return false;
        }

        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllRecipesUnlocked))
        {
            return true;
        }

        if (player.m_noPlacementCost || player.NoCostCheat())
        {
            return true;
        }

        return player.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name);
    }

    internal static bool IsPieceActuallyKnown(Player player, Piece piece)
    {
        if (player == null || piece == null)
        {
            return false;
        }

        if (piece.m_repairPiece || piece.m_removePiece)
        {
            return true;
        }

        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllPiecesUnlocked))
        {
            return true;
        }

        if (player.m_noPlacementCost || player.NoCostCheat())
        {
            return true;
        }

        return player.m_knownRecipes.Contains(piece.m_name);
    }

    internal static bool CanPreviewRecipe(Player player, Recipe recipe)
    {
        if (!ShowUnknownCraftingRecipes || player == null || recipe == null || recipe.m_item == null)
        {
            return false;
        }

        if (IsRecipeActuallyKnown(player, recipe) || !IsRecipeEnabledForPlayer(player, recipe))
        {
            return false;
        }

        if (IsRecipePreviewBlacklisted(recipe))
        {
            return false;
        }

        if (!DlcInstalled(recipe.m_item.m_itemData.m_shared.m_dlc))
        {
            return false;
        }

        if (!PassesCraftFilter(recipe))
        {
            return false;
        }

        bool checkStationLevel = RequireStationLevelForUnknownCraftingRecipes && recipe.GetRequiredStation(1) != null;
        return player.RequiredCraftingStation(recipe, 1, checkStationLevel);
    }

    internal static bool CanPreviewPiece(Player player, Piece piece)
    {
        if (!ShowUnknownBuildPieces || player == null || piece == null)
        {
            return false;
        }

        if (IsPieceActuallyKnown(player, piece) || !IsPieceEnabledForPlayer(player, piece))
        {
            return false;
        }

        if (IsPiecePreviewBlacklisted(piece))
        {
            return false;
        }

        return DlcInstalled(piece.m_dlc);
    }

    internal static bool CanDiscoverRecipe(Player player, Recipe recipe)
    {
        if (player == null || recipe == null || recipe.m_item == null)
        {
            return false;
        }

        if (recipe.m_craftingStation != null && !HasKnownStationLevel(player, recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
        {
            return false;
        }

        if (!DlcInstalled(recipe.m_item.m_itemData.m_shared.m_dlc))
        {
            return false;
        }

        return HasDiscoveredRecipeMaterials(player, recipe);
    }

    internal static bool CanDiscoverPiece(Player player, Piece piece)
    {
        if (player == null || piece == null)
        {
            return false;
        }

        if (piece.m_craftingStation != null && GetKnownStationLevel(player, piece.m_craftingStation.m_name) <= 0)
        {
            return false;
        }

        if (!DlcInstalled(piece.m_dlc))
        {
            return false;
        }

        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (requirement.m_resItem != null && requirement.m_amount > 0 && !IsMaterialKnown(player, requirement))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool KnowsRecipeStationRequirement(Player player, Recipe recipe, int quality)
    {
        if (player == null || recipe == null)
        {
            return false;
        }

        CraftingStation requiredStation = recipe.GetRequiredStation(quality);
        return requiredStation == null || HasKnownStationLevel(player, requiredStation.m_name, recipe.GetRequiredStationLevel(quality));
    }

    internal static bool KnowsPieceStationRequirement(Player player, Piece piece)
    {
        if (player == null || piece == null || piece.m_craftingStation == null)
        {
            return true;
        }

        return GetKnownStationLevel(player, piece.m_craftingStation.m_name) > 0;
    }

    internal static bool IsMaterialKnown(Player player, Piece.Requirement requirement)
    {
        if (player == null || requirement == null || requirement.m_resItem == null)
        {
            return false;
        }

        return player.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name);
    }

    private static bool HasDiscoveredRecipeMaterials(Player player, Recipe recipe)
    {
        bool foundKnownOnlyOneIngredient = false;
        foreach (Piece.Requirement requirement in recipe.m_resources)
        {
            if (requirement.m_resItem == null || requirement.m_amount <= 0)
            {
                continue;
            }

            bool materialKnown = IsMaterialKnown(player, requirement);
            if (recipe.m_requireOnlyOneIngredient)
            {
                foundKnownOnlyOneIngredient |= materialKnown;
            }
            else if (!materialKnown)
            {
                return false;
            }
        }

        return !recipe.m_requireOnlyOneIngredient || foundKnownOnlyOneIngredient;
    }

    private static int GetKnownStationLevel(Player player, string stationName)
    {
        if (player == null || string.IsNullOrEmpty(stationName))
        {
            return 0;
        }

        if (RequireStationInteractionForUnlock)
        {
            return GetInteractedStationLevel(player, stationName);
        }

        return player.m_knownStations.TryGetValue(stationName, out int level) ? level : 0;
    }

    private static bool HasKnownStationLevel(Player player, string stationName, int requiredLevel)
    {
        return GetKnownStationLevel(player, stationName) >= Math.Max(1, requiredLevel);
    }

    private static int GetInteractedStationLevel(Player player, string stationName)
    {
        if (player.m_customData.TryGetValue(StationInteractionKey(stationName), out string value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
        {
            return level;
        }

        return 0;
    }

    private static string StationInteractionKey(string stationName)
    {
        return StationInteractionPrefix + stationName;
    }

    private static bool IsRecipeEnabledForPlayer(Player player, Recipe recipe)
    {
        bool seasonal = player.CurrentSeason != null && player.CurrentSeason.Recipes.Contains(recipe);
        return recipe.m_enabled || seasonal;
    }

    private static bool IsPieceEnabledForPlayer(Player player, Piece piece)
    {
        bool seasonal = player.CurrentSeason != null && player.CurrentSeason.Pieces.Contains(piece.gameObject);
        return piece.m_enabled || seasonal;
    }

    private static bool PassesCraftFilter(Recipe recipe)
    {
        if (Player.s_FilterCraft.Count == 0)
        {
            return true;
        }

        string prefabName = recipe.m_item.name.ToLowerInvariant();
        string sharedName = recipe.m_item.m_itemData.m_shared.m_name.ToLowerInvariant();
        string localizedName = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name).ToLowerInvariant();

        foreach (string filter in Player.s_FilterCraft)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                continue;
            }

            string query = filter.ToLowerInvariant();
            if (prefabName.Contains(query) || sharedName.Contains(query) || localizedName.Contains(query))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRecipePreviewBlacklisted(Recipe recipe)
    {
        HashSet<string> blacklist = GetRecipePreviewPrefabBlacklist();
        return blacklist.Count > 0 && ContainsPrefabName(
            blacklist,
            recipe.name,
            recipe.m_item?.name,
            recipe.m_item?.gameObject?.name,
            recipe.m_item?.m_itemData.m_dropPrefab?.name);
    }

    private static bool IsPiecePreviewBlacklisted(Piece piece)
    {
        HashSet<string> blacklist = GetPiecePreviewPrefabBlacklist();
        return blacklist.Count > 0 && ContainsPrefabName(
            blacklist,
            piece.name,
            piece.gameObject?.name);
    }

    private static HashSet<string> GetRecipePreviewPrefabBlacklist()
    {
        return GetPrefabBlacklist(
            SecretRecipesPlugin.RecipePreviewPrefabBlacklist,
            ref _recipePreviewPrefabBlacklistRaw,
            ref _recipePreviewPrefabBlacklist);
    }

    private static HashSet<string> GetPiecePreviewPrefabBlacklist()
    {
        return GetPrefabBlacklist(
            SecretRecipesPlugin.PiecePreviewPrefabBlacklist,
            ref _piecePreviewPrefabBlacklistRaw,
            ref _piecePreviewPrefabBlacklist);
    }

    private static HashSet<string> GetPrefabBlacklist(ConfigEntry<string> entry, ref string cachedRaw, ref HashSet<string> cached)
    {
        string raw = entry?.Value ?? "";
        if (string.Equals(raw, cachedRaw, StringComparison.Ordinal))
        {
            return cached;
        }

        cachedRaw = raw;
        cached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string token in raw.Split(PrefabBlacklistSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            string prefabName = NormalizePrefabName(token);
            if (!string.IsNullOrEmpty(prefabName))
            {
                cached.Add(prefabName);
            }
        }

        return cached;
    }

    private static bool ContainsPrefabName(HashSet<string> blacklist, params string?[] names)
    {
        foreach (string? name in names)
        {
            string prefabName = NormalizePrefabName(name);
            if (!string.IsNullOrEmpty(prefabName) && blacklist.Contains(prefabName))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePrefabName(string? name)
    {
        string normalized = name?.Trim() ?? "";
        if (normalized.Length == 0)
        {
            return "";
        }

        const string cloneSuffix = "(Clone)";
        if (normalized.EndsWith(cloneSuffix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - cloneSuffix.Length).Trim();
        }

        return normalized;
    }

    private static bool DlcInstalled(string dlc)
    {
        return string.IsNullOrEmpty(dlc) || DLCMan.instance == null || DLCMan.instance.IsDLCInstalled(dlc);
    }

    private static bool IsOn(ConfigEntry<SecretRecipesPlugin.Toggle> entry)
    {
        return entry != null && entry.Value == SecretRecipesPlugin.Toggle.On;
    }

    private static string SafeText(ConfigEntry<string> entry, string fallback)
    {
        if (entry == null || string.IsNullOrEmpty(entry.Value))
        {
            return fallback;
        }

        return entry.Value;
    }
}
