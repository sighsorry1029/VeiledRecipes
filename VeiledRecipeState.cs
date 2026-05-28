using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    private static readonly List<Func<Piece, bool>> KnownPieceOverrides = new();
    private static readonly HashSet<string> KnownPiecePrefabOverrides = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> KnownPieceTypeOverrides = new(StringComparer.Ordinal)
    {
        "InfinityHammer.BuildMenuTool"
    };

    internal static void RegisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        if (predicate == null || KnownPieceOverrides.Contains(predicate))
        {
            return;
        }

        KnownPieceOverrides.Add(predicate);
    }

    internal static void UnregisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        if (predicate == null)
        {
            return;
        }

        KnownPieceOverrides.Remove(predicate);
    }

    internal static void RegisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        KnownPiecePrefabOverrides.Add(prefabName.Trim());
    }

    internal static void UnregisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        KnownPiecePrefabOverrides.Remove(prefabName.Trim());
    }

    internal static void RegisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        KnownPieceTypeOverrides.Add(typeName.Trim());
    }

    internal static void UnregisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        KnownPieceTypeOverrides.Remove(typeName.Trim());
    }

    internal static void RecordStationInteraction(Player player, CraftingStation station)
    {
        if (player == null || station == null)
        {
            return;
        }

        if (!EnableStationProximityDiscovery)
        {
            player.AddKnownStation(station);
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
            VeiledRecipesPlugin.PluginLogger.LogDebug($"Could not refresh known recipes after station interaction: {ex.Message}");
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

        if (HasKnownPieceOverride(piece))
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

        if (HasPreviewBlacklistedRequirement(recipe.m_resources))
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

        if (HasPreviewBlacklistedRequirement(piece.m_resources))
        {
            return false;
        }

        if (RequireStationKnowledgeForUnknownBuildPieces && !KnowsPieceStationRequirement(player, piece))
        {
            return false;
        }

        return DlcInstalled(piece.m_dlc);
    }

    internal static VeiledRecipeVisibilityState GetRecipeVisibilityState(Player player, Recipe recipe)
    {
        if (IsRecipeActuallyKnown(player, recipe))
        {
            return VeiledRecipeVisibilityState.Known;
        }

        return CanPreviewRecipe(player, recipe)
            ? VeiledRecipeVisibilityState.UnknownPreview
            : VeiledRecipeVisibilityState.Hidden;
    }

    internal static bool IsUnknownRecipePreview(Player player, Recipe recipe)
    {
        return GetRecipeVisibilityState(player, recipe) == VeiledRecipeVisibilityState.UnknownPreview;
    }

    internal static bool CanDiscoverRecipe(Player player, Recipe recipe)
    {
        if (player == null || recipe == null || recipe.m_item == null)
        {
            return false;
        }

        if (recipe.m_craftingStation != null && !HasKnownRecipeStationLevel(player, recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
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

        if (piece.m_craftingStation != null && GetKnownPieceStationLevel(player, piece.m_craftingStation.m_name) <= 0)
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
        return requiredStation == null || HasKnownRecipeStationLevel(player, requiredStation.m_name, recipe.GetRequiredStationLevel(quality));
    }

    internal static bool KnowsPieceStationRequirement(Player player, Piece piece)
    {
        if (player == null || piece == null || piece.m_craftingStation == null)
        {
            return true;
        }

        return GetKnownPieceStationLevel(player, piece.m_craftingStation.m_name) > 0;
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

    private static int GetKnownRecipeStationLevel(Player player, string stationName)
    {
        if (player == null || string.IsNullOrEmpty(stationName))
        {
            return 0;
        }

        if (RequireStationInteractionForRecipeUnlock)
        {
            return GetInteractedStationLevel(player, stationName);
        }

        return GetKnownPieceStationLevel(player, stationName);
    }

    private static int GetKnownPieceStationLevel(Player player, string stationName)
    {
        if (player == null || string.IsNullOrEmpty(stationName))
        {
            return 0;
        }

        return player.m_knownStations.TryGetValue(stationName, out int level) ? level : 0;
    }

    private static bool HasKnownRecipeStationLevel(Player player, string stationName, int requiredLevel)
    {
        return GetKnownRecipeStationLevel(player, stationName) >= Math.Max(1, requiredLevel);
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
        return VeiledRecipeConstants.StationInteractionPrefix + stationName;
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

    private static bool HasKnownPieceOverride(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        string prefabName = Utils.GetPrefabName(piece.gameObject);
        if (!string.IsNullOrEmpty(prefabName) && KnownPiecePrefabOverrides.Contains(prefabName))
        {
            return true;
        }

        string typeName = piece.GetType().FullName;
        if (!string.IsNullOrEmpty(typeName) && KnownPieceTypeOverrides.Contains(typeName))
        {
            return true;
        }

        foreach (Func<Piece, bool> predicate in KnownPieceOverrides)
        {
            try
            {
                if (predicate(piece))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                VeiledRecipesPlugin.PluginLogger.LogDebug($"Known piece override failed for '{prefabName}': {ex.Message}");
            }
        }

        return false;
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

    private static bool DlcInstalled(string dlc)
    {
        return string.IsNullOrEmpty(dlc) || DLCMan.instance == null || DLCMan.instance.IsDLCInstalled(dlc);
    }

}
