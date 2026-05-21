#nullable disable

using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretRecipes;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
internal static class PlayerRecipeRequirementsPatch
{
    private static bool Prefix(Player __instance, Recipe recipe, bool discover, ref bool __result)
    {
        if (recipe == null)
        {
            return true;
        }

        if (discover && SecretRecipeState.RequireStationInteractionForRecipeUnlock)
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

[HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
internal static class PlayerGetAvailableRecipesPatch
{
    private static void Postfix(Player __instance, ref List<Recipe> available)
    {
        if (!SecretRecipeState.ShowUnknownCraftingRecipes || ObjectDB.instance == null)
        {
            return;
        }

        HashSet<Recipe> availableRecipes = new(available);
        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (availableRecipes.Contains(recipe) || !SecretRecipeState.CanPreviewRecipe(__instance, recipe))
            {
                continue;
            }

            available.Add(recipe);
            availableRecipes.Add(recipe);
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
        Image icon = FindComponent<Image>(element.transform, SecretRecipeConstants.RecipeIconChild);
        if (icon != null)
        {
            icon.enabled = true;
            if (recipe?.m_item != null)
            {
                icon.sprite = recipe.m_item.m_itemData.GetIcon();
            }
            icon.color = Color.black;
        }

        TMP_Text name = FindComponent<TMP_Text>(element.transform, SecretRecipeConstants.RecipeNameChild);
        if (name != null)
        {
            name.text = SecretRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        GuiBar durability = FindComponent<GuiBar>(element.transform, SecretRecipeConstants.DurabilityChild);
        if (durability != null)
        {
            durability.gameObject.SetActive(false);
        }

        TMP_Text quality = FindComponent<TMP_Text>(element.transform, SecretRecipeConstants.QualityLevelChild);
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

[HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList", typeof(List<Recipe>))]
internal static class InventoryGuiUpdateRecipeListPatch
{
    private static void Postfix(InventoryGui __instance)
    {
        Player player = Player.m_localPlayer;
        if (!SecretRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes ||
            player == null ||
            __instance.m_availableRecipes.Count <= 1)
        {
            return;
        }

        List<InventoryGui.RecipeDataPair> visibleRecipes = new();
        List<InventoryGui.RecipeDataPair> unknownPreviews = new();

        foreach (InventoryGui.RecipeDataPair pair in __instance.m_availableRecipes)
        {
            if (SecretRecipeState.IsUnknownRecipePreview(player, pair.Recipe))
            {
                unknownPreviews.Add(pair);
            }
            else
            {
                visibleRecipes.Add(pair);
            }
        }

        if (visibleRecipes.Count == 0 || unknownPreviews.Count == 0)
        {
            return;
        }

        __instance.m_availableRecipes.Clear();
        __instance.m_availableRecipes.AddRange(visibleRecipes);
        __instance.m_availableRecipes.AddRange(unknownPreviews);

        for (int i = 0; i < __instance.m_availableRecipes.Count; i++)
        {
            if (__instance.m_availableRecipes[i].InterfaceElement.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = new Vector2(0f, i * -__instance.m_recipeListSpace);
            }
        }
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
        Image icon = FindComponent<Image>(root, SecretRecipeConstants.RequirementIconChild);
        TMP_Text name = FindComponent<TMP_Text>(root, SecretRecipeConstants.RequirementNameChild);
        TMP_Text amount = FindComponent<TMP_Text>(root, SecretRecipeConstants.RequirementAmountChild);
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
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, SecretRecipeConstants.MissingRequirementMessage);
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
            player.Message(MessageHud.MessageType.Center, SecretRecipeConstants.MissingRequirementMessage);
            return false;
        }

        return true;
    }
}
