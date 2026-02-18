# Documentation Generation

This project automatically generates Markdown documentation for the Pexip.Pulse NuGet package.

## How It Works

- **Automatic**: Documentation is regenerated on every build
- **Local Tool**: Uses `xmldocmd` as a local .NET tool (tracked in `dotnet-tools.json`)
- **No Manual Setup**: Other developers just need to restore tools (happens automatically)

## For New Developers

When you first clone this repo, the tools will be automatically restored during the first build. If you need to manually restore:

```sh
dotnet tool restore
```

## Manual Documentation Generation

To manually regenerate documentation:

```powershell
.\generate-docs.ps1
```

Or directly:

```sh
dotnet xmldocmd "%USERPROFILE%\.nuget\packages\pexip.pulse\1.0.16785\lib\net8.0\Pexip.Pulse.dll" .\docs
```

## Generated Documentation

Documentation is generated in the `docs/` folder with the following structure:

- `docs/Pexip.Pulse.md` - Main entry point
- `docs/Pexip.Pulse.NativeMethods/` - Native method documentation
- `docs/Pexip.Pulse.NativeStructs/` - Struct documentation
- `docs/Pexip.Pulse.NativeEnums/` - Enum documentation

## Updating Pexip.Pulse Version

When the Pexip.Pulse package is updated:

1. Update the version in `generate-docs.ps1`
2. Run `.\generate-docs.ps1` to regenerate docs
3. Commit the updated documentation

## Tool Information

- **Tool**: xmldocmd
- **Version**: 2.9.0
- **Repository**: https://github.com/ejball/xmldocmd
- **Manifest**: `dotnet-tools.json`
