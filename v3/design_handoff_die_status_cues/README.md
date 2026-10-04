# Handoff: Die Status Cues ("Face frame", option 1b)

## Overview
One visual language for "something is going on with this die" in Dice Kingdom: forced to block, can't block, unblockable, text blanked, granted keyword, Deadly-engaged, intimidated, buffed/debuffed, spun down, card lockout. It replaces today's one-off treatments (the orange "Must block" pill and the purple "Unblockable" pill, `.dkm-must-block` / `.dk-must-block`). The chosen system is **1b "Face frame"**: each cue becomes part of the die itself (its border, a corner, the art, or a small tab), so nothing hangs wider than the tile.

Applies to both pages: `web/src/dicekingdom/DiceKingdomMobilePage.tsx` (fixed dark) and `DiceKingdomPage.tsx` (light and dark).

## About the design files
The files in this folder are **design references made in HTML**, not production code. Rebuild them in the existing React/TSX code, mainly in `DieCube.tsx`, the mobile `DTile`, the desktop `.dietile`, and `dicekingdom.css`, following that code's patterns. Open `Die Status Cues.dc.html` in a browser to see them (it needs `support.js` beside it). Turn 2 (top of the page) is the finished 1b. Turn 1 shows all three options with every state and combination; tap any die there to open its explainer.

## Fidelity
**High fidelity.** Colours, sizes and positions below are final. The tile itself is unchanged: same `DieCube` gradient (hue 62 for yours, 250 for the opponent's), corners, `dk-damage-badge` and `.picked` ring.

---

## The system

### Colour grammar (shared by every cue)
| Category | Colour | Ink on fill | Used for |
|---|---|---|---|
| Combat | `oklch(0.85 0.15 88)` (amber) | `#14151c` | must/can't block, unblockable, must/can't attack, only blocker |
| Abilities | `#fff8ec` (= `--dkm-cream`) | `#14151c` | text blanked, granted keyword, card lockout |
| Pending KO | `oklch(0.66 0.2 25)` | `#ffffff` | Deadly-engaged |
| Out of play | `#9198b0` (= `--dkm-ink-dim`) | `#14151c` | Intimidated |
| Damage (unchanged) | `oklch(0.62 0.2 25)` | `#fff` | existing `−N` badge |

**Filled = must / has. Outlined + diagonal slash = can't / off.** No status uses an energy colour (Claw/Shell/Wing/Eye). This fixes Unblockable reading as Eye purple. Every cue also carries a shape or glyph, so colour is never the only signal.

### Where each category lives on the tile
Measurements are for the 50px mobile tile and scale with size `S` unless they're noted as fixed.

1. **Combat → tile border + lane-edge tab**
   - The tile's normal border, `1px solid oklch(0.5 0.05 hue / .7)`, is replaced by:
     - Must block / Must attack / Only blocker: `2px solid amber`
     - Can't block / Can't attack: `2px dashed amber`
     - Unblockable: `3.5px double amber`
   - **Tab** centred on the **lane-facing edge**: the bottom edge for your dice, the top edge for the opponent's. Offset `-7px`, height 13px, min-width 15px, padding `0 3px`, radius 4px, border 1.5px of the category colour, font `800 8px/1 Work Sans`, z-index above the tile.
     - Solid cues: amber fill, `#14151c` glyph.
     - Outline cues: fill `rgba(20,21,28,.92)`, amber glyph, plus a 1.5px amber slash rotated 45°.
     - Glyphs: Must block `B!` · Can't block `B` + slash · Unblockable `»` · Must attack `A!` · Can't attack `A` + slash · Only blocker `B1`.
   - Only **one** combat cue is shown at a time, picked by priority (below). The tab adds 7px below the tile (above it for the opponent), so reserve 7px of row padding on that edge whether or not a tab is showing. That way rows don't jump.
   - Can't block plus must block on the same die: show **Can't block** only, since the engine resolves it that way. The tap panel lists both, with Must block greyed and marked "Overridden by Can't block".

2. **Text blanked → struck-through art**
   - The centre animal icon drops to 28% opacity.
   - A cream bar crosses the centre: left and right 18%, 2px tall, rotated −24°, `box-shadow: 0 0 0 1px rgba(20,21,28,.7)`.

3. **Granted keyword → cream tab on the left edge**
   - Left `-6px`, vertically centred, height 12px, padding `0 2px`, radius 3px, cream fill, `#14151c` text, `800 7px/1 Work Sans`.
   - The text is `+` plus the keyword abbreviation, e.g. `+OC` for Overcrush. The tap panel spells it out.

4. **Deadly-engaged → red dog-ear, bottom-left**
   - A CSS triangle inside the tile, bottom-left (the free corner on a creature face): `border-left: 0.3·S solid oklch(0.66 0.2 25); border-top: 0.3·S solid transparent`. The tile has `overflow:hidden`, so the radius clips it.

5. **Stat change → inverted number chip**
   - When ATK or DEF differs from the printed value, the number sits on a chip with padding `1px 2px` and radius 3px:
     - Up: cream fill, `#14151c` text.
     - Down: `oklch(0.66 0.2 25)` fill, white text.
   - Tapping shows the existing `statBreakdown` text.

6. **Intimidated → dashed grey frame** in the existing Intimidated row: `2px dashed #9198b0`, tile `filter: grayscale(1)` and `opacity:.6`. Keep the row and its "Intimidated · back at end of turn" label.

7. **Card lockout (Pangolin) → hatched overlay** on the Reserve/buy tile: `repeating-linear-gradient(45deg, rgba(20,21,28,.82) 0 3px, rgba(20,21,28,.45) 3px 6px)`. The cue lives on the card, not on dice.

8. **Damage:** unchanged (`.dk-damage-badge`, top-left −6px).

### Priority when cues compete
Only the combat cue competes for a slot. Every other category has its own spot, so they stack without overflowing.
1. Deadly-engaged (always shown, own corner)
2. Combat: Can't block > Must block. Then Unblockable, Must attack, Can't attack, Only blocker (one shown)
3. Text blanked (own treatment)
4. Granted keyword (own tab)
5. Stat chips and damage badge (always)

The checked worst case is Deadly + Must block + Blanked + Granted + buffed + damaged. Everything is visible at 50px.

### Yours vs the opponent's
The rendering is the same apart from the side the tab sits on: it always points **toward the lanes** (your tab is below, theirs is above). The explainer copy changes with perspective; see `META.mine` / `META.opp` in `status-cues.js`. Example, Must block: on your die it reads "Has to block if any attacker can be blocked. Blocks won't confirm without it." On theirs: "Your opponent has to block with this if they can."

### Interaction states it must not collide with
- `.picked`: 3px `--claw` outer ring with a −3px lift. This sits **outside** the frame, and the screens show both together.
- Targeting pulse ring: also outside the frame.
- Dimmed (`opacity:.35`): the cues dim along with the tile, which is fine.

### Sizes
Mobile: 50 (your Field), 48 (opponent's), 40 (Reserve, Intimidated row, lanes), 54 (picker sheet). The border widths and tab size stay fixed at every size; the dog-ear (0.3·S) and the fonts (0.3·S) scale. Desktop: 64px tiles, same frame values.

---

## Screens (turn 2 in the HTML)

### 2a: Mobile board in context
- The opponent's Field shows Must block (tab above), Can't block, Blanked, and Buffed.
- **Unblockable lane:** the lane container's `1px hairline` border becomes `3px double amber` with background `oklch(0.85 0.15 88 / .06)`. The lane label is prefixed with an amber `»` tag (11px tall, min-width 13px, radius 3px). The empty blocker slot reads "can't be blocked" (amber, `500 8.5px Work Sans`). This needs the new **unblockable-lane** server flag.
- **Tap-to-explain panel** replaces `.dkm-inspect`'s middle content and keeps its actions below:
  - Container: `#2e3548` background, `1px solid #e78a5a` border, radius 10px, padding `10px 11px`.
  - Header: name in `400 17px Anton` uppercase white. Sub-line (`400 10px Work Sans #9198b0`) gives side and zone ("Your die · Attack · Lane 2"), then the `statBreakdown` line if there is one.
  - One row per cue: a 1px `rgba(92,100,132,.45)` top rule; a grid of `[chip | text]` with a 9px gap.
    - Chip: min-width 24px, height 16px, radius 4px, in the cue's fill/outline style with its glyph.
    - Title: `600 11.5px #fff`, followed by "· duration" in `400 #9198b0`.
    - Body: `400 10.5px/1.4 #c7cce0`, written as "From {source}. {rule}".
  - Damage row: chip `−N` on red. Copy is "KO'd when damage reaches DEF X (Y more)."
  - Durations shown as text only: "This turn", "Until Clean Up", "While {source} is active". They get no visual distinction on the tile. That was a deliberate choice: the tile stays simple and the panel says how long.
  - Tapping the same die again closes the panel.

### 2b: Desktop, light and dark
- 64px tiles in a 3-column grid with a 14px/10px gap, inside the existing panel. Light: `#fff` with `#c3c8d4` border. Dark: `#2e3548` with `#5c6484`.
- The cube is the same dark material in both themes, so frame colours don't change between them.
- The top cue's word goes in the existing `.lbl` slot under the tile: `700 9px` uppercase, letter-spacing .03em, `--text`. This replaces `.dk-must-block`. Words: MUST BLOCK, CAN'T BLOCK, UNBLOCKABLE, NO TEXT, KO AT END, "DEF 2 (was 4)".
- Hover shows the word. Click pins a popover: 220px wide, panel colours, `0 12px 30px -10px rgba(0,0,0,.45)`, arrow on the left. Its rows are the same as the mobile panel.

### 2c: Motion
Spin up/down (Energy Drain, Cuttlefish, Mutation) is the only status that animates:
- 0ms: the existing `flip` spin lands on the new face.
- 0–600ms: a 2px white ring at inset −4px with a `0 0 16px 5px rgba(255,255,255,.6)` glow, fading out.
- At the same time, a chip pops in above the die: "▼ L2 · Energy Drain", `700 9px`, white fill, `#14151c` text, radius 5px. It follows the `dkDamagePop` curve and fades out at about 1400ms.
- `prefers-reduced-motion`: no ring and no pop; the chip just shows for 1.4s.
- Ongoing states never animate.
- This needs a spin event carrying its source. The match log line can stay as it is.

### 2d: First-time legend
- A bottom sheet in the `.dkm-sheet` style, shown **once**, the first time any frame cue appears. Store a `seen` flag in localStorage. Also reachable from Help (?) → "Die frames".
- Five rows, each with 44px example dice and a title and body:
  1. Amber border = a combat rule
  2. Struck-through art = no abilities
  3. Red corner = KO'd at end of turn
  4. Cream tab = a keyword it doesn't print
  5. Number chip = changed stat
- Primary "Got it" button: `--claw` fill, 40px tall, radius 9px.

---

## Permanent keywords: recommendation
**No marker on the tile.** Mark changes, not identity: the animal icon already identifies the card, and printed keywords live on it. A badge on every die would drown out the cues that matter. Keywords stay in the tap panel. Granted keywords do get a cue, because they're a change.

## Server / state needed
Extend `Die` in `types.ts`. `mustBlock` exists today; the rest are new.
- `cantBlock`, `mustAttack`, `cantAttack`, `onlyBlocker`: boolean plus a source
- `unblockable` (exists), plus a lane-level `unblockableLanes: number[]`
- `textBlanked: { source, duration }`
- `grantedKeywords: { keyword, source, duration }[]`
- `deadlyEngaged: { source }`
- Card-level `lockedCards: { cardId, source }[]`
- A spin event: `{ dieId, fromLevel, toLevel, source }`

Recommended shape: one `statuses: { kind, source, duration: "turn" | "cleanup" | "whileActive", sourceCardId? }[]` per die. The tile and the panel both read it, so they can't drift apart (the same idea as `statBreakdown`).

## Assets
Animal icons are the existing `icons.tsx` components; the HTML uses copies of their path data. Chameleon has no icon in `CHARACTER_ICONS`, so the mock shows "Ch". Glyphs are text characters (`B! B » A! A B1 + KO`) plus CSS shapes; no new icon files are needed. Fonts: Anton and Work Sans, as the app already uses.

## Files
- `Die Status Cues.dc.html`: the page. Turn 2 is the final 1b; turn 1 has all three options with the full state and combination grid.
- `StatusTile.dc.html`: the tile with every cue. **The `sys === 'b'` branches are the spec.**
- `StatusColumn.dc.html`: the per-system gallery and the tap panel logic.
- `status-cues.js`: cue metadata (names, glyphs, priority, perspective copy) and the sample states and combinations.
- `support.js`: runtime for opening the HTML; not for implementation.

Verify on real boards afterwards with `~/.devtools/playwright/keyword-check.js`.
