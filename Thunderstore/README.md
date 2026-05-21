# SecretRecipes

SecretRecipes changes how Valheim presents unknown recipes and build pieces. Instead of keeping every undiscovered recipe completely invisible, it can show configurable masked previews: the player learns that something exists, while names, icons, descriptions, hidden materials, and hidden station requirements stay secret until the real unlock conditions are met.

The mod does not grant recipes early. Unknown previews cannot be crafted or placed.

## Strengths

- Shows discovery progress without spoiling full recipes.
- Keeps Valheim's actual recipe and piece unlock rules intact.
- Masks crafting recipes, build pieces, item icons, descriptions, requirements, station levels, tooltips, and pinned tooltip content where supported.
- Reveals known materials and station information gradually.
- Blocks crafting and placement for entries that are only previews.
- Supports strict station discovery rules for recipe unlocks.
- Provides server-synced blacklist controls for item outputs, build pieces, and requirement ingredients.
- Offers client-side options for recipe grouping and noisy notifications.
- Exposes a compatibility API for custom crafting UI mods.

## Crafting Recipe Previews

When a crafting recipe is visible only as an unknown preview:

- The recipe list icon is rendered as a black silhouette.
- The recipe name is replaced by `Unknown Name Text`.
- The selected recipe icon is rendered as a black silhouette.
- The recipe description is replaced by `Unknown Description Text`.
- Unknown requirement names and amounts are replaced by `Unknown Requirement Text`.
- Unknown station level text is replaced by `Unknown Requirement Text`.
- Requirements the player already knows are shown normally.
- The craft button is disabled and crafting is blocked.

Recipe previews can be grouped below actually unlocked recipes so normal crafting remains easy to scan.

## Build Piece Previews

Unknown build pieces can appear in hammer/build-piece tables as masked entries.

Until the piece is actually unlocked:

- The piece icon is rendered as a black silhouette.
- The piece name and description are hidden.
- Unknown requirements are masked.
- The placement ghost is blocked.
- Placement is denied with Valheim's missing requirement message.

Once Valheim considers the piece known, SecretRecipes stops masking it.

## Station Discovery

Valheim normally discovers crafting stations when the player walks close enough to them. That station awareness can affect recipe and piece unlocks.

SecretRecipes separates two concepts:

- `Require Station Interaction For Recipe Unlock` controls whether station-gated crafting recipes require direct interaction with the required station level before the recipe can truly unlock.
- `Enable Station Proximity Discovery` controls whether walking near a crafting station discovers it through Valheim's normal proximity behavior.

Build pieces use Valheim's normal station awareness. If proximity discovery is enabled, walking near a station can help piece unlocks. If proximity discovery is disabled, interacting with the station is required before that station is known.

## Server-Synced Config

These options are synced through ServerSync when configuration locking is enabled.

- `Lock Configuration`
  Locks synced config so only the server/admin config controls gameplay behavior.

- `Show Unknown Crafting Recipes`
  Enables masked crafting recipe previews at relevant crafting stations.
  Default: `On`

- `Show Unknown Build Pieces`
  Enables masked build-piece previews in build-piece tables.
  Default: `On`

- `Require Station Level For Unknown Crafting Recipes`
  If enabled, unknown crafting recipe previews only appear when the current crafting station meets the required station level. A forge level 2 recipe will not appear at a forge level 1 unless it is already truly unlocked.
  Default: `On`

- `Require Station Interaction For Recipe Unlock`
  If enabled, station-gated crafting recipes unlock only after the player has interacted with the required crafting station level. If disabled, recipe station knowledge follows Valheim's normal known station behavior.
  Default: `On`

- `Enable Station Proximity Discovery`
  If enabled, walking near a crafting station discovers it normally. If disabled, proximity discovery is blocked and station knowledge requires interaction.
  Default: `On`

- `Recipe Preview Prefab Blacklist`
  Item prefab names whose unknown crafting recipe previews should never appear. This affects preview visibility only; unlocked recipes still appear normally.
  Default: empty

- `Piece Preview Prefab Blacklist`
  Piece prefab names whose unknown build-piece previews should never appear. This affects preview visibility only; unlocked pieces still appear normally.
  Default: empty

- `Requirement Preview Prefab Blacklist`
  Ingredient/resource prefab names that prevent unknown previews from appearing for any recipe or piece that requires them. This is useful for cheat, admin, or hidden resource prefabs.
  Default: `SwordCheat, SledgeCheat`

- `Unknown Name Text`
  Text shown when a recipe, piece, material, or station name is hidden.
  Default: `???`

- `Unknown Description Text`
  Text shown when a recipe or piece description is hidden.
  Default: `Not enough info`

- `Unknown Requirement Text`
  Text shown when an amount or station level is hidden.
  Default: `?`

## Client-Side Config

These options are not synced. Each player can choose their own UI behavior.

- `Group Unknown Recipe Previews Below Known Recipes`
  Groups masked crafting recipe previews below actually unlocked recipes in crafting station recipe lists.
  Default: `true`

- `Show Recipe Unlock Notifications`
  Shows or hides Valheim's recipe unlock popup.
  Default: `true`

- `Show Piece Unlock Notifications`
  Shows or hides Valheim's build piece and dish unlock popup.
  Default: `false`

- `Show Skill Level Up Notification/Effect`
  Shows or hides skill level-up messages and VFX/SFX, including the first time a skill reaches level 1.
  Default: `true`

## Blacklist Format

All prefab blacklist entries are case-insensitive. Whitespace is trimmed, and a trailing `(Clone)` suffix is ignored.

Entries can be separated by commas, semicolons, pipes, or line breaks.

Output blacklists match the item or piece being previewed:

```text
ArmorIronLegs, SwordIron
```

```text
piece_workbench_ext1
piece_chest
```

The requirement blacklist matches ingredient/resource prefabs used by the recipe or piece:

```text
SwordCheat, SledgeCheat
```

## Compatibility API

SecretRecipes patches Valheim's vanilla crafting and build-piece UI. Mods that draw their own recipe UI may need to call the public API and mask their own controls.

API type:

```csharp
SecretRecipes.SecretRecipesCompat
```

Useful members include:

- `PluginGuid`
- `PluginName`
- `PluginVersion`
- `Author`
- `UnknownNameText`
- `UnknownDescriptionText`
- `UnknownRequirementText`
- `GroupUnknownRecipePreviewsBelowKnownRecipes`
- `GetRecipeVisibilityState(recipe)`
- `GetRecipeVisibilityState(player, recipe)`
- `IsUnknownRecipePreview(recipe)`
- `IsUnknownRecipePreview(player, recipe)`
- `ShouldMaskRecipe(recipe)`
- `ShouldMaskRecipe(player, recipe)`
- `ShouldMaskRecipePair(pair)`
- `ShouldMaskRecipePair(player, pair)`
- `IsRecipeActuallyKnown(player, recipe)`
- `ShouldMaskPiece(piece)`
- `ShouldMaskPiece(player, piece)`
- `IsPieceActuallyKnown(player, piece)`
- `IsMaterialKnown(requirement)`
- `IsMaterialKnown(player, requirement)`
- `KnowsRecipeStationRequirement(recipe, quality)`
- `KnowsRecipeStationRequirement(player, recipe, quality)`
- `KnowsPieceStationRequirement(piece)`
- `KnowsPieceStationRequirement(player, piece)`

Soft dependency GUID:

```text
sighsorry.SecretRecipes
```

## Notes

SecretRecipes is meant to reveal possibility, not grant power. If an entry is only a preview, the player still needs to satisfy Valheim's real discovery rules before crafting or building it.
