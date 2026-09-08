using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VeiledRecipes;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
internal static class PlayerRecipeRequirementsPatch
{
    private static bool Prefix(Player __instance, Recipe recipe, bool discover, ref bool __result)
    {
        if (recipe == null)
        {
            return true;
        }

        if (VeiledRecipeState.ShouldBypassForAdmin(__instance))
        {
            return true;
        }

        if (discover && VeiledRecipeState.RequireStationInteractionForRecipeUnlock)
        {
            __result = VeiledRecipeState.CanDiscoverRecipe(__instance, recipe);
            return false;
        }

        if (!discover && VeiledRecipeState.RequiresRecipeKnowledge(__instance, recipe))
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
        if (!VeiledRecipeState.ShowUnknownCraftingRecipes || ObjectDB.instance == null)
        {
            return;
        }

        HashSet<Recipe> availableRecipes = new(available);
        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (availableRecipes.Contains(recipe) ||
                VeiledRecipeState.GetRecipeVisibilityState(__instance, recipe) != VeiledRecipeVisibilityState.UnknownPreview)
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
    private static void Prefix(Player player, Recipe recipe, ItemDrop.ItemData item, ref bool canCraft, out bool __state)
    {
        __state = VeiledRecipeState.ShouldMaskRecipe(player, recipe, item);
        if (__state)
        {
            canCraft = false;
        }
    }

    private static void Postfix(InventoryGui __instance, Recipe recipe, ItemDrop.ItemData item, bool __state)
    {
        if (!__state)
        {
            return;
        }

        GameObject? element = FindRecipeElement(__instance, recipe, item);
        if (element != null)
        {
            MaskRecipeListElement(element, recipe);
        }
    }

    private static GameObject? FindRecipeElement(InventoryGui gui, Recipe recipe, ItemDrop.ItemData item)
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

    internal static void MaskRecipeListElement(GameObject element, Recipe recipe)
    {
        Image? icon = VeiledRecipeRequirementUi.FindComponent<Image>(element.transform, VeiledRecipeConstants.RecipeIconChild);
        if (icon != null)
        {
            icon.enabled = true;
            if (recipe?.m_item != null)
            {
                icon.sprite = recipe.m_item.m_itemData.GetIcon();
            }
            icon.color = Color.black;
        }

        TMP_Text? name = VeiledRecipeRequirementUi.FindComponent<TMP_Text>(element.transform, VeiledRecipeConstants.RecipeNameChild);
        if (name != null)
        {
            name.text = VeiledRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        GuiBar? durability = VeiledRecipeRequirementUi.FindComponent<GuiBar>(element.transform, VeiledRecipeConstants.DurabilityChild);
        if (durability != null)
        {
            durability.gameObject.SetActive(false);
        }

        TMP_Text? quality = VeiledRecipeRequirementUi.FindComponent<TMP_Text>(element.transform, VeiledRecipeConstants.QualityLevelChild);
        if (quality != null)
        {
            quality.gameObject.SetActive(false);
        }
    }

}

[HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList", typeof(List<Recipe>))]
internal static class InventoryGuiUpdateRecipeListPatch
{
    [HarmonyAfter(VeiledRecipeAaaCraftingCompat.PluginGuid)]
    private static void Postfix(InventoryGui __instance)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        VeiledRecipeAaaCraftingCompat.MaskRecipeList(__instance, player);
        if (!VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes ||
            __instance.m_availableRecipes.Count <= 1)
        {
            return;
        }

        List<InventoryGui.RecipeDataPair> visibleRecipes = new();
        List<InventoryGui.RecipeDataPair> unknownPreviews = new();

        foreach (InventoryGui.RecipeDataPair pair in __instance.m_availableRecipes)
        {
            if (VeiledRecipeState.GetRecipeVisibilityState(player, pair.Recipe, pair.ItemData) == VeiledRecipeVisibilityState.UnknownPreview)
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

        LayoutGroup? layout = __instance.m_recipeListRoot.GetComponent<LayoutGroup>();
        bool usesLayout = layout != null && layout.isActiveAndEnabled;
        for (int i = 0; i < __instance.m_availableRecipes.Count; i++)
        {
            GameObject? element = __instance.m_availableRecipes[i].InterfaceElement;
            if (element == null)
            {
                continue;
            }

            if (usesLayout)
            {
                element.transform.SetSiblingIndex(i);
            }
            else if (element.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = new Vector2(0f, i * -__instance.m_recipeListSpace);
            }
        }

        if (usesLayout)
        {
            LayoutRebuilder.MarkLayoutForRebuild(__instance.m_recipeListRoot);
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
internal static class InventoryGuiUpdateRecipePatch
{
    [HarmonyAfter(VeiledRecipeAaaCraftingCompat.PluginGuid)]
    private static void Postfix(InventoryGui __instance, Player player)
    {
        Recipe? recipe = __instance.m_selectedRecipe.Recipe;
        if (recipe == null)
        {
            return;
        }

        if (!VeiledRecipeState.ShouldMaskRecipe(player, recipe, __instance.m_selectedRecipe.ItemData))
        {
            __instance.m_recipeIcon.color = Color.white;
            return;
        }

        MaskSelectedRecipe(__instance, player, recipe);
    }

    private static void MaskSelectedRecipe(InventoryGui gui, Player player, Recipe recipe)
    {
        ItemDrop.ItemData? itemData = gui.m_selectedRecipe.ItemData;
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
        gui.m_recipeName.text = VeiledRecipeState.UnknownNameText;
        gui.m_recipeDecription.enabled = true;
        gui.m_recipeDecription.text = VeiledRecipeState.UnknownDescriptionText;
        gui.m_variantButton.gameObject.SetActive(false);
        gui.m_itemCraftType.gameObject.SetActive(false);
        gui.m_qualityPanel.gameObject.SetActive(false);
        gui.m_craftButton.interactable = false;
        gui.m_craftButton.GetComponent<UITooltip>().m_text = VeiledRecipeState.UnknownDescriptionText;

        SetupRecipeRequirements(gui, player, recipe, quality, allowedQuality, craftMultiplier);
        SetupRecipeStationLevel(gui, player, recipe, quality, allowedQuality);
    }

    private static void SetupRecipeStationLevel(InventoryGui gui, Player player, Recipe recipe, int quality, bool allowedQuality)
    {
        CraftingStation? requiredStation = recipe.GetRequiredStation(quality);
        if (requiredStation == null || !allowedQuality)
        {
            gui.m_minStationLevelIcon.gameObject.SetActive(false);
            return;
        }

        gui.m_minStationLevelIcon.gameObject.SetActive(true);
        gui.m_minStationLevelText.text = VeiledRecipeState.KnowsRecipeStationRequirement(player, recipe, quality)
            ? recipe.GetRequiredStationLevel(quality).ToString()
            : VeiledRecipeState.UnknownRequirementText;
        gui.m_minStationLevelText.color = gui.m_minStationLevelBasecolor;
    }

    private static void SetupRecipeRequirements(InventoryGui gui, Player player, Recipe recipe, int quality, bool allowedQuality, int craftMultiplier)
    {
        int slot = 0;
        Piece.Requirement[]? requirements = recipe.m_resources;
        if (allowedQuality && requirements != null && gui.m_recipeRequirementList.Length > 0)
        {
            // Count first so cycling still uses the number of visible requirements, without a temporary list.
            int requirementCount = 0;
            foreach (Piece.Requirement requirement in requirements)
            {
                if (requirement?.m_resItem != null && requirement.GetAmount(quality) > 0)
                {
                    requirementCount++;
                }
            }

            int start = VeiledRecipeRequirementUi.GetCyclingStart(requirementCount, gui.m_recipeRequirementList.Length);
            foreach (Piece.Requirement requirement in requirements)
            {
                if (slot >= gui.m_recipeRequirementList.Length)
                {
                    break;
                }

                if (requirement?.m_resItem == null || requirement.GetAmount(quality) <= 0)
                {
                    continue;
                }

                if (start > 0)
                {
                    start--;
                    continue;
                }

                VeiledRecipeRequirementUi.SetupRequirement(gui.m_recipeRequirementList[slot].transform, requirement, player, craft: true, quality, craftMultiplier);
                slot++;
            }
        }

        for (; slot < gui.m_recipeRequirementList.Length; slot++)
        {
            InventoryGui.HideRequirement(gui.m_recipeRequirementList[slot].transform);
        }
    }

}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
internal static class InventoryGuiOnCraftPressedPatch
{
    private static bool Prefix(InventoryGui __instance)
    {
        Recipe? recipe = __instance.m_selectedRecipe.Recipe;
        Player? player = Player.m_localPlayer;
        if (recipe != null &&
            player != null &&
            VeiledRecipeState.ShouldMaskRecipe(player, recipe, __instance.m_selectedRecipe.ItemData))
        {
            player.Message(MessageHud.MessageType.Center, VeiledRecipeConstants.MissingRequirementMessage);
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
        Recipe? recipe = __instance.m_craftRecipe;
        if (recipe != null && VeiledRecipeState.ShouldMaskRecipe(player, recipe, __instance.m_craftUpgradeItem))
        {
            __instance.m_craftTimer = -1f;
            player.Message(MessageHud.MessageType.Center, VeiledRecipeConstants.MissingRequirementMessage);
            return false;
        }

        return true;
    }
}
