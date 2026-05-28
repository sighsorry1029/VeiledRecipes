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
    }

    internal static void UnregisterKnownPieceOverride(Func<Piece, bool> predicate)
    {
        if (predicate == null)
        {
            return;
        }

        KnownPieceOverrides.Remove(predicate);
    }

    internal static void RegisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        KnownPiecePrefabOverrides.Add(prefabName.Trim());
    }

    internal static void UnregisterKnownPiecePrefabOverride(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        KnownPiecePrefabOverrides.Remove(prefabName.Trim());
    }

    internal static void RegisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        KnownPieceTypeOverrides.Add(typeName.Trim());
    }

    internal static void UnregisterKnownPieceTypeOverride(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        KnownPieceTypeOverrides.Remove(typeName.Trim());
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
