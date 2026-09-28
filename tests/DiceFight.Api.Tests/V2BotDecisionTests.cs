using DiceFight.Api;
using Microsoft.AspNetCore.Mvc;

namespace DiceFight.Api.Tests;

// GET .../bot-decision, carried out through the matching endpoint - the
// same loop the web client's "vs computer" seat runs (bot.ts's
// botDecisionToCall), for both seats, so a whole bot-vs-bot game walks
// the real controller: seat tokens, priority, and the pending-choice
// queue stashed on the session.
public class V2BotDecisionTests
{
    [Fact]
    public void Bot_Decisions_Carried_Out_Through_The_Controller_Play_A_Real_Game()
    {
        var store = new V2GameStore();
        var created = V2SeatedController.CreatedDto(V2SeatedController.Anonymous(store).Create(new CreateV2GameRequest("Wolf", "GoldenEagle")));
        var session = store.GetSession(created.Game.GameId);
        var id = session.Id;
        var state = session.State;
        var turnsStarted = 0;

        for (var moves = 0; moves < 3000 && state.PlayerOne.Life > 0 && state.PlayerTwo.Life > 0; moves++)
        {
            // Exactly one seat has a decision at any moment.
            var decisions = new[] { "teamA", "teamB" }
                .Select(seat => (Seat: seat, Result: V2SeatedController.For(store, session, seat, "GET").BotDecision(id, null)))
                .Where(x => x.Result.Result is OkObjectResult)
                .ToList();
            Assert.Single(decisions);
            var (seatId, result) = decisions[0];
            var d = (V2BotDecisionDto)((OkObjectResult)result.Result!).Value!;
            var c = V2SeatedController.For(store, session, seatId);

            if (d.Kind == "clearAndDraw") turnsStarted++;
            _ = d.Kind switch
            {
                "clearAndDraw" => c.ClearAndDraw(id),
                "roll" => c.Roll(id),
                "reroll" => c.Reroll(id, new V2RerollRequest(d.DieIds)),
                "finishRoll" => c.FinishRoll(id),
                "foresight" => c.Foresight(id, new V2UseActionRequest(d.DieId!)),
                "field" => c.Field(id, new V2FieldRequest(d.DieId!, d.EnergyDieIds)),
                "purchase" => c.Purchase(id, new V2PurchaseRequest(d.DieId!, d.EnergyDieIds)),
                "useAction" => c.UseAction(id, new V2UseActionRequest(d.DieId!)),
                "useGlobal" => c.UseGlobal(id, new V2UseGlobalRequest(d.CardId!, d.AbilityIndex, d.EnergyDieIds)),
                "pass" => d.SkipAttack ? c.SkipAttackStep(id) : c.Pass(id),
                "declareAttackers" => c.DeclareAttackers(id, new V2DeclareAttackersRequest(d.Attackers)),
                "declareBlockers" => c.DeclareBlockers(id, new V2DeclareBlockersRequest(d.Assignments)),
                "resolvePendingChoice" => c.ResolvePendingChoice(id, new V2ResolvePendingChoiceRequest(d.DieIds)),
                "cleanUp" => c.CleanUp(id),
                _ => throw new InvalidOperationException($"Unknown bot decision kind '{d.Kind}'."),
            };
        }

        Assert.True(turnsStarted >= 6, $"only {turnsStarted} turns started");
        Assert.True(state.PlayerOne.Life < 20 || state.PlayerTwo.Life < 20, "nobody took any damage");
    }
}
