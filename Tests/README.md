# AAA Compatibility Checks

Build and run with the local Harmony/Cecil dependencies from `environment.props` on Windows:

```powershell
dotnet build Tests/AaaCompatibility.Tests.csproj -c Release -t:Check

# Optionally also inspect the supported AAA DLL's contract:
dotnet build Tests/AaaCompatibility.Tests.csproj -c Release -t:Check '-p:AaaDllPath=C:\path with spaces\AzuAntiArthriticCrafting.dll'
```

The test project is included in `VeiledRecipes.sln`. A normal project or solution build only compiles the runner; the explicit `Check` target builds it and executes it, failing the command if an assertion fails. The optional `AaaDllPath` property adds read-only DLL contract checks. The supported contract is AAA 2.1.6; an older or changed DLL is expected to fail those checks.

The runner links the production compatibility code and visibility enum, and applies real Harmony patches to managed UI doubles. It also patches an already-registered paginator prefix/postfix to exercise fresh and cached craft pages, upgrade pairs, filtering, target-item context, and grouping changes. A temporary test-only transpiler preserves the upgrade fixture's behavior while changing its IL: production initialization successfully installs the craft paging hook, then fails on the upgrade hook. Checks verify paging rollback, preservation of other Harmony owners, live tooltip masking after failure, and successful paging reinstallation. No production test hook is used.

Passing an AAA DLL additionally verifies its actual hook signatures, fields, and pagination IL without loading or modifying the DLL. The state policy and Unity UI are doubles: these checks do not validate production recipe knowledge, persistence, networking, Unity destroyed-object semantics, rendering, or AAA's complete UI. They are automated managed checks, not an in-game test.

In-game checks with AAA 2.1.6:

- List and grid modes, craft and upgrade tabs: unknown names/icons/output counts stay hidden; known entries stay normal.
- Hover by mouse and gamepad, select a recipe, switch pages, search, change filters, reopen the crafting UI.
- Use several pages with mixed known/unknown recipes: all known entries precede the previews across pages in craft and upgrade views. Names and variants keep AAA's order within each group.
- Turn preview grouping off and on, then switch pages: original AAA order restores when off, while masking remains enabled and grid cells retain their layout.
- Learn a recipe or toggle admin bypass and change pages (including cached page flips): grouping uses current knowledge. Test searches, filters, favorites, and partial last pages.
- Learn a recipe or toggle admin bypass while hovering: text masks/restores without moving the pointer.
- Try tracking an unknown recipe. Track with bypass, disable bypass, then learn it: the entry hides and returns without being deleted.
- Verify known ingredients still show, unknown ingredients stay hidden, and recipe-less upgrade/socket targets remain usable.
- Start without AAA: vanilla UI and existing API consumers still behave normally. ZenUI-specific integration is outside this patch.

## Core changes: in-game verification

These checks remain separate from the managed AAA runner:

- Crafting requirements: compare known and unknown ingredients at each quality, multicraft amounts, maximum quality, empty requirements, more requirements than visible slots, and the last partial cycling page. The allocation-free display walks requirements twice; check compatibility with mods that patch `Piece.Requirement.GetAmount`.
- Recipe knowledge: compare ordinary craft entries, recipe-less upgrade/socket targets, and outputs with enabled or seasonal crafting recipes. Check grouping with previews disabled or blacklisted, with and without admin bypass.
- Build UI: check known, unknown, blacklisted, repair/remove, runtime, and InfinityHammer pieces. Include AzuCraftyBoxes HUD updates; both HUD masking patches must remain effective. Reopen and recreate UI to check destroyed or missing elements.
- Crafting and placement: blocked actions must leave inventory and world objects unchanged. Allowed actions, multicraft, repeated input, and cancellation must consume and create only the expected items; preserve the selected upgrade target.
- Station persistence: exercise proximity and interaction settings, station level changes, save/rejoin, and synchronized settings on a client, host, and dedicated server.
- Plugin lifecycle: test configuration read/write failure and a file event near shutdown. Check config saving behavior after failures and watcher cleanup. Test actual BepInEx/ServerSync behavior in addition to any managed failure-injection checks.
- Performance: compare CPU time, GC Alloc, and Canvas rebuilds with the same recipe/piece set, separating list refreshes from an open crafting UI and build HUD's per-frame work.
