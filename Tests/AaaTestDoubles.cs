using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Minimal managed UI doubles. Harmony itself is real; no Unity player is started.
namespace UnityEngine
{
    public class Component
    {
        public GameObject gameObject = null!;
        public Transform transform => gameObject.transform;
        public T? GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }

    public class Transform : Component
    {
        public readonly Dictionary<string, Transform> Children = new();
    }

    public class GameObject
    {
        public readonly Transform transform;
        public bool activeSelf = true;
        public bool Masked;
        public readonly List<Component> Components = new();
        public GameObject() { transform = Add(new Transform()); }
        public T Add<T>(T component) where T : Component
        {
            component.gameObject = this;
            Components.Add(component);
            return component;
        }
        public T? GetComponent<T>() where T : Component => Components.Find(c => c is T) as T;
        public void SetActive(bool value) => activeSelf = value;
    }
}

namespace TMPro
{
    public class TMP_Text : UnityEngine.Component { public string text = ""; }
}

public static class Utils
{
    public static UnityEngine.Transform? FindChild(UnityEngine.Transform root, string name) =>
        root.Children.TryGetValue(name, out var child) ? child : null;
}

public class Localization
{
    public static Localization instance = new();
    public string Localize(string text) => text;
}

public class ItemDrop { public class ItemData { } }
public class Recipe { public bool Masked; public bool RequiresKnowledge; }
public class MessageHud { public enum MessageType { Center } }
public class Player
{
    public static Player? m_localPlayer;
    public bool Bypass;
    public string? LastMessage;
    public void Message(MessageHud.MessageType type, string text) => LastMessage = text;
}

public class InventoryGui
{
    public struct RecipeDataPair
    {
        public Recipe Recipe;
        public ItemDrop.ItemData? ItemData;
        public UnityEngine.GameObject? InterfaceElement;
    }
    public static InventoryGui? instance;
    public readonly List<RecipeDataPair> m_availableRecipes = new();
    public RecipeDataPair m_selectedRecipe;
}

namespace BepInEx.Bootstrap
{
    public class PluginInfo { public object? Instance; }
    public static class Chainloader { public static readonly Dictionary<string, PluginInfo> PluginInfos = new(); }
}

namespace VeiledRecipes
{
    public static class VeiledRecipesPlugin
    {
        public const string ModGUID = "sighsorry.VeiledRecipes";
        public static readonly TestLogger PluginLogger = new();
    }
    public class TestLogger
    {
        public readonly List<string> Warnings = new();
        public void LogInfo(string value) => Console.WriteLine(value);
        public void LogWarning(string value) => Warnings.Add(value);
    }
    public static class VeiledRecipeState
    {
        public static string UnknownNameText = "???";
        public static string UnknownDescriptionText = "Not enough info";
        public static ItemDrop.ItemData? LastTarget;
        public static bool ShouldMaskRecipe(Player player, Recipe recipe, ItemDrop.ItemData? item = null)
        {
            LastTarget = item;
            return recipe.Masked && !player.Bypass;
        }
        public static bool RequiresRecipeKnowledge(Player player, Recipe recipe) => recipe.RequiresKnowledge && !player.Bypass;
    }
    public static class InventoryGuiAddRecipeToListPatch
    {
        public static void MaskRecipeListElement(UnityEngine.GameObject element, Recipe recipe) => element.Masked = true;
    }
}

namespace AzuAntiArthriticCrafting.Handlers
{
    public class RecipeHoverTooltip : UnityEngine.Component
    {
        public static RecipeHoverTooltip? m_current;
        public static UnityEngine.GameObject? m_tooltip;
        public string m_topic = "Original title";
        public string m_text = "Original description";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdateTextElements()
        {
            if (m_tooltip == null) return;
            Utils.FindChild(m_tooltip.transform, "Topic")!.GetComponent<TMPro.TMP_Text>()!.text = m_topic;
            Utils.FindChild(m_tooltip.transform, "Text")!.GetComponent<TMPro.TMP_Text>()!.text = m_text;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LateUpdate() { }
    }
}

namespace AzuAntiArthriticCrafting.RecipeTracking
{
    public class RecipeUI : UnityEngine.Component
    {
        public Recipe Recipe { get; set; } = null!;
        public UnityEngine.GameObject recipeStub = new();
    }
    public class RecipeTrackerUI : UnityEngine.Component
    {
        public readonly List<RecipeUI> RecipeUIs = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddSelectedRecipe(Recipe recipe) => RecipeUIs.Add(new UnityEngine.GameObject().Add(new RecipeUI { Recipe = recipe }));
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ToggleUI() { }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ToggleUI(bool value) { }
    }
}
