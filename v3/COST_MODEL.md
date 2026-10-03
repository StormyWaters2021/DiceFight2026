# Dice Kingdom cost model (2026-09-21, recalibrated; split rebalanced 2026-09-27)

Why: the original roster was ported from Dice Masters cards whose printed costs already priced in their abilities. Once abilities were simplified, costs and stats no longer matched (Elephant cost 6 for tiny stats; Greyhound cost 4 for huge ones).

**2026-09-27 follow-up: the ATK/DEF split, not the formula, was wrong.** Playtest feedback: "the vast majority of the characters seem to have only 1 or 2 attack, while it seems most of them have 4D or higher... the elephant has a 12D, which doesn't exist in Dice Masters." Checked against the ~3,200 three-level Characters in the bulk card catalog: real Dice Masters averages 3.7 ATK / 4.0 DEF per level (DEF/ATK ratio 1.07, max DEF 10 across the whole catalog); the 2026-09-21 recalibration had pushed Dice Kingdom to 1.8 ATK / 5.8 DEF (ratio 3.3, half the roster at 1 ATK or less, Elephant's L3 at 12 DEF - genuinely off the real game's scale). The Homash formula only reads `ATK + DEF` summed per level, never the split, so **the fix doesn't touch Homash at all** - same purchase costs, same fielding costs, same per-card totals, same bands below, same test file. Every level's total was just re-split roughly evenly between ATK and DEF (instead of dumping nearly everything into DEF), then nudged 1 point toward each Champion pairing's own lean: Claw and Wing (the aggro/tempo pairing) 1 point toward ATK, Shell and Eye (defense/control) 1 point toward DEF - stopping short of pushing ATK negative or DEF to 0. Table below is current.

## Homash value

From https://dmunited.eu/what-in-the-world-are-homash-values/ - "add up the attack and defence stats for all 3 levels and divide that number by 3 to get the average. Then, you divide that average by the purchase cost + the average fielding cost":

    Homash = sum(ATK + DEF over 3 levels) / (3 x purchase cost + sum of fielding cost over 3 levels)

(An earlier version of this model counted purchase cost once instead of three times, which put every number on the wrong scale.) The article calls ~2 reasonable and >2 strong, and notes abilities aren't in the formula - so here **the ability is paid for out of the stat budget**. Our dice also carry 3 energy faces, but every Character has the same 3, so it cancels out of comparisons.

**Champion passives are never part of the calculation.** A card can end up on any Champion's team, so Homash uses printed costs only (no Golden Eagle discount, no Wolf +1 ATK).

## Calibration against the real cards

The 128 Dice Masters characters in the DPS catalog, under the same formula: mean Homash **1.49**, median 1.49, deciles 1.0 / 1.14 / 1.24 / 1.41 / 1.5 / 1.56 / 1.67 / 1.78 / 1.9 (max about 3.0). Fielding costs there are **per level** and average about 1.0 (0 in 21% of level slots, 1 in 48%, 2 in 24%, 3 in 6%); common patterns are 1/2/3, 0/0/1, 1/1/2, 1/2/2, 0/1/1. By purchase cost, average total fielding runs 1.9 (cost 2), 2.9 (3), 3.2 (4), 4.4 (5-6). Dice Kingdom cards use the same shapes - cheap early levels, expensive last level - rather than one flat cost (an earlier pass gave every card a flat 1 or 2, which made them ~50% costlier to field than their sources).

## Bands (enforced by `DiceKingdomCostModelTests`)

| Card type | Target Homash |
|---|---|
| Vanilla (no ability, no keyword) | >= 1.75 (roster: 1.83-2.0) - "really good stats" or cheap |
| Keyword-only (Fast/Overcrush) | ~1.65-1.7 |
| Weak ability (1 life/1 damage/draw) | ~1.45-1.6 |
| Medium (2 damage, auras, direct damage) | ~1.25-1.45 |
| Strong (KO, 3 damage, Fast + damage, Deadly) | ~1.05-1.25 |
| Anything | 1.0-2.1 |

## Keywords

**Trigger keywords** (codified, not free text - the trigger is the keyword; the effect after the colon varies): `On Field` (die is fielded), `On Attack` (declared as an attacker), `On Block` (declared as a blocker), `Awaken` (die levels up). Every card's RawText leads its trigger clause with the keyword, and `Trigger_Keywords_Match_Each_Cards_Ability_Triggers` fails if a card's keywords and abilities disagree.

**Deadly**: a die engaged with a Deadly die (blocking it or blocked by it) is KO'd at Clean Up, even if the Deadly die dealt no damage or left combat. Recorded at declare-blockers, resolved in `TurnEngine.CleanUp`. On Opossum.

**Intimidate** (2026-10-03): when fielded, remove a target opposing creature from the Field until end of turn - it waits in `Zone.Intimidated` (can't block, can't be targeted, its "while active" text is off) and `TurnEngine.CleanUp` returns it on the same face. Engine-built: `KeywordAbilities.Intimidate`, folded into `QueryEngine.AbilitiesOf` for any die with the keyword, so the card text is just the keyword. On Frilled Lizard (catalog only).

**Infiltrate** (2026-10-03, Dice Masters rule kept as-is): when an attacker's whole lane is unblocked, its controller may return it to the Field and deal the opponent 1 damage instead of its full ATK. Queued in `CombatEngine.DeclareBlockers` after the On Block abilities, so it resolves before the Action/Global window; back on the Field it is still targetable. Its value here is the body: an unblocked attacker otherwise leaves play. On Flying Squirrel (catalog only).

**Attune** / **Attune N** (2026-10-03, Dice Masters rule): while active, each time you use an action die, deal N damage (plain "Attune" = 1; Boom Boom reads as Attune 2) to the opponent or a target character die (`TargetKind.CharacterDieOrOpponent` - never yourself). Global uses don't count; a Global-triggered keyword would be its own keyword. Each Attune keyword on a die fires separately. On Electric Eel (catalog only).

**Obscure** (2026-10-03, Dice Masters rule): while active, each time you use an action die, this creature can't be blocked this turn. A lane is the unit of combat, so its lane-mates can't be blocked either (existing `ValidateUnblockable` behavior). On Chameleon (catalog only).

Don't price Attune/Obscure off the simulator's action-die usage: Dice Kingdom has only four Basic Actions, none of them the cheap ping/ramp/churn actions real decks buy for exactly this combo, and having a payoff on the team makes buying any action more attractive (user, 2026-10-03). Both sit in the weak-ability band for now.

**Seven more, 2026-10-03** (all Dice Masters rules, all on catalog-only animals; X keywords read "Range" = 1, "Range 2" = 2 via `KeywordAbilities.ParamOf`):
- **Energize: <effect>** - when the die rolls a double energy face (checked at the start of Main, and on any later reroll). `KeywordAbilities.Energize(effect)`. Firefly: 2 damage to a target character die or the opponent.
- **Energy Drain X** - after blockers are assigned, every die engaged with it (lane-wide, like Deadly) spins down X levels, all at once, never below level 1. Leech.
- **Tag Out** - after blockers are declared, either player may move a Field Zone (not fighting) Tag Out die to their Prep Area to give a target creature +2A/+2D this turn. Meerkat.
- **Sacrifice** - a cost: the creature goes Out of Play on its owner's turn (Used Pile at Clean Up), the Used Pile otherwise, and is never a KO. `Sacrifice` effect node, used as `MayPay.Cost`. Army Ant: On Field, may Sacrifice one of your creatures (itself included) to deal damage equal to its ATK to a target creature.
- **Range X** - when a Range creature attacks, every active Range creature on both sides picks a target opposing creature (Active player first), then all the shots land together, so a Range die KO'd by the other side still fires. Archerfish.
- **Aftershock: <effect>** - when the opponent makes it leave the Field (combat KO, their ability's damage/KO/move - Intimidate included; not your own Sacrifice or Tag Out). New `TriggerKind.DieRemovedByOpponent`, fired where a die leaves play with a known cause. Bombardier Beetle: 2 damage to a target character die.
- **Breath Weapon X** - when it attacks, you may pay X energy (tap your Reserve dice, paid like a Global) to deal X damage to the opponent and every creature they have; once per unique creature however many attack. Komodo Dragon.

**Fast** and **Overcrush** (both in `CombatEngine`): Wolverine (Fast + 1 direct), Peregrine Falcon (Fast + 3 dmg), Greyhound (Fast), Grizzly Bear (Overcrush), Tiger (Overcrush + 2 direct). Vanilla: Elephant, Hippopotamus. Also On Block: Box Turtle. Not yet engine-supported: Regenerate, Retaliation, Swarm, Range.

## Current roster

| Card | Buy | Levels (field/ATK/DEF) | Homash | Ability |
|---|---|---|---|---|
| Hippopotamus | 4 | 1/3/5 · 1/4/6 · 2/5/7 | 1.88 | Vanilla - no ability. |
| Elephant | 6 | 1/5/7 · 2/6/9 · 3/7/10 | 1.83 | Vanilla - no ability. |
| Greyhound | 4 | 1/3/4 · 1/5/5 · 2/5/5 | 1.69 | Fast. |
| Grizzly Bear | 5 | 1/4/5 · 2/5/6 · 2/6/7 | 1.65 | Overcrush. |
| Homing Pigeon | 4 | 1/3/3 · 1/4/5 · 2/5/6 | 1.62 | On Field: gain 2 life. |
| Swift | 2 | 0/1/2 · 1/2/2 · 1/3/3 | 1.62 | On Attack: draw a die into your Prep Area. |
| Cowbird | 3 | 1/1/4 · 1/2/5 · 2/3/6 | 1.62 | Awaken: move an opposing die from their Prep Area back to their Bag. |
| Pangolin | 3 | 1/1/3 · 1/2/4 · 1/3/6 | 1.58 | On Field: name an opposing character; while Pangolin is active, your opponent can't purchase or field it. (Lockout - arguably a stronger band; simulated roughly neutral, so stats left alone, 2026-09-29.) |
| Box Turtle | 3 | 1/0/3 · 1/2/4 · 1/4/6 | 1.58 | On Block: deal 1 damage to a target creature. |
| Hermit Crab | 2 | 0/1/2 · 0/1/3 · 1/1/3 | 1.57 | On Field: target character die must block this turn. (Was vanilla 1/3 · 1/4 · 1/4, Homash 2.00, until 2026-09-29; briefly "gain 1 life" the same day.) |
| Meerkat *(no team yet)* | 2 | 0/1/2 · 0/1/3 · 1/2/2 | 1.57 | Tag Out. |
| Honey Badger | 2 | 0/1/1 · 0/2/2 · 1/2/3 | 1.57 | On Field: deal 1 damage to a target creature. |
| Barn Owl | 4 | 1/2/5 · 1/3/5 · 2/4/6 | 1.56 | On Field: a weak target creature (3 ATK or less) can't block this turn. |
| Chameleon *(no team yet)* | 4 | 1/2/4 · 1/3/5 · 2/5/6 | 1.56 | Obscure. |
| Electric Eel *(no team yet)* | 3 | 0/2/1 · 1/3/3 · 1/4/4 | 1.55 | Attune. |
| Stoat | 4 | 0/2/2 · 1/4/4 · 2/5/6 | 1.53 | On Field: deal 1 damage to the opponent directly. |
| Barn Swallow | 3 | 0/2/2 · 1/2/3 · 2/4/5 | 1.50 | Awaken: draw a die into your Prep Area. |
| Cuttlefish | 2 | 0/0/3 · 1/1/3 · 1/1/4 | 1.50 | On Attack: spin a target opposing level 1 creature to an energy face. |
| Fox | 5 | 1/2/5 · 2/3/6 · 2/5/8 | 1.45 | While active, your creatures get +1 DEF. |
| Flying Squirrel *(no team yet)* | 3 | 0/2/2 · 1/3/3 · 1/4/2 | 1.45 | Infiltrate. (Was 1/5/3 at L3, Homash 1.64 - trimmed 2026-10-03, keeping the body is worth more than the formula sees.) |
| Leech *(no team yet)* | 3 | 0/1/3 · 1/2/3 · 1/2/5 | 1.45 | Energy Drain. |
| Archerfish *(no team yet)* | 3 | 0/2/2 · 1/3/2 · 1/4/3 | 1.45 | Range. |
| Bombardier Beetle *(no team yet)* | 3 | 0/1/3 · 1/2/3 · 1/3/4 | 1.45 | Aftershock: 2 damage to a target character die. |
| Musk Ox | 4 | 1/2/4 · 1/3/5 · 2/3/6 | 1.44 | While active, your creatures get +1 DEF. |
| Queen Termite | 4 | 1/2/4 · 1/3/5 · 2/3/6 | 1.44 | While active, your creatures get +1 ATK. |
| Mongoose | 3 | 0/2/2 · 1/2/3 · 2/4/4 | 1.42 | Awaken: deal 2 damage to a target creature. |
| Mountain Goat | 3 | 1/1/2 · 1/2/3 · 1/4/5 | 1.42 | On Attack: draw a die into your Prep Area. |
| Magpie | 3 | 1/0/3 · 1/1/4 · 1/3/6 | 1.42 | On Field: draw a die into your Prep Area. |
| Osprey | 4 | 1/2/3 · 1/3/4 · 2/5/5 | 1.38 | On Attack: move a die from your discard to your Prep Area. |
| Frilled Lizard *(no team yet)* | 4 | 1/2/4 · 1/3/4 · 2/4/5 | 1.38 | Intimidate. |
| Hyena | 4 | 1/2/4 · 1/2/5 · 2/3/6 | 1.38 | Gets +1 ATK for each weak opposing creature (2 DEF or less). |
| Albatross | 5 | 1/3/3 · 1/4/5 · 2/5/6 | 1.37 | On Field: deal 2 damage to a target creature. |
| Firefly *(no team yet)* | 3 | 0/2/2 · 1/3/2 · 1/3/3 | 1.36 | Energize: 2 damage to a target character die or the opponent. |
| Raven | 5 | 1/2/5 · 2/3/6 · 2/4/7 | 1.35 | On Field: deal 2 damage to a target creature. |
| Cape Buffalo | 6 | 1/3/4 · 2/5/5 · 2/7/7 | 1.35 | While active, your creatures get +1 ATK. |
| Monarch Butterfly | 4 | 0/2/2 · 1/3/4 · 2/4/5 | 1.33 | Gets +2 ATK for each of your creatures waiting in your Prep Area. |
| Tiger | 6 | 1/4/4 · 2/5/5 · 2/6/6 | 1.30 | Overcrush. On Attack: deal 2 damage to the opponent directly. |
| Anglerfish | 6 | 1/2/5 · 2/4/6 · 3/6/8 | 1.29 | On Attack: every weak opposing creature (3 DEF or less) can't block this turn. |
| Wolverine | 4 | 1/2/3 · 1/3/4 · 2/4/4 | 1.25 | Fast. On Attack: deal 1 damage to the opponent directly. |
| Komodo Dragon *(no team yet)* | 5 | 1/4/3 · 2/5/4 · 2/6/3 | 1.25 | Breath Weapon. |
| Army Ant *(no team yet)* | 4 | 1/3/2 · 1/4/2 · 2/5/4 | 1.25 | On Field: may Sacrifice one of your creatures to deal its ATK to a target creature. |
| Orca | 5 | 1/2/3 · 1/4/4 · 2/5/5 | 1.21 | On Field: KO a target creature. |
| Snapping Turtle | 5 | 1/1/4 · 2/3/5 · 2/4/7 | 1.20 | On Field: KO a target creature. |
| Opossum | 3 | 0/0/2 · 1/1/3 · 1/2/5 | 1.18 | Deadly. |
| Peregrine Falcon | 6 | 1/3/3 · 2/4/5 · 3/5/6 | 1.08 | Fast. On Field: deal 3 damage to a target creature. |
| Hummingbird | 4 | 0/1/2 · 1/2/3 · 1/3/4 | 1.07 | On Field: KO a target creature. |
