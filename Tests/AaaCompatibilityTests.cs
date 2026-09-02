using System;
using System.Linq;
using AzuAntiArthriticCrafting.Handlers;
using AzuAntiArthriticCrafting.RecipeTracking;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil;
using TMPro;
using UnityEngine;
using VeiledRecipes;

internal static class AaaCompatibilityTests
{
    private static int _assertions;

    private static void Main(string[] args)
    {
        VeiledRecipeAaaCraftingCompat.Initialize();
        Check(Harmony.GetPatchInfo(typeof(RecipeHoverTooltip).GetMethod("UpdateTextElements")) == null, "AAA absent: no external patches");
        Chainloader.PluginInfos.Add(VeiledRecipeAaaCraftingCompat.PluginGuid, new PluginInfo { Instance = new object() });
        VeiledRecipeAaaCraftingCompat.Initialize();
        Check(VeiledRecipesPlugin.PluginLogger.Warnings.Count == 1, "Unsupported AAA contract logs a warning instead of aborting plugin startup");
        Check(Harmony.GetPatchInfo(typeof(RecipeHoverTooltip).GetMethod("UpdateTextElements")) == null, "Unsupported AAA leaves no compatibility patches");
        VeiledRecipesPlugin.PluginLogger.Warnings.Clear();
        Chainloader.PluginInfos.Clear();
        Chainloader.PluginInfos.Add(VeiledRecipeAaaCraftingCompat.PluginGuid, new PluginInfo { Instance = new RecipeHoverTooltip() });
        VeiledRecipeAaaCraftingCompat.Initialize();
        Check(VeiledRecipesPlugin.PluginLogger.Warnings.Count == 0, "Harmony patches install with injected fields");
        var tooltipPatches = Harmony.GetPatchInfo(typeof(RecipeHoverTooltip).GetMethod("UpdateTextElements"));
        Check(tooltipPatches?.Postfixes.Count == 1, "Tooltip postfix installed");
        VeiledRecipeAaaCraftingCompat.Initialize();
        Check(Harmony.GetPatchInfo(typeof(RecipeHoverTooltip).GetMethod("UpdateTextElements"))!.Postfixes.Count == 1, "Initialization is idempotent");

        var player = Player.m_localPlayer = new Player();
        var gui = InventoryGui.instance = new InventoryGui();
        var unknown = new Recipe { Masked = true, RequiresKnowledge = true };
        var known = new Recipe();
        var target = new ItemDrop.ItemData();
        var unknownElement = new GameObject();
        var knownElement = new GameObject();
        gui.m_availableRecipes.Add(new InventoryGui.RecipeDataPair { Recipe = unknown, InterfaceElement = unknownElement, ItemData = target });
        gui.m_availableRecipes.Add(new InventoryGui.RecipeDataPair { Recipe = known, InterfaceElement = knownElement });
        gui.m_availableRecipes.Add(new InventoryGui.RecipeDataPair { Recipe = unknown });
        VeiledRecipeAaaCraftingCompat.MaskRecipeList(gui, player);
        Check(unknownElement.Masked && !knownElement.Masked, "Only unknown list entries are masked; missing elements are safe");

        var tooltip = unknownElement.Add(new RecipeHoverTooltip());
        var root = new GameObject();
        var topic = new GameObject().Add(new TMP_Text());
        var text = new GameObject().Add(new TMP_Text());
        root.transform.Children.Add("Topic", topic.transform);
        root.transform.Children.Add("Text", text.transform);
        RecipeHoverTooltip.m_tooltip = root;
        tooltip.UpdateTextElements(); // AAA sets m_current only AFTER this initial render.
        Check(topic.text == "???" && text.text == "Not enough info", "First hover render is masked before m_current is assigned");
        Check(ReferenceEquals(VeiledRecipeState.LastTarget, target), "Hover uses the actual pair target");
        Check(tooltip.m_topic == "Original title" && tooltip.m_text == "Original description", "Source tooltip strings are preserved");
        RecipeHoverTooltip.m_current = tooltip;
        player.Bypass = true;
        tooltip.LateUpdate();
        Check(topic.text == "Original title" && text.text == "Original description", "Open tooltip restores on bypass without another hover");
        player.Bypass = false;
        tooltip.LateUpdate();
        Check(topic.text == "???", "Open tooltip is masked again when bypass ends");
        VeiledRecipeState.UnknownNameText = "Hidden";
        VeiledRecipeState.UnknownDescriptionText = "Unknown";
        tooltip.LateUpdate();
        Check(topic.text == "Hidden" && text.text == "Unknown", "Open tooltip reflects display config changes");
        unknown.Masked = false;
        tooltip.LateUpdate();
        Check(topic.text == "Original title", "Open tooltip restores on unlock");
        unknown.Masked = true;
        var unrelatedTooltip = new GameObject().Add(new RecipeHoverTooltip { m_topic = "Inventory item", m_text = "Inventory detail" });
        unrelatedTooltip.UpdateTextElements();
        Check(topic.text == "Inventory item" && text.text == "Inventory detail", "Unrelated tooltips are not masked");
        tooltip.LateUpdate();
        unrelatedTooltip.LateUpdate();
        Check(topic.text == "Hidden", "Inactive tooltip updates cannot restore the active masked tooltip");
        RecipeHoverTooltip.m_tooltip = null;
        tooltip.LateUpdate();
        Check(true, "Destroyed/missing tooltip is safe");

        var tracker = new GameObject().Add(new RecipeTrackerUI());
        gui.m_selectedRecipe = gui.m_availableRecipes[0];
        tracker.AddSelectedRecipe(unknown);
        Check(tracker.RecipeUIs.Count == 0 && player.LastMessage == "Unknown", "Tracking unknown recipes is blocked");
        Check(ReferenceEquals(VeiledRecipeState.LastTarget, target), "Tracking guard preserves selected upgrade target context");
        tracker.AddSelectedRecipe(known);
        Check(tracker.RecipeUIs.Count == 1, "Known and non-masked recipe-less entries can be tracked");
        player.Bypass = true;
        tracker.AddSelectedRecipe(unknown);
        Check(tracker.RecipeUIs.Count == 2, "Bypass permits tracking");
        player.Bypass = false;
        tracker.ToggleUI();
        Check(!tracker.RecipeUIs[1].recipeStub.activeSelf && tracker.RecipeUIs[0].recipeStub.activeSelf, "Previously tracked unknown entries are hidden");
        Check(tracker.RecipeUIs.Count == 2, "Hiding does not delete tracker records");
        unknown.RequiresKnowledge = false;
        tracker.ToggleUI();
        Check(tracker.RecipeUIs[1].recipeStub.activeSelf, "Hidden tracker entry restores on unlock");
        tracker.RecipeUIs[0].recipeStub.SetActive(false);
        tracker.ToggleUI();
        Check(!tracker.RecipeUIs[0].recipeStub.activeSelf, "Other UI visibility decisions are preserved");

        if (args.Length > 0) VerifyDllContract(args[0]);
        Console.WriteLine($"PASS: {_assertions} assertions. Managed UI doubles, not an in-game test.");
    }

    private static void VerifyDllContract(string path)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(path);
        var types = assembly.MainModule.Types;
        var tooltip = types.Single(t => t.FullName == "AzuAntiArthriticCrafting.Handlers.RecipeHoverTooltip");
        var tracker = types.Single(t => t.FullName == "AzuAntiArthriticCrafting.RecipeTracking.RecipeTrackerUI");
        var entry = types.Single(t => t.FullName == "AzuAntiArthriticCrafting.RecipeTracking.RecipeUI");
        Check(tooltip.BaseType.FullName == "UnityEngine.MonoBehaviour", "DLL: tooltip is a Unity component");
        Check(tooltip.Fields.Any(f => f.Name == "m_tooltip" && f.IsStatic && f.FieldType.FullName == "UnityEngine.GameObject"), "DLL: static tooltip root");
        Check(tooltip.Fields.Any(f => f.Name == "m_current" && f.IsStatic && f.FieldType.FullName == tooltip.FullName), "DLL: static active tooltip");
        Check(tooltip.Fields.Count(f => (f.Name == "m_topic" || f.Name == "m_text") && !f.IsStatic && f.FieldType.FullName == "System.String") == 2, "DLL: tooltip source fields");
        Check(tooltip.Methods.Count(m => (m.Name == "UpdateTextElements" || m.Name == "LateUpdate") && !m.IsStatic && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void") == 2, "DLL: tooltip hook signatures");
        Check(tracker.Methods.Any(m => m.Name == "AddSelectedRecipe" && !m.IsStatic && m.ReturnType.FullName == "System.Void" && m.Parameters.Count == 1 && m.Parameters[0].Name == "recipe" && m.Parameters[0].ParameterType.FullName == "Recipe"), "DLL: tracking guard signature");
        Check(tracker.Methods.Any(m => m.Name == "ToggleUI" && !m.IsStatic && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void"), "DLL: tracker display hook");
        Check(tracker.Fields.Any(f => f.Name == "RecipeUIs" && !f.IsStatic && f.FieldType.FullName == "System.Collections.Generic.List`1<AzuAntiArthriticCrafting.RecipeTracking.RecipeUI>"), "DLL: tracked entries list");
        Check(entry.Properties.Any(p => p.Name == "Recipe" && p.PropertyType.FullName == "Recipe" && p.GetMethod.IsPublic), "DLL: tracked recipe getter");
        Check(entry.Fields.Any(f => f.Name == "recipeStub" && f.IsPublic && f.FieldType.FullName == "UnityEngine.GameObject"), "DLL: tracked panel field");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
        _assertions++;
        Console.WriteLine("PASS: " + message);
    }
}
