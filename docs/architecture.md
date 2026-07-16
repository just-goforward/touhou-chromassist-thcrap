# Architecture

Chromassist is a Windows-first, local-only desktop application. It uses C#/.NET 10 and WPF because the MVP needs dependable local file access, process execution, Steam-path discovery, and a portable native Windows executable. It does not need a browser runtime or a Rust/TypeScript bridge.

## Boundaries

- `Chromassist.Core`: domain models, TH18 adapter, hashing, image pipeline, THTK process adapter, and thcrap patch writer. It has no WPF dependency.
- `Chromassist.Presentation`: ViewModels, commands, localization, and UI service interfaces. It has no WPF dependency.
- `Chromassist.App`: WPF views and Windows-only implementations for file picking, dialogs, and process launch. Code-behind only wires the ViewModel.
- `Chromassist.Tests`: synthetic fixtures only.

```text
WPF View -> Presentation ViewModel -> Core interfaces -> TH18/THTK/thcrap adapters
```

This boundary allows a future Avalonia, WinUI, or web front end to reuse the Core and Presentation assemblies. The application remains local-first: game files, extracted textures, generated textures, and profile data are never uploaded.

## Data flow

1. Locate `th18.exe` from Steam libraries or a manual file choice.
2. Hash `th18.exe` and `th18.dat`; reject unknown pairs.
3. Inspect the installed thcrap version and Korean patch stack.
4. Run allowlisted `thdat` and `thanm` binaries into a private temporary directory.
5. Decode RGBA8 PNG, transform only RGB where alpha is nonzero, then verify dimensions, alpha, and fully transparent pixels.
6. Write an app-owned local repository, `repo.js`, and patch under `thcrap/repos/chromassist/`, then create a new run configuration. Never edit `thpatch-ko.js`.
7. Launch `bin/thcrap_loader.exe <generated-config> th18` only after the user presses the final button.
8. Watch newly written thcrap logs. First confirm that the generated run configuration and local patch archive entered the runtime stack; if gameplay requests the target assets, separately report how many of the six expected texture paths resolved from the local patch directory.

## Fail-closed rules

Unknown game hashes, unknown THTK hashes, incompatible thcrap versions, unexpected texture names, unsupported PNG encodings, or invariant violations stop generation. Original game archives and the existing translation configuration are read-only.
