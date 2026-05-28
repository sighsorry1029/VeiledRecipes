using HarmonyLib;

namespace VeiledRecipes;

[HarmonyPatch(typeof(MessageHud), nameof(MessageHud.QueueUnlockMsg))]
internal static class MessageHudQueueUnlockMsgPatch
{
    private static bool Prefix(MessageHud __instance, string topic, string description)
    {
        if (VeiledRecipeState.ShouldShowUnlockNotification(topic))
        {
            return true;
        }

        __instance.AddLog($"{topic}: {description}");
        return false;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
internal static class PlayerSkillLevelUpEffectsPatch
{
    private static bool Prefix()
    {
        return VeiledRecipeState.ShouldShowSkillLevelUpEffect();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Message))]
internal static class PlayerSkillNotificationAlarmPatch
{
    private static bool Prefix(string msg)
    {
        return VeiledRecipeState.ShouldShowSkillLevelUpNotification(msg);
    }
}
