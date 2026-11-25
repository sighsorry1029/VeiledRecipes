# Valheim 1.0.30 compatibility

Reviewed on 2026-11-25, starting from `main` commit `852875e` (including its
existing CraftFrom integration). The compatibility fixes required no dependency
upgrade. The manifest already uses BepInExPack 5.4.2360.

## Original inputs and build references

Preserved snapshots under `C:/Users/blizz/.codex/references/valheim/snapshots/`:

| Role | Snapshot | Original assembly_valheim SHA-256 |
| --- | --- | --- |
| Client | `client-b26870846-windows-x64-20261125T102540Z` | `9b7eeda2705790b33bb66dd0f092d65eb24f73333b061648a186bd687f534ca4` |
| Dedicated server | `dedicated-server-b26870897-windows-x64-20261125T102540Z-depot-restored` | `4299a2e2c22cbdbe52c1985e20319cb6f7c602945edcc703ac48e4c153ba2373` |

Both snapshots report game 1.0.30 and have validated original/derived data.
Original files are under `original/`; C# and metadata were reused from
`derived/ilspy-9.1.0.7988-r1/`. The server's documented depot restoration affects
only `steam_appid.txt`, not a managed DLL. No game files were recollected or
publicized for analysis. The cumulative 1.0.16→1.0.20→1.0.21→1.0.30 guides and
relevant original method bodies were reviewed; this is not a full asset audit.

The local Steam installation used by the original build was still 1.0.21.
`environment.props` now selects the preserved 1.0.30 client using the overridable
`ValheimReferencePath` property. All Unity/game references, including TextMeshPro,
come from that reference directory. The BepInEx/Harmony and deployment paths
remain in the existing Steam installation. That installation must run 1.0.30
before it can serve as an in-game test of this patch.
The existing project fallback can select the installed game if this snapshot
directory is absent; other environments must provide a valid original 1.0.30
`ValheimReferencePath` and check the resolved `CorlibPath` before building.

The existing publicizer remains limited to compilation intermediates. The
private `InventoryGui.RecipeDataPair` public integration contract is preserved;
no new private access was added. Runtime checks use unmodified original game
assemblies on the corresponding preserved Mono runtime. This verifies selected
managed access paths, not all Unity/native execution.

## Changes and preserved behavior

- Removed the obsolete `BuildUi.OnDestroy` patch: this target no longer exists.
  The baseline compiled successfully but failed the original-assembly Harmony
  target check with `MissingMethodException` for `BuildUiDestroyPatch`.
- Removed the old pooled-Setup unsubscribe and special-button cleanup patches.
  In 1.0.30, `BuildUiPieceButton.Awake` subscribes once and its `OnDestroy`
  unsubscribes. Keeping the old Setup prefix would remove the native callback.
- Preserved the weak per-UI knowledge snapshot and throttled refresh. Disabling
  the menu does not destroy its pooled buttons or discard the snapshot; stable
  reopening does not add a mod-triggered rebuild. No replacement lifecycle
  patch, per-frame search, or new cache was introduced.
- Kept icon silhouettes, search masking, favorites, station/material discovery,
  configuration keys/defaults, saved station data, and public/optional APIs.
  Crafting and upgrade RNG remain the game's responsibility. Container learning
  retains local-player, owner, visibility, inventory identity, and cleanup checks.
- Kept the fixed `valheim-1.0.7-r1` ServerSync input. Its hash, final merge,
  relevant login ordering, and 1.0.30 contracts remain valid in the tested scope.
  No new network RPC, permission override, item transfer, or save format is added.

Outside BuildUi, 42 selected method names (45 overloads) were compared between
1.0.16 and 1.0.30. Only `InventoryGui.DoCrafting` changed; known crafting is still
passed to that original method. The relevant client/server discovery, inventory,
piece and UI types match. Optional AAA, CraftFrom and InfinityHammer adapter
boundaries were retained; this does not certify every external mod release.

## Validation

- `dotnet build VeiledRecipes.sln -c Debug -p:DeployToGame=true`: passed with
  zero warnings/errors, including ServerSync internalization. Only the final
  plugin DLL was deployed; its SHA-256 matched the build output.
- Managed doubles with real Harmony: BuildUi **38**, container discovery **30**,
  AAA compatibility **53** assertions passed. The UI fixture now models native
  button-owned subscriptions and verifies pool reuse, actual language callbacks,
  repeated closing/reopening, special-button replacement, and destruction.
- Original 1.0.30 Mono: **128 checks per role**, client and dedicated server.
  Includes Harmony targets/field injection, original lifecycle IL, cached private
  accessors, recipe discovery, piece insertion, and CraftFrom argument forwarding.
  These checks do not install native detours or start a Unity scene/network session.
- ServerSync checker: per role **1,193 references**, **12 Harmony patches**,
  **15 explicit reflection accesses**, **17 ISocket contracts** passed. Buffer
  harness: **19/19 per role** with original ZPackage and fake transport.
  The merged library's 200 method bodies match the fixed input after normalized
  operand comparison. No direct references to the three removed Managed
  libraries (`System.Memory`, `System.IO.Hashing`, `System.Runtime.CompilerServices.Unsafe`)
  were present in the vendor or merged mod.

Commands and check limitations are in [Tests/README.md](../Tests/README.md).
Existing generated outputs, external mod implementations, all Unity assets,
and unrelated game subsystems were excluded from implementation changes.

## Remaining in-game checks

Actual client, listen-host, dedicated-server, crossplay, and non-Windows sessions
were not run. Verify repeated Hammer-menu opening, language changes, masked/known
icons, search/favorites/gamepad selection, and repair/remove button replacement.
Verify live knowledge changes, container denial/ownership handoff/closing,
server configuration sync, saving/rejoining, and known crafting/upgrading without
item loss or duplication. Exercise AAA/CraftFrom/InfinityHammer combinations
separately. The compatibility checks above preceded the separate 1.1.8 release
preparation and do not establish in-game compatibility or publication status.
