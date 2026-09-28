// The "vs computer" opponent's client half. The DECISIONS now come from
// the server (GET .../bot-decision -> DiceFight.V2/Bot/DiceKingdomBot.cs),
// the same policy tools/Simulator plays, so the simulator's balance numbers
// describe the opponent people actually face (user call, 2026-09-28 - this
// file used to hold its own separate TypeScript policy, which had drifted
// from the simulator's). What stays here: whose decision it is right now,
// how to carry a decision out through the API, and pickEnergy, which the
// human auto-pay still uses.
import type { BotDecision, Die, GameState } from "./types";
import type { apiAs } from "./api";

// Same test ../DiceKingdomPage.tsx's own `rolled()` uses - moved here so
// both it and this module share one definition rather than two copies
// drifting apart.
export function rolled(d: Die): boolean {
  return d.effectiveAttack !== null || d.energySymbolId !== null;
}

// Whoever has to act next, or null if the current step runs on its own
// (an engine procedure with nothing for either player to decide). Mirrors
// the same precedence DiceKingdomPage's own `stepContent` ternary chain
// uses: a pending choice outranks the step, and Assign Blockers is the
// one step the INACTIVE player answers rather than the active one.
export function decisionOwner(game: GameState): string | null {
  if (game.pendingChoice) return game.pendingChoice.controllerId;
  // Main / the action window: whoever holds priority (Priority.cs).
  if (game.priorityPlayerId) return game.priorityPlayerId;
  if (game.currentStepId === "assign-blockers") {
    return game.activePlayerId === game.playerOne.id ? game.playerTwo.id : game.playerOne.id;
  }
  const decisionSteps = new Set([
    "start-of-turn",
    "roll-and-reroll",
    "main",
    "select-attackers",
    "action-global-window",
    "return-to-field",
  ]);
  return decisionSteps.has(game.currentStepId) ? game.activePlayerId : null;
}

// Which reserve energy dice to spend on `cost`, with at least one pip matching
// `matchType` (or Wild) when the card has a type requirement - same rule as
// TurnEngine.SpendEnergy. Null if it can't be paid.
//
// The engine spends dice in the order offered and stops once the cost is met;
// only the LAST die can be overspent, and then it spins down to its own
// lower face (TurnEngine.TrySpinDown). What that leaves behind differs:
//   - Tardigrade double-energy -> its single-energy face, which still has
//     stats (1A/1D): best.
//   - Character double-energy -> a bare single-energy face, no stats: ok.
//   - anything else -> the leftover pip is simply lost: worst.
// So this tries every subset/last-die choice that exactly covers the cost
// and keeps the one that spends the fewest Wild pips, then whose leftover
// is most useful, then fewest dice. Wilds first (direct feedback
// 2026-09-26): with 3 Claw and 2 Wild, buying a 4-cost Wolverine spent
// BOTH Wilds, leaving none to pay for the opponent's Global - a Wild is
// the one energy that can pay for anything, so it's the last to go.
export function pickEnergy(pool: Die[], cost: number, matchType: string | null): string[] | null {
  if (cost <= 0) return [];
  const dice = pool.filter((d) => d.energyAmount > 0).sort((a, b) => a.energyAmount - b.energyAmount);
  const matches = (d: Die) => !matchType || d.energySymbolId === matchType || d.energySymbolId === "Wild";
  if (dice.length > 14) return pickEnergyGreedy(dice, cost, matchType);

  let best: { ids: string[]; wilds: number; score: number; count: number } | null = null;
  for (let mask = 1; mask < 1 << dice.length; mask++) {
    const members = dice.filter((_, i) => mask & (1 << i));
    const sum = members.reduce((n, d) => n + d.energyAmount, 0);
    if (sum < cost || !members.some(matches)) continue;
    for (const last of members) {
      if (sum - last.energyAmount >= cost) continue; // engine would have stopped before `last`
      const overspend = sum - cost;
      let score = 4; // exact payment, nothing left over to protect
      if (overspend > 0) {
        const leftover = overspend; // pips still showing on the spun-down die
        score = last.isTardigrade && last.energyAmount === 2 && leftover === 1 ? 3
          : !last.isTardigrade && last.energyAmount === 2 && leftover === 1 ? 2
          : 0;
      }
      const count = members.length;
      // Wild pips actually consumed - an overspent Wild's leftover stays.
      const wilds =
        members.filter((d) => d.energySymbolId === "Wild").reduce((n, d) => n + d.energyAmount, 0) -
        (last.energySymbolId === "Wild" ? overspendOf(sum, cost) : 0);
      const better =
        !best ||
        wilds < best.wilds ||
        (wilds === best.wilds && (score > best.score || (score === best.score && count < best.count)));
      if (better) {
        best = { ids: [...members.filter((d) => d !== last), last].map((d) => d.id), wilds, score, count };
      }
    }
  }
  return best?.ids ?? null;
}

function overspendOf(sum: number, cost: number): number {
  return Math.max(0, sum - cost);
}

function pickEnergyGreedy(dice: Die[], cost: number, matchType: string | null): string[] | null {
  let rest = [...dice];
  const picked: string[] = [];
  let total = 0;
  if (matchType) {
    const exact = rest.findIndex((d) => d.energySymbolId === matchType);
    const idx = exact !== -1 ? exact : rest.findIndex((d) => d.energySymbolId === "Wild");
    if (idx === -1) return null;
    picked.push(rest[idx].id);
    total += rest[idx].energyAmount;
    rest = rest.filter((_, i) => i !== idx);
  }
  // Non-Wild dice first, so any Wild is spent last (see pickEnergy).
  rest.sort((a, b) => Number(a.energySymbolId === "Wild") - Number(b.energySymbolId === "Wild"));
  for (const d of rest) {
    if (total >= cost) break;
    picked.push(d.id);
    total += d.energyAmount;
  }
  return total >= cost ? picked : null;
}

// Carries out one server-side bot decision through the matching endpoint.
export function botDecisionCall(
  client: ReturnType<typeof apiAs>,
  gameId: string,
  d: BotDecision,
): Promise<GameState> {
  switch (d.kind) {
    case "clearAndDraw":
      return client.clearAndDraw(gameId);
    case "roll":
      return client.roll(gameId);
    case "reroll":
      return client.reroll(gameId, d.dieIds);
    case "finishRoll":
      return client.finishRoll(gameId);
    case "foresight":
      return client.foresight(gameId, d.dieId!);
    case "field":
      return client.field(gameId, d.dieId!, d.energyDieIds);
    case "purchase":
      return client.purchase(gameId, d.dieId!, d.energyDieIds);
    case "useAction":
      return client.useAction(gameId, d.dieId!);
    case "useGlobal":
      return client.useGlobal(gameId, d.cardId!, d.abilityIndex, d.energyDieIds);
    case "pass":
      return d.skipAttack ? client.skipAttackStep(gameId) : client.pass(gameId);
    case "declareAttackers":
      return client.declareAttackers(gameId, d.attackers);
    case "declareBlockers":
      return client.declareBlockers(gameId, d.assignments);
    case "resolvePendingChoice":
      return client.resolvePendingChoice(gameId, d.dieIds);
    case "cleanUp":
      return client.cleanUp(gameId);
  }
}
