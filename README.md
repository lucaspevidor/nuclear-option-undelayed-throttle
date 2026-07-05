# UndelayedThrottle

A [BepInEx 5](https://docs.bepinex.dev/) mod for **Nuclear Option** that removes the hidden
negative-throttle "dead zone" from the game's incremental (gamepad / relative-axis) throttle.

By **Lucas Pevidor**.

## The problem

With incremental throttle (e.g. gamepad **RB = increase / LB = decrease**), the game integrates a
private accumulator, `PilotPlayerState.simulatedThrottle`, over the range `[-1, 1]` and feeds the
flight model `Mathf.Clamp01(simulatedThrottle)`.

Because only the **output** is clamped, holding *decrease* past idle drives the accumulator
**below zero** (down to `-1`). Throttle reads 0% the whole time, but you then have to "spend" that
hidden negative distance by holding *increase* before thrust leaves idle — a dead zone of up to a
full second.

## The fix

A Harmony **postfix** on `PilotPlayerState.PlayerThrottleAxis1Controls` floors `simulatedThrottle`
at `0` every frame, so it can never bank negative headroom. Raising thrust from idle then responds
on the very next frame.

The patch is intentionally a no-op when `PlayerSettings.throttleUseNegative` is enabled (a
centre-detented analog throttle, where the negative half of the range is used legitimately).

See [`../docs/`](../docs) for the full investigation and BepInEx/Harmony background.

## Configuration

`BepInEx/config/com.lucaspevidor.undelayedthrottle.cfg`

| Key | Default | Effect |
|-----|---------|--------|
| `General / Enabled` | `true` | Toggle the fix at runtime (no restart needed). |

## Building

Requires the .NET SDK and a local Nuclear Option install.

```bash
dotnet build -c Release
# or point at a non-default install:
dotnet build -c Release -p:GameDirectory="D:\SteamLibrary\steamapps\common\Nuclear Option"
```

On success the DLL is auto-deployed to `<GameDir>/BepInEx/plugins/UndelayedThrottle/`. Launch the
game and look for `UndelayedThrottle ... patch active` in `BepInEx/LogOutput.log`.

## Compatibility

- Nuclear Option (Unity 2022.3.x, Mono) + BepInEx 5.
- Client-side only — it changes how *your* throttle input is generated, so it is multiplayer-safe
  and does not require the host to run the mod.
- If a future game update renames the patched method or field, the mod logs an error and disables
  itself rather than crashing.
