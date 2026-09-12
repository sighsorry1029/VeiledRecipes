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
        new Harmony("veiledrecipes.build-ui.tests").PatchAll(typeof(VeiledRecipeBuildUiRefreshPatch).Assembly);
        var player = Player.m_localPlayer = new Player();
        var wood = new Piece.Requirement("Wood");
        var secret = new Piece.Requirement("Secret Material");
        player.Materials.Add("Wood");
        var piece = new Piece { m_name = "Secret Chair", m_isUpgrade = true, m_resources = new[] { wood, secret } };
        var table = new PieceTable(); table.Pieces.Add(piece);
        var ui = new BuildUi(table);
        ui.Button.Setup(piece, ui);
        UnityEngine.UI.Image veil = ui.Button.Guard.Veil!;
        Check(!ui.Button.Icon.enabled && veil.gameObject.activeSelf && veil.color == UnityEngine.Color.black, "masked button renders only the black silhouette overlay");
        Check(veil.raycastTarget == ui.Button.Icon.raycastTarget, "silhouette preserves the original hover and click raycast area");
        ui.Button.RunImageColorUpdate();
        Check(ui.Button.Icon.color == UnityEngine.Color.white && veil.gameObject.activeSelf, "native icon updates remain covered by the veil");
        Check(!ui.Button.Upgrade.gameObject.activeSelf, "upgrade arrow is hidden");
        Check(ui.Button.LocalizedSearchTerms.SequenceEqual(new[] { "???", "wood" }), "search reveals only known information");
        Check(piece.m_name == "Secret Chair" && secret.m_resItem.m_itemData.m_shared.m_name == "Secret Material", "prefab data remains unchanged");
        ui.Button.Setup(piece, ui);
        Check(Localization.OnLanguageChange!.GetInvocationList().Length == 1, "pooled Setup does not duplicate language subscriptions");
        var other = new Piece { m_name = "Known Chair" };
        player.Recipes.Add(other.m_name);
        var freshKnownButton = new BuildUiPieceButton();
        freshKnownButton.Setup(other, ui);
        Check(freshKnownButton.GetComponent<VeiledBuildPieceIconGuard>() == null, "known button does not allocate an unused silhouette layer");
        Localization.OnLanguageChange -= freshKnownButton.RefreshSearchTerm;
        player.HasRequirements = false; ui.Button.Setup(other, ui);
        Check(ui.Button.Icon.enabled && !ui.Button.Guard.Veil!.gameObject.activeSelf && ui.Button.Icon.color == UnityEngine.Color.grey && !ui.Button.Upgrade.gameObject.activeSelf, "known piece without materials restores the original grey icon");
        Check(ui.Button.LocalizedSearchTerms.SequenceEqual(new[] { "knownchair" }), "pool reuse clears previous masked search terms");
        player.HasRequirements = true;
        ui.Button.Setup(piece, ui);
        Check(!ui.Button.Icon.enabled && ui.Button.Guard.Veil!.gameObject.activeSelf && !ui.Button.LocalizedSearchTerms.Contains("knownchair"), "pool reuse back to unknown remasks without old terms");
        var tags = new ByMaterialPieceList(wood, secret);
        Check(tags.GetTagDisplayName(0) == "Wood" && tags.GetTagDisplayName(1) == "?", "material tags respect knowledge");

        Tick(ui);
        ui.Button.RunImageColorUpdate();
        Check(ui.Button.Guard.Veil!.gameObject.activeSelf && ui.Button.Guard.Veil.color == UnityEngine.Color.black, "silhouette remains after native frame updates");
        int refreshes = ui.Refreshes;
        for (int i = 0; i < 12; i++) Tick(ui);
        Check(ui.Refreshes == refreshes, "stable UI does not rebuild");
        Check(Localization.OnLanguageChange!.GetInvocationList().Length == 1, "stable UI does not add subscriptions");

        player.Recipes.Add(piece.m_name);
        Tick(ui);
        Check(ui.Refreshes == refreshes + 1 && table.m_availablePieces.Count == 1, "unlock refreshes with unchanged availability count");
        Check(ui.Button.Icon.color == UnityEngine.Color.white && ui.Button.Upgrade.gameObject.activeSelf, "unlock restores native appearance");
        Check(ui.Button.LocalizedSearchTerms.Contains("secretchair"), "unlock restores real search terms");
        Check(player.GhostRefreshes == ui.Refreshes, "selection ghost refreshes after availability changes");

        player.Recipes.Remove(piece.m_name); player.Recipes.Add("Other recipe");
        Tick(ui);
        Check(!ui.Button.Icon.enabled && ui.Button.Guard.Veil!.gameObject.activeSelf, "same-size knowledge replacement is detected");
        player.Materials.Add("Secret Material"); Tick(ui);
        Check(ui.Button.LocalizedSearchTerms.Contains("secretmaterial"), "learning a material refreshes masked search");
        Check(tags.GetTagDisplayName(1) == "Secret Material", "learning a material restores its tag");

        player.NoCost = true; Tick(ui);
        Check(ui.Button.Icon.color == UnityEngine.Color.white, "no-cost mode restores open menu");
        player.NoCost = false; Tick(ui);
        Check(!ui.Button.Icon.enabled && ui.Button.Guard.Veil!.gameObject.activeSelf, "leaving no-cost mode remasks open menu");
        VeiledRecipeState.UnknownNameText = "Hidden";
        VeiledRecipeBuildUiRefreshPatch.Invalidate(); Tick(ui);
        Check(ui.Button.LocalizedSearchTerms.Contains("hidden"), "display configuration refreshes search");
        VeiledRecipeState.ShowPreviews = false;
        VeiledRecipeBuildUiRefreshPatch.Invalidate(); Tick(ui);
        Check(table.m_availablePieces.Count == 0, "disabling previews removes unavailable entries");
        VeiledRecipeState.ShowPreviews = true;
        VeiledRecipeBuildUiRefreshPatch.Invalidate(); Tick(ui);
        Check(table.m_availablePieces.Count == 1, "re-enabling previews restores entries");
        Check(ui.SelectedTag == 42 && ui.Search == "wood" && ui.Favorites.SetEquals(new[] { "saved favorite" }), "refresh keeps tag/search/favorite data");
        refreshes = ui.Refreshes;
        ui.gameObject.SetActive(false); player.Materials.Clear(); Tick(ui);
        Check(ui.Refreshes == refreshes, "closed menu does no refresh work");
        ui.gameObject.SetActive(true); Tick(ui);
        Check(ui.Refreshes == refreshes + 1, "reopened menu catches pending knowledge changes");
        player.Stations["workbench"] = 1; Tick(ui); refreshes = ui.Refreshes;
        player.Stations["workbench"] = 2; Tick(ui);
        Check(ui.Refreshes == refreshes + 1, "station level change with unchanged count is detected");
        ui.AddPooledButton(piece);
        ui.CreateSpecial(piece);
        int listeners = Localization.OnLanguageChange!.GetInvocationList().Length;
        ui.ReplaceSpecial();
        Check(Localization.OnLanguageChange.GetInvocationList().Length == listeners - 1, "special-button destruction removes its language callback");
        ui.Destroy();
        Check(Localization.OnLanguageChange == null, "UI destruction unsubscribes pooled button language callback");
        Player.m_localPlayer = null!; Tick(ui);
        Check(true, "no local player is safe");
        Console.WriteLine($"PASS: {assertions} BuildUi assertions; real Harmony with managed UI doubles, not game execution.");
    }
    private static void Tick(BuildUi ui) { UnityEngine.Time.unscaledTime += 0.3f; ui.Tick(); }
    private static void Check(bool result, string description)
    {
        if (!result) throw new InvalidOperationException(description);
        assertions++; Console.WriteLine("PASS: " + description);
    }
}

// Managed doubles for UI ownership/pooling. Production source is linked by the project.
namespace UnityEngine
{
    public class GameObject
    {
        private readonly List<object> components = new();
        public readonly Transform transform = new();
        public bool activeSelf = true;
        public bool activeInHierarchy => activeSelf;
        public GameObject() { }
        public GameObject(string name, params Type[] types) { }
        public void SetActive(bool value) => activeSelf = value;
        public void AddExisting(object component) => components.Add(component);
        public T? GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T AddComponent<T>() where T : new()
        {
            object component = typeof(T) == typeof(UnityEngine.UI.Image) ? new UnityEngine.UI.Image(this) : new T()!;
            components.Add(component);
            return (T)component;
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) => components.OfType<T>().ToArray();
    }
    public class MonoBehaviour { public bool enabled = true; }
    public class Transform
    {
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetAsLastSibling() { }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax;
        public Vector3 localScale;
    }
    public struct Vector2 { public static Vector2 zero => new(); public static Vector2 one => new(); }
    public struct Vector3 { public static Vector3 one => new(); }
    public class Sprite { }
    public static class Time { public static float unscaledTime; }
    public enum Color { white, black, grey }
}
namespace UnityEngine.UI
{
    public class Image
    {
        public UnityEngine.GameObject gameObject;
        public UnityEngine.Transform transform => gameObject.transform;
        public UnityEngine.RectTransform rectTransform = new();
        public UnityEngine.Color color;
        public bool enabled = true;
        public UnityEngine.Sprite? sprite;
        public bool preserveAspect;
        public bool raycastTarget = true;
        public Image() { gameObject = new UnityEngine.GameObject(); gameObject.AddExisting(this); }
        internal Image(UnityEngine.GameObject owner) { gameObject = owner; }
    }
}
public class ItemDrop
{
    public ItemData m_itemData = new();
    public class ItemData { public SharedData m_shared = new(); public class SharedData { public string m_name = ""; } }
}
public class Piece
{
    public string m_name = "";
    public bool m_isUpgrade;
    public Requirement[] m_resources = Array.Empty<Requirement>();
    public class Requirement { public ItemDrop m_resItem = new(); public Requirement(string name) { m_resItem.m_itemData.m_shared.m_name = name; } }
}
public class Player
{
    public static Player m_localPlayer = null!;
    private HashSet<string> m_knownRecipes = new();
    private HashSet<string> m_knownMaterial = new();
    private Dictionary<string, int> m_knownStations = new();
    private bool m_noPlacementCost = false;
    public HashSet<string> Recipes => m_knownRecipes;
    public HashSet<string> Materials => m_knownMaterial;
    public Dictionary<string, int> Stations => m_knownStations;
    public bool NoCost { get => m_noPlacementCost; set => m_noPlacementCost = value; }
    public bool HasRequirements = true;
    public int GhostRefreshes;
    public bool InPlaceMode() => true;
    public bool NoCostCheat() => m_noPlacementCost;
    private void SetupPlacementGhost() => GhostRefreshes++;
}
public enum GlobalKeys { AllPiecesUnlocked }
public class ZoneSystem { public static ZoneSystem instance = new(); public bool GetGlobalKey(GlobalKeys key) => false; }
public class Localization
{
    public static Localization instance = new();
    public static Action? OnLanguageChange;
    public string Localize(string text) => text;
}
public class PieceTable
{
    public readonly List<Piece> Pieces = new();
    public readonly HashSet<Piece> m_availablePieces = new();
    public void UpdateAvailable(HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
    {
        m_availablePieces.Clear();
        foreach (Piece piece in Pieces) if (VeiledRecipeState.ShowPreviews || knownRecipies.Contains(piece.m_name)) m_availablePieces.Add(piece);
    }
}
public class BuildUiPieceButton
{
    public readonly UnityEngine.GameObject gameObject = new();
    private readonly UnityEngine.UI.Image m_icon = new();
    private readonly UnityEngine.UI.Image m_upgradeArrow = new();
    private readonly ButtonImageColor m_imageColor;
    public UnityEngine.UI.Image Icon => m_icon;
    public UnityEngine.UI.Image Upgrade => m_upgradeArrow;
    public ButtonImageColor ImageColor => m_imageColor;
    internal VeiledBuildPieceIconGuard Guard => gameObject.GetComponent<VeiledBuildPieceIconGuard>()!;
    public Piece Piece { get; private set; } = null!;
    public List<string> LocalizedSearchTerms { get; } = new();
    public BuildUiPieceButton()
    {
        m_imageColor = new ButtonImageColor(m_icon);
        gameObject.AddExisting(m_imageColor);
    }
    public T? GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    public T[] GetComponentsInChildren<T>(bool includeInactive) => gameObject.GetComponentsInChildren<T>(includeInactive);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Setup(Piece pieceInfo, BuildUi buildUi)
    {
        Piece = pieceInfo;
        m_upgradeArrow.gameObject.SetActive(Piece.m_isUpgrade);
        RefreshSearchTerm(); Localization.OnLanguageChange += RefreshSearchTerm; UpdateRequirements();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateRequirements() => m_icon.color = Player.m_localPlayer.HasRequirements ? UnityEngine.Color.white : UnityEngine.Color.grey;
    public void RunImageColorUpdate() => m_imageColor.Update(Player.m_localPlayer.HasRequirements);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RefreshSearchTerm()
    {
        LocalizedSearchTerms.Clear(); LocalizedSearchTerms.Add(Piece.m_name.ToLowerInvariant().Replace(" ", ""));
        foreach (var requirement in Piece.m_resources) LocalizedSearchTerms.Add(requirement.m_resItem.m_itemData.m_shared.m_name.ToLowerInvariant().Replace(" ", ""));
    }
}
public class ByMaterialPieceList
{
    private readonly Piece.Requirement[] requirements;
    public ByMaterialPieceList(params Piece.Requirement[] items) => requirements = items;
    public Piece.Requirement GetTagById(int id) => requirements[id];
    public int GetTagIdByIndex(int index) => index;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetTagDisplayName(int index) => requirements[index].m_resItem.m_itemData.m_shared.m_name;
}
public class ButtonImageColor
{
    public readonly UnityEngine.UI.Image m_image;
    public bool enabled = true;
    public ButtonImageColor(UnityEngine.UI.Image target) => m_image = target;
    public void Update(bool interactable)
    {
        if (enabled) m_image.color = interactable ? UnityEngine.Color.white : UnityEngine.Color.grey;
    }
}
public class BuildUi
{
    public readonly UnityEngine.GameObject gameObject = new();
    private readonly PieceTable m_currentBuildTool;
    private readonly List<BuildUiPieceButton> m_pieceButtons = new();
    private readonly Stack<BuildUiPieceButton> m_pieceButtonPool = new();
    private readonly Stack<BuildUiPieceButton> m_previousPieceButtonsTemp = new();
    private BuildUiPieceButton m_specialPieceButton = null!;
    public BuildUiPieceButton Button { get; } = new();
    public int SelectedTag = 42;
    public string Search = "wood";
    public HashSet<string> Favorites = new() { "saved favorite" };
    public int Refreshes;
    public BuildUi(PieceTable table) { m_currentBuildTool = table; m_pieceButtons.Add(Button); }
    public void Tick() => Update();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Update() { }
    private void UpdateTagButtons(bool refreshOnly = true)
    {
        Refreshes++;
        UpdatePieceButtons();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdatePieceButtons()
    {
        if (m_currentBuildTool.m_availablePieces.Count > 0)
        {
            Button.Setup(m_currentBuildTool.m_availablePieces.First(), this);
        }
        m_specialPieceButton = null!;
    }
    public void ReplaceSpecial() => UpdatePieceButtons();
    public void CreateSpecial(Piece piece) { m_specialPieceButton = new BuildUiPieceButton(); m_specialPieceButton.Setup(piece, this); }
    public void AddPooledButton(Piece piece) { var button = new BuildUiPieceButton(); button.Setup(piece, this); m_pieceButtonPool.Push(button); }
    public void Destroy() => OnDestroy();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void OnDestroy() { }
}
namespace VeiledRecipes
{
    internal static class VeiledRecipeState
    {
        public static bool ShowPreviews = true;
        public static string UnknownNameText = "???";
        public static string UnknownRequirementText = "?";
        public static bool ShouldMaskPiece(Player player, Piece piece) => !player.NoCostCheat() && !player.Recipes.Contains(piece.m_name);
        public static bool IsMaterialKnown(Player player, Piece.Requirement requirement) => player.NoCostCheat() || player.Materials.Contains(requirement.m_resItem.m_itemData.m_shared.m_name);
    }
}
