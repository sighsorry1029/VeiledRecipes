# SecretRecipes

SecretRecipes changes Valheim's discovery UI without granting recipes or build pieces early.

Instead of hiding every recipe until the player has discovered every ingredient and station requirement, the mod can show unknown entries as masked previews. The player can see that something exists, but the important details stay secret until Valheim's real unlock conditions are met.

## What It Does

- Adds optional masked previews for unknown crafting recipes.
- Adds optional masked previews for unknown hammer/build pieces.
- Keeps vanilla recipe and piece unlock rules intact.
- Blocks crafting and placement for previews that are not actually unlocked.
- Reveals known requirements gradually, such as materials the player has already discovered.
- Can require interaction with the correct crafting station level before a recipe or piece is actually unlocked.
- Supports synced server configuration through ServerSync.

## Crafting Recipes

When an unknown recipe preview is shown, SecretRecipes masks the recipe's identity:

- The recipe list icon is shown as a black silhouette.
- The recipe name is replaced with the configured unknown name text.
- The selected recipe icon is shown as a black silhouette.
- The recipe description is replaced with the configured unknown description text.
- Unknown material names and amounts are replaced with the configured unknown requirement text.
- Unknown station level information is replaced with the configured unknown requirement text.
- Known materials and known station levels are displayed normally.

The recipe is still not unlocked. The craft button is blocked until the player has actually learned the recipe.

## Build Pieces

Unknown build pieces can also appear in build-piece tables as masked entries.

Until a build piece is actually unlocked:

- The piece icon is shown as a black silhouette.
- The piece name and description are hidden.
- Unknown requirements are masked.
- The build ghost is blocked and the piece cannot be placed.

Once Valheim considers the piece known, it appears and behaves normally.

## Station Interaction

Valheim normally unlocks station-gated recipes based on known materials and known stations. SecretRecipes can make that stricter.

When `Require Station Interaction For Unlock` is enabled, recipes and pieces that require a crafting station unlock only after the player has interacted with the required station level. For example, a forge level 2 recipe can remain unknown until the player has actually used a forge at level 2.

This is separate from preview visibility. A preview can be visible while the real recipe remains locked.

## Config

All gameplay-relevant options are synced with ServerSync when server configuration is locked.

- `Show Unknown Crafting Recipes`
  Shows masked crafting recipe previews in crafting station UIs.

- `Show Unknown Build Pieces`
  Shows masked build-piece previews in hammer/build-piece tables.

- `Require Station Level For Unknown Crafting Recipes`
  When enabled, a masked crafting recipe preview only appears if the current crafting station meets the recipe's required station level. For example, a forge level 2 recipe will not appear at a forge level 1 unless it has already been truly unlocked.

- `Require Station Interaction For Unlock`
  When enabled, station-gated recipes and pieces unlock only after the player has interacted with the required crafting station level. When disabled, Valheim's normal station discovery behavior is used.

- `Recipe Preview Prefab Blacklist`
  Comma-separated item prefab names whose masked recipe previews should never appear. This only hides unknown previews; it does not hide recipes after they are actually unlocked.

- `Piece Preview Prefab Blacklist`
  Comma-separated piece prefab names whose masked build-piece previews should never appear. This only hides unknown previews; it does not hide pieces after they are actually unlocked.

- `Unknown Name Text`
  Text used when a recipe, piece, or requirement name is hidden.

- `Unknown Description Text`
  Text used when a recipe or piece description is hidden.

- `Unknown Requirement Text`
  Text used when a required amount or station level is hidden.

## Blacklist Format

Prefab blacklist entries are matched case-insensitively. The mod also trims whitespace and ignores a trailing `(Clone)` suffix.

You can separate entries with commas, semicolons, pipes, or line breaks.

Example:

```text
ArmorIronLegs, SwordIron
```

```text
piece_workbench_ext1
piece_chest
```

## Compatibility

SecretRecipes patches Valheim's vanilla crafting and build-piece UI. Mods that draw their own recipe UI from raw `Recipe`, `RecipeDataPair`, or `ItemData` data may need to call the SecretRecipes compatibility API and mask their own custom controls.

The public API is available through:

```csharp
SecretRecipes.SecretRecipesCompat
```

Useful members include:

- `PluginGuid`
- `UnknownNameText`
- `UnknownDescriptionText`
- `UnknownRequirementText`
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

For soft dependencies, check for the BepInEx plugin GUID:

```text
sighsorry.SecretRecipes
```

## Notes

SecretRecipes is designed to reveal possibility, not grant power. If a recipe or piece appears as a preview, the player still needs to satisfy the actual discovery requirements before crafting or building it.
