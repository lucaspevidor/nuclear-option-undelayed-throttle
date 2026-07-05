using BepInEx.Configuration;

namespace UndelayedThrottle
{
    /// <summary>
    /// Typed wrapper around the BepInEx config file. Centralises binding so the rest of the mod
    /// reads strongly-typed entries without knowing about section/key strings. Values are live —
    /// editing the .cfg (or the in-game Configuration Manager) takes effect without a restart.
    /// </summary>
    internal static class ModConfig
    {
        /// <summary>
        /// Master switch. When <c>false</c> the patch stays installed but becomes a no-op, so the
        /// fix can be toggled at runtime.
        /// </summary>
        public static ConfigEntry<bool> Enabled { get; private set; }

        /// <summary>Binds all config entries. Call once from the plugin's <c>Awake</c>.</summary>
        /// <param name="config">The plugin's <see cref="ConfigFile"/> (i.e. <c>BaseUnityPlugin.Config</c>).</param>
        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Remove the hidden negative-throttle dead zone so incremental (gamepad / relative-axis) "
                + "throttle responds immediately when raising thrust up from idle.");
        }
    }
}
