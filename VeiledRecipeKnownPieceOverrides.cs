using System;
using System.Collections.Generic;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    private static readonly List<Func<Piece, bool>> KnownPieceOverrides = new();
    private static readonly HashSet<string> KnownPiecePrefabOverrides = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> KnownPieceTypeOverrides = new(StringComparer.Ordinal);

    internal static void RegisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        if (predicate == null || KnownPieceOverrides.Contains(predicate))
        {
            return;
        }

        KnownPieceOverrides.Add(predicate);
        VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    internal static void UnregisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        if (predicate == null)
        {
            return;
        }

        if (KnownPieceOverrides.Remove(predicate)) VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    internal static void RegisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        if (KnownPiecePrefabOverrides.Add(prefabName.Trim())) VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    internal static void UnregisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        if (KnownPiecePrefabOverrides.Remove(prefabName.Trim())) VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    internal static void RegisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        if (KnownPieceTypeOverrides.Add(typeName.Trim())) VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    internal static void UnregisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        if (KnownPieceTypeOverrides.Remove(typeName.Trim())) VeiledRecipeBuildUiRefreshPatch.Invalidate();
    }

    private static bool HasKnownPieceOverride(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        string prefabName = Utils.GetPrefabName(piece.gameObject);
        if (!string.IsNullOrEmpty(prefabName) && KnownPiecePrefabOverrides.Contains(prefabName))
        {
            return true;
        }

        string typeName = piece.GetType().FullName;
        if (!string.IsNullOrEmpty(typeName) && KnownPieceTypeOverrides.Contains(typeName))
        {
            return true;
        }

        foreach (Func<Piece, bool> predicate in KnownPieceOverrides)
        {
            try
            {
                if (predicate(piece))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                VeiledRecipesPlugin.PluginLogger.LogDebug($"Known piece override failed for '{prefabName}': {ex.Message}");
            }
        }

        return false;
    }
}
