# C1 raw archive reader — November 2, 2026

Review status: local verification passed; final independent PR review is pending; see [publication evidence](2026-11-03-c1-publication.md). No publication, original fidelity or milestone completion is claimed.

## Scope and preservation

User request: start C1 as well. Base `origin/main` was `3804707`; branch/worktree is `codex/c1-blo-reader` at `.tools/issue-worktrees/c1-blo-reader`. The prior owner released `claim/c1-blo-reader` at `319a1ac` and main's reviewed brief reserves C1 for Codex. Local claim commit: `7cbd437`. The primary checkout's review-queue edits and the separate C7 branch remain untouched. Old local uncommitted WIP in `blo-hull-resolution` was read only and preserved; its reference/hull resolvers are not part of C1.

Authority: [original C1 brief](../tasks/c1.md), [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). [Raw contract and unresolved dependencies](../original-research/C1_BLO_READER_CONTRACT.md). No canonical source, foundation manifest, toolchain or production gameplay changes. Registry397/22/375 and original feature boxes remain unchanged.

## Implemented evidence

-   Complete 89-entry canonical pin catalog derives from the exact pinned Git LFS pointer blobs, with all hydrated hashes and36 foundation overlaps independently checked. Original bytes total95,243,854;87 v2 archives,2 v1;1,262 members.
-   Raw v1/v2 reader preserves ordinal identity, full name bytes, offset/length and opaque payload. Exact allowlisted canonical paths are checked before I/O; actual read bytes are size/hash verified. Public diagnostic parsing remains unverified. Owned snapshots and copy-out buffers avoid caller mutation.
-   Independent Python-generated per-archive oracle hashes every full raw name field, span and payload hash. Canonical tests reproduce all89/all1262 twice and compare raw source slices; three nonzero trailing name fields are retained. Latin-1 diagnostic names are explicitly NEW/provisional beyond observed ASCII.
-   Root found hash amplification in the first draft: constructing/hash-checking members before overlap validation could hash a repeated large malformed span thousands of times. The original implementer changed this to metadata/bounds/overlap validation first, member construction/hash second, and added both-version4096-entry adversarial fixtures. No timing threshold is used; the source ordering is independently reviewable.

## Focused verification

Pinned SDK8.0.419 with existing ignored SDK/home/NuGet cache; originals explicitly provided through RT_ORIGINAL_ASSETS_ROOT. No global/toolchain changes.

| Check                                            | Actual result                                                                                                                                                                                                  |
| ------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Author initial `FullyQualifiedName~Blo` filter   | Exit0,71 passed,0 failed/skipped. This broad substring also includes50 existing Block-related cases; **21 are new C1 cases**, including the all89 canonical test. `artifacts/test-results/c1-blo-focused.trx`. |
| Root `dotnet test tests/Unit --no-restore`       | Exit0, **1043 passed,0 failed,5 skipped**,1048 total. One NU1900 unavailable vulnerability-feed warning. `artifacts/test-results/c1-unit.trx`.                                                                 |
| `python scripts/original-assets/test_decoder.py` | Exit0,10 passed.                                                                                                                                                                                               |
| `python scripts/original-assets/test_staging.py` | Exit0,4 passed; synthetic stage fixture only.                                                                                                                                                                  |
| Root reference verifier                          | Exit0, `REFERENCE_INTEGRITY_PASS: 46 originals` at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. Root script is unchanged from the C1 branch.                                                                    |
| Tracker/format/diff                              | TRACKERS_CONSISTENT, FORMAT_CLEAN and git diff --check exit0.                                                                                                                                                  |

All89 original archive checks executed; no C1 asset test skipped. Five pre-existing OriginalKeyListingTests skip for separately configured supplemental source. No Godot, native-client, compatibility-runtime/delivery, gameplay simulation, export or benchmark run: no corresponding code or completed gameplay changed. The C1 brief's parser/staging/integrity checks and shared-code Unit map were followed.

## Qualification and review

Unavailable matrix models were substituted explicitly: Sonnet5.5/Medium→GPT-6.1 Sol/Medium for the approved utility and pin generation; Opus5.5/High→GPT-6.1 Terra/High for unfamiliar format/security review after Astra capacity was unavailable; Haiku5.5/Medium→GPT-6.1 Sol/Medium for verified status documentation. Source, metadata and documentation ownership were disjoint; builds were serialized. Root inspected actual source/tests and reproduced evidence rather than accepting completion messages alone.

Independent reviewer has re-derived all89 pointer identities and all1262 directory/member tuples with zero mismatches. The reviewer also reran the focused21 cases, full units1043/0/5, decoder10, staging4 and integrity46 at `55a1218`, all passing as recorded separately. The final status-only reconciliation requires a documentation re-review against that tested source. No author self-merge.

## Resume / publication boundary

Use the [C1 review](2026-11-03-c1-publication.md) for current verification and pending final review; author status-only reconciliation does not change source, tests or canonical data. Publication was authorized November 3, 2026; final PR review, CI and merge remain pending. Before any PR-ready BBQ marker, run the required sprint integration and validation and record its actual verdict. C1 never completes RT-F023/RT-F069 or native dependency closure.
