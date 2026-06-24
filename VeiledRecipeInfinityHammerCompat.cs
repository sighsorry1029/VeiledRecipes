using System;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Bootstrap;

namespace VeiledRecipes;

internal static class VeiledRecipeInfinityHammerCompat
{
    private const string PluginGuid = "infinity_hammer";
    private const string BuildMenuToolTypeName = "InfinityHammer.BuildMenuTool";
    private const string HammerSelectTypeName = "InfinityHammer.HammerSelect";
    private const string SelectionTypeName = "InfinityHammer.Selection";
    private const string BaseSelectionTypeName = "InfinityHammer.BaseSelection";

    private static bool _initialized;
    private static bool _loaded;
    private static MethodInfo? _getSelectionMethod;
    private static MethodInfo? _getSelectedPieceMethod;
    private static PropertyInfo? _isToolProperty;
    private static object? _activeCommandSelection;

    internal static void RegisterKnownPieceOverrides()
    {
        VeiledRecipeState.RegisterKnownPieceTypeOverride(BuildMenuToolTypeName);
        VeiledRecipeState.RegisterKnownPieceOverride(IsActiveToolSelectionPiece);
        VeiledRecipeState.RegisterKnownPieceOverride(IsActiveCommandSelectionPiece);
    }

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

    internal static bool IsActiveCommandSelectionPiece(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        EnsureInitialized();
        if (!_loaded || _getSelectionMethod == null || _getSelectedPieceMethod == null)
        {
            return false;
        }

        try
        {
            object? selection = _getSelectionMethod.Invoke(null, null);
            if (selection == null)
            {
                _activeCommandSelection = null;
                return false;
            }

            Piece? selectedPiece = _getSelectedPieceMethod.Invoke(selection, null) as Piece;
            if (selectedPiece == null || !ReferenceEquals(selectedPiece, piece))
            {
                return false;
            }

            if (ReferenceEquals(selection, _activeCommandSelection))
            {
                return true;
            }

            if (!IsHammerSelectCommandOnStack())
            {
                return false;
            }

            _activeCommandSelection = selection;
            return true;
        }
        catch (Exception ex)
        {
            VeiledRecipesPlugin.PluginLogger.LogDebug($"Infinity Hammer command selection check failed: {ex.Message}");
            return false;
        }
    }

    private static bool IsToolSelection(object? selection)
    {
        return selection != null && _isToolProperty?.GetValue(selection) is true;
    }

    private static bool IsHammerSelectCommandOnStack()
    {
        StackTrace stackTrace = new();
        foreach (StackFrame frame in stackTrace.GetFrames())
        {
            string? typeName = frame.GetMethod()?.DeclaringType?.FullName;
            if (typeName?.StartsWith(HammerSelectTypeName, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
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
