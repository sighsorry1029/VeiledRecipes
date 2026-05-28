using System;

namespace VeiledRecipes;

public static class VeiledRecipesCompat
{
    public const string PluginGuid = VeiledRecipesPlugin.ModGUID;

    public const string PluginName = VeiledRecipesPlugin.ModName;

    public const string PluginVersion = VeiledRecipesPlugin.ModVersion;

    public const string Author = VeiledRecipesPlugin.Author;

    public static string UnknownNameText => VeiledRecipeState.UnknownNameText;

    public static string UnknownDescriptionText => VeiledRecipeState.UnknownDescriptionText;

    public static string UnknownRequirementText => VeiledRecipeState.UnknownRequirementText;

    public static bool GroupUnknownRecipePreviewsBelowKnownRecipes => VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes;

    public static VeiledRecipeVisibilityState GetRecipeVisibilityState(Recipe recipe)
    {
        return GetRecipeVisibilityState(Player.m_localPlayer, recipe);
    }

    public static VeiledRecipeVisibilityState GetRecipeVisibilityState(Player player, Recipe recipe)
    {
        return player != null && recipe != null
            ? VeiledRecipeState.GetRecipeVisibilityState(player, recipe)
            : VeiledRecipeVisibilityState.Hidden;
    }

    public static bool IsUnknownRecipePreview(Recipe recipe)
    {
        return IsUnknownRecipePreview(Player.m_localPlayer, recipe);
    }

    public static bool IsUnknownRecipePreview(Player player, Recipe recipe)
    {
        return player != null && recipe != null && VeiledRecipeState.IsUnknownRecipePreview(player, recipe);
    }

    public static bool ShouldMaskRecipe(Recipe recipe)
    {
        return ShouldMaskRecipe(Player.m_localPlayer, recipe);
    }

    public static bool ShouldMaskRecipe(Player player, Recipe recipe)
    {
        return player != null && recipe != null && !VeiledRecipeState.IsRecipeActuallyKnown(player, recipe);
    }

    public static bool ShouldMaskRecipePair(InventoryGui.RecipeDataPair pair)
    {
        return ShouldMaskRecipePair(Player.m_localPlayer, pair);
    }

    public static bool ShouldMaskRecipePair(Player player, InventoryGui.RecipeDataPair pair)
    {
        return ShouldMaskRecipe(player, pair.Recipe);
    }

    public static bool IsRecipeActuallyKnown(Player player, Recipe recipe)
    {
        return player != null && recipe != null && VeiledRecipeState.IsRecipeActuallyKnown(player, recipe);
    }

    public static bool ShouldMaskPiece(Piece piece)
    {
        return ShouldMaskPiece(Player.m_localPlayer, piece);
    }

    public static bool ShouldMaskPiece(Player player, Piece piece)
    {
        return player != null && piece != null && !VeiledRecipeState.IsPieceActuallyKnown(player, piece);
    }

    public static void RegisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        VeiledRecipeState.RegisterKnownPieceOverride(predicate);
    }

    public static void UnregisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        VeiledRecipeState.UnregisterKnownPieceOverride(predicate);
    }

    public static void RegisterKnownPiecePrefabOverride(string prefabName)
    {
        VeiledRecipeState.RegisterKnownPiecePrefabOverride(prefabName);
    }

    public static void UnregisterKnownPiecePrefabOverride(string prefabName)
    {
        VeiledRecipeState.UnregisterKnownPiecePrefabOverride(prefabName);
    }

    public static void RegisterKnownPieceTypeOverride(string typeName)
    {
        VeiledRecipeState.RegisterKnownPieceTypeOverride(typeName);
    }

    public static void UnregisterKnownPieceTypeOverride(string typeName)
    {
        VeiledRecipeState.UnregisterKnownPieceTypeOverride(typeName);
    }

    public static bool IsPieceActuallyKnown(Player player, Piece piece)
    {
        return player != null && piece != null && VeiledRecipeState.IsPieceActuallyKnown(player, piece);
    }

    public static bool IsMaterialKnown(Piece.Requirement requirement)
    {
        return IsMaterialKnown(Player.m_localPlayer, requirement);
    }

    public static bool IsMaterialKnown(Player player, Piece.Requirement requirement)
    {
        return player != null && requirement != null && VeiledRecipeState.IsMaterialKnown(player, requirement);
    }

    public static bool KnowsRecipeStationRequirement(Recipe recipe, int quality)
    {
        return KnowsRecipeStationRequirement(Player.m_localPlayer, recipe, quality);
    }

    public static bool KnowsRecipeStationRequirement(Player player, Recipe recipe, int quality)
    {
        return player != null && recipe != null && VeiledRecipeState.KnowsRecipeStationRequirement(player, recipe, quality);
    }

    public static bool KnowsPieceStationRequirement(Piece piece)
    {
        return KnowsPieceStationRequirement(Player.m_localPlayer, piece);
    }

    public static bool KnowsPieceStationRequirement(Player player, Piece piece)
    {
        return player != null && piece != null && VeiledRecipeState.KnowsPieceStationRequirement(player, piece);
    }
}
