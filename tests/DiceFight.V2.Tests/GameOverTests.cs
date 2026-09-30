using DiceFight.V2.Bot;
using DiceFight.V2.Data;
using DiceFight.V2.Model;

namespace DiceFight.V2.Tests;

// Rule 2.9 - the game ends the moment a player's Life reaches 0 (added
// 2026-09-30; a playtest game used to run on at -5 life).
public class GameOverTests
{
    // Wolf (p1, active) vs Armadillo (p2), Champion passives zeroed.
    private static GameState NewGame()
    {
        Player Build(string id, string champion)
        {
            var p = new Player { Id = id, Name = champion, ChampionId = champion };
            p.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion[champion]);
            p.TeamCardIds.Add(DiceKingdomConfig.ActionByChampion[champion]);
            return p;
        }
        var config = DiceKingdomConfig.Config with
        {
            Champions = DiceKingdomConfig.Config.Champions.Select(c => c with { Amount = 0 }).ToList(),
        };
        var state = GameSetup.NewGame(config, DiceKingdomConfig.Catalog, Build("p1", "Wolf"), Build("p2", "Armadillo"));
        state.MoveToStep(StepIds.Main);
        return state;
    }

    private static DieInstance Place(GameState state, string owner, string cardId, int face)
    {
        var die = state.Dice.First(d => d.OwnerId == owner && d.CardId == cardId && d.Zone == Zone.Unpurchased);
        die.Zone = Zone.FieldZone;
        die.CurrentFaceIndex = face;
        return die;
    }

    private static void Drain(GameState state, AbilityQueue queue) =>
        EffectInterpreter.DrainQueue(state, queue, new RandomDiceRoller(new Random(1)), new Random(1));

    // Lane 1 unblocked for lethal, lane 2 into a blocking Rhinoceros: the
    // game ends on the lethal hit, so Rhinoceros's reflect - a triggered
    // ability that would follow - never happens (2.9.1).
    [Fact]
    public void Lethal_Damage_Ends_The_Game_Before_Later_Abilities_Resolve()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        state.PlayerTwo.Life = 2;
        var lethal = Place(state, "p1", DiceKingdomConfig.HoneyBadger.Id, 1); // 2/2
        var intoRhino = Place(state, "p1", DiceKingdomConfig.HoneyBadger.Id, 1);
        var rhino = Place(state, "p2", DiceKingdomConfig.Rhinoceros.Id, 1);   // 2/7

        TurnEngine.EnterAttackStep(state, queue);
        CombatEngine.DeclareAttackers(state, queue, new Dictionary<string, int> { [lethal.Id] = 0, [intoRhino.Id] = 1 });
        var blocks = new CombatAssignment();
        blocks.AssignBlocker(intoRhino.Id, rhino.Id);
        CombatEngine.DeclareBlockers(state, queue, blocks, [rhino.Id]);
        CombatEngine.AssignCombatDamage(state, queue, blocks, new Dictionary<string, IReadOnlyDictionary<string, int>>());
        Drain(state, queue);

        Assert.True(state.IsGameOver);
        Assert.Equal("p1", state.WinnerId);
        Assert.Equal(20, state.PlayerOne.Life); // no reflect
        Assert.Contains(state.Log, e => e.Text == "Wolf wins!");
    }

    [Fact]
    public void Deck_Out_Burn_Can_End_The_Game()
    {
        var state = NewGame();
        state.MoveToStep(StepIds.StartOfTurn);
        state.IsFirstTurn = false;
        state.PlayerOne.Life = 2;
        foreach (var die in state.DiceIn("p1", Zone.Bag).ToList()) { die.Zone = Zone.FieldZone; die.CurrentFaceIndex = 0; }

        TurnEngine.ClearAndDraw(state, new AbilityQueue(), new Random(1)); // 4 short - 4 damage

        Assert.True(state.IsGameOver);
        Assert.Equal("p2", state.WinnerId);
    }

    [Fact]
    public void Nothing_Plays_On_After_The_Game_Ends()
    {
        var state = NewGame();
        state.PlayerTwo.Life = 0;
        state.CheckGameOver();

        Assert.Null(DiceKingdomBot.Decide(state, "p1"));
        Assert.Throws<InvalidOperationException>(() =>
            new BotDriver(state, new Random(1)).Apply("p1", new BotDecision(BotActionKind.Pass, "")));
    }
}
