# Changelog

## 1.1.6

- Added the server-synced `Learn Materials From Opened Containers` option, disabled by default. When enabled, items are discovered after a container's contents are displayed and when those contents change while open, without moving items or changing their quantities.
- Uses Valheim's normal item discovery, including trophy records and personal raid eligibility. Station requirements still apply, and discoveries remain on the character after the option is turned off.
- Added automated checks for container discovery, live configuration changes, and subscription cleanup when containers, players, or UI instances change.
- Updated the package's BepInEx dependency to 5.4.2351.

## 1.1.5

- Updated recipe discovery for Valheim 1.0.12 so ordinary and upgrader materials both participate while a crafting station is active, while requirement display and consumption remain station-specific.
- Removed the optional admin bypass and its recurring permission probe. Vanilla global/no-cost unlocks continue to reveal recipes and pieces, and the legacy public `IsAdminBypassActive` member remains available but always reports `false`.
- Simplified InfinityHammer compatibility by relying on the normal registered-piece boundary for runtime build-menu tools while preserving active command selections.
- Verified the built-in AAA Crafting hook, tooltip, tracker, and pagination contracts against AAA Crafting 2.1.8 without adding a dependency or modifying its DLL.

## 1.1.4

- Removed the light anti-aliased edge around veiled Hammer-tab piece icons by rendering only the black silhouette while veiled. Hover and click behavior remains intact, and known pieces without enough materials remain grey.

## 1.1.3

- Fixed unknown piece icons appearing as insufficient-material grey in the Valheim 1.0.7 Hammer piece panel by adding the same black silhouette used in the description panel. Known pieces without enough materials remain grey.

## 1.1.2

- Updated the mod for Valheim 1.0.7 client and dedicated-server assemblies while preserving configuration keys, saved station knowledge, public compatibility APIs, and optional mod boundaries.
- Updated the vendored ServerSync integration for Valheim 1.0.7, including current administrator checks and pre-login buffering for the expanded login message set.
- Added support for the new build UI and PieceTable availability structures, including masked icons, upgrade indicators, material tags, search terms, pooled button reuse, and runtime knowledge refreshes.
- Added enhanced-upgrader resource, quality, and station-level handling to recipe discovery and requirement previews without changing the game's crafting, consumption, ownership, or authority paths.
- Added managed build-UI regression checks and original client/server Mono compatibility checks for Harmony targets, runtime private access, recipe requirements, station knowledge, and build-piece categories.

## 1.1.1

- Reduced redundant preview checks in the build HUD and recipe grouping, preserving recipe-less upgrade/socket targets and existing masking rules.
- Removed the temporary requirement list allocated while updating masked crafting details, preserving quality, multicraft amounts, and cycling pages.
- Ensured configuration saving behavior is restored after initialization or reload failures, and the configuration watcher is disposed even when saving during shutdown fails.
- Simplified configuration registration while preserving existing keys, defaults, synchronization settings, and public compatibility APIs.
- Added regression checks for partial AAA pagination patch failures, rollback, and continued tooltip masking. Included the test runner in the solution with an explicit Check target and expanded the in-game verification checklist.

## 1.1.0

- Extended AAA Crafting preview grouping across the entire filtered recipe list before pagination, in both craft and upgrade tabs.
- Preserved AAA's original order within each group and restored its normal ordering when the existing grouping option is disabled.
- Re-evaluated recipe knowledge on cached page changes without modifying AAA's cache, while preserving recipe-less upgrade target handling.
- Added pagination compatibility checks and regression tests. Unsupported AAA pagination changes fall back to page-local grouping without disabling masking.

## 1.0.9

- Added automatic AAA Crafting compatibility for veiled recipe names, icons, output counts, and hover tooltips in list and grid layouts.
- Restored open hover tooltips when recipes are learned or admin bypass changes, while preserving the original tooltip text.
- Blocked tracking unknown recipes and temporarily hid previously tracked unknown entries without removing them.
- Preserved AAA's grid layout when grouping unknown previews and added compatibility regression checks. Existing compatibility APIs remain available.

## 1.0.8

- Kept loot-only and upgrade-only items unmasked and usable in owned-item upgrade/socket workflows when no enabled crafting recipe can be learned.
- Kept runtime pieces without a regular PieceTable registration unmasked, including generated InfinityHammer blueprint proxies.
- Separated actual recipe knowledge from masking/action policy and made RecipeDataPair compatibility checks target-item aware while preserving normal craft blocking.

## 1.0.7

- Simplified recipe and build-piece visibility evaluation while reducing repeated known-state and InfinityHammer compatibility checks.
- Refined Admin Bypass probing with cached lookups, separate denied/transient retry intervals, and periodic revalidation.
- Removed unused localization template code and colocated notification policy with its patches.
- Hardened release packaging with opt-in live deployment, validated manifest updates, and refreshed documentation.

## 1.0.6

- Added AzuCraftyBoxes compatibility so unknown build-piece names and descriptions stay veiled after build HUD count updates.

## 1.0.5

- Added a client-side Admin Bypass option that only takes effect for verified host/server admins.
- Fixed InfinityHammer command selections losing their placement ghost and failing with missing-info behavior.
- Improved config reload debounce and simplified piece station requirement UI handling.

## 1.0.4

- Simplified internal recipe, piece, station, and override state into focused files.
- Removed redundant custom version-handshake code in favor of ServerSync handling.
- Removed nullable-disabled patch files and shared requirement UI masking helpers.

## 1.0.3

- Fixed InfinityHammer active tool selections staying veiled and unusable.
- Limited the InfinityHammer known-piece override to tool selections so normal unknown pieces remain veiled after selection.

## 1.0.2

- Kept InfinityHammer admin menu tools unmasked by default.
- Added type-based known-piece override API for compatibility mods.
- Improved admin tool compatibility for ZoneSavior.

## 1.0.1

- Added a synced option to require crafting station knowledge before unknown build-piece previews appear.
- Added compat APIs for masking checks and known-piece overrides.
- Added Homestead support so blueprint/tool pseudo-pieces can stay unmasked.

## 1.0.0

- Initial Release
