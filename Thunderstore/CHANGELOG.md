| `Version` | `Update Notes`    |
|-----------|-------------------|
| 1.0.6     | - Added AzuCraftyBoxes compatibility so unknown build-piece names and descriptions stay veiled after build HUD count updates. |
| 1.0.5     | - Added a client-side Admin Bypass option that only takes effect for verified host/server admins.<br>- Fixed InfinityHammer command selections losing their placement ghost and failing with missing-info behavior.<br>- Improved config reload debounce and simplified piece station requirement UI handling. |
| 1.0.4     | - Simplified internal recipe, piece, station, and override state into focused files.<br>- Removed redundant custom version-handshake code in favor of ServerSync handling.<br>- Removed nullable-disabled patch files and shared requirement UI masking helpers. |
| 1.0.3     | - Fixed InfinityHammer active tool selections staying veiled and unusable.<br>- Limited the InfinityHammer known-piece override to tool selections so normal unknown pieces remain veiled after selection. |
| 1.0.2     | - Kept InfinityHammer admin menu tools unmasked by default.<br>- Added type-based known-piece override API for compatibility mods.<br>- Improved admin tool compatibility for ZoneSavior. |
| 1.0.1     | - Added a synced option to require crafting station knowledge before unknown build-piece previews appear.<br>- Added compat APIs for masking checks and known-piece overrides.<br>- Added Homestead support so blueprint/tool pseudo-pieces can stay unmasked. |
| 1.0.0     | - Initial Release |
