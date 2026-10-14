# Supplied Cosmic Rift visual reference

Master: [ROGUE_TRADER_2_PLAN.md](../../ROGUE_TRADER_2_PLAN.md), §2, §§5–8, §16 and §18 M0/M1. Tracking: [research subplan](IMPLEMENTATION_PLAN.md), [InfantryOnline source audit](INFANTRYONLINE.md) and [development progress](../development-log/PROGRESS.md). Reviewed: October 12, 2026.

The user's target is **“exactly this with assets — but for the Rogue Trader mode”**, strengthened to a **100% reproduction** using [canonical fowlmouth originals unchanged](COSMIC_RIFT_ASSETS.md). This sets a fidelity requirement for the preserved Cosmic Rift presentation and content. The first placeholder prototype remains a temporary engineering step; it is not the requested visual/content completion. The [user-designated RT map view](COSMIC_RIFT_ASSETS.md#user-designated-rogue-trader-map-view) is an additional source, separate from this Chaos Zone video.

| Metadata | Value / access evidence |
|---|---|
| Title | Cosmic Rift - Chaos Zone ---- Infantry Online |
| URL | [YouTube reference](https://www.youtube.com/watch?v=A6u1ljFgQVU) |
| Channel | Infantry Online; title/channel also confirmed through public YouTube oEmbed metadata |
| Publication | November 20, 2023; observed in browser by the lead implementation agent |
| Duration | 12:23; observed in browser by the lead implementation agent |
| Mode | Chaos Zone, as identified by the title; not a Rogue Trader economic demonstration |
| Actual visual inspection | One paused frame at **0:25**, inspected in the browser by the lead implementation agent and reported for this shared research record |
| Remaining access | Direct web-text fetch failed. oEmbed returned metadata. No claim that the whole video was watched; no downloaded footage or measured trajectory data. |

## Limited observed presentation

At 0:25 the browser frame showed a black starfield, small rotated textured spacecraft, visible projectile traces, narrow green-outline status/text/player-list panels on the right, and a green-outline chat area along the bottom. These observations establish a presentation reference at that instant. They do not establish physics constants, damage formulas, original ship identity, sprite animation counts, or Rogue Trader inventory, economy and progression behavior.

Evidence classification: **RECONSTRUCTED visual reference**, limited to the observed frame of a community-hosted Cosmic Rift session. Confidence is medium for the listed visible layout features. Implementation status: recorded as a target; matching asset records and rendering have not been implemented or visually validated.

## Visual/content parity checklist

- [x] Preserve the supplied URL, metadata, mode and actual inspected timestamp.
- [x] Record only the frame observations actually reported by the lead agent.
- [ ] Inspect ship rotation, scale, direction frames and animation at actual additional timestamps.
- [ ] Inspect thrust effects, projectile shape/trails, impact/explosion effects and target readability.
- [ ] Measure and compare starfield/world rendering, camera movement and zoom behavior.
- [ ] Compare right-panel widths, typography/colors, status values and player-list layout.
- [ ] Compare bottom chat region, message formatting and overall viewport proportions.
- [ ] Match video-visible items to the preserved blob IDs; validate correct asset variants rather than filenames alone.
- [ ] Inspect Rogue Trader ship selection, cargo, mineral/refinery/trading-post, equipment and progression UI from actual RT footage or recovered primary files.
- [ ] Test and document the completed rendering/content against the reference at matching observations.

Visual similarity in Chaos Zone is useful for baseline parity. Rogue Trader-specific behavior comes from its own recovered sources, including `Rogue.nws`, `rogue.cfg`, `Rogue.veh`, `Rogue_mp.itm`, `Rogue.rpg` and the map/object definitions listed in [INFANTRYONLINE.md](INFANTRYONLINE.md).
