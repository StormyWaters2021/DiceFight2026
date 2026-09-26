using DiceFight.Api.Controllers;
using DiceFight.V2;
using DiceFight.V2.Data;
using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.Api.Tests;

// Basic Actions + Globals (2026-09-26): each Champion brings one shared
// Basic Action whose Global either player can use, paid with 1 energy of
// that Champion's type.
public class V2ActionsAndGlobalsTests
{
    private static (V2GameStore Store, V2GameSession Session, V2GamesController TeamA, V2GamesController TeamB) StartInMain()
    {
        var store = new V2GameStore();
        var anon = V2SeatedController.Anonymous(store);
        var created = V2SeatedController.CreatedDto(anon.Create(new CreateV2GameRequest("Wolf", "GreatHornedOwl")));
        var session = store.GetSession(created.Game.GameId);
        var teamA = V2SeatedController.For(store, session, "teamA");
        var teamB = V2SeatedController.For(store, session, "teamB");
        teamA.ClearAndDraw(session.Id);
        teamA.Roll(session.Id);
        teamA.FinishRoll(session.Id);
        Assert.Equal(TurnStep.Main, session.State.CurrentStep);
        // Clear the rolled Reserve Pool so each test places exactly the dice it needs.
        foreach (var d in session.State.Dice.Where(d => d.Zone == Zone.ReservePool)) { d.Zone = Zone.UsedPile; d.CurrentFaceIndex = null; }
        return (store, session, teamA, teamB);
    }

    private static DieInstance Tardigrade(GameState state, string player, Zone zone, int face)
    {
        var die = state.Dice.First(d => d.ControllerId == player && d.CardId is null && d.Zone is Zone.Bag or Zone.UsedPile);
        die.Zone = zone;
        die.CurrentFaceIndex = face;
        return die;
    }

    [Fact]
    public void Each_Champion_Brings_Three_Of_Its_Own_Basic_Action_Dice()
    {
        var (_, session, _, _) = StartInMain();
        var state = session.State;
        Assert.Equal(3, state.Dice.Count(d => d.OwnerId == "teamA" && d.CardId == DiceKingdomConfig.AngerIssues.Id && d.Zone == Zone.Unpurchased));
        Assert.Equal(3, state.Dice.Count(d => d.OwnerId == "teamB" && d.CardId == DiceKingdomConfig.Mutation.Id && d.Zone == Zone.Unpurchased));
    }

    [Fact]
    public void Either_Player_Can_Buy_The_Opponents_Champions_Action_Die()
    {
        var (_, session, teamA, _) = StartInMain();
        var state = session.State;
        var pay = new[] { Tardigrade(state, "teamA", Zone.ReservePool, 0), Tardigrade(state, "teamA", Zone.ReservePool, 0) }; // 2 Claw each
        var owlsAction = state.Dice.First(d => d.OwnerId == "teamB" && d.CardId == DiceKingdomConfig.Mutation.Id);

        teamA.Purchase(session.Id, new V2PurchaseRequest(owlsAction.Id, pay.Select(d => d.Id).ToList()));

        Assert.Equal("teamA", owlsAction.ControllerId);
        Assert.Equal(Zone.UsedPile, owlsAction.Zone);
    }

    [Fact]
    public void Global_Costs_One_Of_Its_Champions_Energy_And_Only_This_Games_Globals_Are_Usable()
    {
        var (_, session, teamA, teamB) = StartInMain();
        var state = session.State;
        var body = Tardigrade(state, "teamA", Zone.FieldZone, 2); // 1/1, +1A from Wolf = 2
        var claw = Tardigrade(state, "teamA", Zone.ReservePool, 0); // 2 Claw
        var angerIssues = DiceKingdomConfig.AngerIssues;
        var globalIndex = angerIssues.Abilities.ToList().FindIndex(a => a.Trigger == TriggerKind.Global);

        // Not in this game (Wolf vs Owl): Resurrection is Golden Eagle's.
        Assert.Throws<InvalidOperationException>(() =>
            teamA.UseGlobal(session.Id, new V2UseGlobalRequest(DiceKingdomConfig.Resurrection.Id, globalIndex, [claw.Id])));
        // The opponent can't use Globals during your Main Step.
        Assert.Throws<InvalidOperationException>(() =>
            teamB.UseGlobal(session.Id, new V2UseGlobalRequest(angerIssues.Id, globalIndex, [])));

        var dto = V2SeatedController.Dto(teamA.UseGlobal(session.Id, new V2UseGlobalRequest(angerIssues.Id, globalIndex, [claw.Id])));

        Assert.Equal(3, dto.Dice.Single(d => d.Id == body.Id).EffectiveAttack);
        Assert.Equal(1, dto.Dice.Single(d => d.Id == claw.Id).EnergyAmount); // 2 Claw, 1 spent - spun down to its 1-Claw face
        Assert.Contains(dto.Log, l => l.Text.Contains("Anger Issues's Global"));
    }

    // Priority (rules 2.6.6 / 2.7.3.4, Priority.cs): Active acts freely
    // and passes; Inactive gets ONE thing or passes; Inactive passing
    // right after Active ends the window.
    [Fact]
    public void Priority_Walks_Main_And_The_Attack_Window_As_In_Dice_Masters()
    {
        var (_, session, teamA, teamB) = StartInMain();
        var state = session.State;
        var attacker = Tardigrade(state, "teamA", Zone.FieldZone, 2);
        Tardigrade(state, "teamB", Zone.FieldZone, 2); // a creature for Mutation's spins
        var eye1 = Tardigrade(state, "teamB", Zone.ReservePool, 0); // 2 Eye, left over from their turn
        var eye2 = Tardigrade(state, "teamB", Zone.ReservePool, 0); // 2 more
        var mutation = DiceKingdomConfig.Mutation;
        var globalIndex = mutation.Abilities.ToList().FindIndex(a => a.Trigger == TriggerKind.Global);
        var useMutation = (string eyeId) => new V2UseGlobalRequest(mutation.Id, globalIndex, [eyeId]);
        // Mutation's "spin another creature up" has two candidates - answer it.
        void AnswerChoice()
        {
            if (state.PendingChoice is { } c) teamB.ResolvePendingChoice(session.Id, new V2ResolvePendingChoiceRequest([c.CandidateIds[0]]));
        }

        // Main: the Active player holds priority first; the Inactive can't act yet.
        Assert.Equal("teamA", V2SeatedController.Dto(teamA.Get(session.Id)).PriorityPlayerId);
        Assert.Throws<InvalidOperationException>(() => teamB.UseGlobal(session.Id, useMutation(eye1.Id)));

        // Active passes ("Done buying") - Owl can pay for Mutation, so priority goes to them, still in Main.
        var dto = V2SeatedController.Dto(teamA.EnterAttackStep(session.Id));
        Assert.Equal(StepIds.Main, dto.CurrentStepId);
        Assert.Equal("teamB", dto.PriorityPlayerId);
        Assert.Throws<InvalidOperationException>(() => teamA.Purchase(session.Id, new V2PurchaseRequest("x", [])));

        // Inactive does ONE thing - priority returns to the Active player.
        dto = V2SeatedController.Dto(teamB.UseGlobal(session.Id, useMutation(eye1.Id)));
        Assert.Equal("teamA", dto.PriorityPlayerId);
        AnswerChoice();
        Assert.Throws<InvalidOperationException>(() => teamB.UseGlobal(session.Id, useMutation(eye2.Id)));

        // Active passes again; Inactive passes; Main ends and the attack begins.
        teamA.EnterAttackStep(session.Id);
        dto = V2SeatedController.Dto(teamB.Pass(session.Id));
        Assert.Equal(StepIds.SelectAttackers, dto.CurrentStepId);
        Assert.Null(dto.PriorityPlayerId);

        // The Attack Step's action window runs the same way.
        teamA.DeclareAttackers(session.Id, new V2DeclareAttackersRequest([new V2AttackerDeclaration(attacker.Id, 0)]));
        dto = V2SeatedController.Dto(teamB.DeclareBlockers(session.Id, new V2DeclareBlockersRequest([])));
        Assert.Equal(StepIds.ActionGlobalWindow, dto.CurrentStepId);
        Assert.Equal("teamA", dto.PriorityPlayerId);
        dto = V2SeatedController.Dto(teamA.AssignCombatDamage(session.Id, new V2AssignCombatDamageRequest([])));
        Assert.Equal("teamB", dto.PriorityPlayerId); // still has Eye energy
        teamB.UseGlobal(session.Id, useMutation(eye2.Id));
        AnswerChoice();
        teamA.AssignCombatDamage(session.Id, new V2AssignCombatDamageRequest([]));
        dto = V2SeatedController.Dto(teamB.Pass(session.Id));
        Assert.NotEqual(StepIds.ActionGlobalWindow, dto.CurrentStepId); // damage resolved
        Assert.Equal(2, state.Log.Count(l => l.Text == "Great Horned Owl uses Mutation's Global."));
        Assert.Equal(2, state.Log.Count(l => l.Text == "Great Horned Owl passes."));
    }

    [Fact]
    public void An_Opponent_Who_Cant_Pay_For_Any_Global_Passes_Automatically()
    {
        var (_, session, teamA, _) = StartInMain(); // teamB's Reserve Pool is empty
        var dto = V2SeatedController.Dto(teamA.EnterAttackStep(session.Id));
        Assert.Equal(StepIds.SelectAttackers, dto.CurrentStepId);
    }

    [Fact]
    public void Action_Die_Is_Usable_Only_On_Its_Action_Face()
    {
        var (_, session, teamA, _) = StartInMain();
        var state = session.State;
        var body = Tardigrade(state, "teamA", Zone.FieldZone, 2); // 2A with Wolf
        var action = state.Dice.First(d => d.OwnerId == "teamA" && d.CardId == DiceKingdomConfig.AngerIssues.Id);
        action.Zone = Zone.ReservePool;
        action.CurrentFaceIndex = 3; // an energy face

        Assert.Throws<InvalidOperationException>(() => teamA.UseAction(session.Id, new V2UseActionRequest(action.Id)));
        Assert.False(V2SeatedController.Dto(teamA.Get(session.Id)).Dice.Single(d => d.Id == action.Id).IsActionFace);

        action.CurrentFaceIndex = 0; // an action face
        var dto = V2SeatedController.Dto(teamA.UseAction(session.Id, new V2UseActionRequest(action.Id)));

        Assert.Equal(5, dto.Dice.Single(d => d.Id == body.Id).EffectiveAttack); // +3A
        Assert.Contains("Overcrush", QueryEngine.GetKeywords(state, body));
        Assert.Equal(Zone.OutOfPlay, action.Zone);
    }

    [Fact]
    public void Card_Dto_Describes_The_Action_And_Its_Global()
    {
        var dto = V2CardDefDto.From(DiceKingdomConfig.Distraction);
        Assert.True(dto.IsAction);
        Assert.Equal("Shell", dto.DieEnergyType);
        Assert.Equal(1, dto.Global!.Cost);
        Assert.Equal("Shell", dto.Global.EnergyType);
        Assert.StartsWith("Pay 1 Shell.", dto.Global.Text);
        Assert.DoesNotContain("Global", dto.ActionText);
    }
}
