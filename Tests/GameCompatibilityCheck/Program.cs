using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;

internal static class Program
{
    private static int assertions;
    private static Assembly plugin = null!;

    private static int Main(string[] args)
    {
        if (args.Length != 3) return 2;
        string[] paths = { Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetDirectoryName(Path.GetFullPath(args[0]))! };
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string? file = paths.Select(p => Path.Combine(p, new AssemblyName(e.Name).Name + ".dll")).FirstOrDefault(File.Exists);
            return file == null ? null : Assembly.LoadFrom(file);
        };
        try
        {
            Run(Path.GetFullPath(args[0]), paths[0]);
            System.Console.WriteLine($"PASS: {assertions} original-assembly Mono checks. No Unity scene, native detours, or network session.");
            return 0;
        }
        catch (Exception e) { System.Console.Error.WriteLine(e); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string dll, string managed)
    {
        foreach (string name in new[] { "assembly_valheim", "assembly_utils", "UnityEngine.CoreModule" })
            Check(string.Equals(Assembly.Load(name).Location, Path.Combine(managed, name + ".dll"), StringComparison.OrdinalIgnoreCase), "original runtime input: " + name);

        // Queue ServerSync startup work without executing it or constructing a Unity object.
        Type helperType = typeof(BepInEx.ThreadingHelper);
        object helper = FormatterServices.GetUninitializedObject(helperType);
        AccessTools.Field(helperType, "_invokeLock").SetValue(helper, new object());
        AccessTools.Field(helperType, "<Instance>k__BackingField").SetValue(null, helper);
        plugin = Assembly.LoadFrom(dll);
        Check(plugin.GetCustomAttributesData().Any(a => a.AttributeType.Name == "IgnoresAccessChecksToAttribute" && (string)a.ConstructorArguments[0].Value == "assembly_valheim"), "merged runtime access attribute");
        Check(!plugin.GetReferencedAssemblies().Any(a => a.Name == "ServerSync"), "ServerSync is internalized");
        PropertyInfo legacyAdminApi = plugin.GetType("VeiledRecipes.VeiledRecipesCompat", true)!.GetProperty("IsAdminBypassActive", BindingFlags.Public | BindingFlags.Static)!;
        Check(legacyAdminApi != null && !(bool)legacyAdminApi.GetValue(null), "legacy admin bypass API remains present and inactive");
        CheckPatches();
        RuntimeHelpers.RunClassConstructor(plugin.GetType("VeiledRecipes.VeiledRecipeBuildUiRefreshPatch", true)!.TypeHandle);
        Check(true, "new cached private field and method delegates initialize against originals");

        Type pairType = typeof(InventoryGui).GetNestedType("RecipeDataPair", BindingFlags.NonPublic)!;
        object pair = Activator.CreateInstance(pairType)!;
        MethodInfo pairApi = plugin.GetType("VeiledRecipes.VeiledRecipesCompat", true)!.GetMethods().Single(m => m.Name == "ShouldMaskRecipePair" && m.GetParameters().Length == 2);
        Check(!(bool)pairApi.Invoke(null, new object?[] { null, pair })!, "existing private-pair public API loads and executes on game Mono");
        Check(!(bool)pairApi.Invoke(null, new object[] { FakeUnity<Player>(), pair })!, "private RecipeDataPair getter executes on original game type");
        CheckIngredients();
        CheckPieceInsertion();
    }

    private static void CheckPatches()
    {
        foreach (Type type in plugin.GetTypes())
        {
            if (type.Namespace != "VeiledRecipes") continue; // ServerSync has its separate baseline verifier.
            var attrs = type.GetCustomAttributes(typeof(HarmonyPatch), false).Cast<HarmonyPatch>().ToArray();
            if (attrs.Length == 0) continue;
            var info = new HarmonyMethod();
            foreach (var a in attrs) a.info.CopyTo(info);
            MethodInfo target = info.methodType == MethodType.Getter
                ? AccessTools.DeclaredPropertyGetter(info.declaringType, info.methodName)
                : AccessTools.DeclaredMethod(info.declaringType, info.methodName, info.argumentTypes);
            if (target == null) throw new MissingMethodException("Harmony target: " + type.FullName);
            Check(true, "Harmony target: " + type.FullName);
            foreach (MethodInfo patch in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == "Prefix" || m.Name == "Postfix" || m.Name == "Finalizer"))
            foreach (ParameterInfo arg in patch.GetParameters())
            {
                string name = arg.Name!;
                Type? expected;
                if (name == "__instance") expected = target.DeclaringType;
                else if (name == "__result") expected = target.ReturnType;
                else if (name.StartsWith("___")) expected = AccessTools.Field(target.DeclaringType, name.Substring(3))?.FieldType;
                else if (name == "__state")
                {
                    expected = AccessTools.Method(type, "Prefix")?.GetParameters().FirstOrDefault(p => p.Name == name)?.ParameterType;
                }
                else if (name.StartsWith("__")) continue;
                else expected = target.GetParameters().FirstOrDefault(p => p.Name == name)?.ParameterType;
                Type actual = arg.ParameterType.IsByRef ? arg.ParameterType.GetElementType()! : arg.ParameterType;
                if (expected?.IsByRef == true) expected = expected.GetElementType();
                Check(expected != null && actual.IsAssignableFrom(expected), "Harmony injection: " + type.Name + "." + patch.Name + "/" + name);
            }
        }
    }

    private static void CheckIngredients()
    {
        var player = FakeUnity<Player>();
        var normalStation = FakeUnity<CraftingStation>();
        var upgrader = FakeUnity<CraftingStation>(); upgrader.m_upgrader = true;
        var ordinary = new Piece.Requirement { m_resItem = Item("wood"), m_amount = 1 };
        var upgradeOnly = new Piece.Requirement { m_resItem = Item("upgrade"), m_amount = 1, m_upgraderResource = true };
        FieldInfo current = AccessTools.Field(typeof(Player), "m_currentStation");
        foreach (CraftingStation? station in new CraftingStation?[] { null, normalStation, upgrader })
        {
            current.SetValue(player, station);
            bool enhanced = ReferenceEquals(station, upgrader);
            Check((bool)State("IsRecipeRequirementActiveAtCurrentStation", player, ordinary)! == !enhanced, "ordinary ingredient applicability");
            Check((bool)State("IsRecipeRequirementActiveAtCurrentStation", player, upgradeOnly)! == enhanced, "upgrade ingredient applicability");
        }
        var recipe = FakeUnity<Recipe>(); recipe.m_item = Item("result"); recipe.m_resources = new[] { ordinary, upgradeOnly };
        var materials = new HashSet<string> { "wood" };
        AccessTools.Field(typeof(Player), "m_knownMaterial").SetValue(player, materials);
        current.SetValue(player, null);
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "without a station, ordinary materials discover a recipe without the upgrader resource");
        materials.Clear(); materials.Add("upgrade");
        Check(!(bool)State("CanDiscoverRecipe", player, recipe)!, "without a station, an upgrader resource alone cannot discover a recipe");
        current.SetValue(player, normalStation);
        materials.Clear(); materials.Add("wood");
        Check(!(bool)State("CanDiscoverRecipe", player, recipe)!, "at a normal station, discovery also requires the upgrader resource");
        materials.Add("upgrade");
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "at a normal station, both requirement sets can discover a recipe");
        current.SetValue(player, upgrader);
        materials.Clear(); materials.Add("upgrade");
        Check(!(bool)State("CanDiscoverRecipe", player, recipe)!, "at an upgrader, discovery also requires ordinary materials");
        materials.Add("wood");
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "at an upgrader, both requirement sets can discover a recipe");
        recipe.m_requireOnlyOneIngredient = true;
        materials.Clear();
        Check(!(bool)State("CanDiscoverRecipe", player, recipe)!, "one-ingredient recipe still needs an applicable known resource");
        materials.Add("wood");
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "ordinary material can satisfy one-ingredient discovery at an upgrader");
        current.SetValue(player, normalStation);
        materials.Clear(); materials.Add("upgrade");
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "upgrader material can satisfy one-ingredient discovery at a normal station");
        current.SetValue(player, null);
        Check(!(bool)State("CanDiscoverRecipe", player, recipe)!, "without a station, an upgrader material remains inapplicable to one-ingredient discovery");
        materials.Add("wood");
        Check((bool)State("CanDiscoverRecipe", player, recipe)!, "without a station, an ordinary material satisfies one-ingredient discovery");
        normalStation.m_name = "workbench"; upgrader.m_name = "upgrader";
        recipe.m_craftingStation = normalStation; recipe.m_minStationLevel = 2;
        AccessTools.Field(typeof(Player), "m_knownStations").SetValue(player, new Dictionary<string, int> { ["upgrader"] = 1 });
        current.SetValue(player, upgrader);
        Check((bool)State("KnowsRecipeStationRequirement", player, recipe, 20)!, "upgrader display uses its known level 1 beyond ordinary quality limit");
        current.SetValue(player, normalStation);
        Check(!(bool)State("KnowsRecipeStationRequirement", player, recipe, 2)!, "upgrader knowledge does not replace ordinary station knowledge");
    }

    private static void CheckPieceInsertion()
    {
        var table = FakeUnity<PieceTable>();
        AccessTools.Field(typeof(PieceTable), "m_availablePieces").SetValue(table, new HashSet<Piece>());
        var categories = Enumerable.Range(0, (int)Piece.PieceCategory.Max).Select(_ => new List<Piece>()).ToList();
        MethodInfo add = AccessTools.Method(plugin.GetType("VeiledRecipes.PieceTableUpdateAvailablePatch"), "AddAvailablePiece");
        var piece = FakeUnity<Piece>(); piece.m_category = Piece.PieceCategory.DeepNorth;
        add.Invoke(null, new object[] { table, piece, categories });
        add.Invoke(null, new object[] { table, piece, categories });
        Check(table.m_availablePieces.Count == 1 && categories[(int)piece.m_category].SequenceEqual(new[] { piece }), "new category insertion is consistent and idempotent");
        var all = FakeUnity<Piece>(); all.m_category = Piece.PieceCategory.All;
        add.Invoke(null, new object[] { table, all, categories });
        add.Invoke(null, new object[] { table, all, categories });
        Check(table.m_availablePieces.Count == 2 && categories.All(c => c.Count(p => ReferenceEquals(p, all)) == 1), "All appears exactly once in every bucket");
        var invalid = FakeUnity<Piece>(); invalid.m_category = (Piece.PieceCategory)(-2);
        add.Invoke(null, new object[] { table, invalid, categories });
        Check(table.m_availablePieces.Count == 2, "invalid category cannot create a set-only entry");
    }

    private static object? State(string name, params object[] args) => AccessTools.Method(plugin.GetType("VeiledRecipes.VeiledRecipeState"), name).Invoke(null, args);
    private static ItemDrop Item(string name)
    {
        var item = FakeUnity<ItemDrop>();
        item.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
        item.m_itemData.m_shared.m_name = name;
        item.m_itemData.m_shared.m_dlc = "";
        return item;
    }
    private static T FakeUnity<T>() where T : UnityEngine.Object
    {
        // These managed-only paths inspect fields/equality, never dereference a native pointer.
        var value = (T)FormatterServices.GetUninitializedObject(typeof(T));
        AccessTools.Field(typeof(UnityEngine.Object), "m_CachedPtr").SetValue(value, new IntPtr(1));
        return value;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        assertions++; System.Console.WriteLine("PASS: " + name);
    }
}
