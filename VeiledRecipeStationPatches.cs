using HarmonyLib;

namespace VeiledRecipes;

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
internal static class CraftingStationInteractPatch
{
    private static void Prefix(CraftingStation __instance, Humanoid user, bool repeat)
    {
        if (repeat || user != Player.m_localPlayer || user is not Player player)
        {
            return;
        }

        if (!__instance.InUseDistance(user) || !__instance.CheckUsable(player, showMessage: false))
        {
            return;
        }

        VeiledRecipeState.RecordStationInteraction(player, __instance);
    }
}

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.UpdateKnownStationsInRange))]
internal static class CraftingStationUpdateKnownStationsInRangePatch
{
    private static bool Prefix()
    {
        return VeiledRecipeState.EnableStationProximityDiscovery || VeiledRecipeState.ShouldBypassForAdmin(Player.m_localPlayer);
    }
}
