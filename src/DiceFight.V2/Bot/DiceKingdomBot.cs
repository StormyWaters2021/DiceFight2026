using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.V2.Bot;

// The Dice Kingdom computer opponent - ONE policy shared by the web
// client's "vs computer" seat (GET /api/v2/games/{id}/bot-decision) and
// tools/Simulator (user call, 2026-09-28: the two had drifted into two
// different bots, so the simulator's balance numbers didn't describe the
// opponent people actually play). Rule-based, no lookahead: each call
// answers "what is the single next thing I should do" from what's
// visible on GameState, the same information a human player sees
// (it never peeks at the opponent's Bag order or future rolls).
//
// What it does, step by step (see v3/DESIGN_NOTES.md, 2026-09-28, for the
// simulator findings behind each choice):
//   - Roll & Reroll: rerolls a Character die that rolled energy (a body
//     beats 1-2 energy), and fishes for a Wild with 1-pip Tardigrades when
//     a card it wants needs an energy type it can't otherwise pay.
//   - Main: Field real Characters first (unfielded ones go to the Used
//     Pile at the end of Main anyway), then buy the best-VALUE plan of up
//     to three cards the energy affords - not simply the cheapest or the
//     priciest; extra copies of a card are worth progressively less, and
//     an opponent's Basic Action is worth more when its energy type pays
//     for an off-type card on this team. Only THEN field leftover
//     Tardigrades - fielding one first burns the energy it carries, which
//     is what kept the old simulator bot from ever buying a finisher.
//   - Uses Basic Actions and Globals (Anger Issues on an unblocked
//     attacker, Distraction's Global against the biggest unblocked
//     attacker, Resurrection's draw with spare Wing energy, ...).
//   - Attack: never swings a 0-ATK die, never swings into a blocker that
//     kills it for free (an unblocked attacker leaves play - rule
//     2.7.4.3.1 - so a swing has a real cost), keeps enough blockers home
//     to survive the crack-back, and goes all-in when the damage is lethal.
//   - Block: chump-blocks only to survive or when life is low, otherwise
//     takes good trades and free walls.
//   - Pending choices: harmful effects aim at the opponent's best die (or
//     this player's least valuable one when the choice is forced),
//     helpful ones at its own best (PendingChoice.Intent).
public static class DiceKingdomBot
{
    private const int MaxLanes = 4;

    // `skip`: dice the caller already saw rejected this turn (a legality
    // rule this policy doesn't model) - never offered again. Null when
    // it isn't this player's decision.
    public static BotDecision? Decide(GameState state, string botId, IReadOnlySet<string>? skip = null, BotPersona? persona = null)
    {
        skip ??= new HashSet<string>();
        persona ??= BotPersona.Default;
        var active = state.ActivePlayerId;

        if (state.PendingChoice is { } pending)
            return pending.ControllerId == botId ? AnswerChoice(state, botId, pending) : null;

        if (Priority.IsWindow(state))
        {
            var holder = state.PriorityPlayerId ?? active; // Priority.Sync opens every window with the Active player
            if (holder != botId) return null;
            if (state.CurrentStepId == StepIds.Main)
                return botId == active ? ActiveMain(state, botId, skip) : InactiveMain(state, botId);
            return botId == active ? ActiveWindow(state, botId) : InactiveWindow(state, botId);
        }

        switch (state.CurrentStep)
        {
            case TurnStep.StartOfTurn or TurnStep.ClearAndDraw when botId == active:
                return new(BotActionKind.ClearAndDraw, "Start of turn.");
            case TurnStep.RollAndReroll when botId == active:
                return state.DiceIn(botId, Zone.DiceFromBag).Any() || state.DiceIn(botId, Zone.DiceFromPrep).Any()
                    ? new(BotActionKind.Roll, "Roll the drawn dice.")
                    : DecideReroll(state, botId);
        }

        return state.CurrentStepId switch
        {
            StepIds.SelectAttackers when botId == active => DecideAttackers(state, botId, persona),
            StepIds.AssignBlockers when botId == state.OpponentOf(active) => DecideBlockers(state, botId, persona),
            StepIds.ReturnToField when botId == active => new(BotActionKind.CleanUp, "End of turn."),
            _ => null,
        };
    }

    // ------------------------------------------------------------------
    // Roll & Reroll
    // ------------------------------------------------------------------

    private static BotDecision DecideReroll(GameState state, string botId)
    {
        var reserve = state.DiceIn(botId, Zone.ReservePool).Where(d => !state.RerolledThisStep.Contains(d.Id)).ToList();
        var picks = new List<string>();

        // A purchased Character that rolled energy: 3 of its 6 faces are
        // a body, and a body is why it was bought (user call 2026-09-28:
        // "at the very least, prefer stat faces to energy").
        foreach (var die in reserve.Where(d => IsCharacterCard(state, d) && state.GetCurrentFace(d)?.Character is null))
            picks.Add(die.Id);

        // Fish for a Wild (the Tardigrade Surge face, 1 in 6) when a card
        // worth buying needs an energy type nothing in the pool can pay -
        // but only with Tardigrades showing a single pip, so the gamble
        // can't cost more than one energy.
        var wanted = OffTypeDemand(state, botId);
        var canPayWanted = wanted.Count == 0 || reserve.Any(d => BotEnergy.IsWild(state, d) || wanted.Any(t => BotEnergy.Pays(state, d, t)));
        if (!canPayWanted)
        {
            foreach (var die in reserve.Where(d => d.CardId is null && BotEnergy.Pips(state, d) == 1 && !BotEnergy.IsWild(state, d)))
                picks.Add(die.Id);
        }

        return picks.Count > 0
            ? new(BotActionKind.Reroll, picks.Count == 1 ? "Reroll one die." : $"Reroll {picks.Count} dice.") { DieIds = picks }
            : new(BotActionKind.FinishRoll, "Keep the roll.");
    }

    // ------------------------------------------------------------------
    // Main Step
    // ------------------------------------------------------------------

    private static BotDecision ActiveMain(GameState state, string botId, IReadOnlySet<string> skip)
    {
        var reserve = state.DiceIn(botId, Zone.ReservePool).Where(d => !skip.Contains(d.Id)).ToList();

        // Great Horned Owl's Foresight - on a Character die that rolled
        // energy, or failing that the least useful energy die.
        if (TurnEngine.HasForesight(state, botId) && !state.ForesightUsedThisTurn.Contains(botId))
        {
            var target = reserve.FirstOrDefault(d => IsCharacterCard(state, d) && state.GetCurrentFace(d)?.Character is null)
                ?? reserve.Where(d => d.CardId is null && state.GetCurrentFace(d)?.Character is null && !BotEnergy.IsWild(state, d))
                    .OrderBy(d => BotEnergy.Pips(state, d)).FirstOrDefault();
            if (target is not null)
                return new(BotActionKind.Foresight, "Foresight: reroll a weak die.") { DieId = target.Id };
        }

        // Basic Actions that pay off in Main (a combat pump waits for the
        // Attack Step's window - see ActiveWindow).
        foreach (var die in reserve.Where(d => IsActionFace(state, d)))
        {
            if (ShouldUseActionInMain(state, botId, die))
                return new(BotActionKind.UseAction, $"Use {CardName(state, die)}.") { DieId = die.Id };
        }

        // Real Characters first: an unfielded one goes to the Used Pile at
        // the end of Main (TurnEngine.EnterAttackStep), and a Tardigrade's
        // zero-energy Bulwark face (1/3) likewise has nothing else to give.
        // Deck-out burn (TurnEngine.ClearAndDraw): each die short of a full
        // draw costs 1 life per turn, paid back as 1 generic energy. A
        // real player takes that burn on purpose when there's life to
        // spare - a bigger board early is worth a few points - but never
        // lets it get anywhere near lethal (user, 2026-09-28). So the
        // allowed shortfall scales with life left after the opponent's
        // current board swings in.
        var drawCount = state.Config.Rules.DrawCount;
        var allowedShortfall = BurnBudget(state, botId);
        foreach (var die in reserve
            .Where(d => state.GetCurrentFace(d)?.Character is not null && (IsCharacterCard(state, d) || BotEnergy.Pips(state, d) == 0))
            .Where(d => d.CardId is null || QueryEngine.CanField(state, botId, d.CardId))
            .OrderByDescending(d => DieValue(state, d)))
        {
            if (Circulating(state, botId) - 1 < drawCount - allowedShortfall) break;
            var pay = BotEnergy.PickWithVirtual(state, reserve.Where(x => x.Id != die.Id).ToList(), QueryEngine.GetFieldingCost(state, die), null,
                state.VirtualEnergyOf(botId));
            if (pay is not null)
                return new(BotActionKind.Field, $"Field {CardName(state, die)}.") { DieId = die.Id, EnergyDieIds = pay.Value.Dice };
        }

        // Then spend energy on the best purchase plan.
        if (PlanPurchase(state, botId, reserve, skip) is { } buy) return buy;

        // Spare Wing energy: Resurrection's Global draws a die into Prep.
        if (CanDraw(state, botId) && GlobalDecision(state, botId, onlyRealPips: true, g => g.Effect is DrawToZone) is { } draw) return draw;

        // Leftover Tardigrades: fielded now or swept to the Used Pile at
        // the end of Main. A weak body is worth less burn than a real
        // Character - one point less of budget.
        var leftover = reserve
            .Where(d => d.CardId is null && state.GetCurrentFace(d)?.Character is not null)
            .OrderByDescending(d => DieValue(state, d))
            .FirstOrDefault();
        if (leftover is not null && Circulating(state, botId) - 1 >= drawCount - Math.Max(0, allowedShortfall - 1))
            return new(BotActionKind.Field, "Field a leftover Tardigrade.") { DieId = leftover.Id, EnergyDieIds = [] };

        return new(BotActionKind.Pass, "Done in Main - on to the Attack Step.");
    }

    // The opponent's Main: one Global, or pass. Energy left in the Reserve
    // Pool now is cleared at this player's next Clear and Draw anyway.
    private static BotDecision InactiveMain(GameState state, string botId) =>
        (CanDraw(state, botId) ? GlobalDecision(state, botId, onlyRealPips: false, g => g.Effect is DrawToZone) : null)
        ?? new(BotActionKind.Pass, "Pass.");

    // A draw-into-Prep effect is worth it only with something to draw, and
    // only if moving that die from the Bag to Prep doesn't push next turn's
    // deck-out burn past this player's budget (a Resurrection Global drawn
    // on the opponent's turn decked an Owl to death at 1 life, 2026-09-28).
    private static bool CanDraw(GameState state, string botId) =>
        (state.DiceIn(botId, Zone.Bag).Any() || state.DiceIn(botId, Zone.UsedPile).Any())
        && Circulating(state, botId) - 1 >= state.Config.Rules.DrawCount - BurnBudget(state, botId);

    // Dice available to next turn's draw from the Bag: everything this
    // player controls that isn't on the board, unbought, or in the Prep
    // Area (spent energy in Out of Play returns to the Used Pile at Clean
    // Up, and an empty Bag refills from the Used Pile). Prep dice come back
    // too, but ON TOP of the draw - they never cover a shortfall. Counting
    // them was the bug behind a Wolf losing at 1 life to 1 burn: Mountain
    // Goat's "draw a die into your Prep Area" kept moving dice from the
    // Bag into Prep, one short of what this counted (2026-09-28).
    private static int Circulating(GameState state, string botId) =>
        state.Dice.Count(d => d.ControllerId == botId
            && d.Zone is not (Zone.FieldZone or Zone.AttackZone or Zone.Unpurchased or Zone.PrepArea))
        - PendingAttackDraws(state, botId);

    // Dice that on-attack draws (Mountain Goat, Swift, ...) would still
    // pull from the Bag into Prep this turn if everything able to swing
    // does - counted against circulation, since that's exactly the second
    // way a Wolf decked itself to death at low life (2026-09-28). Only
    // before attacks are declared; afterward the draws already happened.
    private static int PendingAttackDraws(GameState state, string botId) =>
        state.ActivePlayerId == botId && state.CurrentStep == TurnStep.Main
            ? PotentialAttackers(state, botId).Count(d => DrawsOnAttack(state, d))
            : 0;

    private static bool DrawsOnAttack(GameState state, DieInstance die) =>
        QueryEngine.AbilitiesOf(state, die).Any(a => a.Trigger == TriggerKind.DieAttacks && a.Effect is DrawToZone);

    // How many dice short of a full draw (so how many life per turn) this
    // player will accept, from its life left after the opponent's whole
    // board hits: plenty -> 2, some -> 1, tight -> none, and at 3 life or
    // less -1: keep a spare die circulating, since an On Attack/Awaken draw
    // into Prep can still pull one out of the Bag before next turn.
    private static int BurnBudget(GameState state, string botId)
    {
        var life = state.GetPlayer(botId).Life;
        if (life <= 3) return -1;
        var theirSwing = PotentialAttackers(state, state.OpponentOf(botId)).Sum(d => QueryEngine.GetAttack(state, d));
        var cushion = life - theirSwing;
        return cushion >= 14 ? 2 : cushion >= 8 ? 1 : 0;
    }

    private static bool ShouldUseActionInMain(GameState state, string botId, DieInstance die)
    {
        var card = state.CardCatalog[die.CardId!];
        var effect = card.Abilities.FirstOrDefault(a => a.Trigger == TriggerKind.DieUsed)?.Effect;
        var oppId = state.OpponentOf(botId);
        switch (effect)
        {
            // Anger Issues-style combat pump: wait for the window.
            case Sequence s when s.Steps.Any(x => x is ModifyStat): return false;
            case ModifyStat: return false;
            // Distraction: only worth it with an attack coming into blockers.
            case CombatFlag { Flag: CombatFlagKind.CantBlock }:
                return PotentialAttackers(state, botId).Any() && PotentialBlockers(state, oppId).Any();
            // Mutation: swap only for a real Character waiting in the Used Pile.
            case Sequence s when s.Steps.FirstOrDefault() is MoveDie { ToZone: Zone.UsedPile }:
                return state.DiceIn(botId, Zone.FieldZone).Any()
                    && state.DiceIn(botId, Zone.UsedPile).Any(d => IsCharacterCard(state, d) && state.CardCatalog[d.CardId!].PurchaseCost >= 3);
            // Resurrection: a free die back, as long as there's one to take.
            case Sequence s when s.Steps.FirstOrDefault() is MoveDie { ToZone: Zone.ReservePool }:
                return state.DiceIn(botId, Zone.UsedPile).Any();
            default:
                return true;
        }
    }

    // Best purchase plan: up to three buys (repeats allowed), each paid
    // from what's left after the previous one, scored by PurchaseValue.
    // Returns the plan's first step; the next call re-plans from there.
    private static BotDecision? PlanPurchase(GameState state, string botId, List<DieInstance> reserve, IReadOnlySet<string> skip)
    {
        var options = state.Dice
            .Where(d => d.Zone == Zone.Unpurchased && d.CardId is not null && !skip.Contains(d.Id))
            .Where(d => state.CardCatalog[d.CardId!].CardType.IsCommunity() || d.OwnerId == botId)
            .GroupBy(d => d.CardId!)
            .Select(g => (Card: state.CardCatalog[g.Key], Dice: g.ToList()))
            .Where(x => QueryEngine.CanPurchase(state, botId, x.Card.Id))
            .ToList();
        if (options.Count == 0) return null;

        var owned = state.Dice.Where(d => d.ControllerId == botId && d.CardId is not null && d.Zone != Zone.Unpurchased)
            .GroupBy(d => d.CardId!).ToDictionary(g => g.Key, g => g.Count());
        var offType = OffTypeDemand(state, botId);

        (double Value, (CardDef Card, DieInstance Die, IReadOnlyList<string> Pay)? First) best = (0, null);

        void Search(List<DieInstance> pool, int virtualLeft, Dictionary<string, int> copies, double value, int depth,
            (CardDef, DieInstance, IReadOnlyList<string>)? first)
        {
            if (value > best.Value) best = (value, first);
            if (depth == 3) return;
            foreach (var (card, dice) in options)
            {
                var bought = copies.GetValueOrDefault(card.Id) - owned.GetValueOrDefault(card.Id);
                if (bought >= dice.Count) continue;
                var gain = PurchaseValue(state, botId, card, copies.GetValueOrDefault(card.Id), offType);
                if (gain < 2.0) continue; // not worth diluting the bag for
                var cost = QueryEngine.GetPurchaseCost(state, card, botId);
                if (BotEnergy.PickWithVirtual(state, pool, cost, card.EnergySymbolIds.FirstOrDefault(), virtualLeft) is not { } pay) continue;
                var nextCopies = new Dictionary<string, int>(copies) { [card.Id] = copies.GetValueOrDefault(card.Id) + 1 };
                Search(pool.Where(d => !pay.Dice.Contains(d.Id)).ToList(), virtualLeft - pay.VirtualUsed, nextCopies, value + gain, depth + 1,
                    first ?? (card, dice[bought], pay.Dice));
            }
        }

        Search(reserve.Where(d => BotEnergy.Pips(state, d) > 0).ToList(), state.VirtualEnergyOf(botId),
            new Dictionary<string, int>(owned), 0, 0, null);
        if (best.First is not { } step) return null;
        return new(BotActionKind.Purchase, $"Buy {step.Card.Name}.") { DieId = step.Die.Id, EnergyDieIds = step.Pay };
    }

    // How much one more copy of `card` is worth. Purchase cost is the
    // baseline - the roster is costed by the Homash model, so cost already
    // tracks power - discounted for copies already owned, and for cheap
    // cards once the bag is already full of them.
    private static double PurchaseValue(GameState state, string botId, CardDef card, int copiesOwned, IReadOnlySet<string> offTypeDemand)
    {
        if (card.CardType.IsActionDie())
        {
            var energyType = ActionEnergyType(card);
            var v = 2.5 + (energyType is not null && offTypeDemand.Contains(energyType) ? 1.5 : 0);
            return v * (copiesOwned switch { 0 => 1.0, 1 => 0.6, _ => 0.3 });
        }
        var purchasedDice = state.Dice.Count(d => d.ControllerId == botId && d.CardId is not null && d.Zone != Zone.Unpurchased);
        var baseValue = card.PurchaseCost + 1.0;
        if (card.PurchaseCost <= 3 && purchasedDice >= 8) baseValue *= 0.6;
        return baseValue * (copiesOwned switch { 0 => 1.0, 1 => 0.9, 2 => 0.7, _ => 0.45 });
    }

    // Energy types this player's team needs but its own dice don't make:
    // any card still Unpurchased whose type isn't the Champion's own.
    private static IReadOnlySet<string> OffTypeDemand(GameState state, string botId)
    {
        var own = OwnEnergyType(state, botId);
        return state.Dice
            .Where(d => d.Zone == Zone.Unpurchased && d.OwnerId == botId && d.CardId is not null)
            .Select(d => state.CardCatalog[d.CardId!])
            .Where(c => !c.CardType.IsCommunity())
            .SelectMany(c => c.EnergySymbolIds)
            .Where(t => t != own)
            .ToHashSet();
    }

    private static string? OwnEnergyType(GameState state, string botId) =>
        state.GetPlayer(botId).ChampionId is { } id ? state.Config.Champions.FirstOrDefault(c => c.Id == id)?.EnergySymbolId : null;

    private static string? ActionEnergyType(CardDef card) =>
        card.Die.Faces.SelectMany(f => f.Symbols).Select(s => s.SymbolId).FirstOrDefault();

    // ------------------------------------------------------------------
    // Globals
    // ------------------------------------------------------------------

    private sealed record GlobalOption(CardDef Card, int Index, EffectNode Effect, int Cost, string? Type);

    // Every Global this player could pay for right now whose effect passes
    // `want`, cheapest first; `onlyRealPips` keeps Wilds for later.
    private static BotDecision? GlobalDecision(GameState state, string botId, bool onlyRealPips, Func<GlobalOption, bool> want)
    {
        var pool = state.DiceIn(botId, Zone.ReservePool).Where(d => BotEnergy.Pips(state, d) > 0).ToList();
        if (onlyRealPips) pool = pool.Where(d => !BotEnergy.IsWild(state, d)).ToList();
        foreach (var cardId in state.PlayerOne.TeamCardIds.Concat(state.PlayerTwo.TeamCardIds).Distinct())
        {
            if (!state.CardCatalog.TryGetValue(cardId, out var card) || !QueryEngine.CardTextActive(state, botId, cardId)) continue;
            for (var i = 0; i < card.Abilities.Count; i++)
            {
                var ability = card.Abilities[i];
                if (ability.Trigger != TriggerKind.Global) continue;
                if (ability.OncePerTurn && state.GlobalsUsedThisTurn.Contains((botId, cardId))) continue;
                var option = new GlobalOption(card, i, ability.Effect, QueryEngine.GetGlobalEnergyCost(state, card, ability, botId),
                    ability.EnergyCost?.RequiredSymbolId);
                if (!want(option)) continue;
                if (BotEnergy.PickWithVirtual(state, pool, option.Cost, option.Type, state.VirtualEnergyOf(botId)) is not { } pay) continue;
                return new(BotActionKind.UseGlobal, $"Use {card.Name}'s Global.") { CardId = cardId, AbilityIndex = i, EnergyDieIds = pay.Dice };
            }
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Attack Step
    // ------------------------------------------------------------------

    private static BotDecision DecideAttackers(GameState state, string botId, BotPersona persona)
    {
        var oppId = state.OpponentOf(botId);
        var oppLife = state.GetPlayer(oppId).Life;
        var myLife = state.GetPlayer(botId).Life;
        var candidates = PotentialAttackers(state, botId).OrderByDescending(d => QueryEngine.GetAttack(state, d)).ToList();
        var blockers = PotentialBlockers(state, oppId).ToList();
        if (candidates.Count == 0) return Declare([], "Nothing worth attacking with.");

        // Lethal: even if they block our biggest lanes, the rest kills.
        var spread = candidates.Take(MaxLanes).ToList();
        var unblockableDamage = spread.Skip(Math.Min(blockers.Count, spread.Count)).Sum(d => QueryEngine.GetAttack(state, d))
            + candidates.Where(d => d.CombatFlags.Contains(CombatFlagKind.Unblockable)).Except(spread).Sum(d => QueryEngine.GetAttack(state, d));
        if (unblockableDamage >= oppLife) return Declare(candidates, "All in - lethal.");

        // Crack-back: whatever stays home has to keep their next swing
        // below our life (home dice block their biggest attackers).
        var theirAttack = PotentialAttackers(state, oppId).Select(d => QueryEngine.GetAttack(state, d)).OrderByDescending(a => a).ToList();

        // An on-attack draw pulls a die out of the Bag into Prep, which
        // doesn't help next turn's draw - don't let those swings push the
        // deck-out burn past what this player can afford.
        var drawable = Circulating(state, botId);
        var minDrawable = state.Config.Rules.DrawCount - BurnBudget(state, botId);

        var attackers = new List<DieInstance>();
        foreach (var die in candidates)
        {
            if (!WorthSwinging(state, die, blockers, persona)) continue;
            if (DrawsOnAttack(state, die))
            {
                if (drawable - 1 < minDrawable) continue;
                drawable--;
            }
            var home = PotentialBlockers(state, botId).Count(d => d != die && !attackers.Contains(d));
            var crackBack = theirAttack.Skip(home).Sum();
            if (crackBack >= myLife) continue;
            attackers.Add(die);
        }
        return Declare(attackers, attackers.Count == 0 ? "Hold back - no good attacks." : $"Attack with {attackers.Count}.");
    }

    // Would the defender's best response to this attacker be bad for us?
    // A blocker that kills it and survives is a free kill for them.
    private static bool WorthSwinging(GameState state, DieInstance attacker, IReadOnlyList<DieInstance> blockers, BotPersona persona)
    {
        if (attacker.CombatFlags.Contains(CombatFlagKind.Unblockable)) return true;
        var atk = QueryEngine.GetAttack(state, attacker);
        var def = QueryEngine.GetDefense(state, attacker) - attacker.Damage;
        var deadly = QueryEngine.GetKeywords(state, attacker).Contains("Deadly");
        var myValue = DieValue(state, attacker);
        foreach (var b in blockers)
        {
            var kills = QueryEngine.GetAttack(state, b) >= def || QueryEngine.GetKeywords(state, b).Contains("Deadly");
            var survives = QueryEngine.GetDefense(state, b) - b.Damage > atk && !deadly;
            if (kills && survives && persona.Aggression < 2) return false;
            if (kills && !survives && persona.Aggression < 1) return false;
            if (kills && !survives && DieValue(state, b) < myValue * 0.75 && persona.Aggression < 2) return false; // a bad trade
        }
        return true;
    }

    private static BotDecision Declare(IReadOnlyList<DieInstance> attackers, string reason)
    {
        // One attacker per lane first (each needs its own blocker), extras
        // stacked onto the lanes in turn (a 2+ lane gains Overcrush).
        var lanes = attackers.Select((d, i) => (d.Id, Lane: i % MaxLanes)).ToDictionary(x => x.Id, x => x.Lane);
        return new(BotActionKind.DeclareAttackers, reason) { AttackerLanes = lanes };
    }

    private static BotDecision DecideBlockers(GameState state, string botId, BotPersona persona)
    {
        var activeId = state.OpponentOf(botId);
        var myLife = state.GetPlayer(botId).Life;
        var lanes = state.DiceIn(activeId, Zone.AttackZone)
            .Where(d => !d.CombatFlags.Contains(CombatFlagKind.Unblockable))
            .GroupBy(d => d.Lane ?? -1)
            .Select(g => new Lane(g.ToList(),
                g.Sum(a => QueryEngine.GetAttack(state, a)),
                g.Count() >= 2 || g.Any(a => QueryEngine.GetKeywords(state, a).Contains("Overcrush")),
                g.Any(a => QueryEngine.GetKeywords(state, a).Contains("Deadly"))))
            .OrderByDescending(l => l.Attack)
            .ToList();
        var unblockable = state.DiceIn(activeId, Zone.AttackZone).Where(d => d.CombatFlags.Contains(CombatFlagKind.Unblockable))
            .Sum(d => QueryEngine.GetAttack(state, d));
        var available = PotentialBlockers(state, botId).ToList();
        var blocks = new List<(string, string)>();
        var incoming = lanes.Sum(l => l.Attack) + unblockable;

        // How much a point of damage matters, rising as life falls.
        double DamageWeight() => persona.ChumpThreshold / Math.Max(1, myLife) * 0.5;

        foreach (var lane in lanes)
        {
            if (available.Count == 0) break;
            var mustBlock = incoming >= myLife;
            (DieInstance Blocker, double Score)? best = null;
            foreach (var b in available)
            {
                var score = BlockScore(state, b, lane, DamageWeight());
                if (best is null || score > best.Value.Score) best = (b, score);
            }
            if (best is not { } pick) break;
            if (pick.Score <= 0 && !mustBlock) continue;

            blocks.Add((lane.Attackers[0].Id, pick.Blocker.Id));
            available.Remove(pick.Blocker);
            var prevented = lane.Overcrush ? Math.Min(lane.Attack, QueryEngine.GetDefense(state, pick.Blocker)) : lane.Attack;
            incoming -= prevented;
        }

        return new(BotActionKind.DeclareBlockers, blocks.Count == 0 ? "No blocks." : $"Block {blocks.Count}.") { Blocks = blocks };
    }

    private sealed record Lane(IReadOnlyList<DieInstance> Attackers, int Attack, bool Overcrush, bool Deadly);

    // Outcome of putting `b` in front of a lane: its attackers' value if
    // it kills one, minus its own if it dies, plus the damage it keeps off
    // this player's life (weighted by how much life matters right now).
    private static double BlockScore(GameState state, DieInstance b, Lane lane, double damageWeight)
    {
        var bAtk = QueryEngine.GetAttack(state, b);
        var bDef = QueryEngine.GetDefense(state, b) - b.Damage;
        var survives = bDef > lane.Attack && !lane.Deadly;
        var bDeadly = QueryEngine.GetKeywords(state, b).Contains("Deadly");
        var killed = lane.Attackers.Where(a => bDeadly || QueryEngine.GetDefense(state, a) - a.Damage <= bAtk)
            .OrderByDescending(a => DieValue(state, a)).FirstOrDefault();
        var prevented = lane.Overcrush ? Math.Min(lane.Attack, bDef) : lane.Attack;

        var score = prevented * damageWeight;
        if (killed is not null) score += DieValue(state, killed);
        if (!survives) score -= DieValue(state, b);
        if (survives && killed is null) score += 0.5; // a free wall
        return score;
    }

    // ------------------------------------------------------------------
    // Attack Step's Action/Global window
    // ------------------------------------------------------------------

    private static BotDecision ActiveWindow(GameState state, string botId)
    {
        var unblocked = UnblockedAttackers(state).Where(d => d.ControllerId == botId).ToList();
        var attacking = state.DiceIn(botId, Zone.AttackZone).ToList();

        // A combat pump action (Anger Issues) is only good now.
        var pump = state.DiceIn(botId, Zone.ReservePool).FirstOrDefault(d => IsActionFace(state, d) && IsCombatPump(state, d));
        if (pump is not null && attacking.Count > 0)
            return new(BotActionKind.UseAction, $"Use {CardName(state, pump)}.") { DieId = pump.Id };

        // +ATK Globals on an unblocked attacker are straight extra damage.
        if (unblocked.Count > 0 && GlobalDecision(state, botId, onlyRealPips: false, g => g.Effect is ModifyStat { AtkDelta: > 0 }) is { } g)
            return g;

        return new(BotActionKind.Pass, "Pass - resolve combat.");
    }

    private static BotDecision InactiveWindow(GameState state, string botId)
    {
        // Send the biggest unblocked attacker home (Distraction's Global).
        var biggest = UnblockedAttackers(state).Where(d => d.ControllerId != botId)
            .Select(d => QueryEngine.GetAttack(state, d)).DefaultIfEmpty(0).Max();
        if (biggest >= 2 && GlobalDecision(state, botId, onlyRealPips: false,
                g => g.Effect is MoveDie { Target.AttackersOnly: true }) is { } send)
            return send;
        return new(BotActionKind.Pass, "Pass.");
    }

    private static IEnumerable<DieInstance> UnblockedAttackers(GameState state)
    {
        var blocks = state.DeclaredBlocks;
        var attackers = state.DiceIn(state.ActivePlayerId, Zone.AttackZone).ToList();
        var blockedLanes = attackers.Where(a => blocks?.BlockersOf(a.Id).Count > 0).Select(a => a.Lane).ToHashSet();
        return attackers.Where(a => !blockedLanes.Contains(a.Lane));
    }

    // ------------------------------------------------------------------
    // Pending choices
    // ------------------------------------------------------------------

    private static BotDecision AnswerChoice(GameState state, string botId, PendingChoice pending)
    {
        double Score(string id)
        {
            if (state.IsPlayerId(id))
            {
                // A "you may" stand-in, or a player target.
                if (pending.CandidateIds.Count == 1 && pending.MinCount == 0) return pending.Intent == ChoiceIntent.Harmful ? -1 : 1;
                var mine = id == botId;
                return pending.Intent == ChoiceIntent.Harmful ? (mine ? -10 : 3) : (mine ? 3 : -10);
            }
            var die = state.Dice.First(d => d.Id == id);
            var own = die.ControllerId == botId;
            var value = ChoiceValue(state, die);
            // In the window, what matters most is who's hitting face.
            if (state.CurrentStepId == StepIds.ActionGlobalWindow && UnblockedAttackers(state).Any(a => a.Id == id))
                value += 2 * QueryEngine.GetAttack(state, die);
            return pending.Intent == ChoiceIntent.Harmful ? (own ? -value : value) : (own ? value : -value);
        }

        var ranked = pending.CandidateIds.Select(id => (Id: id, Score: Score(id))).OrderByDescending(x => x.Score).ToList();
        var picks = ranked.Take(pending.MinCount).Select(x => x.Id).ToList();
        picks.AddRange(ranked.Skip(pending.MinCount).Take(pending.MaxCount - pending.MinCount).Where(x => x.Score > 0).Select(x => x.Id));
        return new(BotActionKind.ResolvePendingChoice, $"Answer: {pending.Description}") { DieIds = picks };
    }

    // A die in play is worth its stats; one out of play (Used Pile, Bag,
    // Prep) is worth what its card cost.
    private static double ChoiceValue(GameState state, DieInstance die)
    {
        if (die.Zone is Zone.FieldZone or Zone.AttackZone && state.GetCurrentFace(die)?.Character is not null)
            return DieValue(state, die);
        return die.CardId is { } cardId ? state.CardCatalog[cardId].PurchaseCost : 0.5;
    }

    // ------------------------------------------------------------------
    // Shared helpers
    // ------------------------------------------------------------------

    private static double DieValue(GameState state, DieInstance die)
    {
        if (state.GetCurrentFace(die)?.Character is null) return die.CardId is { } c ? state.CardCatalog[c].PurchaseCost * 0.5 : 0.5;
        var value = QueryEngine.GetAttack(state, die) * 1.2 + (QueryEngine.GetDefense(state, die) - die.Damage) * 0.8;
        if (die.CardId is { } cardId)
        {
            var card = state.CardCatalog[cardId];
            value += 1 + (card.Abilities.Count > 0 || card.Continuous.Count > 0 || card.Keywords.Count > 0 ? 1 : 0);
        }
        return value;
    }

    private static IEnumerable<DieInstance> PotentialAttackers(GameState state, string playerId) =>
        state.DiceIn(playerId, Zone.FieldZone)
            .Where(d => state.GetCurrentFace(d)?.Character is not null)
            .Where(d => !d.CombatFlags.Contains(CombatFlagKind.CantAttack) && !d.CombatFlags.Contains(CombatFlagKind.OnlyBlocker))
            .Where(d => QueryEngine.GetAttack(state, d) > 0);

    private static IEnumerable<DieInstance> PotentialBlockers(GameState state, string playerId) =>
        state.DiceIn(playerId, Zone.FieldZone)
            .Where(d => state.GetCurrentFace(d)?.Character is not null && !d.CombatFlags.Contains(CombatFlagKind.CantBlock));

    private static bool IsCharacterCard(GameState state, DieInstance die) =>
        die.CardId is { } id && state.CardCatalog[id].CardType == CardType.Character;

    private static bool IsActionFace(GameState state, DieInstance die) =>
        die.CardId is { } id && state.CardCatalog[id].CardType.IsActionDie() && state.GetCurrentFace(die)?.Kind == FaceKind.ActionFace;

    private static bool IsCombatPump(GameState state, DieInstance die) =>
        state.CardCatalog[die.CardId!].Abilities.FirstOrDefault(a => a.Trigger == TriggerKind.DieUsed)?.Effect switch
        {
            ModifyStat => true,
            Sequence s => s.Steps.Any(x => x is ModifyStat),
            _ => false,
        };

    private static string CardName(GameState state, DieInstance die) =>
        die.CardId is { } id ? state.CardCatalog[id].Name : "a Tardigrade";
}
