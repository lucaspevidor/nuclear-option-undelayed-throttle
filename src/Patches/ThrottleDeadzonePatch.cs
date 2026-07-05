using HarmonyLib;

namespace UndelayedThrottle.Patches
{
    /// <summary>
    /// Removes the hidden negative accumulation in <c>PilotPlayerState.simulatedThrottle</c>.
    ///
    /// <para>
    /// Nuclear Option models incremental throttle (gamepad RB/LB, or any axis in "relative" mode)
    /// by integrating a private accumulator, <c>simulatedThrottle</c>, over the range [-1, 1] and
    /// then feeding the flight model <c>Mathf.Clamp01(simulatedThrottle)</c>. Because only the
    /// <em>output</em> is clamped, holding "decrease" past idle drives the accumulator below zero
    /// (down to -1). Throttle reads 0% the whole time, but the pilot must then "spend" that hidden
    /// negative distance by holding "increase" before thrust leaves idle — a dead zone of up to a
    /// full second.
    /// </para>
    ///
    /// <para>
    /// This postfix floors the accumulator at zero every frame, so it can never bank negative
    /// headroom; raising thrust from idle then responds on the very next frame.
    /// </para>
    ///
    /// <para>
    /// Guard: when <c>PlayerSettings.throttleUseNegative</c> is enabled the game maps the full
    /// [-1, 1] accumulator onto [0, 1] (a centre-detented analog throttle). There the negative half
    /// is meaningful, so the patch intentionally does nothing.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(PilotPlayerState), MethodName)]
    internal static class ThrottleDeadzonePatch
    {
        private const string MethodName = "PlayerThrottleAxis1Controls";
        private const string FieldName = "simulatedThrottle";

        /// <summary>
        /// Cached, delegate-based accessor to the private accumulator. Resolved once in
        /// <see cref="Prepare"/>; invoked every FixedUpdate, so per-call reflection is avoided.
        /// </summary>
        private static AccessTools.FieldRef<PilotPlayerState, float> _simulatedThrottle;

        /// <summary>
        /// Harmony calls this before patching. Verifies the target method and backing field still
        /// exist in the loaded game assembly and caches the field accessor. Returning <c>false</c>
        /// skips the patch, so a game update degrades gracefully instead of throwing.
        /// </summary>
        private static bool Prepare()
        {
            bool hasMethod = AccessTools.Method(typeof(PilotPlayerState), MethodName) != null;
            bool hasField = AccessTools.Field(typeof(PilotPlayerState), FieldName) != null;

            if (!hasMethod || !hasField)
            {
                Plugin.Log?.LogError(
                    $"Incompatible game build: PilotPlayerState.{MethodName} found={hasMethod}, "
                    + $"field '{FieldName}' found={hasField}. Patch skipped.");
                return false;
            }

            _simulatedThrottle = AccessTools.FieldRefAccess<PilotPlayerState, float>(FieldName);
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(PilotPlayerState __instance)
        {
            if (!ModConfig.Enabled.Value)
            {
                return;
            }

            // Centre-detented analog throttle uses the negative half of the range legitimately.
            if (PlayerSettings.throttleUseNegative)
            {
                return;
            }

            ref float simulatedThrottle = ref _simulatedThrottle(__instance);
            if (simulatedThrottle < 0f)
            {
                simulatedThrottle = 0f;
            }
        }
    }
}
