#nullable disable

using HarmonyLib;

namespace SecretRecipes;

[HarmonyPatch(typeof(MessageHud), nameof(MessageHud.QueueUnlockMsg))]
internal static class MessageHudQueueUnlockMsgPatch
{
    private static bool Prefix(MessageHud __instance, string topic, string description)
    {
        if (SecretRecipeState.ShouldShowUnlockNotification(topic))
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
        return SecretRecipeState.ShouldShowSkillLevelUpEffect();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Message))]
internal static class PlayerSkillNotificationAlarmPatch
{
    private static bool Prefix(string msg)
    {
        return SecretRecipeState.ShouldShowSkillLevelUpNotification(msg);
    }
}
