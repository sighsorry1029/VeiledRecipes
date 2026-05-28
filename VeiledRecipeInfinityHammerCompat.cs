using System;
using System.Reflection;
using BepInEx.Bootstrap;

namespace VeiledRecipes;

internal static class VeiledRecipeInfinityHammerCompat
{
    private const string PluginGuid = "infinity_hammer";
    private const string SelectionTypeName = "InfinityHammer.Selection";
    private const string BaseSelectionTypeName = "InfinityHammer.BaseSelection";

    private static bool _initialized;
    private static bool _loaded;
    private static MethodInfo? _getSelectionMethod;
    private static MethodInfo? _getSelectedPieceMethod;
    private static PropertyInfo? _isToolProperty;

    internal static bool IsActiveToolSelectionPiece(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        EnsureInitialized();
        if (!_loaded || _getSelectionMethod == null || _getSelectedPieceMethod == null || _isToolProperty == null)
        {
            return false;
        }

        try
        {
            object? selection = _getSelectionMethod.Invoke(null, null);
            if (!IsToolSelection(selection))
            {
                return false;
            }

            Piece? selectedPiece = selection == null ? null : _getSelectedPieceMethod.Invoke(selection, null) as Piece;
            return selectedPiece != null && ReferenceEquals(selectedPiece, piece);
        }
        catch (Exception ex)
        {
            VeiledRecipesPlugin.PluginLogger.LogDebug($"Infinity Hammer active tool selection check failed: {ex.Message}");
            return false;
        }
    }

    private static bool IsToolSelection(object? selection)
    {
        return selection != null && _isToolProperty?.GetValue(selection) is true;
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var pluginInfo))
        {
            return;
        }

        Assembly? assembly = pluginInfo.Instance?.GetType().Assembly;
        Type? selectionType = assembly?.GetType(SelectionTypeName, throwOnError: false);
        Type? baseSelectionType = assembly?.GetType(BaseSelectionTypeName, throwOnError: false);
        _getSelectionMethod = selectionType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
        _getSelectedPieceMethod = baseSelectionType?.GetMethod("GetSelectedPiece", BindingFlags.Public | BindingFlags.Instance);
        _isToolProperty = baseSelectionType?.GetProperty("IsTool", BindingFlags.Public | BindingFlags.Instance);
        _loaded = _getSelectionMethod != null && _getSelectedPieceMethod != null && _isToolProperty != null;
    }
}
