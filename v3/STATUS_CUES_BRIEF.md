# Dice Kingdom — Die Status Cues: Design Brief

For a Claude Design pass (2026-10-03). The goal is **one visual language for
"something is going on with this die"**: forced to block, can't block,
unblockable, text blanked, temporarily removed, marked for death, buffed,
and so on. Today each state got its own one-off treatment as it shipped,
and they've started to collide. Implementation comes back to the main
session afterward (server flags + both pages), so this brief describes
what exists and what's needed. It doesn't prescribe a look.

## The game in one paragraph

A two-player dice-battler (Dice Masters–style, animal theme). Each player
fields creature dice into their **Field**. On your turn you send attackers
into one of four shared **lanes**, and the opponent assigns blockers.
**A lane is the unit of combat:** every attacker in a lane fights every
blocker assigned to it. Effects put temporary states on individual dice,
mostly "this turn", some "until Clean Up" (end of turn), and some "while
the source is in play". Most states matter to *both* players, but for
different reasons: the defender has to see "must block", while the attacker
cares that their creature is "unblockable".

## The canvas

### The die tile

A die in play is a small square cube face:

| Spot | What's there now |
|---|---|
| Top-left | Fielding cost (small number) |
| Top-right | ATK (large) |
| Bottom-right | DEF (large) |
| Center | The animal's icon (identity) |
| Bottom-left | Energy pips, on energy faces only (not shown on a creature in play) |
| Red "−N" badge, corner | Damage marked this turn |
| Pill hanging below the tile | "Must block" (orange) / "Unblockable" (purple) |

Sizes:
- **Mobile:** 50px for your Field, 48px for the opponent's Field, 40px
  for Reserve and the Intimidated row, 54px in the target-picker sheet.
- **Desktop:** larger tiles in a grid mat.

Tiles sit 7px apart in wrapping rows, so a pill wider than the tile runs
into its neighbour. That already happened with two "Unblockable"
Chameleons side by side.

### Two front ends

- **Mobile** (`/dice-kingdom/mobile`): fixed dark palette. Accents are
  you = Claw orange, opponent = Eye purple.
- **Desktop** (`/dice-kingdom`): light and dark themes.

Energy-type colors (Claw orange, Shell green, Wing blue, Eye purple) are
already used for energy pips, card borders and some badges. The current
"Unblockable" pill reuses Eye purple, so it reads as an energy-type
color. That's worth untangling.

### Interaction states the status cues must not collide with

- **Selected:** a ring, when you're choosing attackers or blockers.
- **Targetable / picked:** a glow, when an ability asks you to pick a die.
- **Not clickable:** dimmed.
- **The Intimidated row:** dimmed and greyscale (see below).

## State inventory

**"Data now"** = whether the server already tells the client about it.
States without data aren't shown anywhere yet; the flag gets added
during implementation.

### Combat restrictions (this turn, cleared at Clean Up)

| State | Meaning | Caused by (Dice Kingdom) | Where | Shown today | Data now |
|---|---|---|---|---|---|
| **Must block** | Has to block if any attacker can be blocked | Hermit Crab (On Field) | Field | Orange pill below the tile; blocks won't confirm without it | yes |
| **Can't block** | Can't be assigned as a blocker | Barn Owl; Anglerfish and Distraction (both in the card pool but not on a team right now) | Field | nothing; the server just rejects | no |
| **Unblockable** | Can't be blocked, *and neither can anything in its lane* | Chameleon (Obscure), when its owner uses an action die | Field, Attack lane | Purple pill below the tile | yes |
| **Must attack / Can't attack / Only blocker** | As named | No Dice Kingdom card yet; the engine supports them | Field | nothing | no |

### Out of play for now

| State | Meaning | Caused by | Duration | Shown today | Data now |
|---|---|---|---|---|---|
| **Intimidated** | Removed from the Field. It can't block or be targeted, and its "while active" effects are off. It returns on the same face. | Frilled Lizard (Intimidate) | until Clean Up | Separate dimmed row under the Field: "Intimidated · back at end of turn" | yes (its own zone) |

### Text / abilities

| State | Meaning | Caused by | Duration | Shown today | Data now |
|---|---|---|---|---|---|
| **Text blanked** | The card's abilities and keywords are off (a vanilla body) | No Dice Kingdom card yet; the engine fully supports it (some Dice Masters cards do it). The user wants it designed for. | this turn, or while the source is active | nothing | no |
| **Granted keyword** | Temporarily has a keyword it doesn't print | Anger Issues (Overcrush this turn); any future keyword grant | this turn | nothing; the stat change shows, the keyword doesn't | no |
| **Locked out (card-level)** | The opponent can't buy or field this card | Pangolin names a card | while Pangolin is active | nothing | no (card-level, not die) |

### Pending consequences

| State | Meaning | Caused by | Duration | Shown today | Data now |
|---|---|---|---|---|---|
| **Deadly-engaged** | Will be KO'd at Clean Up, whatever the damage | Fought an Opossum (Deadly) | until Clean Up | nothing | no |
| **Damaged** | Damage marked; KO'd when it reaches DEF | Combat, pings, Range, Breath Weapon, Aftershock... | until Clean Up | Red "−N" badge, plus defence meters in lanes | yes |

### Stat changes

| State | Meaning | Caused by | Duration | Shown today | Data now |
|---|---|---|---|---|---|
| **Buffed / debuffed** | ATK or DEF differs from the printed number | Champion passives (Wolf +1 ATK, Armadillo +1 DEF), auras (Queen Termite, Musk Ox, Fox, Cape Buffalo), Anger Issues +3A, Tag Out +2/+2, Archnemesis (D = A), Silverback doubling | this turn, or while the source is active | The number changes; tap shows a breakdown | yes (breakdown list) |
| **Spun down / up** | Level changed by an effect | Leech (Energy Drain), Cuttlefish, Mutation | instant | The face just changes, with no "why" | no (event only) |

### Lane-level (already designed; keep it consistent)

- "N TO FACE" chips.
- The Rhinoceros reflect chip.
- Defence meters.
- The Overcrush lane cue (2+ attackers in a lane).
- An unblockable lane: currently implied only by the die's pill.

### Permanent keywords (open question)

Fast, Overcrush, Deadly, Range, Infiltrate, Tag Out, Attune, Obscure,
Energize, Energy Drain, Aftershock and Breath Weapon are printed on the
card. There's no on-tile indicator; you find them by opening the card.
Is a tiny keyword marker worth it at 50px, or is that clutter? The
designer's call; even "no, here's why" is useful.

## Combinations that really happen

Design for stacking, not single states:

- Must block + damaged + buffed (an Armadillo's Hermit-Crabbed blocker).
- Unblockable + buffed + attacking (a Chameleon after Anger Issues).
- Deadly-engaged + damaged + still in a lane.
- Can't block + must block on the same die (the engine resolves it as
  can't).
- Blanked + must block.
- Two adjacent tiles with the same long label (the Chameleon collision).
- Opponent-side tiles at 48px vs your own at 50px.

## Constraints and preferences

- **Readable at 48–50px on a phone**, and **not color alone**: each state
  needs a shape, icon or word too.
- **Tells you why on tap:** the source card and how long it lasts, like
  the existing stat breakdown. This is the pattern the user has asked for
  repeatedly. Any cue should lead to an explanation.
- **"This turn" vs "until Clean Up" vs "while X is active"** may want
  distinguishable treatments, or not. The designer's call.
- **Good-for-owner vs bad-for-owner** (unblockable vs must block) is a
  possible axis.
- **Works on both sides:** your dice and the opponent's.
- **Both front ends:** mobile dark, and desktop in light and dark.
- **Motion:** the app has a motion system (flying dice, a spin on reroll).
  A brief "this just happened" flash may suit instant changes like Energy
  Drain's spin-down; ongoing states should be static.

## What to bring back

1. **The system:** which kinds of state get which treatment (badge, ring,
   tint, corner glyph, overlay), plus how they stack and their priority
   when space runs out.
2. **Mockups of the 50px mobile tile** for each state above and the
   combinations listed, in your Field and the opponent's.
3. **Desktop equivalents**, in light and dark.
4. **Tap-to-explain:** what a tap on a cue shows.
5. **A legend or first-time explanation**, if the system needs one.
6. **A recommendation on permanent-keyword markers.**

## Afterwards (main session)

- Add the missing server flags: can't block, blanked, granted keywords,
  Deadly-engaged, lockouts, and an unblockable lane.
- Implement on both pages.
- Verify on real boards with the bot-driven capture script
  (`~/.devtools/playwright/keyword-check.js`), which can now steer
  games into specific states.
