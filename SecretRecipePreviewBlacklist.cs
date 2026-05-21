using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace SecretRecipes;

internal static partial class SecretRecipeState
{
    private static string _recipePreviewPrefabBlacklistRaw = "";
    private static string _piecePreviewPrefabBlacklistRaw = "";
    private static string _requirementPreviewPrefabBlacklistRaw = "";
    private static HashSet<string> _recipePreviewPrefabBlacklist = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> _piecePreviewPrefabBlacklist = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> _requirementPreviewPrefabBlacklist = new(StringComparer.OrdinalIgnoreCase);

    private static bool IsRecipePreviewBlacklisted(Recipe recipe)
    {
        HashSet<string> blacklist = GetRecipePreviewPrefabBlacklist();
        return blacklist.Count > 0 && ContainsPrefabName(
            blacklist,
            recipe.name,
            recipe.m_item?.name,
            recipe.m_item?.gameObject?.name,
            recipe.m_item?.m_itemData.m_dropPrefab?.name);
    }

    private static bool IsPiecePreviewBlacklisted(Piece piece)
    {
        HashSet<string> blacklist = GetPiecePreviewPrefabBlacklist();
        return blacklist.Count > 0 && ContainsPrefabName(
            blacklist,
            piece.name,
            piece.gameObject?.name);
    }

    private static HashSet<string> GetRecipePreviewPrefabBlacklist()
    {
        return GetPrefabBlacklist(
            SecretRecipesPlugin.RecipePreviewPrefabBlacklist,
            ref _recipePreviewPrefabBlacklistRaw,
            ref _recipePreviewPrefabBlacklist);
    }

    private static HashSet<string> GetPiecePreviewPrefabBlacklist()
    {
        return GetPrefabBlacklist(
            SecretRecipesPlugin.PiecePreviewPrefabBlacklist,
            ref _piecePreviewPrefabBlacklistRaw,
            ref _piecePreviewPrefabBlacklist);
    }

    private static bool HasPreviewBlacklistedRequirement(Piece.Requirement[] requirements)
    {
        HashSet<string> blacklist = GetRequirementPreviewPrefabBlacklist();
        if (blacklist.Count == 0 || requirements == null)
        {
            return false;
        }

        foreach (Piece.Requirement requirement in requirements)
        {
            if (requirement?.m_resItem == null || requirement.GetAmount(1) <= 0)
            {
                continue;
            }

            if (ContainsPrefabName(
                    blacklist,
                    requirement.m_resItem.name,
                    requirement.m_resItem.gameObject?.name,
                    requirement.m_resItem.m_itemData.m_dropPrefab?.name))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> GetRequirementPreviewPrefabBlacklist()
    {
        return GetPrefabBlacklist(
            SecretRecipesPlugin.RequirementPreviewPrefabBlacklist,
            ref _requirementPreviewPrefabBlacklistRaw,
            ref _requirementPreviewPrefabBlacklist);
    }

    private static HashSet<string> GetPrefabBlacklist(ConfigEntry<string> entry, ref string cachedRaw, ref HashSet<string> cached)
    {
        string raw = entry?.Value ?? "";
        if (string.Equals(raw, cachedRaw, StringComparison.Ordinal))
        {
            return cached;
        }

        cachedRaw = raw;
        cached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string token in raw.Split(SecretRecipeConstants.PrefabBlacklistSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            string prefabName = NormalizePrefabName(token);
            if (!string.IsNullOrEmpty(prefabName))
            {
                cached.Add(prefabName);
            }
        }

        return cached;
    }

    private static bool ContainsPrefabName(HashSet<string> blacklist, params string?[] names)
    {
        foreach (string? name in names)
        {
            string prefabName = NormalizePrefabName(name);
            if (!string.IsNullOrEmpty(prefabName) && blacklist.Contains(prefabName))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePrefabName(string? name)
    {
        string normalized = name?.Trim() ?? "";
        if (normalized.Length == 0)
        {
            return "";
        }

        if (normalized.EndsWith(SecretRecipeConstants.CloneSuffix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - SecretRecipeConstants.CloneSuffix.Length).Trim();
        }

        return normalized;
    }
}
