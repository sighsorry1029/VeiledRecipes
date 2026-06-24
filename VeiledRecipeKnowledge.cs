namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    internal static bool IsRecipeActuallyKnown(Player player, Recipe recipe)
    {
        if (player == null || recipe == null || recipe.m_item == null)
        {
            return false;
        }

        if (ShouldBypassForAdmin(player))
        {
            return true;
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

        if (ShouldBypassForAdmin(player))
        {
            return true;
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

    internal static bool IsMaterialKnown(Player player, Piece.Requirement requirement)
    {
        if (player == null || requirement == null || requirement.m_resItem == null)
        {
            return false;
        }

        if (ShouldBypassForAdmin(player))
        {
            return true;
        }

        return player.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name);
    }

    private static bool DlcInstalled(string dlc)
    {
        return string.IsNullOrEmpty(dlc) || DLCMan.instance == null || DLCMan.instance.IsDLCInstalled(dlc);
    }
}
