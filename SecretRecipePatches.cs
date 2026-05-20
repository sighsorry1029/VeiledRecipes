#nullable disable

using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretRecipes;

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
internal static class CraftingStationInteractPatch
{
    private static void Prefix(CraftingStation __instance, Humanoid user, bool repeat)
    {
        if (repeat || user != Player.m_localPlayer || user is not Player player)
        {
            return;
        }

        if (!__instance.InUseDistance(user) || !__instance.CheckUsable(player, showMessage: false))
        {
            return;
        }

        SecretRecipeState.RecordStationInteraction(player, __instance);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
internal static class PlayerRecipeRequirementsPatch
{
    private static bool Prefix(Player __instance, Recipe recipe, bool discover, ref bool __result)
    {
        if (recipe == null)
        {
            return true;
        }

        if (discover && SecretRecipeState.RequireStationInteractionForUnlock)
        {
            __result = SecretRecipeState.CanDiscoverRecipe(__instance, recipe);
            return false;
        }

        if (!discover && !SecretRecipeState.IsRecipeActuallyKnown(__instance, recipe))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class PlayerPieceRequirementsPatch
{
    private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        if (piece == null)
        {
            return true;
        }

        if (mode == Player.RequirementMode.IsKnown && SecretRecipeState.RequireStationInteractionForUnlock)
        {
            __result = SecretRecipeState.CanDiscoverPiece(__instance, piece);
            return false;
        }

        if (mode != Player.RequirementMode.IsKnown && !SecretRecipeState.IsPieceActuallyKnown(__instance, piece))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
internal static class PlayerGetAvailableRecipesPatch
{
    private static void Postfix(Player __instance, ref List<Recipe> available)
    {
        if (!SecretRecipeState.ShowUnknownCraftingRecipes || ObjectDB.instance == null)
        {
            return;
        }

        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (!available.Contains(recipe) && SecretRecipeState.CanPreviewRecipe(__instance, recipe))
            {
                available.Add(recipe);
            }
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
internal static class InventoryGuiAddRecipeToListPatch
{
    private static void Prefix(Player player, Recipe recipe, ref bool canCraft)
    {
        if (!SecretRecipeState.IsRecipeActuallyKnown(player, recipe))
        {
            canCraft = false;
        }
    }

    private static void Postfix(InventoryGui __instance, Player player, Recipe recipe, ItemDrop.ItemData item)
    {
        if (SecretRecipeState.IsRecipeActuallyKnown(player, recipe))
        {
            return;
        }

        GameObject element = FindRecipeElement(__instance, recipe, item);
        if (element != null)
        {
            MaskRecipeListElement(element, recipe);
        }
    }

    private static GameObject FindRecipeElement(InventoryGui gui, Recipe recipe, ItemDrop.ItemData item)
    {
        for (int i = gui.m_availableRecipes.Count - 1; i >= 0; i--)
        {
            InventoryGui.RecipeDataPair pair = gui.m_availableRecipes[i];
            if (pair.Recipe == recipe && pair.ItemData == item)
            {
                return pair.InterfaceElement;
            }
        }

        return null;
    }

    private static void MaskRecipeListElement(GameObject element, Recipe recipe)
    {
        Image icon = FindComponent<Image>(element.transform, "icon");
        if (icon != null)
        {
            icon.enabled = true;
            if (recipe?.m_item != null)
            {
                icon.sprite = recipe.m_item.m_itemData.GetIcon();
            }
            icon.color = Color.black;
        }

        TMP_Text name = FindComponent<TMP_Text>(element.transform, "name");
        if (name != null)
        {
            name.text = SecretRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        GuiBar durability = FindComponent<GuiBar>(element.transform, "Durability");
        if (durability != null)
        {
            durability.gameObject.SetActive(false);
        }

        TMP_Text quality = FindComponent<TMP_Text>(element.transform, "QualityLevel");
        if (quality != null)
        {
            quality.gameObject.SetActive(false);
        }
    }

    private static T FindComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = root.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
internal static class InventoryGuiUpdateRecipePatch
{
    private static void Postfix(InventoryGui __instance, Player player)
    {
        Recipe recipe = __instance.m_selectedRecipe.Recipe;
        if (recipe == null)
        {
            return;
        }

        if (SecretRecipeState.IsRecipeActuallyKnown(player, recipe))
        {
            __instance.m_recipeIcon.color = Color.white;
            return;
        }

        MaskSelectedRecipe(__instance, player, recipe);
    }

    private static void MaskSelectedRecipe(InventoryGui gui, Player player, Recipe recipe)
    {
        ItemDrop.ItemData itemData = gui.m_selectedRecipe.ItemData;
        int quality = itemData == null ? 1 : itemData.m_quality + 1;
        bool multiCrafting = itemData == null && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick"));
        int craftMultiplier = multiCrafting ? gui.m_multiCraftAmount : 1;
        bool allowedQuality = recipe.m_item != null && quality <= recipe.m_item.m_itemData.m_shared.m_maxQuality;

        gui.m_recipeIcon.enabled = true;
        if (recipe.m_item != null)
        {
            gui.m_recipeIcon.sprite = recipe.m_item.m_itemData.GetIcon();
        }
        gui.m_recipeIcon.color = Color.black;
        gui.m_recipeName.enabled = true;
        gui.m_recipeName.text = SecretRecipeState.UnknownNameText;
        gui.m_recipeDecription.enabled = true;
        gui.m_recipeDecription.text = SecretRecipeState.UnknownDescriptionText;
        gui.m_variantButton.gameObject.SetActive(false);
        gui.m_itemCraftType.gameObject.SetActive(false);
        gui.m_qualityPanel.gameObject.SetActive(false);
        gui.m_craftButton.interactable = false;
        gui.m_craftButton.GetComponent<UITooltip>().m_text = SecretRecipeState.UnknownDescriptionText;

        SetupRecipeRequirements(gui, player, recipe, quality, allowedQuality, craftMultiplier);
        SetupRecipeStationLevel(gui, player, recipe, quality, allowedQuality);
    }

    private static void SetupRecipeStationLevel(InventoryGui gui, Player player, Recipe recipe, int quality, bool allowedQuality)
    {
        CraftingStation requiredStation = recipe.GetRequiredStation(quality);
        if (requiredStation == null || !allowedQuality)
        {
            gui.m_minStationLevelIcon.gameObject.SetActive(false);
            return;
        }

        gui.m_minStationLevelIcon.gameObject.SetActive(true);
        gui.m_minStationLevelText.text = SecretRecipeState.KnowsRecipeStationRequirement(player, recipe, quality)
            ? recipe.GetRequiredStationLevel(quality).ToString()
            : SecretRecipeState.UnknownRequirementText;
        gui.m_minStationLevelText.color = gui.m_minStationLevelBasecolor;
    }

    private static void SetupRecipeRequirements(InventoryGui gui, Player player, Recipe recipe, int quality, bool allowedQuality, int craftMultiplier)
    {
        int slot = 0;
        List<Piece.Requirement> requirements = GetVisibleRequirements(recipe.m_resources, quality);
        int start = GetCyclingStart(requirements.Count, gui.m_recipeRequirementList.Length);

        if (allowedQuality)
        {
            for (int i = start; i < requirements.Count && slot < gui.m_recipeRequirementList.Length; i++)
            {
                SetupRequirement(gui.m_recipeRequirementList[slot].transform, requirements[i], player, craft: true, quality, craftMultiplier);
                slot++;
            }
        }

        for (; slot < gui.m_recipeRequirementList.Length; slot++)
        {
            InventoryGui.HideRequirement(gui.m_recipeRequirementList[slot].transform);
        }
    }

    internal static void SetupRequirement(Transform root, Piece.Requirement requirement, Player player, bool craft, int quality, int multiplier = 1)
    {
        if (requirement == null || requirement.m_resItem == null || requirement.GetAmount(quality) <= 0)
        {
            InventoryGui.HideRequirement(root);
            return;
        }

        if (SecretRecipeState.IsMaterialKnown(player, requirement))
        {
            InventoryGui.SetupRequirement(root, requirement, player, craft, quality, multiplier);
        }
        else
        {
            SetupMaskedRequirement(root, requirement);
        }
    }

    internal static void SetupMaskedRequirement(Transform root, Piece.Requirement requirement)
    {
        Image icon = FindComponent<Image>(root, "res_icon");
        TMP_Text name = FindComponent<TMP_Text>(root, "res_name");
        TMP_Text amount = FindComponent<TMP_Text>(root, "res_amount");
        UITooltip tooltip = root.GetComponent<UITooltip>();

        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.enabled = true;
            if (requirement?.m_resItem != null)
            {
                icon.sprite = requirement.m_resItem.m_itemData.GetIcon();
            }
            icon.color = Color.black;
        }

        if (name != null)
        {
            name.gameObject.SetActive(true);
            name.text = SecretRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        if (amount != null)
        {
            amount.gameObject.SetActive(true);
            amount.text = SecretRecipeState.UnknownRequirementText;
            amount.color = Color.white;
        }

        if (tooltip != null)
        {
            tooltip.m_text = SecretRecipeState.UnknownNameText;
        }
    }

    internal static T FindComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = root.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    private static List<Piece.Requirement> GetVisibleRequirements(Piece.Requirement[] source, int quality)
    {
        List<Piece.Requirement> requirements = new();
        foreach (Piece.Requirement requirement in source)
        {
            if (requirement?.m_resItem != null && requirement.GetAmount(quality) > 0)
            {
                requirements.Add(requirement);
            }
        }

        return requirements;
    }

    private static int GetCyclingStart(int requirementCount, int slotCount)
    {
        if (slotCount <= 0 || requirementCount <= slotCount)
        {
            return 0;
        }

        int pageCount = Mathf.CeilToInt((float)requirementCount / slotCount);
        return (int)Time.fixedTime % pageCount * slotCount;
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
internal static class InventoryGuiOnCraftPressedPatch
{
    private static bool Prefix(InventoryGui __instance)
    {
        Recipe recipe = __instance.m_selectedRecipe.Recipe;
        if (recipe != null && !SecretRecipeState.IsRecipeActuallyKnown(Player.m_localPlayer, recipe))
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
internal static class InventoryGuiDoCraftingPatch
{
    private static bool Prefix(InventoryGui __instance, Player player)
    {
        Recipe recipe = __instance.m_craftRecipe;
        if (recipe != null && !SecretRecipeState.IsRecipeActuallyKnown(player, recipe))
        {
            __instance.m_craftTimer = -1f;
            player.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
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
        foreach (GameObject prefab in __instance.m_pieces)
        {
            Piece piece = prefab == null ? null : prefab.GetComponent<Piece>();
            if (piece == null || !SecretRecipeState.CanPreviewPiece(player, piece) || IsAlreadyAvailable(__instance, piece))
            {
                continue;
            }

            AddAvailablePiece(__instance, piece);
        }
    }

    private static void EnsureAvailablePieceBuckets(PieceTable table)
    {
        while (table.m_availablePieces.Count < 8)
        {
            table.m_availablePieces.Add(new List<Piece>());
        }
    }

    private static bool IsAlreadyAvailable(PieceTable table, Piece piece)
    {
        foreach (List<Piece> category in table.m_availablePieces)
        {
            foreach (Piece existing in category)
            {
                if (Utils.GetPrefabName(existing.gameObject) == Utils.GetPrefabName(piece.gameObject))
                {
                    return true;
                }
            }
        }

        return false;
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
        Image icon = InventoryGuiUpdateRecipePatch.FindComponent<Image>(root, "res_icon");
        TMP_Text name = InventoryGuiUpdateRecipePatch.FindComponent<TMP_Text>(root, "res_name");
        TMP_Text amount = InventoryGuiUpdateRecipePatch.FindComponent<TMP_Text>(root, "res_amount");
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
                    amount.text = Localization.instance.Localize("$menu_none");
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
            __instance.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
            __result = false;
            return false;
        }

        return true;
    }
}
