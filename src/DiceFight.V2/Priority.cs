using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.V2;

// Priority in the Main Step and the Attack Step's Action/Global window
// (rules 2.6.5.7, 2.6.6, 2.7.3.4), added 2026-09-26 at the user's request
// in place of the looser "who may use a Global when" rules the API had:
//
//   - The Active player has priority first and may do as many things as
//     they like, then passes.
//   - The Inactive player may then do ONE thing (use one Global), which
//     hands priority straight back to the Active player - or pass.
//   - The window ends when the Inactive player passes right after the
//     Active player did (2.6.6.6: nothing changed in between, so the
//     Active player has nothing new to respond to).
//
// Ending Main moves on to the Attack Step, or skips it if that's what the
// Active player chose when they passed (2.6.7.1(3)); ending the Attack
// window resolves combat damage from the declared blocks.
//
// An Inactive player who couldn't use any Global anyway (no payable
// Global) passes automatically, so the Active player never waits on a
// decision that doesn't exist.
public static class Priority
{
    public static bool IsWindow(GameState state) =>
        state.CurrentStepId is StepIds.Main or StepIds.ActionGlobalWindow;

    // Opens a window on first sight of a window step (Active player first)
    // and clears priority outside the windows. Idempotent - callers run it
    // after every action.
    public static void Sync(GameState state)
    {
        if (!IsWindow(state))
        {
            state.PriorityPlayerId = null;
            state.PriorityWindowStepId = null;
            return;
        }
        if (state.PriorityWindowStepId == state.CurrentStepId && state.PriorityPlayerId is not null) return;
        state.PriorityWindowStepId = state.CurrentStepId;
        state.PriorityPlayerId = state.ActivePlayerId;
        state.SkipAttackWhenMainEnds = false;
        state.InactiveActedThisWindow = false;
    }

    public static void RequireHolder(GameState state, string playerId)
    {
        Sync(state);
        if (!IsWindow(state))
            throw new InvalidOperationException("That can only be done in the Main Step or the Attack Step's action window.");
        if (state.PriorityPlayerId != playerId)
            throw new InvalidOperationException(playerId == state.ActivePlayerId
                ? "Your opponent has priority - wait for them to use a Global or pass."
                : "You don't have priority right now.");
    }

    public static void Pass(GameState state, AbilityQueue queue, string playerId, bool skipAttack = false)
    {
        RequireHolder(state, playerId);

        if (playerId == state.ActivePlayerId)
        {
            if (state.CurrentStepId == StepIds.Main) state.SkipAttackWhenMainEnds = skipAttack;
            var inactiveId = state.OpponentOf(playerId);
            if (CanUseAnyGlobal(state, inactiveId) || ChampionPowers.CanUse(state, inactiveId))
            {
                state.PriorityPlayerId = inactiveId;
                return;
            }
            // Their automatic pass. Logged once they've acted in this
            // window (direct feedback 2026-09-26: silent, it read as if
            // their Global had also been their pass).
            if (state.InactiveActedThisWindow)
                state.LogEvent(inactiveId, $"{state.NameOf(inactiveId)} passes (no energy left for a Global).");
            CloseWindow(state, queue);
            return;
        }

        state.LogEvent(playerId, $"{state.NameOf(playerId)} passes.");
        CloseWindow(state, queue);
    }

    // Rule 2.6.6.4 - the Inactive player gets ONE thing, then priority
    // goes back to the Active player.
    public static void AfterGlobal(GameState state, string playerId)
    {
        if (IsWindow(state) && playerId != state.ActivePlayerId)
        {
            state.PriorityPlayerId = state.ActivePlayerId;
            state.InactiveActedThisWindow = true;
        }
    }

    // Whether this player could pay for any Global in the game right now -
    // the cards on either team (rule 2.6.5.2: Globals are available to
    // both players), its once-per-turn limit, blanking, and its cost
    // against the energy in their Reserve Pool.
    public static bool CanUseAnyGlobal(GameState state, string playerId)
    {
        var reserveFaces = state.DiceIn(playerId, Zone.ReservePool)
            .Select(d => state.GetCurrentFace(d)).OfType<Face>().ToList();
        var wildIds = state.Config.EnergySymbols.Where(s => s.IsWild).Select(s => s.Id).ToHashSet();
        // Virtual energy (deck-out) counts toward the amount but, being
        // generic, never toward a type requirement.
        var totalPips = reserveFaces.Sum(f => f.Symbols.Sum(s => s.Count)) + state.VirtualEnergyOf(playerId);

        foreach (var cardId in state.PlayerOne.TeamCardIds.Concat(state.PlayerTwo.TeamCardIds).Distinct())
        {
            if (!state.CardCatalog.TryGetValue(cardId, out var card)) continue;
            if (!QueryEngine.CardTextActive(state, playerId, cardId)) continue;
            foreach (var ability in card.Abilities.Where(a => a.Trigger == TriggerKind.Global))
            {
                if (ability.OncePerTurn && state.GlobalsUsedThisTurn.Contains((playerId, cardId))) continue;
                if (!AbilityPreview.CouldHaveResult(state, playerId, ability.Effect, ProtectionFrom.Global)) continue; // rule 3.1.10
                var cost = QueryEngine.GetGlobalEnergyCost(state, card, ability, playerId);
                if (totalPips < cost) continue;
                var required = ability.EnergyCost?.RequiredSymbolId;
                if (required is null || reserveFaces.Any(f => f.Symbols.Any(s => s.SymbolId == required || wildIds.Contains(s.SymbolId))))
                    return true;
            }
        }
        return false;
    }

    private static void CloseWindow(GameState state, AbilityQueue queue)
    {
        var step = state.CurrentStepId;
        state.PriorityPlayerId = null;
        state.PriorityWindowStepId = null;
        if (step == StepIds.Main)
        {
            if (state.SkipAttackWhenMainEnds) TurnEngine.SkipAttackStep(state, queue);
            else TurnEngine.EnterAttackStep(state, queue);
        }
        else
        {
            CombatEngine.AssignCombatDamage(state, queue, state.DeclaredBlocks ?? new CombatAssignment(),
                new Dictionary<string, IReadOnlyDictionary<string, int>>());
        }
    }
}
