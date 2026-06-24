using BepInEx.Configuration;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    internal static bool EnableAdminBypass => VeiledRecipesPlugin.EnableAdminBypass?.Value ?? false;

    internal static bool ShowUnknownCraftingRecipes => IsOn(VeiledRecipesPlugin.ShowUnknownCraftingRecipes);

    internal static bool ShowUnknownBuildPieces => IsOn(VeiledRecipesPlugin.ShowUnknownBuildPieces);

    internal static bool RequireStationLevelForUnknownCraftingRecipes => IsOn(VeiledRecipesPlugin.RequireStationLevelForUnknownCraftingRecipes);

    internal static bool RequireStationKnowledgeForUnknownBuildPieces => IsOn(VeiledRecipesPlugin.RequireStationKnowledgeForUnknownBuildPieces);

    internal static bool RequireStationInteractionForRecipeUnlock => IsOn(VeiledRecipesPlugin.RequireStationInteractionForRecipeUnlock);

    internal static bool EnableStationProximityDiscovery => IsOn(VeiledRecipesPlugin.EnableStationProximityDiscovery);

    internal static string UnknownNameText => SafeText(VeiledRecipesPlugin.UnknownNameText, VeiledRecipeConstants.UnknownNameFallback);

    internal static string UnknownDescriptionText => SafeText(VeiledRecipesPlugin.UnknownDescriptionText, VeiledRecipeConstants.UnknownDescriptionFallback);

    internal static string UnknownRequirementText => SafeText(VeiledRecipesPlugin.UnknownRequirementText, VeiledRecipeConstants.UnknownRequirementFallback);

    internal static bool GroupUnknownRecipePreviewsBelowKnownRecipes => VeiledRecipesPlugin.GroupUnknownRecipePreviewsBelowKnownRecipes?.Value ?? true;

    internal static bool ShowRecipeUnlockNotifications => VeiledRecipesPlugin.ShowRecipeUnlockNotifications?.Value ?? true;

    internal static bool ShowPieceUnlockNotifications => VeiledRecipesPlugin.ShowPieceUnlockNotifications?.Value ?? true;

    internal static bool ShowSkillLevelUpNotificationAndEffect => VeiledRecipesPlugin.ShowSkillLevelUpNotificationAndEffect?.Value ?? true;

    private static bool IsOn(ConfigEntry<VeiledRecipesPlugin.Toggle> entry)
    {
        return entry != null && entry.Value == VeiledRecipesPlugin.Toggle.On;
    }

    private static string SafeText(ConfigEntry<string> entry, string fallback)
    {
        if (entry == null || string.IsNullOrEmpty(entry.Value))
        {
            return fallback;
        }

        return entry.Value;
    }
}
