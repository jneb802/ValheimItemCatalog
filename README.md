# Valheim Item Catalog

A BepInEx mod that exports the loaded item catalogue once per server process.
Created from `valheimModTemplateMacOS`. No client installation is required.

The exporter waits for the world, ObjectDB, and network prefab scene, then waits
for five stable item-count checks. It writes every item accepted by ObjectDB's
prefab lookup, including modded items. It records display names, localization keys,
stack sizes, quality limits, item types, game version, loaded plugin versions,
language, and export time. Unresolved translations are marked explicitly.

Output defaults to `BepInEx/item-catalog.json`. A complete temporary file replaces
the previous export atomically. Export failure leaves the previous file intact
and writes an error to the BepInEx log. No network calls or game-state changes occur.

## Configuration

`BepInEx/config/warpalicious.ValheimItemCatalog.cfg`:

```ini
[Export]
OutputPath = item-catalog.json
ExportOnClients = false
```

OutputPath can be absolute or relative to BepInEx. Dedicated servers export by
default. Set ExportOnClients to true only for local validation. Export uses the
loaded language; the refund bot requires English. Do not change the language
during startup. Items registered later during gameplay require a fresh startup.

## Refund integration

Point Praetoris bot's `REFUND_CATALOG_PATH` at the export. The bot must be able to
read it. Restart the game server after mod updates so the catalogue represents the
deployed release. No OpenRouter key is needed on the exporter.

## Prerequisites

- macOS with Valheim installed via Steam
- .NET SDK 8.0+ (`brew install dotnet`)
- [BepInEx for macOS](https://github.com/BepInEx/BepInEx/releases) installed in Valheim
- Publicized assemblies in `Managed/publicized_assemblies/`

## Quick Start

```bash
dotnet build -c Release
```

Copy `bin/Release/ValheimItemCatalog.dll` into `BepInEx/plugins/ValheimItemCatalog/`.

## Build paths

Edit `Environment.props` if your Steam library is in a non-standard location. By default it uses `$HOME/Library/Application Support/Steam/steamapps/common/Valheim`.

## License

MIT

## Validation

Release build: zero warnings and errors. On 2026-09-30, Valnet client 01 loaded
the mod with the shared DLLs from deployed Praetoris Season 8 release 8.0.30
(verified by SHA-256). With ExportOnClients enabled, startup exported 2,007 items
in English. Twelve unresolved translations were marked; no exporter error occurred.
The refund service successfully used this export in live OpenRouter/Jev requests.

Dedicated-server startup remains untested because Valdev was leased by another
test. Server-only mod and configuration effects still require a server export check.
This mod does not alter Server Chest delivery behavior.
