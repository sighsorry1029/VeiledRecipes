using System;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    internal static bool ShouldShowUnlockNotification(string topic)
    {
        return NormalizeToken(topic) switch
        {
            VeiledRecipeConstants.NewRecipeMessage => ShowRecipeUnlockNotifications,
            VeiledRecipeConstants.NewPieceMessage or VeiledRecipeConstants.NewDishMessage => ShowPieceUnlockNotifications,
            _ => true
        };
    }

    internal static bool ShouldShowSkillLevelUpNotification(string message)
    {
        return !IsSkillNotificationMessage(message) || ShowSkillLevelUpNotificationAndEffect;
    }

    internal static bool ShouldShowSkillLevelUpEffect()
    {
        return ShowSkillLevelUpNotificationAndEffect;
    }

    private static bool IsSkillNotificationMessage(string message)
    {
        return NormalizeToken(message).StartsWith(VeiledRecipeConstants.SkillUpMessagePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeToken(string value)
    {
        return (value ?? "").Trim();
    }
}
