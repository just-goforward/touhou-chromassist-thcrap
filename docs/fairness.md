# Fairness envelope

Chromassist is intended to recover visual distinguishability, not to increase gameplay information.

## Allowed

- RGB changes to allowlisted enemy-projectile textures;
- bounded hue and chroma changes;
- limited lightness adjustment after a documented threshold is adopted;
- preservation of existing palette-role relationships where possible.

## Forbidden

- size, resolution, alpha mask, silhouette, hitbox, trajectory, speed, count, animation timing, or game logic changes;
- background or screen-effect removal;
- added outlines, letters, symbols, patterns, or markers;
- visibility increases without a documented cap.

The MVP also excludes player projectiles, the player sprite, items, backgrounds, effects, and UI. Low-chroma neutral pixels such as white or grey projectile cores are preserved byte-for-byte. These constraints keep the intervention focused on color-role separation rather than making all moving objects more conspicuous.

The automated prototype currently enforces the enemy-projectile role allowlist, dimensions, alpha bytes, fully transparent RGBA bytes, and low-chroma neutral-pixel preservation. It records changed opaque-pixel counts and color difference but does not yet enforce empirically justified lightness or background-contrast thresholds. Therefore every non-original preset is labeled `experimental_unvalidated`.

Stage-context screenshots support direct visual comparison but are not evidence that the candidate is perceptually equivalent. They must not be used to tune a separate palette for each stage, because the generated thcrap patch currently contains one global preset.

The intended future comparison is:

1. measure the original palette under a normal-vision baseline;
2. measure the candidate under the user's selected CVD condition;
3. approach the baseline without materially exceeding it;
4. cap original-to-candidate lightness and background-contrast changes;
5. combine metrics with affected-user A/B testing and normal-vision blind review.

All initial thresholds must be configuration values described as experimental defaults, not scientific constants.
