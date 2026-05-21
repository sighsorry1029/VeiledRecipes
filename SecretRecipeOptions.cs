using BepInEx.Configuration;

namespace SecretRecipes;

internal static partial class SecretRecipeState
{
    internal static bool ShowUnknownCraftingRecipes => IsOn(SecretRecipesPlugin.ShowUnknownCraftingRecipes);

    internal static bool ShowUnknownBuildPieces => IsOn(SecretRecipesPlugin.ShowUnknownBuildPieces);

    internal static bool RequireStationLevelForUnknownCraftingRecipes => IsOn(SecretRecipesPlugin.RequireStationLevelForUnknownCraftingRecipes);

    internal static bool RequireStationInteractionForRecipeUnlock => IsOn(SecretRecipesPlugin.RequireStationInteractionForRecipeUnlock);

    internal static bool EnableStationProximityDiscovery => IsOn(SecretRecipesPlugin.EnableStationProximityDiscovery);

    internal static string UnknownNameText => SafeText(SecretRecipesPlugin.UnknownNameText, SecretRecipeConstants.UnknownNameFallback);

    internal static string UnknownDescriptionText => SafeText(SecretRecipesPlugin.UnknownDescriptionText, SecretRecipeConstants.UnknownDescriptionFallback);

    internal static string UnknownRequirementText => SafeText(SecretRecipesPlugin.UnknownRequirementText, SecretRecipeConstants.UnknownRequirementFallback);

    internal static bool GroupUnknownRecipePreviewsBelowKnownRecipes => SecretRecipesPlugin.GroupUnknownRecipePreviewsBelowKnownRecipes?.Value ?? true;

    internal static bool ShowRecipeUnlockNotifications => SecretRecipesPlugin.ShowRecipeUnlockNotifications?.Value ?? true;

    internal static bool ShowPieceUnlockNotifications => SecretRecipesPlugin.ShowPieceUnlockNotifications?.Value ?? true;

    internal static bool ShowSkillLevelUpNotificationAndEffect => SecretRecipesPlugin.ShowSkillLevelUpNotificationAndEffect?.Value ?? true;

    private static bool IsOn(ConfigEntry<SecretRecipesPlugin.Toggle> entry)
    {
        return entry != null && entry.Value == SecretRecipesPlugin.Toggle.On;
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
