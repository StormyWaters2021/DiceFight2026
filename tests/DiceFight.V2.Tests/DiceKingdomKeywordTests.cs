using DiceFight.V2.Bot;
using DiceFight.V2.Data;
using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.V2.Tests;

// Intimidate and Infiltrate (2026-10-03) - engine-built keywords
// (KeywordAbilities), proven on the real Dice Kingdom cards that carry
// them (Frilled Lizard, Flying Squirrel) through the real firing path:
// TurnEngine/CombatEngine action -> AbilityQueue -> DrainQueue.
public class DiceKingdomKeywordTests
{
    private sealed class FixedRoller(int index) : IDiceRoller
    {
        public int Roll(DieDefinition die) => index;
    }

    private static void Drain(GameState state, AbilityQueue queue) =>
        EffectInterpreter.DrainQueue(state, queue, new FixedRoller(0), new Random(1));

    // Armadillo (own +1 DEF) attacks and Great Horned Owl (Foresight)
    // defends: neither passive touches an ATK or DEF these tests read.
    private static GameState NewGame()
    {
        var playerOne = new Player { Id = "p1", Name = "One", ChampionId = "Armadillo" };
        playerOne.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion["Armadillo"]);
        var playerTwo = new Player { Id = "p2", Name = "Two", ChampionId = "GreatHornedOwl" };
        playerTwo.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion["GreatHornedOwl"]);
        var state = GameSetup.NewGame(DiceKingdomConfig.Config, DiceKingdomConfig.Catalog, playerOne, playerTwo);
        state.ActivePlayerId = "p1";
        state.MoveToStep(StepIds.Main);
        return state;
    }

    private static DieInstance AddDie(GameState state, string cardId, string controllerId, Zone zone, int level = 1, string? tag = null)
    {
        var die = new DieInstance
        {
            Id = $"{controllerId}-{cardId}-{tag ?? zone.ToString()}", CardId = cardId, OwnerId = controllerId,
            ControllerId = controllerId, Zone = zone, CurrentFaceIndex = level - 1,
        };
        state.Dice.Add(die);
        return die;
    }

    private static string[] Energy(GameState state, string controllerId, string energyType, int count)
    {
        var ids = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var die = new DieInstance
            {
                Id = $"{controllerId}-energy-{i}", PoolDieId = $"Tardigrade{energyType}", OwnerId = controllerId,
                ControllerId = controllerId, Zone = Zone.ReservePool, CurrentFaceIndex = 0, // L1: 2 energy
            };
            state.Dice.Add(die);
            ids.Add(die.Id);
        }
        return [.. ids];
    }

    private static DieInstance FieldLizard(GameState state, AbilityQueue queue)
    {
        var lizard = AddDie(state, DiceKingdomConfig.FrilledLizard.Id, "p1", Zone.ReservePool);
        TurnEngine.Field(state, queue, lizard.Id, Energy(state, "p1", "Eye", 1)); // L1 fields for 1
        Drain(state, queue);
        return lizard;
    }

    // --- Intimidate ---

    [Fact]
    public void Intimidate_Removes_The_Target_Until_CleanUp_Then_It_Returns_On_The_Same_Face()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var target = AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone, level: 3);

        FieldLizard(state, queue); // the only opposing creature: auto-targeted

        Assert.Null(state.PendingChoice);
        Assert.Equal(Zone.Intimidated, target.Zone);
        Assert.Equal(2, target.CurrentFaceIndex);

        state.MoveToStep(StepIds.ReturnToField);
        TurnEngine.CleanUp(state, queue);

        Assert.Equal(Zone.FieldZone, target.Zone);
        Assert.Equal(2, target.CurrentFaceIndex); // still level 3
    }

    [Fact]
    public void Intimidate_Asks_Which_Opposing_Creature_And_Never_Offers_Your_Own()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var mine = AddDie(state, DiceKingdomConfig.HoneyBadger.Id, "p1", Zone.FieldZone);
        var big = AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone);
        var small = AddDie(state, DiceKingdomConfig.HermitCrab.Id, "p2", Zone.FieldZone);

        FieldLizard(state, queue);

        var pending = Assert.IsType<PendingChoice>(state.PendingChoice);
        Assert.Equal("p1", pending.ControllerId);
        Assert.Equal([big.Id, small.Id], pending.CandidateIds.Order());
        Assert.DoesNotContain(mine.Id, pending.CandidateIds);

        EffectInterpreter.AnswerPendingChoice(state, [big.Id]);
        Drain(state, queue);
        Assert.Equal(Zone.Intimidated, big.Zone);
        Assert.Equal(Zone.FieldZone, small.Zone);
    }

    [Fact]
    public void An_Intimidated_Creature_Cannot_Block_And_Its_While_Active_Aura_Is_Off()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var muskOx = AddDie(state, DiceKingdomConfig.MuskOx.Id, "p2", Zone.FieldZone); // "your creatures get +1 DEF"
        var crab = AddDie(state, DiceKingdomConfig.HermitCrab.Id, "p2", Zone.FieldZone);
        var crabDefense = QueryEngine.GetDefense(state, crab);

        FieldLizard(state, queue);
        if (state.PendingChoice is not null)
        {
            EffectInterpreter.AnswerPendingChoice(state, [muskOx.Id]);
            Drain(state, queue);
        }

        Assert.Equal(Zone.Intimidated, muskOx.Zone);
        Assert.Equal(crabDefense - 1, QueryEngine.GetDefense(state, crab));

        var attacker = AddDie(state, DiceKingdomConfig.HoneyBadger.Id, "p1", Zone.FieldZone);
        state.MoveToStep(StepIds.SelectAttackers);
        CombatEngine.DeclareAttackers(state, queue, [attacker.Id]);
        var assignment = new CombatAssignment();
        assignment.AssignBlocker(attacker.Id, muskOx.Id);
        Assert.Throws<InvalidOperationException>(() => CombatEngine.DeclareBlockers(state, queue, assignment, [muskOx.Id]));
    }

    [Fact]
    public void Intimidate_Fizzles_With_No_Opposing_Creature()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var lizard = FieldLizard(state, queue);

        Assert.Null(state.PendingChoice);
        Assert.Equal(Zone.FieldZone, lizard.Zone);
    }

    // --- Infiltrate ---

    // Flying Squirrel L2 is 3 ATK. Declares it alone (lane 0) against an
    // opponent with one potential blocker, `blocked` deciding whether it
    // is used.
    private static (GameState state, AbilityQueue queue, DieInstance squirrel, CombatAssignment assignment) SwingSquirrel(bool blocked)
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var squirrel = AddDie(state, DiceKingdomConfig.FlyingSquirrel.Id, "p1", Zone.FieldZone, level: 2);
        var wall = AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone);
        state.MoveToStep(StepIds.SelectAttackers);
        CombatEngine.DeclareAttackers(state, queue, [squirrel.Id]);
        Drain(state, queue);

        var assignment = new CombatAssignment();
        if (blocked) assignment.AssignBlocker(squirrel.Id, wall.Id);
        CombatEngine.DeclareBlockers(state, queue, assignment, blocked ? [wall.Id] : []);
        Drain(state, queue);
        return (state, queue, squirrel, assignment);
    }

    [Fact]
    public void Unblocked_Infiltrate_Accepted_Returns_To_The_Field_For_1_Damage_And_Stays_In_Play()
    {
        var (state, queue, squirrel, assignment) = SwingSquirrel(blocked: false);

        var pending = Assert.IsType<PendingChoice>(state.PendingChoice);
        Assert.Equal("p1", pending.ControllerId);
        Assert.Contains("Infiltrate", pending.Description);

        EffectInterpreter.AnswerPendingChoice(state, [squirrel.Id]);
        Drain(state, queue);
        Assert.Equal(Zone.FieldZone, squirrel.Zone);
        Assert.Null(squirrel.Lane);
        Assert.Equal(19, state.PlayerTwo.Life);

        CombatEngine.AssignCombatDamage(state, queue, assignment, new Dictionary<string, IReadOnlyDictionary<string, int>>());
        TurnEngine.CleanUp(state, queue);
        Assert.Equal(19, state.PlayerTwo.Life); // no combat damage on top
        Assert.Equal(Zone.FieldZone, squirrel.Zone); // never went Out of Play
    }

    [Fact]
    public void Unblocked_Infiltrate_Declined_Hits_For_Full_Attack_And_Leaves_Play()
    {
        var (state, queue, squirrel, assignment) = SwingSquirrel(blocked: false);

        EffectInterpreter.AnswerPendingChoice(state, []);
        Drain(state, queue);
        Assert.Equal(Zone.AttackZone, squirrel.Zone);

        CombatEngine.AssignCombatDamage(state, queue, assignment, new Dictionary<string, IReadOnlyDictionary<string, int>>());
        Assert.Equal(17, state.PlayerTwo.Life);
        Assert.Equal(Zone.OutOfPlay, squirrel.Zone);
    }

    [Fact]
    public void A_Blocked_Infiltrate_Attacker_Gets_No_Offer()
    {
        var (state, _, squirrel, _) = SwingSquirrel(blocked: true);

        Assert.Null(state.PendingChoice);
        Assert.Equal(Zone.AttackZone, squirrel.Zone);
    }

    [Fact]
    public void Bot_Takes_Infiltrate_On_A_Small_Hit_And_Declines_When_The_Full_Hit_Is_Lethal()
    {
        var (state, _, squirrel, _) = SwingSquirrel(blocked: false);
        var decision = DiceKingdomBot.Decide(state, "p1")!;
        Assert.Equal(BotActionKind.ResolvePendingChoice, decision.Kind);
        Assert.Empty(decision.DieIds!); // 3 ATK: worth more than the die's trip home

        squirrel.CurrentFaceIndex = 0; // L1: 2 ATK
        Assert.Equal([squirrel.Id], DiceKingdomBot.Decide(state, "p1")!.DieIds!);

        state.PlayerTwo.Life = 2; // the full 2 is lethal, 1 isn't
        Assert.Empty(DiceKingdomBot.Decide(state, "p1")!.DieIds!);
    }

    // --- Attune / Obscure ---

    // An Anger Issues die already on its action face in p1's Reserve Pool.
    private static DieInstance ActionDieReady(GameState state)
    {
        var die = AddDie(state, DiceKingdomConfig.AngerIssues.Id, "p1", Zone.ReservePool, level: 1, tag: "action");
        if (!state.PlayerOne.TeamCardIds.Contains(DiceKingdomConfig.AngerIssues.Id))
            state.PlayerOne.TeamCardIds.Add(DiceKingdomConfig.AngerIssues.Id);
        return die;
    }

    // Answers every pending choice: Attune's with `attunePick`, anything
    // else (Anger Issues' own pump) with its first candidate.
    private static void ResolveAll(GameState state, AbilityQueue queue, string attunePick)
    {
        while (state.PendingChoice is { } pending)
        {
            var pick = pending.Description.Contains("Attune") ? attunePick : pending.CandidateIds[0];
            EffectInterpreter.AnswerPendingChoice(state, [pick]);
            Drain(state, queue);
        }
    }

    [Fact]
    public void Attune_Deals_1_To_The_Opponent_Or_A_Character_Each_Time_You_Use_An_Action_Die()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var eel = AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.FieldZone);
        var crab = AddDie(state, DiceKingdomConfig.HermitCrab.Id, "p2", Zone.FieldZone);

        TurnEngine.UseAction(state, queue, ActionDieReady(state).Id);
        Drain(state, queue);

        var pending = Assert.IsType<PendingChoice>(state.PendingChoice);
        Assert.Contains("Attune", pending.Description);
        Assert.Contains("p2", pending.CandidateIds);
        Assert.DoesNotContain("p1", pending.CandidateIds); // never yourself
        Assert.Contains(crab.Id, pending.CandidateIds);
        Assert.Contains(eel.Id, pending.CandidateIds); // any character die

        ResolveAll(state, queue, "p2");
        Assert.Equal(19, state.PlayerTwo.Life);
    }

    [Fact]
    public void Attune_Can_KO_A_Creature()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.FieldZone);
        var badger = AddDie(state, DiceKingdomConfig.HoneyBadger.Id, "p2", Zone.FieldZone); // L1 1 DEF

        TurnEngine.UseAction(state, queue, ActionDieReady(state).Id);
        Drain(state, queue);
        ResolveAll(state, queue, badger.Id);

        Assert.Equal(Zone.PrepArea, badger.Zone); // KO'd
    }

    [Fact]
    public void Attune_Does_Not_Fire_On_A_Global_Or_While_Inactive()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.PrepArea); // not active
        var activeEel = AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.FieldZone, tag: "field");
        ActionDieReady(state);

        // A Global is not an action die.
        TurnEngine.UseGlobal(state, queue, DiceKingdomConfig.AngerIssues.Id, "p1", 1, Energy(state, "p1", "Claw", 1));
        Drain(state, queue);
        while (state.PendingChoice is { } pending)
        {
            Assert.DoesNotContain("Attune", pending.Description);
            EffectInterpreter.AnswerPendingChoice(state, [pending.CandidateIds[0]]);
            Drain(state, queue);
        }
        Assert.Equal(20, state.PlayerTwo.Life);

        // The die in the Prep Area adds nothing: one action, one Attune.
        activeEel.Zone = Zone.FieldZone;
        TurnEngine.UseAction(state, queue, state.Dice.First(d => d.Id.EndsWith("-action")).Id);
        Drain(state, queue);
        var attunes = 0;
        while (state.PendingChoice is { } pending)
        {
            if (pending.Description.Contains("Attune")) attunes++;
            EffectInterpreter.AnswerPendingChoice(state, [pending.Description.Contains("Attune") ? "p2" : pending.CandidateIds[0]]);
            Drain(state, queue);
        }
        Assert.Equal(1, attunes);
    }

    [Fact]
    public void Attune_N_Deals_N_And_A_Granted_Attune_Stacks_With_The_Printed_One()
    {
        Assert.Equal(1, KeywordAbilities.AttuneAmount("Attune"));
        Assert.Equal(2, KeywordAbilities.AttuneAmount("Attune 2"));
        Assert.Null(KeywordAbilities.AttuneAmount("Attuned"));

        var state = NewGame();
        var queue = new AbilityQueue();
        var eel = AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.FieldZone);
        eel.GrantedTags.Add(new GrantedTag("Attune 2", Duration.EndOfTurn, "p1"));

        TurnEngine.UseAction(state, queue, ActionDieReady(state).Id);
        Drain(state, queue);
        ResolveAll(state, queue, "p2");

        Assert.Equal(17, state.PlayerTwo.Life); // 1 + 2
    }

    [Fact]
    public void Obscure_Makes_The_Creature_Unblockable_This_Turn_When_You_Use_An_Action_Die()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var chameleon = AddDie(state, DiceKingdomConfig.Chameleon.Id, "p1", Zone.FieldZone);
        var wall = AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone);

        TurnEngine.UseAction(state, queue, ActionDieReady(state).Id);
        Drain(state, queue);
        ResolveAll(state, queue, "p2");
        Assert.Contains(CombatFlagKind.Unblockable, chameleon.CombatFlags);

        state.MoveToStep(StepIds.SelectAttackers);
        CombatEngine.DeclareAttackers(state, queue, [chameleon.Id]);
        var assignment = new CombatAssignment();
        assignment.AssignBlocker(chameleon.Id, wall.Id);
        Assert.Throws<InvalidOperationException>(() => CombatEngine.DeclareBlockers(state, queue, assignment, [wall.Id]));

        state.MoveToStep(StepIds.ReturnToField);
        chameleon.Zone = Zone.FieldZone;
        TurnEngine.CleanUp(state, queue);
        Assert.DoesNotContain(CombatFlagKind.Unblockable, chameleon.CombatFlags); // this turn only
    }

    [Fact]
    public void Bot_Attune_Finishes_A_Creature_It_Can_Kill_Else_Hits_Face()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        AddDie(state, DiceKingdomConfig.ElectricEel.Id, "p1", Zone.FieldZone);
        var hippo = AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone); // 5 DEF: a ping does nothing
        TurnEngine.UseAction(state, queue, ActionDieReady(state).Id);
        Drain(state, queue);
        Assert.Contains("Attune", state.PendingChoice!.Description);
        Assert.Equal(["p2"], DiceKingdomBot.Decide(state, "p1")!.DieIds!);

        hippo.Damage = 4; // now 1 more kills it
        Assert.Equal([hippo.Id], DiceKingdomBot.Decide(state, "p1")!.DieIds!);
    }

    // A lane holding an unblockable attacker can't be blocked at all, so
    // the bot must not block its lane-mate either (the simulator's first
    // Chameleon run threw "is unblockable this turn" in 20 games).
    [Fact]
    public void Bot_Never_Blocks_A_Lane_With_An_Unblockable_Attacker_In_It()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var chameleon = AddDie(state, DiceKingdomConfig.Chameleon.Id, "p1", Zone.FieldZone);
        var badger = AddDie(state, DiceKingdomConfig.HoneyBadger.Id, "p1", Zone.FieldZone);
        AddDie(state, DiceKingdomConfig.Hippopotamus.Id, "p2", Zone.FieldZone, level: 3); // blocks anything for free
        chameleon.CombatFlags.Add(CombatFlagKind.Unblockable);
        state.MoveToStep(StepIds.SelectAttackers);
        CombatEngine.DeclareAttackers(state, queue, new Dictionary<string, int> { [chameleon.Id] = 0, [badger.Id] = 0 });

        var decision = DiceKingdomBot.Decide(state, "p2")!;
        Assert.Equal(BotActionKind.DeclareBlockers, decision.Kind);
        Assert.Empty(decision.Blocks);
    }
}
