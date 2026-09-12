using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VeiledRecipes;

[HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.Setup))]
internal static class BuildUiPieceSetupPatch
{
    private static void Prefix(BuildUiPieceButton __instance)
    {
        // The vanilla pool calls Setup repeatedly, and Setup subscribes every time.
        // Our state refresh must not multiply those subscriptions.
        Localization.OnLanguageChange -= __instance.RefreshSearchTerm;
    }
}

[HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.UpdateRequirements))]
internal static class BuildUiPieceRequirementsPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(BuildUiPieceButton __instance, Image ___m_icon, Image ___m_upgradeArrow)
    {
        ApplyAppearance(__instance, Player.m_localPlayer, ___m_icon, ___m_upgradeArrow);
    }

    private static void ApplyAppearance(BuildUiPieceButton button, Player? player, Image icon, Image upgradeArrow)
    {
        Piece piece = button.Piece;
        bool masked = player != null && piece != null && VeiledRecipeState.ShouldMaskPiece(player, piece);

        VeiledBuildPieceIconGuard? guard = button.GetComponent<VeiledBuildPieceIconGuard>();
        if (!masked)
        {
            guard?.Configure(icon, false);
            return;
        }

        if (guard == null) guard = button.gameObject.AddComponent<VeiledBuildPieceIconGuard>();
        guard.Configure(icon, true);
        upgradeArrow.gameObject.SetActive(false);
    }
}

// The 1.0 build-button prefab can overwrite its original icon tint. Render a
// separate black silhouette over it so native requirement greying cannot leak through.
internal sealed class VeiledBuildPieceIconGuard : MonoBehaviour
{
    private Image? _veil;

    internal Image? Veil => _veil;

    internal void Configure(Image icon, bool veiled)
    {
        if (_veil == null)
        {
            GameObject veilObject = new("VeiledRecipes Piece Veil", typeof(RectTransform));
            Image createdVeil = veilObject.AddComponent<Image>();
            _veil = createdVeil;
            createdVeil.transform.SetParent(icon.transform, worldPositionStays: false);
            RectTransform rect = createdVeil.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image veil = _veil;
        veil.sprite = icon.sprite;
        veil.preserveAspect = icon.preserveAspect;
        veil.raycastTarget = icon.raycastTarget;
        veil.color = Color.black;
        icon.enabled = !veiled;
        veil.gameObject.SetActive(veiled);
        if (veiled) veil.transform.SetAsLastSibling();
    }
}

[HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.RefreshSearchTerm))]
internal static class BuildUiPieceSearchPatch
{
    private static bool Prefix(BuildUiPieceButton __instance)
    {
        Player player = Player.m_localPlayer;
        Piece piece = __instance.Piece;
        if (player == null || piece == null || !VeiledRecipeState.ShouldMaskPiece(player, piece)) return true;

        List<string> terms = __instance.LocalizedSearchTerms;
        terms.Clear();
        terms.Add(Normalize(VeiledRecipeState.UnknownNameText));
        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (requirement?.m_resItem != null && VeiledRecipeState.IsMaterialKnown(player, requirement))
            {
                terms.Add(Normalize(Localization.instance.Localize(requirement.m_resItem.m_itemData.m_shared.m_name)));
            }
        }
        return false;
    }

    private static string Normalize(string text) => text.ToLowerInvariant().Replace(" ", "");
}

[HarmonyPatch(typeof(ByMaterialPieceList), nameof(ByMaterialPieceList.GetTagDisplayName))]
internal static class BuildUiMaterialTagPatch
{
    private static void Postfix(ByMaterialPieceList __instance, int index, ref string __result)
    {
        Player player = Player.m_localPlayer;
        Piece.Requirement requirement = __instance.GetTagById(__instance.GetTagIdByIndex(index));
        if (player != null && requirement != null && !VeiledRecipeState.IsMaterialKnown(player, requirement))
        {
            __result = VeiledRecipeState.UnknownRequirementText;
        }
    }
}

// The game's count-only invalidation misses preview -> known transitions. Keep the
// state with its UI lifetime and rebuild only after a relevant state change, never per frame.
[HarmonyPatch(typeof(BuildUi), "Update")]
internal static class VeiledRecipeBuildUiRefreshPatch
{
    private static int _revision;
    private static readonly ConditionalWeakTable<BuildUi, Snapshot> Snapshots = new();
    private static readonly AccessTools.FieldRef<Player, HashSet<string>> KnownRecipes = AccessTools.FieldRefAccess<Player, HashSet<string>>("m_knownRecipes");
    private static readonly AccessTools.FieldRef<Player, HashSet<string>> KnownMaterials = AccessTools.FieldRefAccess<Player, HashSet<string>>("m_knownMaterial");
    private static readonly AccessTools.FieldRef<Player, Dictionary<string, int>> KnownStations = AccessTools.FieldRefAccess<Player, Dictionary<string, int>>("m_knownStations");
    private static readonly AccessTools.FieldRef<Player, bool> NoPlacementCost = AccessTools.FieldRefAccess<Player, bool>("m_noPlacementCost");
    private static readonly Action<BuildUi, bool> RefreshTags = AccessTools.MethodDelegate<Action<BuildUi, bool>>(AccessTools.Method(typeof(BuildUi), "UpdateTagButtons"));
    private static readonly Action<Player> RefreshGhost = AccessTools.MethodDelegate<Action<Player>>(AccessTools.Method(typeof(Player), "SetupPlacementGhost"));

    internal static void Invalidate() => Interlocked.Increment(ref _revision);

    private static void Postfix(BuildUi __instance, PieceTable ___m_currentBuildTool)
    {
        Player player = Player.m_localPlayer;
        if (player == null || ___m_currentBuildTool == null || !__instance.gameObject.activeInHierarchy || !player.InPlaceMode()) return;
        Snapshot snapshot = Snapshots.GetOrCreateValue(__instance);
        if (Time.unscaledTime < snapshot.NextCheck) return;
        snapshot.NextCheck = Time.unscaledTime + 0.25f;

        HashSet<string> recipes = KnownRecipes(player);
        HashSet<string> materials = KnownMaterials(player);
        Dictionary<string, int> stations = KnownStations(player);
        bool allPieces = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllPiecesUnlocked);
        bool freePlacement = NoPlacementCost(player) || allPieces;
        bool noCostCheat = player.NoCostCheat();
        int revision = Volatile.Read(ref _revision);
        bool changed = snapshot.Player != player || snapshot.Table != ___m_currentBuildTool ||
                       snapshot.Revision != revision || snapshot.FreePlacement != freePlacement || snapshot.NoCostCheat != noCostCheat ||
                       !snapshot.Recipes.SetEquals(recipes) || !snapshot.Materials.SetEquals(materials) || !snapshot.SameStations(stations);
        if (!changed) return;

        snapshot.Player = player;
        snapshot.Table = ___m_currentBuildTool;
        snapshot.Revision = revision;
        snapshot.FreePlacement = freePlacement;
        snapshot.NoCostCheat = noCostCheat;
        snapshot.Recipes.Clear(); snapshot.Recipes.UnionWith(recipes);
        snapshot.Materials.Clear(); snapshot.Materials.UnionWith(materials);
        snapshot.Stations.Clear();
        foreach (var station in stations) snapshot.Stations.Add(station.Key, station.Value);

        // Update both availability representations through the normal patched path.
        ___m_currentBuildTool.UpdateAvailable(recipes, player, hideUnavailable: false, freePlacement);
        RefreshTags(__instance, true); // Keeps the selected tag, search and saved favorites; reuses the game's pools.
        RefreshGhost(player);
    }

    internal static void Forget(BuildUi ui) => Snapshots.Remove(ui);

    private sealed class Snapshot
    {
        public Snapshot() { }
        public Player? Player;
        public PieceTable? Table;
        public int Revision;
        public bool FreePlacement;
        public bool NoCostCheat;
        public float NextCheck;
        public readonly HashSet<string> Recipes = new();
        public readonly HashSet<string> Materials = new();
        public readonly Dictionary<string, int> Stations = new();

        public bool SameStations(Dictionary<string, int> stations)
        {
            if (Stations.Count != stations.Count) return false;
            foreach (var station in stations)
                if (!Stations.TryGetValue(station.Key, out int level) || station.Value != level) return false;
            return true;
        }
    }
}

[HarmonyPatch(typeof(BuildUi), "OnDestroy")]
internal static class BuildUiDestroyPatch
{
    private static void Prefix(BuildUi __instance, List<BuildUiPieceButton> ___m_pieceButtons,
        Stack<BuildUiPieceButton> ___m_pieceButtonPool, Stack<BuildUiPieceButton> ___m_previousPieceButtonsTemp,
        BuildUiPieceButton ___m_specialPieceButton)
    {
        VeiledRecipeBuildUiRefreshPatch.Forget(__instance);
        foreach (BuildUiPieceButton button in ___m_pieceButtons) Unsubscribe(button);
        foreach (BuildUiPieceButton button in ___m_pieceButtonPool) Unsubscribe(button);
        foreach (BuildUiPieceButton button in ___m_previousPieceButtonsTemp) Unsubscribe(button);
        Unsubscribe(___m_specialPieceButton);
    }

    internal static void Unsubscribe(BuildUiPieceButton button)
    {
        if (button != null) Localization.OnLanguageChange -= button.RefreshSearchTerm;
    }
}

[HarmonyPatch(typeof(BuildUi), "UpdatePieceButtons")]
internal static class BuildUiSpecialPieceCleanupPatch
{
    // Vanilla destroys/recreates the repair/remove button instead of pooling it.
    private static void Prefix(BuildUiPieceButton ___m_specialPieceButton) => BuildUiDestroyPatch.Unsubscribe(___m_specialPieceButton);
}
