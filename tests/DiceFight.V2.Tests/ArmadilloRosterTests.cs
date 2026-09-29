using DiceFight.V2.Data;
using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.V2.Tests;

// 2026-09-29 engine/roster changes: the end-of-Main sweep keeps energy
// faces, Hermit Crab's forced block, and Armadillo's Archnemesis.
public class ArmadilloRosterTests
{
    // Armadillo (p1) vs Wolf (p2), Champion passives zeroed so stats are
    // the printed ones.
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
        var state = GameSetup.NewGame(config, DiceKingdomConfig.Catalog, Build("p1", "Armadillo"), Build("p2", "Wolf"));
        state.MoveToStep(StepIds.Main);
        return state;
    }

    private static DieInstance Place(GameState state, string owner, Func<DieInstance, bool> which, Zone zone, int face)
    {
        var die = state.Dice.First(d => d.OwnerId == owner && which(d) && d.Zone is Zone.Bag or Zone.Unpurchased);
        die.Zone = zone;
        die.ControllerId = owner;
        die.CurrentFaceIndex = face;
        return die;
    }

    private static void Drain(GameState state, AbilityQueue queue) =>
        EffectInterpreter.DrainQueue(state, queue, new RandomDiceRoller(new Random(1)), new Random(1));

    [Fact]
    public void Main_End_Sweeps_Stats_Only_Faces_But_Keeps_Any_Face_With_Energy()
    {
        var state = NewGame();
        var hybrid = Place(state, "p1", d => d.CardId is null, Zone.ReservePool, 0);  // Tardigrade L1: 0/1 + 2 Shell
        var bulwark = Place(state, "p1", d => d.CardId is null, Zone.ReservePool, 4); // Tardigrade Bulwark: 1/3, no energy
        var crab = Place(state, "p1", d => d.CardId == DiceKingdomConfig.HermitCrab.Id, Zone.ReservePool, 0); // stats only

        TurnEngine.EnterAttackStep(state, new AbilityQueue());

        Assert.Equal(Zone.ReservePool, hybrid.Zone);
        Assert.Equal(Zone.UsedPile, bulwark.Zone);
        Assert.Equal(Zone.UsedPile, crab.Zone);
    }

    [Fact]
    public void Hermit_Crab_Forces_A_Block_But_Only_When_There_Is_Something_To_Block()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var target = Place(state, "p2", d => d.CardId == DiceKingdomConfig.HoneyBadger.Id, Zone.FieldZone, 1);
        var crab = Place(state, "p1", d => d.CardId == DiceKingdomConfig.HermitCrab.Id, Zone.ReservePool, 0);

        TurnEngine.Field(state, queue, crab.Id, []);
        Drain(state, queue);
        if (state.PendingChoice is { } choice) EffectInterpreter.AnswerPendingChoice(state, [target.Id]);
        Assert.Contains(CombatFlagKind.MustBlock, target.CombatFlags);

        TurnEngine.EnterAttackStep(state, queue);
        CombatEngine.DeclareAttackers(state, queue, new Dictionary<string, int> { [crab.Id] = 0 });
        Assert.Throws<InvalidOperationException>(() =>
            CombatEngine.DeclareBlockers(state, queue, new CombatAssignment(), []));

        var assignment = new CombatAssignment();
        assignment.AssignBlocker(crab.Id, target.Id);
        CombatEngine.DeclareBlockers(state, queue, assignment, [target.Id]);
    }

    [Fact]
    public void A_Forced_Blocker_With_No_Attackers_Doesnt_Block_The_Empty_Declaration()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var target = Place(state, "p2", d => d.CardId == DiceKingdomConfig.HoneyBadger.Id, Zone.FieldZone, 1);
        target.CombatFlags.Add(CombatFlagKind.MustBlock);

        TurnEngine.EnterAttackStep(state, queue);
        CombatEngine.DeclareAttackers(state, queue, new Dictionary<string, int>());
        CombatEngine.DeclareBlockers(state, queue, new CombatAssignment(), []); // no throw
    }

    // "Deal damage to each other" is simultaneous: Honey Badger (2/2) is
    // KO'd by Hummingbird's 2, and still hits back for the 2 ATK it had.
    [Fact]
    public void Archnemesis_Fights_Simultaneously()
    {
        var state = NewGame();
        var queue = new AbilityQueue();
        var mine = Place(state, "p1", d => d.CardId == DiceKingdomConfig.Hummingbird.Id, Zone.FieldZone, 1);  // L2: 2/3
        var theirs = Place(state, "p2", d => d.CardId == DiceKingdomConfig.HoneyBadger.Id, Zone.FieldZone, 1); // L2: 2/2
        var action = Place(state, "p1", d => d.CardId == DiceKingdomConfig.Archnemesis.Id, Zone.ReservePool, 0);

        TurnEngine.UseAction(state, queue, action.Id);
        Drain(state, queue);

        Assert.Null(state.PendingChoice); // one candidate each side - no choice needed
        Assert.Equal(Zone.PrepArea, theirs.Zone); // KO'd
        Assert.Equal(Zone.FieldZone, mine.Zone);
        Assert.Equal(2, mine.Damage); // ...but it still hit back
    }
}
