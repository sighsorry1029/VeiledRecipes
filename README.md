# VeiledRecipes

See undiscovered crafting recipes and build pieces as masked previews. Learn their materials and meet the configured station requirements to reveal them; unknown entries cannot be crafted or placed.

![Crafting preview with a silhouette and hidden recipe details](https://i.ibb.co/fVF9LcWq/Screenshot-2026-05-21-215826.png)

Known ingredients stay visible while undiscovered details remain hidden.

![Unknown recipes grouped below known recipes](https://i.ibb.co/chcXxxgp/Screenshot-2026-05-21-221859.png)

Known recipes appear first by default.

![Crafting previews limited by the current station level](https://i.ibb.co/tMV4ryLM/Screenshot-2026-05-21-221754.png)

By default, previews appear only when the current station meets the required level.

![VeiledRecipes used alongside InventorySlots](https://i.ibb.co/Z1tWJtG2/Screenshot-2026-05-21-214653.png)

Masked previews alongside InventorySlots.

![Unknown build piece with masked details and requirements](https://i.ibb.co/5XDR5gkR/Screenshot-2026-05-21-215857.png)

Build pieces use silhouettes and hide unknown requirements too.

## Getting started

Install with your mod manager and the required BepInEx dependency. For shared multiplayer settings, install VeiledRecipes on the server or host and on each player's client.

Launch once to create `BepInEx/config/sighsorry.VeiledRecipes.cfg`. Existing saved settings are preserved when updating.

## Settings

**Station discovery:** By default, recipes that need a crafting station require interaction with that station at the required level before unlocking. Build pieces use normal station knowledge, so walking near a station is enough to discover it. Unknown build-piece previews also require any needed station to be known by default. Adjust `Require Station Interaction For Recipe Unlock` and `Enable Station Proximity Discovery` to change discovery behavior.

**Container learning:** `Learn Materials From Opened Containers` is **On** by default. Opening an accessible container discovers its displayed items without moving them, including equipment and trophies. Station requirements still apply. This uses normal item discovery, so it also updates trophy records and can affect personal raid eligibility. Turning it off does not erase knowledge already learned.

You can also:

- Toggle crafting and build previews, change placeholder text, or exclude previews by item, piece, or ingredient prefab name. Blacklists affect unknown previews only.
- Change recipe grouping and notifications. Recipe unlock and skill notifications are on by default; build-piece and dish unlock notifications are off.

Gameplay settings and placeholder text sync from the server and are locked to admins by default. Grouping and notification settings are personal to each client. The generated config describes every option.

## Compatibility

- **AAA Crafting:** Built-in support masks unknown recipes in list/grid views and tooltips, and hides them from the recipe tracker. Preview grouping also applies to its recipe pages.
- **CraftFrom:** Discovered ingredients show CraftFrom's available and required amounts in masked previews. Unknown ingredients stay hidden.
- **[InventorySlots](https://thunderstore.io/c/valheim/p/sighsorry/InventorySlots/):** Can be used alongside VeiledRecipes, as shown above.
- **InfinityHammer:** Tool and generated-piece workflows remain usable. Normal registered build pieces still follow discovery rules.
- **Recipe-less upgrades and sockets:** Operations on an existing item remain unmasked when that item has no learnable crafting recipe.

Other custom crafting panels may need their own integration. ZenUI's custom panels are not covered by the AAA integration.

[GitHub repository](https://github.com/sighsorry1029/VeiledRecipes)
