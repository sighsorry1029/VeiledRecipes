# Vendored ServerSync

`ServerSync.dll` is the reviewed `valheim-1.0.7-r1` common baseline, applied on
2026-09-10. Its assembly version remains `1.0.0.0`; the baseline ID and hash identify
this update. The license is in `ServerSync.LICENSE.txt`.

- Upstream: https://github.com/blaxxun-boop/ServerSync
- Upstream commit: `c57c2aa54e07cdcc7630d6068699ea781622323e`
- SHA-256: `b4dd786997f4e90d770f09ef3e9d64154754fe7e8edfb4841795751895b35846`
- Replaced SHA-256: `166956302a294e224474b26f4c7d58409084ad3f48bd0af1feb7551f229c8f60`
  (exact match to the common baseline input; no additional binary differences).
- Source, diff, original reference hashes, reproduction and verification tools:
  `C:/Users/blizz/.codex/references/valheim/integrations/serversync/versions/valheim-1.0.7-r1/`.

## Changes and integration

The library was rebuilt against original Valheim 1.0.7 assemblies: client Steam
build 25185596 and dedicated server build 25185644. This removes obsolete field
loads of `ZRoutedRpc.Everybody`, uses the equivalent public `ZNet.IsAdmin(string)`
administrator check, and also buffers `PlayerList`, `HistoricalPlayerList`,
`AdminList` and `NetTime` before login completes. Existing queue order, package
copy/cursor behavior, VersionMatch boundary and control-message pass-through stay
in place. The administrator-list reload watcher remains.

`VeiledRecipes.csproj` references this local DLL and copies it to
`bin/<configuration>/Libs/ServerSync.dll`. `ILRepack.targets` merges that exact
input into `VeiledRecipes.dll` with internalization. Install only the merged mod
DLL; do not install ServerSync as a separate runtime plugin. Do not apply an old
binary patch to this already fixed library.

The mod's ConfigSync identifier, version requirements, configuration keys,
locking policy and public integration API are unchanged. This update does not
claim compatibility with older Valheim versions.

## Verification scope

Consumer-specific results and commands are recorded under:
`C:/Users/blizz/.codex/references/valheim/comparisons/0.221.12--1.0.7-windows-x64/mods/VeiledRecipes-ServerSync-20260910/`.

That ServerSync-only report predates the subsequent main-mod compatibility
patch. The main build now derives its compile reference from the 1.0.7 original;
see [the full implementation and verification scope](../docs/valheim-1.0.7.md).
ServerSync results alone do not certify the rest of the mod. Actual Unity client,
host, dedicated-server and crossplay sessions remain unverified.
