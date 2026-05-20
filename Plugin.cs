using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace SecretRecipes;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class SecretRecipesPlugin : BaseUnityPlugin
{
    internal const string ModName = "SecretRecipes";
    internal const string ModVersion = "1.0.0";
    internal const string Author = "sighsorry";
    public const string ModGUID = $"{Author}.{ModName}";
    private static string ConfigFileName = $"{ModGUID}.cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    private readonly Harmony _harmony = new(ModGUID);
    public static readonly ManualLogSource PluginLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
    private FileSystemWatcher? _watcher;
    private readonly object _reloadLock = new();
    private DateTime _lastConfigReloadTime;
    private const long RELOAD_DELAY = 10000000; // One second

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        bool saveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;

        // Uncomment the line below to use the LocalizationManager for localizing your mod.
        // Make sure to populate the English.yml file in the translation folder with your keys to be localized and the values associated before uncommenting!.
        //Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).

        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        ShowUnknownCraftingRecipes = config("2 - Secret Recipes", "Show Unknown Crafting Recipes", Toggle.On, "Shows crafting recipes at the relevant crafting station before the recipe is fully unlocked.");
        ShowUnknownBuildPieces = config("2 - Secret Recipes", "Show Unknown Build Pieces", Toggle.On, "Shows build pieces in build-piece tables before the piece is fully unlocked.");
        RequireStationLevelForUnknownCraftingRecipes = config("2 - Secret Recipes", "Require Station Level For Unknown Crafting Recipes", Toggle.On, "If on, unknown crafting recipe previews are shown only when the current crafting station meets the recipe's required station level.");
        RequireStationInteractionForUnlock = config("2 - Secret Recipes", "Require Station Interaction For Unlock", Toggle.On, "If on, recipes and pieces that require a crafting station unlock only after the player has interacted with the required station level. If off, Valheim's normal station discovery is used.");
        RecipePreviewPrefabBlacklist = config("2 - Secret Recipes", "Recipe Preview Prefab Blacklist", "", "Comma-separated item prefab names whose unknown crafting recipe previews should never be shown. This does not hide recipes after they are actually unlocked. Example: ArmorIronLegs, SwordIron");
        PiecePreviewPrefabBlacklist = config("2 - Secret Recipes", "Piece Preview Prefab Blacklist", "", "Comma-separated piece prefab names whose unknown build piece previews should never be shown. This does not hide pieces after they are actually unlocked. Example: piece_workbench_ext1, piece_chest");
        UnknownNameText = config("3 - Display", "Unknown Name Text", "???", "Text shown for unknown recipe and piece names.");
        UnknownDescriptionText = config("3 - Display", "Unknown Description Text", "Not enough info", "Text shown for unknown recipe and piece descriptions.");
        UnknownRequirementText = config("3 - Display", "Unknown Requirement Text", "?", "Text shown for unknown requirement names, amounts, and station levels.");

        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();

        Config.Save();
        if (saveOnSet)
        {
            Config.SaveOnConfigSet = saveOnSet;
        }
    }

    private void OnDestroy()
    {
        SaveWithRespectToConfigSet();
        _watcher?.Dispose();
    }

    private void SetupWatcher()
    {
        _watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        _watcher.Changed += ReadConfigValues;
        _watcher.Created += ReadConfigValues;
        _watcher.Renamed += ReadConfigValues;
        _watcher.IncludeSubdirectories = true;
        _watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        DateTime now = DateTime.Now;
        long time = now.Ticks - _lastConfigReloadTime.Ticks;
        if (time < RELOAD_DELAY)
        {
            return;
        }

        lock (_reloadLock)
        {
            if (!File.Exists(ConfigFileFullPath))
            {
                PluginLogger.LogWarning("Config file does not exist. Skipping reload.");
                return;
            }

            try
            {
                PluginLogger.LogDebug("Reloading configuration...");
                SaveWithRespectToConfigSet(true);
                PluginLogger.LogInfo("Configuration reload complete.");
            }
            catch (Exception ex)
            {
                PluginLogger.LogError($"Error reloading configuration: {ex.Message}");
            }
        }

        _lastConfigReloadTime = now;
    }

    private void SaveWithRespectToConfigSet(bool reload = false)
    {
        bool originalSaveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;
        if (reload)
            Config.Reload();
        Config.Save();
        if (originalSaveOnSet)
        {
            Config.SaveOnConfigSet = originalSaveOnSet;
        }
        
        // If you want to do something once localization completes, LocalizationManager has a hook for that.
        /*Localizer.OnLocalizationComplete += () =>
        {
            // Do something
            PluginLogger.LogDebug("OnLocalizationComplete called");
        };*/
    }


    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    internal static ConfigEntry<Toggle> ShowUnknownCraftingRecipes = null!;
    internal static ConfigEntry<Toggle> ShowUnknownBuildPieces = null!;
    internal static ConfigEntry<Toggle> RequireStationLevelForUnknownCraftingRecipes = null!;
    internal static ConfigEntry<Toggle> RequireStationInteractionForUnlock = null!;
    internal static ConfigEntry<string> RecipePreviewPrefabBlacklist = null!;
    internal static ConfigEntry<string> PiecePreviewPrefabBlacklist = null!;
    internal static ConfigEntry<string> UnknownNameText = null!;
    internal static ConfigEntry<string> UnknownDescriptionText = null!;
    internal static ConfigEntry<string> UnknownRequirementText = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }

    class AcceptableShortcuts() : AcceptableValueBase(typeof(KeyboardShortcut))
    {
        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;

        public override string ToDescriptionString() => $"# Acceptable values: {string.Join(", ", UnityInput.Current.SupportedKeyCodes)}";
    }

    #endregion
}

public static class KeyboardExtensions
{
    extension(KeyboardShortcut shortcut)
    {
        public bool IsKeyDown()
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public bool IsKeyHeld()
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}

public static class ToggleExtentions
{
    extension(SecretRecipesPlugin.Toggle value)
    {
        public bool IsOn()
        {
            return value == SecretRecipesPlugin.Toggle.On;
        }

        public bool IsOff()
        {
            return value == SecretRecipesPlugin.Toggle.Off;
        }
    }
}
