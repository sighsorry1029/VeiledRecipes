using System;
using HarmonyLib;

namespace VeiledRecipes;

[HarmonyPatch(typeof(MessageHud), nameof(MessageHud.QueueUnlockMsg))]
internal static class MessageHudQueueUnlockMsgPatch
{
    private static bool Prefix(MessageHud __instance, string topic, string description)
    {
        if (ShouldShowUnlockNotification(topic))
        {
            return true;
        }

        __instance.AddLog($"{topic}: {description}");
        return false;
    }

    private static bool ShouldShowUnlockNotification(string topic)
    {
        return (topic ?? "").Trim() switch
        {
            VeiledRecipeConstants.NewRecipeMessage => VeiledRecipeState.ShowRecipeUnlockNotifications,
            VeiledRecipeConstants.NewPieceMessage or VeiledRecipeConstants.NewDishMessage => VeiledRecipeState.ShowPieceUnlockNotifications,
            _ => true
        };
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
internal static class PlayerSkillLevelUpEffectsPatch
{
    private static bool Prefix()
    {
        return VeiledRecipeState.ShowSkillLevelUpNotificationAndEffect;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Message))]
internal static class PlayerSkillNotificationAlarmPatch
{
    private static bool Prefix(string msg)
    {
        return VeiledRecipeState.ShowSkillLevelUpNotificationAndEffect ||
               !(msg ?? "").Trim().StartsWith(VeiledRecipeConstants.SkillUpMessagePrefix, StringComparison.OrdinalIgnoreCase);
    }
}
