# Threat model

## Protected assets

- the user's original game files and translation setup;
- local filesystem paths and hashes;
- temporary extracted images and generated derivative images;
- trust in downloaded THTK binaries;
- the fairness envelope.

## Main threats and controls

- **Wrong installation or game build:** exact EXE/DAT hash pair, expected filenames, fail closed.
- **Compromised or changed THTK nightly:** archive and executable hash allowlists; no shell command construction; fixed argument arrays; timeouts.
- **Path traversal from extracted names:** fixed texture mapping and contained-path checks.
- **Corrupt/decompression-bomb PNG:** CRC checks, RGBA8-only parsing, dimension and decoded-byte caps.
- **Original-file mutation:** THTK extraction only; patch output goes under a separate local repository; source run configuration is cloned, not edited.
- **Partial patch writes:** unique temporary directory and directory-level commit with rollback.
- **Private data disclosure:** no telemetry or network upload; update checker is disabled until a documented opt-in implementation exists.
- **Cheating-capability drift:** geometry/alpha tests, explicit forbidden-change policy, synthetic fixtures, code review.

The application launches the existing local `thcrap.exe` setup tool only after explicit user action. It never launches the game during scanning or preview; thcrap owns shortcut generation and later game launches.
