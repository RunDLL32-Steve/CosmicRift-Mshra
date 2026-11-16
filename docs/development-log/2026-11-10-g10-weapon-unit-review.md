# G10 W2/W3 review follow-up — November 10, 2026

PR #133 still derives the three starting cannons from the pinned original ITM records. Muzzle velocity, lifetime and delay retain the reconstructed units. The fire-delay and lifetime conversions now divide the raw tick count by 100 in floating point so valid 32-bit raw fields cannot wrap before conversion. A synthetic v57 projectile row at the positive integer limit exercises both paths. The original-asset oracle verifies the committed cannon JSON.

Windows SDK 8.0.419 unit run passed 1,240 cases with five archived-key skips and zero failures. Native runtime and export checks were skipped because the change affects only original-data conversion and its unit coverage. The master and feature checklist counts remain unchanged.
