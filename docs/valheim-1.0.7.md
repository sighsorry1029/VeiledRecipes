# Valheim 1.0.7 compatibility patch — 2026-09-10

## Scope and changes

Target: Windows client Steam build **25185596** and dedicated server build
**25185644**, both Valheim **1.0.7**. Comparison baseline: 0.221.12 client
21981559 / dedicated server 21981590. The compatibility patch is released as
mod version **1.1.2**.

- `VeiledRecipePiecePatches`: use the new `HashSet<Piece>` availability index
  together with Harmony-injected category buckets. Populate buckets before the
  original update; insert previews into both representations once, including
  Deep North and All categories. Preserve placement guards.
- `VeiledRecipeBuildUiPatches`: mask the new pooled build buttons, upgrade arrows,
  search terms and material tag labels. Known material terms remain searchable;
  user favorite labels and prefab data remain intact. Refresh after knowledge,
  settings, override registration or bypass changes even when counts are equal.
  Reuse game pools and preserve the selected tag, search and favorites. Unknown
  buttons receive a reusable black silhouette layer over the native icon, so
  later requirement-color updates cannot expose its grey image. Known pieces hide
  the layer and retain the game's insufficient-material grey state.
- The new UI refresh caches private accessors and checks reusable knowledge
  snapshots at most four times per second while building. Stable frames do not
  rebuild the UI or search the scene. Set comparison still costs O(known entries)
  per check; actual CPU, allocations and Canvas work require game profiling.
- Make pooled button language subscriptions idempotent and remove subscriptions
  from active, pooled and special buttons when destroyed. Config events are
  removed during plugin shutdown.
- `VeiledRecipePreviewRules` / `VeiledRecipePatches`: use the game's ordinary vs
  upgrader resource applicability rule for discovery and requirement display.
  Keep discovery, blacklist and consumption policies separate. Allow enhanced
  upgrade previews past ordinary maximum quality and show upgrader station level
  1, matching the original game. Existing crafting and item mutation paths remain.
- Recompile message calls against the new five-parameter `Message` signature.
  Use the equivalent public recipe/material knowledge getters where available.
- Vendor the fixed ServerSync `valheim-1.0.7-r1`; see
  [its provenance and integration notes](../Libs/ServerSync.md).

No configuration keys, customData station keys, save format, public integration
signatures or mod version requirements changed. AAA and InfinityHammer remain
optional. No new RPC, inventory mutation, network ownership or authority policy
is introduced. Existing Harmony guards and state transfer remain; new UI patches
are listed in the source rather than relying on reflection-only discovery.

## Game references and runtime access

The previous installed publicized game DLL predated 1.0.7. Build inputs now start
from the current original `$(CorlibPath)` DLLs. `assembly_utils` and
`assembly_guiutils` are referenced directly. `assembly_valheim` is publicized
**only into build intermediates** using the pinned
`BepInEx.AssemblyPublicizer.MSBuild` **0.4.2** task in `GameReferences.targets`.
The project is non-SDK, so the task is invoked explicitly. The final DLL carries
the generated `IgnoresAccessChecksTo` attribute and existing unsafe compilation
metadata. See the [upstream tool documentation](https://github.com/BepInEx/BepInEx.AssemblyPublicizer).

This limited compile-time step preserves the already published API containing
the game's private `InventoryGui.RecipeDataPair` type. Moving every reference to
an original-only compiler input would require changing that API. Original DLLs
were used for analysis and runtime checks, never overwritten or included in the
mod package. New private UI access uses cached Harmony accessors/field injection.
Successful compilation alone does not establish runtime access compatibility.

## Verification completed

- Debug solution build with `DeployToGame=true`: **0 warnings, 0 errors**. Only
  the final merged plugin is deployed; source and installed SHA-256 match.
- Release solution build: **0 warnings, 0 errors**. Assembly and manifest version
  are 1.1.2; the Thunderstore package contains the matching merged DLL, manifest,
  changelog, README and icon. The Nexus package contains the matching merged DLL.
- Existing AAA managed checks: **53 passed**.
- New build UI managed checks: **34 passed**, including the native icon/overlay
  distinction, single-layer silhouette edges and raycast preservation, pooled
  reuse and equal-size
  knowledge replacement, bypass toggles, search/tag masking, stable updates,
  deferred reopen refresh and language-event cleanup.
- Original-assembly checks on the actual client and server Mono libraries:
  **118 passed each**. Resolve Harmony targets, signatures and injected fields;
  initialize cached accessors; execute the existing private-pair API and managed
  ingredient/station/category paths against original game types.
- ServerSync isolated checks: **19 passed per target**. Final merged ServerSync
  method instructions match the reviewed vendor input; no external ServerSync
  assembly reference remains.
- Static comparison: public/protected API unchanged (**58 entries**), no missing
  game member references. The generic visibility checker still reports existing
  private accesses and server type-forwarding injection mismatches. These are
  not a clean static pass; runtime signature resolution and the limited Mono
  access checks above provide separate evidence.

Commands and test limitations are in [Tests/README.md](../Tests/README.md).
The Mono runner does not start Unity: it supplies two native initializer stubs
(Object layout query and Animator hash), uses managed fixtures, and queues but
does not run ServerSync startup. It does not install native Harmony detours.

## Still required in the game

No actual Unity client, host, dedicated session or crossplay session was run.

1. Open new build UI with unknown/known pieces; switch categories, material tags,
   search, favorites and repair/remove. Unlock without changing available count,
   change language and settings, close/reopen and recreate UI. Profile GC Alloc,
   CPU and Canvas rebuilds with a large piece list.
2. Switch repeatedly between ordinary stations and the enhanced upgrader, with
   new and already recorded stations, both station-discovery settings, missing
   ingredients and qualities past the ordinary maximum. Verify crafting,
   multicraft, cancellation and placement preserve inventory and world counts.
3. Verify station knowledge save/rejoin, admin bypass revocation, locked settings
   and version rejection on client/host/dedicated and crossplay. Include repeated
   input, duplicate messages and disconnect/reconnect; confirm no item loss or
   duplication. The patch does not replace the game's consumption/authority code.
4. Repeat relevant UI flows with AAA 2.1.6, InfinityHammer and AzuCraftyBoxes, then
   without those optional mods. Verify plugin shutdown/config watcher cleanup.

Station timing is intentionally unchanged: original `SetCraftingStation` calls
`AddKnownStation` (which may refresh recipes) before assigning `m_currentStation`.
The existing interaction prefix also records knowledge before that assignment.
The patch follows vanilla's current-station resource rule; it does not introduce
a speculative alternate unlock policy or temporary station mutation. Station
transition behavior remains an explicit in-game verification item.

## Evidence

Local original snapshots and preserved audit/implementation results are under
`C:/Users/blizz/.codex/references/valheim/`. The comparison directory is
`comparisons/0.221.12--1.0.7-windows-x64/mods/VeiledRecipes-1.0.7-audit-20260910`.
Its original audit is retained separately from the subsequent implementation
record, hashes and test logs.
