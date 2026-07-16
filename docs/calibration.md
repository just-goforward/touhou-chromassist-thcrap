# Calibration and preset selection

The simplified launcher MVP does not run a diagnostic or screening test. A user directly chooses a broad internal Protan, Deutan, or Tritan category using localized user-facing terminology, then adjusts an experimental 0–100% strength while comparing the original and adjusted local bullet texture continuously during slider movement.

User-facing terminology is intentionally broader than a diagnosis. Korean uses recognizable red-family, green-family, and blue-yellow-family color-vision-deficiency labels. Japanese follows the current `1型色覚`, `2型色覚`, and `3型色覚` convention described by the [Japanese Ophthalmological Society](https://www.nichigan.or.jp/public/disease/symptoms.html?catid=84). English uses Protan/Deutan red-green CVD and Tritan blue-yellow CVD, aligned with the category structure explained by the [US National Eye Institute](https://www.nei.nih.gov/eye-health-information/eye-conditions-and-diseases/color-blindness/types-color-vision-deficiency). The application does not distinguish anomaly from anopia and must not imply that it has diagnosed either.

Before a future screening or manual-calibration flow begins, the UI must recommend:

- disabling night mode and blue-light filters;
- disabling unusual color filters;
- using a comfortable brightness;
- using sRGB or a standard display mode when available;
- otherwise matching the environment in which the user normally plays.

It must also display this or equivalent text prominently:

> This result is not a medical diagnosis and may be affected by the display and surrounding environment.

Future screening tasks must be independently generated color-discrimination tasks, not copied Ishihara plates. Users must be able to skip the task, override its result, choose `unknown`, undo choices, and use manual settings. Actual user choice takes precedence over model recommendations.
