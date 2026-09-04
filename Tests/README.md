# AAA Compatibility Checks

Build and run with the local Harmony/Cecil dependencies from `environment.props`:

```powershell
dotnet build Tests/AaaCompatibility.Tests.csproj -c Release
& ./Tests/bin/Release/net48/AaaCompatibility.Tests.exe 'path/to/AzuAntiArthriticCrafting.dll'
```

The runner links the production compatibility code and applies real Harmony patches to managed UI doubles. It also patches an already-registered paginator prefix/postfix to exercise fresh and cached craft pages, upgrade pairs, filtering, target-item context, and grouping changes. Passing an AAA DLL additionally verifies its actual hook signatures, fields, and pagination IL without loading or modifying the DLL. These checks do not emulate Unity rendering or AAA's complete UI.

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
