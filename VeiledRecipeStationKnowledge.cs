using System;
using System.Globalization;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
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
}
