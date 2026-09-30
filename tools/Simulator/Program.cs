using DiceFight.V2;
using DiceFight.V2.Bot;
using DiceFight.V2.Data;
using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

// Win-rate round-robin for the Dice Kingdom Champions: every pairing,
// GamesPerMatchup games each, seats alternated to cancel first-player
// advantage. Both seats are played by the SAME bot the web client's
// "vs computer" opponent uses (DiceFight.V2/Bot/DiceKingdomBot.cs), one
// decision at a time through BotDriver - the same step/priority machinery
// a browser game walks through - so the numbers describe the opponent
// people actually play, and isolate matchup/card-design skew from bot
// skill. Teams are built the way V2GamesController builds them: the
// Champion's 8 Characters plus its Basic Action.
//
// Run (from this folder): dotnet run -c Release
// Environment switches:
//   SIM_POWERS=on    also run a pass WITH Champion passives (default:
//                    powers-off only - every passive's Amount zeroed and
//                    Foresight removed)
//   SIM_GAMES=N      games per matchup (default 200)
//   SIM_DUMP=N       print the full match log of game N of each matchup
//   SIM_SWAP=Champ:oldId=newId;...  what-if roster swap, e.g.
//                    Armadillo:DK-SHELL-02=DK-SHELL-06 (Musk Ox -> Queen Termite)
//   SIM_PERSONAS=off play every seat with the Default persona instead of
//                    its Champion's (BotPersona.ForChampion)
//   SIM_NO_GLOBALS=id,id  strip the Global abilities off these card ids
//   SIM_BULWARK_DEF=N  what-if: the Tardigrade's Bulwark face (L3, no
//                    energy - 1/3 live) gets N DEF instead
//                    (e.g. DK-ACT-03 = Resurrection) - a what-if switch
//
// Known limitation: two walled-off boards can stall (blocked damage
// clears every Clean Up); games that hit MaxTurns are decided on a life
// tiebreak and counted separately.

var championIds = new[] { "Wolf", "Armadillo", "GreatHornedOwl", "GoldenEagle" };
var displayName = new Dictionary<string, string> { ["Wolf"] = "Wolf", ["Armadillo"] = "Armadillo", ["GreatHornedOwl"] = "Owl", ["GoldenEagle"] = "Eagle" };

var gamesPerMatchup = int.TryParse(Environment.GetEnvironmentVariable("SIM_GAMES"), out var n) ? n : 200;
var dumpGame = int.TryParse(Environment.GetEnvironmentVariable("SIM_DUMP"), out var dg) ? dg : -1;
const int MaxTurns = 50; // safety valve - two walled-off boards can stall

var matchups = new List<(string A, string B)>();
for (var i = 0; i < championIds.Length; i++)
    for (var j = i + 1; j < championIds.Length; j++)
        matchups.Add((championIds[i], championIds[j]));

if (Environment.GetEnvironmentVariable("SIM_POWERS") == "on")
{
    Console.WriteLine("############ WITH champion powers ############");
    RunAllMatchups(championPowersEnabled: true);
    Console.WriteLine();
}
Console.WriteLine("############ WITHOUT champion powers (Amounts zeroed, Foresight removed) ############");
RunAllMatchups(championPowersEnabled: false);

void RunAllMatchups(bool championPowersEnabled)
{
    Stats.Reset();
    foreach (var (champA, champB) in matchups)
    {
        int winsA = 0, winsB = 0, timeouts = 0, errors = 0;
        var turnCounts = new List<int>();

        for (var g = 0; g < gamesPerMatchup; g++)
        {
            var aGoesFirst = g % 2 == 0;
            var seatOneChamp = aGoesFirst ? champA : champB;
            var seatTwoChamp = aGoesFirst ? champB : champA;

            // StableHash, not string.GetHashCode() - .NET randomizes the
            // latter per process, which made reruns non-reproducible.
            var rng = new Random(unchecked(StableHash(champA) * 397 ^ StableHash(champB) * 31 ^ g ^ 0x5EED));
            try
            {
                Stats.BeginGame(seatOneChamp, seatTwoChamp);
                var (winner, turns) = PlayOneGame(seatOneChamp, seatTwoChamp, rng, championPowersEnabled, dump: g == dumpGame);
                Stats.EndGame(winner, seatOneChamp, seatTwoChamp, turns);
                turnCounts.Add(turns);
                if (winner == champA) winsA++;
                else if (winner == champB) winsB++;
                else timeouts++;
            }
            catch (Exception ex)
            {
                errors++;
                if (errors <= 3) Console.Error.WriteLine($"[{champA} vs {champB} game {g}] ERROR: {ex.Message}");
            }
        }

        var decided = Math.Max(1, winsA + winsB);
        Console.WriteLine($"=== {displayName[champA]} vs {displayName[champB]} ({gamesPerMatchup} games) ===");
        Console.WriteLine($"  {displayName[champA]}: {winsA} wins ({100.0 * winsA / decided:F1}%)   {displayName[champB]}: {winsB} wins ({100.0 * winsB / decided:F1}%)");
        Console.WriteLine($"  exact-tie timeouts: {timeouts}, errors (excluded): {errors}, avg game length: {(turnCounts.Count > 0 ? turnCounts.Average() : 0):F1} turns");
    }
    Console.WriteLine();
    Stats.Report(displayName);
}

// ---------------------------------------------------------------------

static (string WinnerChampionIdOrSentinel, int Turns) PlayOneGame(
    string championOne, string championTwo, Random rng, bool championPowersEnabled, bool dump)
{
    var config = championPowersEnabled ? DiceKingdomConfig.Config : StripChampionPowers(DiceKingdomConfig.Config);
    if (int.TryParse(Environment.GetEnvironmentVariable("SIM_BULWARK_DEF"), out var bulwarkDef))
        config = WithBulwarkDefense(config, bulwarkDef);
    var state = GameSetup.NewGame(config, Catalog.Cards, BuildPlayer("p1", championOne), BuildPlayer("p2", championTwo));
    var driver = new BotDriver(state, rng);
    Priority.Sync(state);

    // Dice the engine rejected this turn (a rule the bot doesn't model) -
    // the web client keeps the same list (botSkipIdsRef).
    var skip = new HashSet<string>();
    var turn = 1;
    var lastActive = state.ActivePlayerId;

    for (var guard = 0; guard < 20_000; guard++)
    {
        if (state.PlayerOne.Life <= 0 || state.PlayerTwo.Life <= 0) break;
        if (state.ActivePlayerId != lastActive)
        {
            lastActive = state.ActivePlayerId;
            skip.Clear();
            if (++turn > MaxTurns) break;
        }

        var owner = driver.DecisionOwner();
        var decision = DiceKingdomBot.Decide(state, owner, skip,
                Environment.GetEnvironmentVariable("SIM_PERSONAS") == "off" ? BotPersona.Default : null)
            ?? throw new InvalidOperationException($"Bot had no decision for {owner} at step {state.CurrentStepId}.");
        var lifeBefore = (state.PlayerOne.Life, state.PlayerTwo.Life);
        if (state.PendingChoice?.Intent == ChoiceIntent.NameCard && decision.DieIds.Count > 0)
            Stats.Named(state, owner, decision.DieIds[0]);
        try
        {
            driver.Apply(owner, decision);
        }
        catch (InvalidOperationException ex) when (decision.DieId is not null)
        {
            if (dump) Console.WriteLine($"  [rejected] {decision.Kind} {decision.DieId}: {ex.Message}");
            skip.Add(decision.DieId);
            continue;
        }
        Stats.Record(state, owner, decision, lifeBefore);
    }

    if (dump) DumpLog(state, championOne, championTwo);

    var p1Dead = state.PlayerOne.Life <= 0;
    var p2Dead = state.PlayerTwo.Life <= 0;
    if (p1Dead && p2Dead) return ("__draw__", turn);
    if (p1Dead) return (championTwo, turn);
    if (p2Dead) return (championOne, turn);
    // Turn cap - higher life wins; an exact tie is a real timeout.
    Stats.CapHit();
    if (state.PlayerOne.Life == state.PlayerTwo.Life) return ("__timeout__", turn);
    return (state.PlayerOne.Life > state.PlayerTwo.Life ? championOne : championTwo, turn);
}

static Player BuildPlayer(string id, string championId)
{
    var player = new Player { Id = id, Name = championId, ChampionId = championId };
    var swaps = (Environment.GetEnvironmentVariable("SIM_SWAP") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Split(':')).Where(s => s[0] == championId)
        .Select(s => s[1].Split('=')).ToDictionary(s => s[0], s => s[1]);
    player.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion[championId].Select(id => swaps.GetValueOrDefault(id, id)));
    player.TeamCardIds.Add(DiceKingdomConfig.ActionByChampion[championId]);
    return player;
}

// Neutralizes every Champion's passive without touching team shape
// (TardigradePool and EnergySymbolId stay, so own-energy Characters are
// bought and fielded exactly as before). The stat/cost buffs key off
// Amount; Foresight isn't Amount-gated, so it's swapped for a zero buff.
static GameConfig StripChampionPowers(GameConfig config) =>
    config with
    {
        Champions = config.Champions.Select(c => c with
        {
            Amount = 0,
            PassiveKind = c.PassiveKind == ChampionPassiveKind.Foresight ? ChampionPassiveKind.AttackBuff : c.PassiveKind,
        }).ToList(),
    };

// The Bulwark is the Tardigrade die's stats-only level-3 face (the one
// face with a creature and no energy); rebuild each Champion's Tardigrade
// pool with its DEF replaced.
static GameConfig WithBulwarkDefense(GameConfig config, int defense) =>
    config with
    {
        Champions = config.Champions.Select(c => c with
        {
            TardigradePool = c.TardigradePool.Select(entry => entry with
            {
                Die = entry.Die with
                {
                    Faces = entry.Die.Faces.Select(f => f.Character is { Level: 3 } ch && f.SymbolCount == 0
                        ? f with { Character = ch with { Defense = defense } }
                        : f).ToList(),
                },
            }).ToList(),
        }).ToList(),
    };

static int StableHash(string s)
{
    unchecked
    {
        var h = (int)2166136261;
        foreach (var ch in s) h = (h ^ ch) * 16777619;
        return h;
    }
}

static void DumpLog(GameState state, string one, string two)
{
    Console.WriteLine($"--- match log: p1={one} p2={two} ---");
    foreach (var entry in state.Log)
        Console.WriteLine($"[{entry.PlayerId ?? "-"}] {entry.Text}");
    Console.WriteLine($"Final life: p1={state.PlayerOne.Life} p2={state.PlayerTwo.Life}");
}

static class Catalog
{
    public static IReadOnlyDictionary<string, CardDef> Cards => Built.Value;
    private static readonly Lazy<IReadOnlyDictionary<string, CardDef>> Built = new(() =>
    {
        var strip = (Environment.GetEnvironmentVariable("SIM_NO_GLOBALS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var cards = DiceKingdomConfig.Catalog.ToDictionary(kv => kv.Key, kv => strip.Contains(kv.Key)
            ? kv.Value with { Abilities = kv.Value.Abilities.Where(a => a.Trigger != TriggerKind.Global).ToList() }
            : kv.Value);
        foreach (var proto in Prototypes.All) cards[proto.Id] = proto;
        return cards;
    });
}

// Candidate cards under evaluation - simulator-only, NOT in the live
// catalog or any real roster; reach them with SIM_SWAP. Promote one into
// DiceKingdomConfig only once the user has seen the numbers. (The lockout
// Pangolin started here and went live 2026-09-29.) Build a variant from a
// live card, e.g. `DiceKingdomConfig.HermitCrab with { Id = "PROTO-...", ... }`.
static class Prototypes
{
    public static readonly CardDef[] All = [];
}

static class Stats
{
    static string[] _champ = new string[2]; // p1, p2 champion ids this game
    static readonly HashSet<(string P, string Card)> _fieldedThisGame = [];
    static readonly Dictionary<(string Champ, string Card), int> Buys = [];
    static readonly Dictionary<(string Champ, string Card), int> Fields = [];
    static readonly Dictionary<(string Champ, string Card), int> GamesFielded = [];
    static readonly Dictionary<(string Champ, string Card), int> WinsWhenFielded = [];
    static readonly Dictionary<(string Champ, string Src), int> DamageDealt = [];
    static readonly Dictionary<(string Champ, string What), int> Uses = [];
    static readonly Dictionary<string, int> Games = [], Wins = [], Rerolls = [];
    static int _seatOneWins, _decided, _capHits, _deckOutDeaths;

    static string C(string pid) => _champ[pid == "p1" ? 0 : 1];
    static void Inc<K>(Dictionary<K, int> d, K k, int by = 1) where K : notnull => d[k] = d.GetValueOrDefault(k) + by;

    public static void Reset()
    {
        Buys.Clear(); Fields.Clear(); GamesFielded.Clear(); WinsWhenFielded.Clear(); DamageDealt.Clear();
        Uses.Clear(); Games.Clear(); Wins.Clear(); Rerolls.Clear(); NamedCards.Clear(); CapWins.Clear();
        _seatOneWins = _decided = _capHits = _deckOutDeaths = 0;
    }

    public static void BeginGame(string one, string two) { _champ = [one, two]; _fieldedThisGame.Clear(); _capThisGame = false; }
    static bool _capThisGame;
    static readonly Dictionary<string, int> CapWins = [];
    public static void CapHit() { _capHits++; _capThisGame = true; }
    static readonly Dictionary<(string Champ, string Card), int> NamedCards = [];
    public static void Named(GameState s, string pid, string dieId) =>
        Inc(NamedCards, (C(pid), s.CardCatalog[s.Dice.First(d => d.Id == dieId).CardId!].Name));

    public static void Record(GameState s, string pid, BotDecision d, (int P1, int P2) lifeBefore)
    {
        string Name(string? dieId) => s.Dice.First(x => x.Id == dieId) is { CardId: { } c } ? s.CardCatalog[c].Name : "Tardigrade";
        switch (d.Kind)
        {
            case BotActionKind.Purchase: Inc(Buys, (C(pid), Name(d.DieId))); break;
            case BotActionKind.Field:
                Inc(Fields, (C(pid), Name(d.DieId)));
                _fieldedThisGame.Add((pid, Name(d.DieId)));
                break;
            case BotActionKind.UseAction: Inc(Uses, (C(pid), $"action: {Name(d.DieId)}")); break;
            case BotActionKind.UseGlobal: Inc(Uses, (C(pid), $"global: {s.CardCatalog[d.CardId!].Name}")); break;
            case BotActionKind.Foresight: Inc(Uses, (C(pid), "Foresight")); break;
            case BotActionKind.Reroll: Inc(Rerolls, C(pid), d.DieIds.Count); break;
        }

        var source = d.Kind switch
        {
            BotActionKind.Pass or BotActionKind.DeclareAttackers or BotActionKind.DeclareBlockers => "combat",
            BotActionKind.CleanUp => "end of turn (Basilisk)",
            BotActionKind.ClearAndDraw => "opponent's deck-out",
            BotActionKind.UseAction or BotActionKind.UseGlobal => "actions/globals",
            _ => "main (On Field/Awaken/On Attack)",
        };
        if (d.Kind == BotActionKind.ClearAndDraw && s.GetPlayer(pid).Life <= 0)
        {
            _deckOutDeaths++;
            if (Environment.GetEnvironmentVariable("SIM_DEBUG_DECKOUT") == "1" && _deckOutDeaths <= 2)
            {
                Console.WriteLine($"--- deck-out death #{_deckOutDeaths} ({C(pid)}) - last 60 log lines ---");
                foreach (var e in s.Log.TakeLast(60)) Console.WriteLine($"  [{e.PlayerId}] {e.Text}");
            }
        }
        var toP2 = lifeBefore.P2 - s.PlayerTwo.Life;
        var toP1 = lifeBefore.P1 - s.PlayerOne.Life;
        if (toP2 > 0) Inc(DamageDealt, (C("p1"), source), toP2);
        if (toP1 > 0) Inc(DamageDealt, (C("p2"), source), toP1);
    }

    public static void EndGame(string winner, string one, string two, int turns)
    {
        Inc(Games, one); Inc(Games, two);
        if (winner is not ("__timeout__" or "__draw__"))
        {
            _decided++;
            if (winner == one) _seatOneWins++;
            Inc(Wins, winner);
            if (_capThisGame) Inc(CapWins, winner);
        }
        foreach (var (pid, card) in _fieldedThisGame)
        {
            var c = C(pid);
            Inc(GamesFielded, (c, card));
            if (winner == c) Inc(WinsWhenFielded, (c, card));
        }
    }

    public static void Report(Dictionary<string, string> names)
    {
        Console.WriteLine($"=== STATS === seat one (goes first) wins {_seatOneWins}/{_decided} decided ({100.0 * _seatOneWins / Math.Max(1, _decided):F1}%); games that hit the turn cap: {_capHits}; lost to deck-out burn: {_deckOutDeaths}");
        foreach (var champ in Games.Keys)
        {
            var games = Games[champ];
            Console.WriteLine($"--- {names[champ]} ({games} games, win {100.0 * Wins.GetValueOrDefault(champ) / games:F1}%, {100.0 * CapWins.GetValueOrDefault(champ) / games:F1}% of games won on the turn-cap life tiebreak) - rerolled dice/game: {(double)Rerolls.GetValueOrDefault(champ) / games:F1} ---");
            var totalDmg = Math.Max(1, DamageDealt.Where(k => k.Key.Champ == champ).Sum(k => k.Value));
            foreach (var kv in DamageDealt.Where(k => k.Key.Champ == champ).OrderByDescending(k => k.Value))
                Console.WriteLine($"  dmg via {kv.Key.Src}: {(double)kv.Value / games:F2}/game ({100.0 * kv.Value / totalDmg:F0}%)");
            var named = NamedCards.Where(k => k.Key.Champ == champ).OrderByDescending(k => k.Value).ToList();
            if (named.Count > 0)
                Console.WriteLine($"  lockout named ({(double)named.Sum(k => k.Value) / games:F2}/game): " +
                    string.Join(", ", named.Take(6).Select(k => $"{k.Key.Card} {k.Value}")));
            foreach (var kv in Uses.Where(k => k.Key.Champ == champ).OrderByDescending(k => k.Value))
                Console.WriteLine($"  {kv.Key.What}: {(double)kv.Value / games:F2}/game");
            Console.WriteLine($"  {"card",-18}{"buys/g",8}{"fields/g",10}{"%games fielded",16}{"win% when fielded",20}");
            var cards = Fields.Keys.Concat(Buys.Keys).Where(k => k.Champ == champ).Select(k => k.Card).Distinct()
                .OrderByDescending(c => Fields.GetValueOrDefault((champ, c)));
            foreach (var card in cards)
            {
                var gf = GamesFielded.GetValueOrDefault((champ, card));
                var wf = gf > 0 ? $"{100.0 * WinsWhenFielded.GetValueOrDefault((champ, card)) / gf:F1}%" : "-";
                Console.WriteLine($"  {card,-18}{(double)Buys.GetValueOrDefault((champ, card)) / games,8:F2}{(double)Fields.GetValueOrDefault((champ, card)) / games,10:F2}{100.0 * gf / games,15:F0}%{wf,20}");
            }
        }
    }
}
