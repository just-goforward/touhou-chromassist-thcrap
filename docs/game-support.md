# Game support

## MVP: Touhou 18

The current adapter supports one locally observed installation only:

| Field | Value |
|---|---|
| Game | Touhou 18 ~ Unconnected Marketeers |
| Version label | v1.00a |
| Distribution | Steam original |
| EXE SHA-256 | `9ED66E6952459E81515C17A671410BEE7014A83E3C6CC6A7E360E7B4904C62F4` |
| DAT SHA-256 | `3949E7C01BDEF9C3FE75711E088BFE4E195F3A657585C79B6A1AFB9D117DC800` |
| Minimum tested thcrap | `2025-12-02` |

These hashes identify the user's local installation observed on 2026-07-15. They are not claimed to represent every legitimate Steam installation. Other hashes fail closed until independently inspected and added with evidence.

The adapter reads `bullet.anm` from `th18.dat` and uses THTK's unique texture extraction. The six current runtime replacement paths are:

```text
th18/bullet/bullet1@bullet@0.png
th18/bullet/bullet2@bullet@1.png
th18/bullet/bullet3@bullet@2.png
th18/bullet/bullet4@bullet@6.png
th18/bullet/bullet5@bullet@7.png
th18/bullet/bullet6@bullet@9.png
```

The six extracted textures are classified as `EnemyProjectile`. Player shots, the player sprite, items, lasers, effects, backgrounds, UI, and additional sprite archives are outside the first implementation scope. The patch writer fails closed if an adapter supplies any role other than `EnemyProjectile`; adding another role requires an explicit adapter change and separate fairness validation.

The strength-step context preview uses a checked-in list of representative 16×16 atlas coordinates, not checked-in game pixels. It composites only those cells over a screenshot selected by the user. This layout is a usability sample, not a complete semantic map of every TH18 projectile frame, and must be reverified if a new asset hash is admitted.
