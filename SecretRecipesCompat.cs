namespace SecretRecipes;

public static class SecretRecipesCompat
{
    public const string PluginGuid = SecretRecipesPlugin.ModGUID;

    public const string PluginName = SecretRecipesPlugin.ModName;

    public const string PluginVersion = SecretRecipesPlugin.ModVersion;

    public const string Author = SecretRecipesPlugin.Author;

    public static string UnknownNameText => SecretRecipeState.UnknownNameText;

    public static string UnknownDescriptionText => SecretRecipeState.UnknownDescriptionText;

    public static string UnknownRequirementText => SecretRecipeState.UnknownRequirementText;

    public static bool GroupUnknownRecipePreviewsBelowKnownRecipes => SecretRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes;

    public static SecretRecipeVisibilityState GetRecipeVisibilityState(Recipe recipe)
    {
        return GetRecipeVisibilityState(Player.m_localPlayer, recipe);
    }

    public static SecretRecipeVisibilityState GetRecipeVisibilityState(Player player, Recipe recipe)
    {
        return player != null && recipe != null
            ? SecretRecipeState.GetRecipeVisibilityState(player, recipe)
            : SecretRecipeVisibilityState.Hidden;
    }

    public static bool IsUnknownRecipePreview(Recipe recipe)
    {
        return IsUnknownRecipePreview(Player.m_localPlayer, recipe);
    }

    public static bool IsUnknownRecipePreview(Player player, Recipe recipe)
    {
        return player != null && recipe != null && SecretRecipeState.IsUnknownRecipePreview(player, recipe);
    }

    public static bool ShouldMaskRecipe(Recipe recipe)
    {
        return ShouldMaskRecipe(Player.m_localPlayer, recipe);
    }

    public static bool ShouldMaskRecipe(Player player, Recipe recipe)
    {
        return player != null && recipe != null && !SecretRecipeState.IsRecipeActuallyKnown(player, recipe);
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
        return player != null && recipe != null && SecretRecipeState.IsRecipeActuallyKnown(player, recipe);
    }

    public static bool ShouldMaskPiece(Piece piece)
    {
        return ShouldMaskPiece(Player.m_localPlayer, piece);
    }

    public static bool ShouldMaskPiece(Player player, Piece piece)
    {
        return player != null && piece != null && !SecretRecipeState.IsPieceActuallyKnown(player, piece);
    }

    public static bool IsPieceActuallyKnown(Player player, Piece piece)
    {
        return player != null && piece != null && SecretRecipeState.IsPieceActuallyKnown(player, piece);
    }

    public static bool IsMaterialKnown(Piece.Requirement requirement)
    {
        return IsMaterialKnown(Player.m_localPlayer, requirement);
    }

    public static bool IsMaterialKnown(Player player, Piece.Requirement requirement)
    {
        return player != null && requirement != null && SecretRecipeState.IsMaterialKnown(player, requirement);
    }

    public static bool KnowsRecipeStationRequirement(Recipe recipe, int quality)
    {
        return KnowsRecipeStationRequirement(Player.m_localPlayer, recipe, quality);
    }

    public static bool KnowsRecipeStationRequirement(Player player, Recipe recipe, int quality)
    {
        return player != null && recipe != null && SecretRecipeState.KnowsRecipeStationRequirement(player, recipe, quality);
    }

    public static bool KnowsPieceStationRequirement(Piece piece)
    {
        return KnowsPieceStationRequirement(Player.m_localPlayer, piece);
    }

    public static bool KnowsPieceStationRequirement(Player player, Piece piece)
    {
        return player != null && piece != null && SecretRecipeState.KnowsPieceStationRequirement(player, piece);
    }
}
