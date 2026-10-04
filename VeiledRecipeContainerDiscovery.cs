using HarmonyLib;
using UnityEngine;

namespace VeiledRecipes;

[HarmonyPatch(typeof(InventoryGui), "UpdateContainer", typeof(Player))]
internal static class VeiledRecipeContainerDiscoveryPatch
{
    private static InventoryGui? _gui;
    private static Player? _player;
    private static Inventory? _inventory;
    private static bool _dirty;
    private static bool _running;

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(InventoryGui __instance, Player player, Container ___m_currentContainer, Animator ___m_animator)
    {
        // Interact/Show can precede owner handoff and inventory display. Observe only
        // the inventory the native UI actually accepted; never change access or ownership.
        if (!_running || !VeiledRecipeState.LearnMaterialsFromOpenedContainers || !player || player != Player.m_localPlayer ||
            !___m_currentContainer || !___m_currentContainer.IsOwner() ||
            !___m_animator || !___m_animator.GetBool("visible") ||
            !__instance.m_container || !__instance.m_container.gameObject.activeInHierarchy || !__instance.ContainerGrid)
        {
            Reset();
            return;
        }

        Inventory inventory = ___m_currentContainer.GetInventory();
        if (inventory == null || !ReferenceEquals(inventory, __instance.ContainerGrid.GetInventory()))
        {
            Reset();
            return;
        }

        if (!ReferenceEquals(_gui, __instance) || !ReferenceEquals(_player, player) || !ReferenceEquals(_inventory, inventory))
        {
            Reset();
            _gui = __instance;
            _player = player;
            _inventory = inventory;
            _inventory.m_onChanged += MarkDirty;
            _dirty = true;
        }

        if (!_dirty)
            return;

        _dirty = false;
        // AddKnownItem can invoke other mods. Snapshot only on opening/change so
        // their inventory callbacks cannot invalidate this iteration.
        ItemDrop.ItemData[] items = inventory.GetAllItems().ToArray();
        foreach (ItemDrop.ItemData item in items)
        {
            if (!ReferenceEquals(_inventory, inventory) || !ReferenceEquals(_player, player) ||
                !VeiledRecipeState.LearnMaterialsFromOpenedContainers)
                break;

            if (item?.m_shared != null && !string.IsNullOrEmpty(item.m_shared.m_name) &&
                !player.IsKnownMaterial(item.m_shared.m_name))
                player.AddKnownItem(item);
        }
    }

    private static void MarkDirty() => _dirty = true;

    internal static void Start()
    {
        Reset();
        _running = true;
    }

    internal static void Stop()
    {
        _running = false;
        Reset();
    }

    internal static void Reset()
    {
        if (_inventory != null)
            _inventory.m_onChanged -= MarkDirty;
        _inventory = null;
        _player = null;
        _gui = null;
        _dirty = false;
    }

    internal static void Reset(InventoryGui gui)
    {
        if (ReferenceEquals(_gui, gui))
            Reset();
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
internal static class VeiledRecipeContainerDiscoveryHidePatch
{
    private static void Postfix(InventoryGui __instance) => VeiledRecipeContainerDiscoveryPatch.Reset(__instance);
}

[HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
internal static class VeiledRecipeContainerDiscoveryDestroyPatch
{
    private static void Postfix(InventoryGui __instance) => VeiledRecipeContainerDiscoveryPatch.Reset(__instance);
}
