#nullable disable

using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretRecipes;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class PlayerPieceRequirementsPatch
{
    private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        if (piece == null)
        {
            return true;
        }

        if (mode != Player.RequirementMode.IsKnown && !SecretRecipeState.IsPieceActuallyKnown(__instance, piece))
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
        if (!SecretRecipeState.ShowUnknownBuildPieces || player == null)
        {
            return;
        }

        EnsureAvailablePieceBuckets(__instance);
        HashSet<string> availablePieceNames = GetAvailablePiecePrefabNames(__instance);
        foreach (GameObject prefab in __instance.m_pieces)
        {
            Piece piece = prefab == null ? null : prefab.GetComponent<Piece>();
            string prefabName = piece == null ? "" : Utils.GetPrefabName(piece.gameObject);
            if (piece == null ||
                string.IsNullOrEmpty(prefabName) ||
                availablePieceNames.Contains(prefabName) ||
                !SecretRecipeState.CanPreviewPiece(player, piece))
            {
                continue;
            }

            AddAvailablePiece(__instance, piece);
            availablePieceNames.Add(prefabName);
        }
    }

    private static void EnsureAvailablePieceBuckets(PieceTable table)
    {
        while (table.m_availablePieces.Count < SecretRecipeConstants.PieceCategoryBucketCount)
        {
            table.m_availablePieces.Add(new List<Piece>());
        }
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
        List<Piece> pieces = player?.GetBuildPieces();
        if (pieces == null)
        {
            return;
        }

        for (int i = 0; i < __instance.m_pieceIcons.Count && i < pieces.Count; i++)
        {
            Piece piece = pieces[i];
            if (SecretRecipeState.IsPieceActuallyKnown(player, piece))
            {
                continue;
            }

            Hud.PieceIconData iconData = __instance.m_pieceIcons[i];
            iconData.m_icon.enabled = true;
            iconData.m_icon.sprite = piece.m_icon;
            iconData.m_icon.color = Color.black;
            iconData.m_tooltip.m_text = SecretRecipeState.UnknownNameText;
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

        if (SecretRecipeState.IsPieceActuallyKnown(Player.m_localPlayer, piece))
        {
            __instance.m_buildIcon.color = Color.white;
            return;
        }

        MaskPieceInfo(__instance, Player.m_localPlayer, piece);
    }

    private static void MaskPieceInfo(Hud hud, Player player, Piece piece)
    {
        hud.m_buildSelection.text = SecretRecipeState.UnknownNameText;
        hud.m_pieceDescription.text = SecretRecipeState.UnknownDescriptionText;
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
            InventoryGuiUpdateRecipePatch.SetupRequirement(hud.m_requirementItems[slot].transform, requirement, player, piece.FreeBuildKey() == GlobalKeys.NoCraftCost, 0);
            slot++;
        }

        if (piece.m_craftingStation != null && slot < hud.m_requirementItems.Length)
        {
            hud.m_requirementItems[slot].SetActive(true);
            SetupStationRequirement(hud.m_requirementItems[slot].transform, player, piece);
            slot++;
        }

        for (; slot < hud.m_requirementItems.Length; slot++)
        {
            hud.m_requirementItems[slot].SetActive(false);
        }
    }

    private static void SetupStationRequirement(Transform root, Player player, Piece piece)
    {
        Image icon = InventoryGuiUpdateRecipePatch.FindComponent<Image>(root, SecretRecipeConstants.RequirementIconChild);
        TMP_Text name = InventoryGuiUpdateRecipePatch.FindComponent<TMP_Text>(root, SecretRecipeConstants.RequirementNameChild);
        TMP_Text amount = InventoryGuiUpdateRecipePatch.FindComponent<TMP_Text>(root, SecretRecipeConstants.RequirementAmountChild);
        UITooltip tooltip = root.GetComponent<UITooltip>();
        bool knownStation = SecretRecipeState.KnowsPieceStationRequirement(player, piece);

        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.enabled = true;
            icon.sprite = piece.m_craftingStation.m_icon;
            icon.color = knownStation ? Color.white : Color.black;
        }

        if (name != null)
        {
            name.gameObject.SetActive(true);
            name.text = knownStation ? Localization.instance.Localize(piece.m_craftingStation.m_name) : SecretRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        if (amount != null)
        {
            amount.gameObject.SetActive(true);
            if (knownStation)
            {
                CraftingStation station = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position);
                if (station != null)
                {
                    station.ShowAreaMarker();
                    amount.text = "";
                    amount.color = Color.white;
                }
                else
                {
                    amount.text = Localization.instance.Localize(SecretRecipeConstants.MenuNoneMessage);
                    amount.color = Color.white;
                }
            }
            else
            {
                amount.text = SecretRecipeState.UnknownRequirementText;
                amount.color = Color.white;
            }
        }

        if (tooltip != null)
        {
            tooltip.m_text = knownStation ? piece.m_craftingStation.m_name : SecretRecipeState.UnknownNameText;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
internal static class PlayerSetupPlacementGhostPatch
{
    private static void Postfix(Player __instance)
    {
        Piece selectedPiece = __instance.GetSelectedPiece();
        if (selectedPiece == null || SecretRecipeState.IsPieceActuallyKnown(__instance, selectedPiece))
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
        if (piece != null && !SecretRecipeState.IsPieceActuallyKnown(__instance, piece))
        {
            __instance.Message(MessageHud.MessageType.Center, SecretRecipeConstants.MissingRequirementMessage);
            __result = false;
            return false;
        }

        return true;
    }
}
