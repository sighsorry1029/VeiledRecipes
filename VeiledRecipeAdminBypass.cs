using System;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace VeiledRecipes;

internal static partial class VeiledRecipeState
{
    private const string AdminProbePrefix = "veiledrecipes_admintest_";
    private const float AdminProbeRetrySeconds = 5f;
    private const float AdminProbeTimeoutSeconds = 3f;

    private static ZNet? _adminProbeZNet;
    private static long _adminProbePlayerId;
    private static string _adminProbeToken = "";
    private static bool _adminProbePending;
    private static bool? _adminProbeVerified;
    private static float _adminProbeNextTime;
    private static float _adminProbeDeadline;

    internal static void UpdateAdminBypassAccess()
    {
        if (!EnableAdminBypass)
        {
            ResetAdminBypassProbe();
            return;
        }

        PrimeAdminBypassProbe();
    }

    internal static bool ShouldBypassForAdmin(Player? player)
    {
        if (!EnableAdminBypass || player == null || player != Player.m_localPlayer)
        {
            return false;
        }

        return HasAdminBypassAccess();
    }

    internal static bool HandleAdminBypassRemotePrint(string text)
    {
        if (!EnableAdminBypass || !_adminProbePending)
        {
            return false;
        }

        if (string.Equals(text, $"Unbanning user {_adminProbeToken}", StringComparison.Ordinal))
        {
            MarkAdminBypassProbeSuccess(ZNet.instance);
            return true;
        }

        if (string.Equals(text, "You are not admin", StringComparison.Ordinal))
        {
            _adminProbePending = false;
            _adminProbeVerified = false;
            _adminProbeNextTime = Time.realtimeSinceStartup + AdminProbeRetrySeconds;
            return true;
        }

        return false;
    }

    private static bool HasAdminBypassAccess()
    {
        ZNet? znet = ZNet.instance;
        if (znet == null)
        {
            return false;
        }

        if (znet.IsServer() || znet.LocalPlayerIsAdminOrHost())
        {
            MarkAdminBypassProbeSuccess(znet);
            return true;
        }

        UpdateAdminBypassProbeState();
        if (_adminProbeVerified == true)
        {
            return true;
        }

        StartAdminBypassProbe(force: false);
        return false;
    }

    private static void PrimeAdminBypassProbe()
    {
        ZNet? znet = ZNet.instance;
        if (znet == null)
        {
            ResetAdminBypassProbe();
            return;
        }

        if (znet.IsServer() || znet.LocalPlayerIsAdminOrHost())
        {
            MarkAdminBypassProbeSuccess(znet);
            return;
        }

        UpdateAdminBypassProbeState();
        if (_adminProbeVerified == null)
        {
            StartAdminBypassProbe(force: false);
        }
    }

    private static void UpdateAdminBypassProbeState()
    {
        ZNet? znet = ZNet.instance;
        long playerId = GetLocalPlayerId();
        if (!ReferenceEquals(_adminProbeZNet, znet) || _adminProbePlayerId != playerId)
        {
            ResetAdminBypassProbe();
            _adminProbeZNet = znet;
            _adminProbePlayerId = playerId;
            _adminProbeToken = playerId > 0 ? AdminProbePrefix + playerId.ToString(CultureInfo.InvariantCulture) : "";
        }

        if (_adminProbePending && Time.realtimeSinceStartup > _adminProbeDeadline)
        {
            _adminProbePending = false;
            _adminProbeVerified = false;
            _adminProbeNextTime = Time.realtimeSinceStartup + AdminProbeRetrySeconds;
        }
    }

    private static void StartAdminBypassProbe(bool force)
    {
        ZNet? znet = ZNet.instance;
        if (znet == null || znet.IsServer())
        {
            return;
        }

        UpdateAdminBypassProbeState();
        if (_adminProbePending || string.IsNullOrWhiteSpace(_adminProbeToken))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!force && now < _adminProbeNextTime)
        {
            return;
        }

        try
        {
            _adminProbePending = true;
            _adminProbeDeadline = now + AdminProbeTimeoutSeconds;
            _adminProbeNextTime = now + AdminProbeRetrySeconds;
            znet.Unban(_adminProbeToken);
        }
        catch (Exception ex)
        {
            _adminProbePending = false;
            _adminProbeVerified = false;
            _adminProbeNextTime = Time.realtimeSinceStartup + AdminProbeRetrySeconds;
            VeiledRecipesPlugin.PluginLogger.LogDebug($"Admin bypass probe failed: {ex.Message}");
        }
    }

    private static void MarkAdminBypassProbeSuccess(ZNet? znet)
    {
        _adminProbeZNet = znet;
        _adminProbePlayerId = GetLocalPlayerId();
        _adminProbePending = false;
        _adminProbeVerified = true;
        _adminProbeNextTime = Time.realtimeSinceStartup + AdminProbeRetrySeconds;
    }

    private static void ResetAdminBypassProbe()
    {
        _adminProbeZNet = null;
        _adminProbePlayerId = 0;
        _adminProbeToken = "";
        _adminProbePending = false;
        _adminProbeVerified = null;
        _adminProbeNextTime = 0f;
        _adminProbeDeadline = 0f;
    }

    private static long GetLocalPlayerId()
    {
        long playerId = Game.instance?.GetPlayerProfile()?.GetPlayerID() ?? 0L;
        if (playerId != 0L)
        {
            return playerId;
        }

        Player? localPlayer = Player.m_localPlayer;
        return localPlayer != null ? localPlayer.GetPlayerID() : 0L;
    }
}

[HarmonyPatch(typeof(ZNet), "RPC_RemotePrint")]
internal static class AdminBypassRemotePrintPatch
{
    private static bool Prefix(string text)
    {
        return !VeiledRecipeState.HandleAdminBypassRemotePrint(text);
    }
}
