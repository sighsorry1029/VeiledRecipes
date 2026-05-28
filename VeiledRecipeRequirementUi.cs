using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VeiledRecipes;

internal static class VeiledRecipeRequirementUi
{
    internal static void SetupRequirement(Transform root, Piece.Requirement? requirement, Player player, bool craft, int quality, int multiplier = 1)
    {
        if (requirement == null || requirement.m_resItem == null || requirement.GetAmount(quality) <= 0)
        {
            InventoryGui.HideRequirement(root);
            return;
        }

        if (VeiledRecipeState.IsMaterialKnown(player, requirement))
        {
            InventoryGui.SetupRequirement(root, requirement, player, craft, quality, multiplier);
            return;
        }

        SetupMaskedRequirement(root, requirement);
    }

    internal static void SetupMaskedRequirement(Transform root, Piece.Requirement requirement)
    {
        Image? icon = FindComponent<Image>(root, VeiledRecipeConstants.RequirementIconChild);
        TMP_Text? name = FindComponent<TMP_Text>(root, VeiledRecipeConstants.RequirementNameChild);
        TMP_Text? amount = FindComponent<TMP_Text>(root, VeiledRecipeConstants.RequirementAmountChild);
        UITooltip? tooltip = root.GetComponent<UITooltip>();

        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.enabled = true;
            icon.sprite = requirement.m_resItem.m_itemData.GetIcon();
            icon.color = Color.black;
        }

        if (name != null)
        {
            name.gameObject.SetActive(true);
            name.text = VeiledRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        if (amount != null)
        {
            amount.gameObject.SetActive(true);
            amount.text = VeiledRecipeState.UnknownRequirementText;
            amount.color = Color.white;
        }

        if (tooltip != null)
        {
            tooltip.m_text = VeiledRecipeState.UnknownNameText;
        }
    }

    internal static void SetupPieceStationRequirement(Transform root, Player player, Piece piece)
    {
        if (piece.m_craftingStation == null)
        {
            InventoryGui.HideRequirement(root);
            return;
        }

        Image? icon = FindComponent<Image>(root, VeiledRecipeConstants.RequirementIconChild);
        TMP_Text? name = FindComponent<TMP_Text>(root, VeiledRecipeConstants.RequirementNameChild);
        TMP_Text? amount = FindComponent<TMP_Text>(root, VeiledRecipeConstants.RequirementAmountChild);
        UITooltip? tooltip = root.GetComponent<UITooltip>();
        bool knownStation = VeiledRecipeState.KnowsPieceStationRequirement(player, piece);

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
            name.text = knownStation ? Localization.instance.Localize(piece.m_craftingStation.m_name) : VeiledRecipeState.UnknownNameText;
            name.color = Color.white;
        }

        if (amount != null)
        {
            amount.gameObject.SetActive(true);
            SetupStationAmount(amount, player, piece, knownStation);
        }

        if (tooltip != null)
        {
            tooltip.m_text = knownStation ? piece.m_craftingStation.m_name : VeiledRecipeState.UnknownNameText;
        }
    }

    internal static List<Piece.Requirement> GetVisibleRequirements(Piece.Requirement[]? source, int quality)
    {
        List<Piece.Requirement> requirements = new();
        if (source == null)
        {
            return requirements;
        }

        foreach (Piece.Requirement requirement in source)
        {
            if (requirement?.m_resItem != null && requirement.GetAmount(quality) > 0)
            {
                requirements.Add(requirement);
            }
        }

        return requirements;
    }

    internal static int GetCyclingStart(int requirementCount, int slotCount)
    {
        if (slotCount <= 0 || requirementCount <= slotCount)
        {
            return 0;
        }

        int pageCount = Mathf.CeilToInt((float)requirementCount / slotCount);
        return (int)Time.fixedTime % pageCount * slotCount;
    }

    internal static T? FindComponent<T>(Transform root, string childName) where T : Component
    {
        Transform? child = root.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    private static void SetupStationAmount(TMP_Text amount, Player player, Piece piece, bool knownStation)
    {
        if (!knownStation)
        {
            amount.text = VeiledRecipeState.UnknownRequirementText;
            amount.color = Color.white;
            return;
        }

        CraftingStation? station = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position);
        if (station != null)
        {
            station.ShowAreaMarker();
            amount.text = "";
            amount.color = Color.white;
            return;
        }

        amount.text = Localization.instance.Localize(VeiledRecipeConstants.MenuNoneMessage);
        amount.color = Color.white;
    }
}
