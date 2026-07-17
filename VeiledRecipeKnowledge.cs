using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    private static readonly HashSet<string> RegisteredBuildPiecePrefabNames = new(StringComparer.OrdinalIgnoreCase);
    private static ObjectDB? _registeredPieceObjectDb;
    private static int _registeredPieceItemCount = -1;

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

        return player.m_knownRecipes.Contains(piece.m_name) || HasKnownPieceOverride(piece);
    }

    internal static bool RequiresRecipeKnowledge(Player player, Recipe recipe)
    {
        return player != null &&
               recipe != null &&
               recipe.m_item != null &&
               !IsRecipeActuallyKnown(player, recipe) &&
               HasDiscoverableCraftingRecipe(player, recipe);
    }

    internal static bool ShouldMaskRecipe(Player player, Recipe recipe, ItemDrop.ItemData? targetItem = null)
    {
        if (player == null || recipe == null || recipe.m_item == null || IsRecipeActuallyKnown(player, recipe))
        {
            return false;
        }

        if (HasDiscoverableCraftingRecipe(player, recipe))
        {
            return true;
        }

        // Upgrade/socket entries supply a target item or wrap one already in inventory.
        return targetItem == null && !IsBackedByOwnedItem(player, recipe);
    }

    internal static bool ShouldMaskPiece(Player player, Piece piece)
    {
        return player != null &&
               piece != null &&
               !IsPieceActuallyKnown(player, piece) &&
               HasRegisteredBuildPiece(piece);
    }

    internal static void RegisterBuildPieceTable(PieceTable table)
    {
        if (table == null)
        {
            return;
        }

        ResetRegisteredBuildPiecesIfObjectDbChanged();
        foreach (GameObject prefab in table.m_pieces)
        {
            if (prefab == null)
            {
                continue;
            }

            string prefabName = Utils.GetPrefabName(prefab);
            if (!string.IsNullOrEmpty(prefabName))
            {
                RegisteredBuildPiecePrefabNames.Add(prefabName);
            }
        }
    }

    private static bool HasDiscoverableCraftingRecipe(Player player, Recipe recipe)
    {
        if (ObjectDB.instance == null || recipe.m_item == null)
        {
            return false;
        }

        string itemName = recipe.m_item.m_itemData.m_shared.m_name;
        foreach (Recipe candidate in ObjectDB.instance.m_recipes)
        {
            if (candidate != null &&
                candidate.m_item != null &&
                candidate.m_item.m_itemData.m_shared.m_name == itemName &&
                IsRecipeEnabledForPlayer(player, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBackedByOwnedItem(Player player, Recipe recipe)
    {
        ItemDrop.ItemData itemData = recipe.m_item.m_itemData;
        return itemData != null && player.GetInventory().ContainsItem(itemData);
    }

    private static bool HasRegisteredBuildPiece(Piece piece)
    {
        RefreshRegisteredBuildPieces();
        string prefabName = Utils.GetPrefabName(piece.gameObject);
        return !string.IsNullOrEmpty(prefabName) && RegisteredBuildPiecePrefabNames.Contains(prefabName);
    }

    private static void RefreshRegisteredBuildPieces()
    {
        ObjectDB? objectDb = ObjectDB.instance;
        ResetRegisteredBuildPiecesIfObjectDbChanged();

        if (objectDb == null || _registeredPieceItemCount == objectDb.m_items.Count)
        {
            return;
        }

        foreach (GameObject itemPrefab in objectDb.m_items)
        {
            ItemDrop? itemDrop = itemPrefab == null ? null : itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop?.m_itemData.m_shared.m_buildPieces != null)
            {
                RegisterBuildPieceTable(itemDrop.m_itemData.m_shared.m_buildPieces);
            }
        }

        _registeredPieceItemCount = objectDb.m_items.Count;
    }

    private static void ResetRegisteredBuildPiecesIfObjectDbChanged()
    {
        ObjectDB? objectDb = ObjectDB.instance;
        if (ReferenceEquals(_registeredPieceObjectDb, objectDb))
        {
            return;
        }

        _registeredPieceObjectDb = objectDb;
        _registeredPieceItemCount = -1;
        RegisteredBuildPiecePrefabNames.Clear();
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
