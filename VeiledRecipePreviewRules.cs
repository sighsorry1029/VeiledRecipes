namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    internal static VeiledRecipeVisibilityState GetRecipeVisibilityState(Player player, Recipe recipe, ItemDrop.ItemData? targetItem = null)
    {
        if (player == null || recipe == null || recipe.m_item == null)
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (!ShouldMaskRecipe(player, recipe, targetItem))
        {
            return VeiledRecipeVisibilityState.Known;
        }

        if (!ShowUnknownCraftingRecipes || !IsRecipeEnabledForPlayer(player, recipe))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (IsRecipePreviewBlacklisted(recipe))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (HasPreviewBlacklistedRequirement(recipe.m_resources))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (!DlcInstalled(recipe.m_item.m_itemData.m_shared.m_dlc))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (!PassesCraftFilter(recipe))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        bool checkStationLevel = RequireStationLevelForUnknownCraftingRecipes && recipe.GetRequiredStation(1) != null;
        return player.RequiredCraftingStation(recipe, 1, checkStationLevel)
            ? VeiledRecipeVisibilityState.UnknownPreview
            : VeiledRecipeVisibilityState.Hidden;
    }

    internal static VeiledRecipeVisibilityState GetPieceVisibilityState(Player player, Piece piece)
    {
        if (player == null || piece == null)
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (!ShouldMaskPiece(player, piece))
        {
            return VeiledRecipeVisibilityState.Known;
        }

        if (!ShowUnknownBuildPieces || !IsPieceEnabledForPlayer(player, piece))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (IsPiecePreviewBlacklisted(piece))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (HasPreviewBlacklistedRequirement(piece.m_resources))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        if (RequireStationKnowledgeForUnknownBuildPieces && !KnowsPieceStationRequirement(player, piece))
        {
            return VeiledRecipeVisibilityState.Hidden;
        }

        return DlcInstalled(piece.m_dlc)
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

        if (ShouldBypassForAdmin(player))
        {
            return true;
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
