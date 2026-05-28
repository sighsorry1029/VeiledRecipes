namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
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

    internal static VeiledRecipeVisibilityState GetPieceVisibilityState(Player player, Piece piece)
    {
        if (IsPieceActuallyKnown(player, piece))
        {
            return VeiledRecipeVisibilityState.Known;
        }

        return CanPreviewPiece(player, piece)
            ? VeiledRecipeVisibilityState.UnknownPreview
            : VeiledRecipeVisibilityState.Hidden;
    }

    internal static bool IsUnknownRecipePreview(Player player, Recipe recipe)
    {
        return GetRecipeVisibilityState(player, recipe) == VeiledRecipeVisibilityState.UnknownPreview;
    }

    internal static bool IsUnknownPiecePreview(Player player, Piece piece)
    {
        return GetPieceVisibilityState(player, piece) == VeiledRecipeVisibilityState.UnknownPreview;
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
}
