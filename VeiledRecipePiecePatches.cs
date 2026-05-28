using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VeiledRecipes;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class PlayerPieceRequirementsPatch
{
    private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        if (piece == null)
        {
            return true;
        }

        if (mode != Player.RequirementMode.IsKnown && !VeiledRecipeState.IsPieceActuallyKnown(__instance, piece))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
internal static class PieceTableUpdateAvailablePatch
{
    private static void Postfix(PieceTable __instance, Player player)
    {
        if (!VeiledRecipeState.ShowUnknownBuildPieces || player == null)
        {
            return;
        }

        EnsureAvailablePieceBuckets(__instance);
        HashSet<string> availablePieceNames = GetAvailablePiecePrefabNames(__instance);
        foreach (GameObject prefab in __instance.m_pieces)
        {
            Piece? piece = prefab == null ? null : prefab.GetComponent<Piece>();
            string prefabName = piece == null ? "" : Utils.GetPrefabName(piece.gameObject);
            if (piece == null ||
                string.IsNullOrEmpty(prefabName) ||
                availablePieceNames.Contains(prefabName) ||
                VeiledRecipeState.GetPieceVisibilityState(player, piece) != VeiledRecipeVisibilityState.UnknownPreview)
            {
                continue;
            }

            AddAvailablePiece(__instance, piece);
            availablePieceNames.Add(prefabName);
        }
    }

    private static void EnsureAvailablePieceBuckets(PieceTable table)
    {
        int requiredCount = GetRequiredCategoryBucketCount(table);
        while (table.m_availablePieces.Count < requiredCount)
        {
            table.m_availablePieces.Add(new List<Piece>());
        }
    }

    private static int GetRequiredCategoryBucketCount(PieceTable table)
    {
        int count = table.m_availablePieces.Count;
        foreach (GameObject prefab in table.m_pieces)
        {
            Piece? piece = prefab == null ? null : prefab.GetComponent<Piece>();
            if (piece == null || piece.m_category == Piece.PieceCategory.All)
            {
                continue;
            }

            int index = (int)piece.m_category;
            if (index >= 0)
            {
                count = Math.Max(count, index + 1);
            }
        }

        return count;
    }

    private static HashSet<string> GetAvailablePiecePrefabNames(PieceTable table)
    {
        HashSet<string> names = new();
        foreach (List<Piece> category in table.m_availablePieces)
        {
            foreach (Piece existing in category)
            {
                if (existing != null)
                {
                    names.Add(Utils.GetPrefabName(existing.gameObject));
                }
            }
        }

        return names;
    }

    private static void AddAvailablePiece(PieceTable table, Piece piece)
    {
        if (piece.m_category == Piece.PieceCategory.All)
        {
            foreach (List<Piece> category in table.m_availablePieces)
            {
                category.Add(piece);
            }
            return;
        }

        int index = (int)piece.m_category;
        if (index >= 0 && index < table.m_availablePieces.Count)
        {
            table.m_availablePieces[index].Add(piece);
        }
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdatePieceList))]
internal static class HudUpdatePieceListPatch
{
    private static void Postfix(Hud __instance, Player player)
    {
        if (player == null)
        {
            return;
        }

        List<Piece>? pieces = player.GetBuildPieces();
        if (pieces == null)
        {
            return;
        }

        for (int i = 0; i < __instance.m_pieceIcons.Count && i < pieces.Count; i++)
        {
            Piece piece = pieces[i];
            if (VeiledRecipeState.GetPieceVisibilityState(player, piece) == VeiledRecipeVisibilityState.Known)
            {
                continue;
            }

            Hud.PieceIconData iconData = __instance.m_pieceIcons[i];
            iconData.m_icon.enabled = true;
            iconData.m_icon.sprite = piece.m_icon;
            iconData.m_icon.color = Color.black;
            iconData.m_tooltip.m_text = VeiledRecipeState.UnknownNameText;
            iconData.m_upgrade.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
internal static class HudSetupPieceInfoPatch
{
    private static void Postfix(Hud __instance, Piece piece)
    {
        if (piece == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (VeiledRecipeState.GetPieceVisibilityState(Player.m_localPlayer, piece) == VeiledRecipeVisibilityState.Known)
        {
            __instance.m_buildIcon.color = Color.white;
            return;
        }

        MaskPieceInfo(__instance, Player.m_localPlayer, piece);
    }

    private static void MaskPieceInfo(Hud hud, Player player, Piece piece)
    {
        hud.m_buildSelection.text = VeiledRecipeState.UnknownNameText;
        hud.m_pieceDescription.text = VeiledRecipeState.UnknownDescriptionText;
        hud.m_buildIcon.enabled = true;
        hud.m_buildIcon.sprite = piece.m_icon;
        hud.m_buildIcon.color = Color.black;
        hud.m_snappingIcon.enabled = false;

        int slot = 0;
        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (slot >= hud.m_requirementItems.Length)
            {
                break;
            }

            if (requirement?.m_resItem == null || requirement.m_amount <= 0)
            {
                continue;
            }

            hud.m_requirementItems[slot].SetActive(true);
            VeiledRecipeRequirementUi.SetupRequirement(hud.m_requirementItems[slot].transform, requirement, player, piece.FreeBuildKey() == GlobalKeys.NoCraftCost, 0);
            slot++;
        }

        if (piece.m_craftingStation != null && slot < hud.m_requirementItems.Length)
        {
            hud.m_requirementItems[slot].SetActive(true);
            VeiledRecipeRequirementUi.SetupPieceStationRequirement(hud.m_requirementItems[slot].transform, player, piece);
            slot++;
        }

        for (; slot < hud.m_requirementItems.Length; slot++)
        {
            hud.m_requirementItems[slot].SetActive(false);
        }
    }

}

[HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
internal static class PlayerSetupPlacementGhostPatch
{
    private static void Postfix(Player __instance)
    {
        Piece? selectedPiece = __instance.GetSelectedPiece();
        if (selectedPiece == null || VeiledRecipeState.IsPieceActuallyKnown(__instance, selectedPiece))
        {
            return;
        }

        if (__instance.m_placementGhost != null)
        {
            UnityEngine.Object.Destroy(__instance.m_placementGhost);
            __instance.m_placementGhost = null;
        }

        if (__instance.m_placementMarkerInstance != null)
        {
            __instance.m_placementMarkerInstance.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class PlayerTryPlacePiecePatch
{
    private static bool Prefix(Player __instance, Piece piece, ref bool __result)
    {
        if (piece != null && !VeiledRecipeState.IsPieceActuallyKnown(__instance, piece))
        {
            __instance.Message(MessageHud.MessageType.Center, VeiledRecipeConstants.MissingRequirementMessage);
            __result = false;
            return false;
        }

        return true;
    }
}
