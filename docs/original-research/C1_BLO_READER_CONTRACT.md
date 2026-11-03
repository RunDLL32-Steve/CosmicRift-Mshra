# C1 raw BLO directory and member contract

Authority: [C1 brief](../tasks/c1.md), [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). This component reads archive structure and opaque member bytes. It does not decode pixels, resolve gameplay references or establish native behavior. RT-F023 and RT-F069 remain open; registry **397 entries, 22 scoped completions, 375 open** is unchanged.

## Sources and identity

Canonical source is `fowlmouth/cosmic-rift-assets@eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. All 89 BLO files are Git LFS objects at that pin. Their exact-case paths, raw byte lengths and SHA-256 identities are independently derived from its committed LFS pointer blobs into [PinnedBloArchives.cs](../../shared/RogueTrader.OriginalData/PinnedBloArchives.cs). All 89 hydrated files match those identities; 36 also occur in the unchanged 46-entry [foundation manifest](ASSET_MANIFEST.json). The new derived catalog does not expand or replace that manifest.

The archive set totals 95,243,854 raw bytes: 87 version-2 archives, two version-1 archives (`BLO/icons.blo`, `BLO/uiwav.blo`), and 1,262 members. These are verified local byte observations, not inferred runtime coverage. [Existing Python directory parser](../../scripts/original-assets/index_dependencies.py) and [decoder directory parser](../../scripts/original-assets/decode_blo.py) supply independently implemented comparisons. Their maintained [format provenance](../../scripts/original-assets/IMPLEMENTATION_PLAN.md#format-provenance) identifies the pinned primary format references. No upstream implementation is copied into C1.

The [test oracle](../../tests/Unit/Fixtures/BloDirectoryOracle.json) records each archive's identities, version, count, directory end and a hash over every member's ordinal, complete raw name field, offset, length and payload hash. Its directory records were produced by the existing Python parser, independently of the new C# parser. The JSON describes its exact UTF-8/tab/LF digest convention; no original payload or pixel is embedded.

The [third-party notices](../../THIRD_PARTY_NOTICES.md) and manifest retain the source's redistribution limits. Public source availability is not a blanket license. Canonical bytes stay in the ignored reference checkout, and derived metadata stays separate.

## Format facts and preservation

| Region          | Verified layout                                                                               |
| --------------- | --------------------------------------------------------------------------------------------- |
| Header          | Little-endian unsigned 32-bit version at byte0, member count at byte4                         |
| Directory start | Byte8, original zero-based ordinal order                                                      |
| Version1 entry  | 14 raw name bytes, unsigned 32-bit offset, unsigned 32-bit length                             |
| Version2 entry  | 32 raw name bytes, unsigned 32-bit offset, unsigned 32-bit length                             |
| Directory end   | `8 + count * (nameLength + 8)`, computed with widened arithmetic before allocation or slicing |
| Payload         | Exactly the source slice at its stored offset/length; no decompression or interpretation      |

Every raw name field is retained, including absent terminators and bytes after the first NUL. Three canonical entries have nonzero trailing bytes: `SkithMulti` ordinal0, `gravity` ordinal5 and `shipselect` ordinal4. Requiring zero padding would discard original evidence.

Canonical bytes before the first NUL are all ASCII. `Name` is a **NEW/provisional Latin-1 diagnostic projection**, with no normalization; it is not proof of a historical non-ASCII codec. `RawNameField` is authoritative. Synthetic duplicate, empty and path-like names remain separate ordinal identities. No method uses those diagnostic names to write or extract filesystem paths.

Gaps and zero-length spans are permitted by the raw reader; all current canonical payloads are nonempty and contiguous. Unknown versions, truncated headers/directories, spans inside the directory or past EOF, and overlapping nonempty spans fail with `BloArchiveFormatException`. All metadata bounds and overlaps are validated before member payload hashing, preventing repeated overlapping spans from amplifying hash work. Unsigned fields are widened before addition/multiplication and only narrowed after bounds prove they fit the owned byte array.

## Public API and integrity boundary

-   `BloArchiveReader.Parse(bytes, sourcePath)` copies caller bytes and always returns `IsVerified=false`. Synthetic and diagnostic parsing cannot acquire canonical acceptance merely by choosing a filename.
-   `ReadCanonical(assetsRoot, relativePath)` first requires an exact ordinal path in the complete 89-entry allowlist. Unknown, rooted, traversal and case-changed paths fail before filesystem access; there is no unguarded fallback. It allocates only the pinned size, checks read length, verifies SHA-256 over the actual owned bytes, then parses and returns `IsVerified=true`.
-   Archive bytes, member payloads and complete name fields are held privately. Copy-out APIs return detached arrays; changing the caller's original input or a returned copy cannot change stored identities, hashes or payloads. The member collection is read-only.

Missing files, altered sizes, unhydrated LFS pointers and same-size corruption cannot pass the canonical integrity boundary. The component performs no source writes. Byte/format integrity is distinct from runtime eligibility.

## Dependencies still unresolved

The [existing directory inventory](../../scripts/original-assets/DEPENDENCY_PLAN.md) and [G9 ledger](../../scripts/original-assets/plan.d/g9.md) retain **eight raw residual candidates**:

| Raw residuals                                                                          | Availability and remaining scope                                                                                                                    |
| -------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Five help filenames: `keys.txt`, `infcmdlst.txt`, `comms.txt`, `rules.txt`, `mods.txt` | Absent from the canonical archive set; authentic supplemental acquisition is dated accepted history. Native HelpMenu binding remains open.          |
| `UtilityEffects/PrizeCrateA`                                                           | Canonical member is absent; runtime role/revision remains open under RT-F377.                                                                       |
| `cruiart/logo`                                                                         | Canonical exact-name member is absent; intended visual/fallback behavior remains open under RT-F378.                                                |
| Numeric Ball role                                                                      | Supplemental `Soccer/Ball` is distinct from canonical `ball.blo/GameBall`; trailing field and native evaluator semantics remain open under RT-F379. |

The separate missing item66 relationship remains RT-F386; it is not an additional raw archive-inventory residual. Name encoding outside the observed ASCII set, opaque payload decoding, asset delivery, native appearance and all runtime reference eligibility remain outside this component. No reader success closes those boxes.

Acceptance results, revisions and review are recorded in the [dated C1 evidence](../development-log/2026-11-02-c1-blo-reader.md).
