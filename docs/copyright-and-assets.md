# Copyright and local assets

The repository and public release packages must not contain Touhou game images, music, executable files, archives, extracted assets, or generated derivatives.

Chromassist only reads a game installation that the user selects or that Steam reports on the same Windows computer. Temporary extraction occurs under the user's local application-data directory and is deleted when the application closes. Generated patches remain local and are ignored by Git.

Optional gameplay screenshots selected for context comparison are decoded locally into bounded in-memory images. Their paths, pixels, and composed previews are not written to a profile, cache, patch, log, telemetry service, or repository. Users remain responsible for the lawful creation and handling of screenshots.

This local-only compromise avoids the project maintainer redistributing game data, but it does not itself establish permission to modify data or guarantee legal compliance. Users should not upload or redistribute generated patches containing derived images.

The project is unofficial and is not affiliated with 上海アリス幻樂団, ZUN, Touhou Project, Steam, THTK, thcrap, or thpatch. Touhou Project and its game assets remain the property of their respective rights holders.

Relevant policy sources should be reviewed before each release:

- [Touhou Project derivative-work guidelines](https://touhou-project.news/guideline/)
- [THTK source and license](https://github.com/thpatch/thtk)
- [thcrap source and license](https://github.com/thpatch/thcrap)
