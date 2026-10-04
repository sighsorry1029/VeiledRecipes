using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VeiledRecipes;

internal static class Program
{
    private static int assertions;

    private static void Main()
    {
        new Harmony("veiledrecipes.container-discovery.tests").PatchAll(typeof(VeiledRecipeContainerDiscoveryPatch).Assembly);
        VeiledRecipeContainerDiscoveryPatch.Start();
        var player = Player.m_localPlayer = new Player();
        var inventory = new Inventory();
        var wood = new ItemDrop.ItemData("wood", 12);
        var trophy = new ItemDrop.ItemData("trophy", 1);
        inventory.Items.AddRange(new[] { wood, wood, trophy });
        int nativeChanges = 0;
        Action nativeCallback = () => nativeChanges++;
        inventory.m_onChanged += nativeCallback;
        var chest = new Container(inventory);
        var gui = new InventoryGui();
        gui.Show(chest);
        gui.Tick(player);
        Check(player.Discoveries.Count == 0 && inventory.Reads == 0, "Off does not scan or learn");
        Check(Subscriptions(inventory) == 1, "Off preserves only the existing inventory callback");

        VeiledRecipeState.LearnMaterialsFromOpenedContainers = true;
        gui.Hide();
        gui.Tick(player);
        Check(player.Discoveries.Count == 0, "no successful open means no discovery");
        gui.Show(chest);
        chest.Owner = false;
        gui.Tick(player);
        Check(inventory.Reads == 0, "waits for native ownership before learning");
        chest.Owner = true;
        gui.m_container.gameObject.activeInHierarchy = false;
        gui.Tick(player);
        Check(inventory.Reads == 0, "hidden container panel cannot teach items");
        gui.m_container.gameObject.activeInHierarchy = true;
        gui.ContainerGrid.Inventory = new Inventory();
        gui.Tick(player);
        Check(inventory.Reads == 0, "stale grid inventory cannot teach items");
        gui.ContainerGrid.Inventory = inventory;
        gui.SetVisible(false);
        gui.Tick(player);
        Check(inventory.Reads == 0, "hidden animator cannot teach even with a stale active panel and current container");
        gui.SetVisible(true);
        gui.Tick(new Player());
        gui.Tick(null!);
        Check(inventory.Reads == 0, "only a present local player can learn");

        gui.Tick(player);
        Check(player.Discoveries.SequenceEqual(new[] { wood, trophy }), "learns each unknown name once using the original item, regardless of type");
        Check(Subscriptions(inventory) == 2, "subscribes without replacing the native callback");
        for (int i = 0; i < 20; i++) gui.Tick(player);
        Check(inventory.Reads == 1 && player.Discoveries.Count == 2 && Subscriptions(inventory) == 2, "unchanged frames neither scan nor resubscribe");
        Check(inventory.Items.SequenceEqual(new[] { wood, wood, trophy }) && wood.m_stack == 12 && trophy.m_stack == 1 && !wood.m_pickedUp && !trophy.m_pickedUp, "items, stack sizes and pickup flags remain untouched");

        var iron = new ItemDrop.ItemData("iron", 3);
        inventory.Items[0] = iron;
        inventory.Changed();
        Check(!player.Known.Contains("iron") && nativeChanges == 1, "inventory callbacks remain active and defer learning until the UI update");
        gui.Tick(player);
        Check(player.Known.Contains("iron") && inventory.Reads == 2, "same-size inventory replacement is discovered");

        var copper = new ItemDrop.ItemData("copper", 2);
        var silver = new ItemDrop.ItemData("silver", 1);
        player.AfterLearn = item =>
        {
            if (ReferenceEquals(item, copper)) { inventory.Items.Add(silver); inventory.Changed(); }
        };
        inventory.Items.Add(copper);
        inventory.Changed();
        gui.Tick(player);
        Check(player.Known.Contains("copper") && !player.Known.Contains("silver"), "callbacks may change inventory without invalidating the snapshot");
        gui.Tick(player);
        Check(player.Known.Contains("silver"), "dirty notification during discovery survives until the next update");
        player.AfterLearn = null;

        inventory.Items.Add(new ItemDrop.ItemData("closed", 1)); inventory.Changed();
        gui.DuringUpdate = gui.Hide;
        gui.Tick(player);
        Check(!player.Known.Contains("closed") && Subscriptions(inventory) == 1, "Hide during the original update prevents pending learning and unsubscribes");
        gui.DuringUpdate = null;
        gui.Show(chest);
        gui.DuringUpdate = gui.CloseContainer;
        gui.Tick(player);
        Check(!player.Known.Contains("closed") && Subscriptions(inventory) == 1, "automatic CloseContainer during the original update prevents learning");
        gui.DuringUpdate = null;
        gui.Show(chest); gui.Tick(player);
        Check(player.Known.Contains("closed"), "reopening learns pending contents");

        var secondInventory = new Inventory();
        secondInventory.Items.Add(new ItemDrop.ItemData("second", 1));
        gui.Show(new Container(secondInventory)); gui.Tick(player);
        Check(Subscriptions(inventory) == 1 && Subscriptions(secondInventory) == 1 && player.Known.Contains("second"), "switching containers detaches the old inventory");
        int reads = secondInventory.Reads;
        inventory.Changed(); gui.Tick(player);
        Check(secondInventory.Reads == reads, "old inventory events cannot dirty the new container");
        var replacement = Player.m_localPlayer = new Player();
        gui.Tick(replacement);
        Check(replacement.Known.SetEquals(new[] { "second" }) && Subscriptions(secondInventory) == 1, "player changes relearn for the new character without duplicate subscriptions");

        VeiledRecipeState.LearnMaterialsFromOpenedContainers = false;
        gui.Tick(replacement);
        secondInventory.Items.Add(new ItemDrop.ItemData("off", 1)); secondInventory.Changed();
        gui.Tick(replacement);
        Check(Subscriptions(secondInventory) == 0 && !replacement.Known.Contains("off") && replacement.Known.Contains("second"), "live Off detaches, stops learning and retains existing knowledge");
        VeiledRecipeState.LearnMaterialsFromOpenedContainers = true;
        gui.Tick(replacement);
        Check(replacement.Known.Contains("off") && Subscriptions(secondInventory) == 1, "live On learns from the already open container");

        var newerGui = new InventoryGui(); newerGui.Show(chest); newerGui.Tick(replacement);
        Check(Subscriptions(secondInventory) == 0 && Subscriptions(inventory) == 2, "GUI replacement detaches the previous inventory");
        gui.Destroy();
        Check(Subscriptions(inventory) == 2, "destroying an obsolete GUI does not detach the current GUI");
        newerGui.Destroy();
        Check(Subscriptions(inventory) == 1, "current GUI destruction unsubscribes");
        gui = new InventoryGui(); gui.Show(chest); gui.Tick(replacement);
        chest.Owner = false; gui.Tick(replacement);
        Check(Subscriptions(inventory) == 1, "losing ownership releases the inventory subscription");
        chest.Owner = true; gui.Tick(replacement);
        Player.m_localPlayer = null; gui.Tick(replacement);
        Check(Subscriptions(inventory) == 1, "losing the local player releases the subscription");
        Player.m_localPlayer = replacement; gui.Tick(replacement);
        VeiledRecipeContainerDiscoveryPatch.Stop();
        inventory.Items.Add(new ItemDrop.ItemData("shutdown", 1)); inventory.Changed(); gui.Tick(replacement);
        Check(Subscriptions(inventory) == 1 && !replacement.Known.Contains("shutdown"), "plugin shutdown prevents still-installed patches from restarting discovery");
        Console.WriteLine($"PASS: {assertions} container discovery assertions; real Harmony with managed doubles, not game execution.");
    }

    private static int Subscriptions(Inventory inventory) => inventory.m_onChanged?.GetInvocationList().Length ?? 0;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
        assertions++;
        Console.WriteLine("PASS: " + message);
    }
}

// Managed boundary doubles: native authorization, persistence, raids and Unity
// rendering are not simulated. The production Harmony patches run unchanged.
public sealed class Player : UnityEngine.Object
{
    public static Player? m_localPlayer;
    public readonly HashSet<string> Known = new();
    public readonly List<ItemDrop.ItemData> Discoveries = new();
    public Action<ItemDrop.ItemData>? AfterLearn;
    public bool IsKnownMaterial(string name) => Known.Contains(name);
    public void AddKnownItem(ItemDrop.ItemData item)
    {
        Known.Add(item.m_shared.m_name);
        Discoveries.Add(item);
        AfterLearn?.Invoke(item);
    }
}

public sealed class ItemDrop
{
    public sealed class ItemData
    {
        public sealed class SharedData { public string m_name = ""; }
        public SharedData m_shared = new();
        public int m_stack;
        public bool m_pickedUp;
        public ItemData(string name, int stack) { m_shared.m_name = name; m_stack = stack; }
    }
}

public sealed class Inventory
{
    public Action? m_onChanged;
    public readonly List<ItemDrop.ItemData> Items = new();
    public int Reads;
    public List<ItemDrop.ItemData> GetAllItems() { Reads++; return Items; }
    public void Changed() => m_onChanged?.Invoke();
}

public sealed class Container : UnityEngine.Object
{
    private readonly Inventory inventory;
    public bool Owner = true;
    public Container(Inventory inventory) => this.inventory = inventory;
    public bool IsOwner() => Owner;
    public Inventory GetInventory() => inventory;
}

public sealed class InventoryGrid : UnityEngine.Object
{
    public Inventory? Inventory;
    public Inventory? GetInventory() => Inventory;
}

public sealed class InventoryGui : UnityEngine.Object
{
    private Container? m_currentContainer;
    private readonly UnityEngine.Animator m_animator = new();
    public readonly UnityEngine.RectTransform m_container = new();
    public InventoryGrid ContainerGrid { get; } = new();
    public Action? DuringUpdate;
    public void Show(Container container)
    {
        m_currentContainer = container;
        m_animator.Visible = true;
        m_container.gameObject.activeInHierarchy = true;
        ContainerGrid.Inventory = container.GetInventory();
    }
    public void Tick(Player player) => UpdateContainer(player);
    public void SetVisible(bool visible) => m_animator.Visible = visible;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdateContainer(Player player) => DuringUpdate?.Invoke();
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Hide() { m_currentContainer = null; m_animator.Visible = false; }
    public void CloseContainer() { m_currentContainer = null; m_container.gameObject.activeInHierarchy = false; }
    public void Destroy() => OnDestroy();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void OnDestroy() { }
}

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object? value) => value != null; }
    public sealed class GameObject { public bool activeInHierarchy; }
    public sealed class RectTransform : Object { public GameObject gameObject = new(); }
    public sealed class Animator : Object
    {
        public bool Visible;
        public bool GetBool(string key) => Visible;
    }
}

namespace VeiledRecipes
{
    internal static class VeiledRecipeState { internal static bool LearnMaterialsFromOpenedContainers; }
}
