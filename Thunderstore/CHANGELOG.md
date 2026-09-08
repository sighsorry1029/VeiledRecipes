# Changelog

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
