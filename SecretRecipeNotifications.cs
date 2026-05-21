using System;

namespace SecretRecipes;

internal static partial class SecretRecipeState
{
    internal static bool ShouldShowUnlockNotification(string topic)
    {
        return NormalizeToken(topic) switch
        {
            SecretRecipeConstants.NewRecipeMessage => ShowRecipeUnlockNotifications,
            SecretRecipeConstants.NewPieceMessage or SecretRecipeConstants.NewDishMessage => ShowPieceUnlockNotifications,
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
        return NormalizeToken(message).StartsWith(SecretRecipeConstants.SkillUpMessagePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeToken(string value)
    {
        return (value ?? "").Trim();
    }
}
