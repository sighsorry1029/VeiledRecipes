using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AzuAntiArthriticCrafting.Handlers;
using AzuAntiArthriticCrafting.RecipeTracking;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil;
using TMPro;
using UnityEngine;
using VeiledRecipes;
using Paginator = AzuAntiArthriticCrafting.Patches.PaginatorPatches.InventoryGuiUpdateRecipeListPatch;

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
        // AAA's prefix/postfix are already attached before VeiledRecipes loads.
        new Harmony(VeiledRecipeAaaCraftingCompat.PluginGuid).CreateClassProcessor(typeof(Paginator)).Patch();
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

        VerifyPagination(gui, player);
        if (args.Length > 0) VerifyDllContract(args[0]);
        Console.WriteLine($"PASS: {_assertions} assertions. Managed UI doubles, not an in-game test.");
    }

    private static void VerifyPagination(InventoryGui gui, Player player)
    {
        var ready = new Recipe { CanCraft = true, SortOrder = 40 };
        var known1 = new Recipe { SortOrder = 25 };
        var known2 = new Recipe { SortOrder = 30 };
        var known3 = new Recipe { SortOrder = 50 };
        var unknown1 = new Recipe { Masked = true, SortOrder = 10 };
        var unknown2 = new Recipe { Masked = true, SortOrder = 20 };
        var excluded = new Recipe { Visible = false, SortOrder = 0 };
        var source = new List<Recipe> { known3, unknown2, ready, excluded, known1, unknown1, known2 };
        var originalOrder = new[] { ready, unknown1, unknown2, known1, known2, known3 };
        Paginator.Reuse = false;
        Paginator.Page = 0;
        gui.UpdateRecipeList(source);
        Check(PageIs(ready, known1), "Craft: first page fills with known recipes, not just locally sorted previews");
        Check(Paginator.Cached.SequenceEqual(originalOrder), "Craft: AAA's filtered, sorted cache is not modified");
        Check(source.Count == 7 && source[0] == known3, "Craft: original input is not modified");
        Paginator.Reuse = true;
        Paginator.Page = 1;
        gui.UpdateRecipeList(source);
        Check(PageIs(known2, known3), "Cached craft: remaining known recipes precede every preview");
        Paginator.Page = 2;
        gui.UpdateRecipeList(source);
        Check(PageIs(unknown1, unknown2), "Cached craft: previews retain AAA order on the last page");
        Paginator.Page = 3;
        gui.UpdateRecipeList(source);
        Check(PageIs(), "Craft: filtering and page bounds are preserved");

        VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes = false;
        Paginator.Page = 1;
        gui.UpdateRecipeList(source);
        Check(PageIs(unknown2, known1), "Grouping off: original AAA order restores even on the cached path");
        VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes = true;
        gui.UpdateRecipeList(source);
        Check(PageIs(known2, known3), "Grouping on: cached pages regroup without rebuilding AAA's cache");
        unknown1.Masked = false;
        Paginator.Page = 0;
        gui.UpdateRecipeList(source);
        Check(PageIs(ready, unknown1), "Unlock: cached recipes are reclassified using current knowledge");
        Paginator.Page = 2;
        gui.UpdateRecipeList(source);
        Check(PageIs(known3, unknown2), "Unlock: no recipe is lost or duplicated across groups");
        player.Bypass = true;
        Paginator.Page = 1;
        gui.UpdateRecipeList(source);
        Check(PageIs(unknown2, known1), "Admin bypass: cached pages use AAA order");
        player.Bypass = false;
        gui.UpdateRecipeList(source);
        Check(PageIs(known1, known2), "Bypass disabled: cached pages regroup again");
        Player.m_localPlayer = null;
        gui.UpdateRecipeList(source);
        Check(PageIs(unknown2, known1), "No local player: paging remains unchanged");
        Player.m_localPlayer = player;
        unknown2.PreviewAllowed = false;
        gui.UpdateRecipeList(source);
        Check(PageIs(unknown2, known1), "Non-preview entries keep the same grouping policy as the vanilla patch");
        unknown2.PreviewAllowed = true;
        Check(Paginator.Cached.SequenceEqual(originalOrder), "Knowledge/config/bypass changes leave AAA's cache intact");

        var noRecipe = new Recipe { Masked = true, NoLearnableRecipe = true, SortOrder = 5 };
        var owned = new ItemDrop.ItemData();
        var unknownOwned = new ItemDrop.ItemData();
        InventoryGui.RecipeDataPair[] pairs =
        {
            new() { Recipe = known1 },
            new() { Recipe = noRecipe },
            new() { Recipe = unknown2, ItemData = unknownOwned },
            new() { Recipe = noRecipe, ItemData = owned },
            new() { Recipe = excluded }
        };
        gui.CraftTab = false;
        Paginator.Page = 0;
        ShowPairs();
        Check(PageIs(noRecipe, known1) && ReferenceEquals(gui.m_availableRecipes[0].ItemData, owned), "Upgrade: recipe-less owned target stays ahead of veiled pairs");
        Paginator.Page = 1;
        ShowPairs();
        Check(PageIs(noRecipe, unknown2) && gui.m_availableRecipes[0].ItemData == null && ReferenceEquals(gui.m_availableRecipes[1].ItemData, unknownOwned), "Upgrade: target context and duplicate-recipe pair identity are preserved");
        VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes = false;
        Paginator.Page = 0;
        ShowPairs();
        Check(PageIs(noRecipe, noRecipe) && gui.m_availableRecipes[0].ItemData == null, "Upgrade grouping off: AAA pair order restores");
        VeiledRecipeState.GroupUnknownRecipePreviewsBelowKnownRecipes = true;
        gui.CraftTab = true;
        Paginator.Reuse = false;
        gui.UpdateRecipeList(new List<Recipe> { unknown2 });
        Check(PageIs(unknown2), "Single all-unknown page is unchanged");
        gui.UpdateRecipeList(new List<Recipe>());
        Check(PageIs(), "Empty recipe list is safe");

        MethodInfo transpiler = typeof(VeiledRecipeAaaCraftingCompat).GetMethod("GroupBeforePagination", BindingFlags.NonPublic | BindingFlags.Static)!;
        bool rejected = false;
        try { transpiler.Invoke(null, new object[] { new[] { new CodeInstruction(OpCodes.Ret) }, typeof(Paginator).GetMethod("Prefix")! }); }
        catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
        Check(rejected, "Unsupported pagination IL is rejected instead of partially rewriting the method");

        bool PageIs(params Recipe[] expected) => gui.m_availableRecipes.Select(p => p.Recipe).SequenceEqual(expected);
        void ShowPairs()
        {
            gui.m_availableRecipes.Clear();
            gui.m_availableRecipes.AddRange(pairs);
            gui.UpdateRecipeList(new List<Recipe>());
        }
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
        var paginator = types.Single(t => t.FullName == "AzuAntiArthriticCrafting.Patches.PaginatorPatches")
            .NestedTypes.Single(t => t.Name == "InventoryGuiUpdateRecipeListPatch");
        var prefix = paginator.Methods.Single(m => m.Name == "Prefix");
        var postfix = paginator.Methods.Single(m => m.Name == "Postfix");
        Check(new[] { prefix, postfix }.All(m => m.IsStatic && m.ReturnType.FullName == "System.Void" && m.Parameters.Count == 2 &&
            m.Parameters[0].ParameterType.FullName == "InventoryGui" && m.Parameters[1].ParameterType.FullName == "System.Collections.Generic.List`1<Recipe>&"), "DLL: paginator patch signatures");
        Check(SkipCount(prefix, "Recipe") == 2, "DLL: fresh and cached craft pagination hooks");
        Check(SkipCount(postfix, "InventoryGui/RecipeDataPair") == 1, "DLL: upgrade pair pagination hook");

        int SkipCount(MethodDefinition method, string typeName) => method.Body.Instructions.Count(i =>
            i.OpCode == Mono.Cecil.Cil.OpCodes.Call && i.Operand is GenericInstanceMethod call &&
            call.DeclaringType.FullName == "System.Linq.Enumerable" && call.Name == "Skip" &&
            call.GenericArguments.Count == 1 && call.GenericArguments[0].FullName == typeName &&
            call.Parameters.Count == 2 && call.Parameters[1].ParameterType.FullName == "System.Int32");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
        _assertions++;
        Console.WriteLine("PASS: " + message);
    }
}
