using DiceFight.V2;
using DiceFight.V2.Data;
using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

// Win-rate round-robin for the Dice Kingdom Champions: every pairing,
// GamesPerMatchup games each, seats alternated to cancel first-player
// advantage. Bot policy is IDENTICAL for both sides in every game, so
// the numbers isolate matchup/card-design skew rather than bot skill.
//
// Run (from this folder): dotnet run -c Release
// Environment switches:
//   SIM_POWERS=on        also run a pass WITH Champion passives (default:
//                        powers-off only - Amount zeroed, Foresight unused)
//   SIM_BUY=expensive    purchase the most expensive affordable card first
//                        (default: cheapest first)
//   SIM_ORDER=buyfirst   Purchase before Field in Main (default: Field first)
//   SIM_DEBUG_ERR=1      dump state when an attacker/blocker declaration
//                        is rejected
//
// Known limitations (2026-09-28 analysis, v3/DESIGN_NOTES.md): teams get
// Characters only - no Basic Actions, no Globals; no rerolls; pending
// choices are answered with random candidates. Balance conclusions flip
// with the buy policy, so treat results as bot-dependent until the bot
// improves. Two walled-off boards can stall (blocked damage clears every
// Clean Up); games that hit MaxTurns are decided on a life tiebreak.

var championIds = new[] { "Wolf", "Armadillo", "GreatHornedOwl", "GoldenEagle" };
var displayName = new Dictionary<string, string> { ["Wolf"] = "Wolf", ["Armadillo"] = "Armadillo", ["GreatHornedOwl"] = "Owl", ["GoldenEagle"] = "Eagle" };

const int GamesPerMatchup = 200;
const int MaxTurns = 50; // safety valve; the two guarded champs (Armadillo/Owl) can wall into a genuine board stalemate under this simple bot, broken by life tiebreak

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
Console.WriteLine("############ WITHOUT champion powers (Amount zeroed, Foresight disabled) ############");
RunAllMatchups(championPowersEnabled: false);

void RunAllMatchups(bool championPowersEnabled)
{
    Stats.Reset();
    var overallWins = championIds.ToDictionary(c => c, _ => 0);
    var overallGames = championIds.ToDictionary(c => c, _ => 0);

    foreach (var (champA, champB) in matchups)
    {
        int winsA = 0, winsB = 0, draws = 0, timeouts = 0, errors = 0;
        var turnCounts = new List<int>();

        for (var g = 0; g < GamesPerMatchup; g++)
        {
            // Alternate who sits in Seat One (goes first) to cancel out
            // first-turn advantage across the sample.
            var aGoesFirst = g % 2 == 0;
            var seatOneChamp = aGoesFirst ? champA : champB;
            var seatTwoChamp = aGoesFirst ? champB : champA;

            // StableHash, not string.GetHashCode() - .NET randomizes the
            // latter per process, which made reruns non-reproducible.
            var rng = new Random(unchecked(StableHash(champA) * 397 ^ StableHash(champB) * 31 ^ g ^ 0x5EED));
            try
            {
                Stats.BeginGame(seatOneChamp, seatTwoChamp);
                var (winnerChampId, turns) = PlayOneGame(seatOneChamp, seatTwoChamp, rng, MaxTurns, championPowersEnabled);
                Stats.EndGame(winnerChampId, seatOneChamp, seatTwoChamp, turns);
                turnCounts.Add(turns);
                if (winnerChampId == champA) winsA++;
                else if (winnerChampId == champB) winsB++;
                else if (winnerChampId == "__timeout__") { timeouts++; }
                else draws++;
            }
            catch (Exception ex)
            {
                errors++;
                if (errors <= 3)
                    Console.Error.WriteLine($"[{champA} vs {champB} game {g}] ERROR: {ex.Message}");
            }
        }

        overallWins[champA] += winsA;
        overallWins[champB] += winsB;
        overallGames[champA] += winsA + winsB + draws + timeouts;
        overallGames[champB] += winsA + winsB + draws + timeouts;

        var decided = winsA + winsB;
        var rateA = decided > 0 ? 100.0 * winsA / decided : 0;
        var rateB = decided > 0 ? 100.0 * winsB / decided : 0;
        var avgTurns = turnCounts.Count > 0 ? turnCounts.Average() : 0;

        Console.WriteLine($"=== {displayName[champA]} vs {displayName[champB]} ({GamesPerMatchup} games) ===");
        Console.WriteLine($"  {displayName[champA]}: {winsA} wins ({rateA:F1}% of decided games)");
        Console.WriteLine($"  {displayName[champB]}: {winsB} wins ({rateB:F1}% of decided games)");
        Console.WriteLine($"  draws: {draws}, timeouts (hit {MaxTurns}-turn cap, life tiebreak used): {timeouts}, errors (excluded): {errors}");
        Console.WriteLine($"  avg game length: {avgTurns:F1} turns");
        Console.WriteLine();
    }

    Stats.Report(displayName);
    Console.WriteLine("--- Overall (all matchups combined) ---");
    foreach (var c in championIds)
        Console.WriteLine($"  {displayName[c]}: {overallWins[c]}/{overallGames[c]} decided-or-timeout games won");
}

// ---------------------------------------------------------------------

static (string WinnerChampionIdOrSentinel, int Turns) PlayOneGame(
    string championOne, string championTwo, Random rng, int maxTurns, bool championPowersEnabled, bool dump = false)
{
    var config = championPowersEnabled ? DiceKingdomConfig.Config : StripChampionPowers(DiceKingdomConfig.Config);
    var catalog = DiceKingdomConfig.Catalog;

    var p1 = new Player { Id = "p1", Name = championOne, ChampionId = championOne };
    p1.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion[championOne]);
    var p2 = new Player { Id = "p2", Name = championTwo, ChampionId = championTwo };
    p2.TeamCardIds.AddRange(DiceKingdomConfig.CharactersByChampion[championTwo]);

    var state = GameSetup.NewGame(config, catalog, p1, p2);
    var queue = new AbilityQueue();
    var roller = new RandomRoller(rng);

    var turn = 0;
    while (turn < maxTurns)
    {
        turn++;
        var activeChamp = state.ActivePlayerId == "p1" ? championOne : championTwo;

        TurnEngine.ClearAndDraw(state, queue, rng);
        Drain(state, queue, roller, rng);

        TurnEngine.Roll(state, queue, roller);
        Drain(state, queue, roller, rng);
        TurnEngine.FinishRoll(state, queue);
        Drain(state, queue, roller, rng);

        if (championPowersEnabled)
        {
            TryUseForesight(state, queue, roller, rng);
            Drain(state, queue, roller, rng);
        }

        var lifeBefore = (state.PlayerOne.Life, state.PlayerTwo.Life);
        RunMainPhase(state, queue, roller, rng);
        Stats.Damage(state, lifeBefore, "main (On Field/Awaken)");
        lifeBefore = (state.PlayerOne.Life, state.PlayerTwo.Life);

        TurnEngine.EnterAttackStep(state, queue);
        Drain(state, queue, roller, rng);

        var attackerIds = ChooseAttackers(state);
        try { CombatEngine.DeclareAttackers(state, queue, attackerIds); }
        catch (InvalidOperationException) when (MaybeDiagnose(state, attackerIds, "attacker")) { throw; }
        Drain(state, queue, roller, rng);

        var (assignment, blockerIds) = BuildSensibleBlocks(state);
        try { CombatEngine.DeclareBlockers(state, queue, assignment, blockerIds); }
        catch (InvalidOperationException) when (MaybeDiagnose(state, blockerIds, "blocker")) { throw; }
        Drain(state, queue, roller, rng);

        CombatEngine.AssignCombatDamage(state, queue, assignment, new Dictionary<string, IReadOnlyDictionary<string, int>>());
        Drain(state, queue, roller, rng);
        Stats.Damage(state, lifeBefore, "attack step (combat, On Attack, Rhino)");
        Stats.Board(state);

        if (state.PlayerOne.Life <= 0 || state.PlayerTwo.Life <= 0)
        {
            if (dump) DumpLog(state);
            return (DecideWinner(state, championOne, championTwo), turn);
        }

        lifeBefore = (state.PlayerOne.Life, state.PlayerTwo.Life);
        TurnEngine.CleanUp(state, queue);
        Drain(state, queue, roller, rng);
        Stats.Damage(state, lifeBefore, "clean up (Basilisk)");

        if (dump && turn % 10 == 0)
        {
            var p1Field = state.DiceIn("p1", Zone.FieldZone).Count();
            var p2Field = state.DiceIn("p2", Zone.FieldZone).Count();
            Console.WriteLine($"  turn {turn}: p1({championOne}) life={state.PlayerOne.Life} field={p1Field} | p2({championTwo}) life={state.PlayerTwo.Life} field={p2Field}");
        }

        if (state.PlayerOne.Life <= 0 || state.PlayerTwo.Life <= 0)
        {
            if (dump) DumpLog(state);
            return (DecideWinner(state, championOne, championTwo), turn);
        }
    }

    if (dump) DumpLog(state);

    // Hit the turn cap - break the tie by life total (higher life wins);
    // a real tie is vanishingly unlikely and just falls through to draw.
    if (state.PlayerOne.Life == state.PlayerTwo.Life) return ("__timeout__", turn);
    var leader = state.PlayerOne.Life > state.PlayerTwo.Life ? championOne : championTwo;
    return (leader, turn);
}

static int StableHash(string s)
{
    unchecked
    {
        var h = (int)2166136261;
        foreach (var ch in s) h = (h ^ ch) * 16777619;
        return h;
    }
}

static void DumpLog(GameState state)
{
    foreach (var entry in state.Log)
        Console.WriteLine($"[{entry.PlayerId ?? "-"}] {entry.Text}");
    Console.WriteLine($"Final life: p1={state.PlayerOne.Life} p2={state.PlayerTwo.Life}");
}

// Neutralizes every Champion's passive without touching anything else
// team-shape-related (TardigradePool, EnergySymbolId stay put, so a
// Champion's own-energy Characters are still purchasable/fieldable
// exactly as before - only the stat/cost delta itself goes to 0).
// ChampionRegistry.Register's AttackBuff/DefenseBuff/*Discount cases all
// key off `champion.Amount`, so zeroing it makes every one of those a
// no-op modifier. Foresight isn't Amount-gated (it's an action, not a
// modifier - ChampionDef's own remarks), so disabling it is handled
// separately by just not calling TryUseForesight when powers are off.
static GameConfig StripChampionPowers(GameConfig config) =>
    config with { Champions = config.Champions.Select(c => c with { Amount = 0 }).ToList() };

static string DecideWinner(GameState state, string championOne, string championTwo)
{
    var p1Dead = state.PlayerOne.Life <= 0;
    var p2Dead = state.PlayerTwo.Life <= 0;
    if (p1Dead && p2Dead) return "__draw__";
    return p1Dead ? championTwo : championOne;
}

// Answers any PendingChoice by taking the minimum required number of
// random candidates, then keeps draining. Loops because a Resolve
// closure can itself raise a brand-new PendingChoice before returning.
static void Drain(GameState state, AbilityQueue queue, IDiceRoller roller, Random rng)
{
    EffectInterpreter.DrainQueue(state, queue, roller, rng);
    var guard = 0;
    while (state.PendingChoice is { } pending && guard++ < 500)
    {
        var count = Math.Max(0, Math.Min(pending.MinCount, pending.CandidateIds.Count));
        var picks = pending.CandidateIds.OrderBy(_ => rng.Next()).Take(count).ToList();
        EffectInterpreter.AnswerPendingChoice(state, picks);
        EffectInterpreter.DrainQueue(state, queue, roller, rng);
    }
}

// Greedy Main-step bot: Field the cheapest affordable rolled Character
// die, else Purchase the cheapest affordable card, repeat until stuck.
static void RunMainPhase(GameState state, AbilityQueue queue, IDiceRoller roller, Random rng)
{
    for (var i = 0; i < 60; i++)
    {
        var buyFirst = Environment.GetEnvironmentVariable("SIM_ORDER") == "buyfirst";
        if (buyFirst && TryPurchaseOne(state, queue, state.ActivePlayerId)) { Drain(state, queue, roller, rng); continue; }
        if (TryFieldOne(state, queue, state.ActivePlayerId)) { Drain(state, queue, roller, rng); continue; }
        if (!buyFirst && TryPurchaseOne(state, queue, state.ActivePlayerId)) { Drain(state, queue, roller, rng); continue; }
        break;
    }
}

static bool TryFieldOne(GameState state, AbilityQueue queue, string playerId)
{
    var reserve = state.DiceIn(playerId, Zone.ReservePool).ToList();
    // Prefer the HIGHEST-level rolled face first, cheapest-to-field as
    // the tiebreak only - not cheapest-first outright. A cheapest-first
    // bot systematically fields its worst rolls, which starves any card
    // that cares about board level (Owl's Basilisk needs 2+ level-2+
    // creatures) and is generally worse play anyway (direct feedback,
    // 2026-09-28, after Basilisk's condition almost never fired).
    var candidates = reserve
        .Where(d => state.GetCurrentFace(d)?.Character is not null)
        .Where(d => d.CardId is null || QueryEngine.CanField(state, playerId, d.CardId))
        .OrderByDescending(d => state.GetCurrentFace(d)!.Character!.Level)
        .ThenBy(d => QueryEngine.GetFieldingCost(state, d))
        .ToList();

    foreach (var d in candidates)
    {
        var payers = reserve.Where(x => x.Id != d.Id).Select(x => x.Id).ToList();
        try
        {
            TurnEngine.Field(state, queue, d.Id, payers);
            Stats.Fielded(playerId, d.CardId is null ? "Tardigrade" : state.CardCatalog[d.CardId].Name, state.GetCurrentFace(d)!.Character!.Level);
            return true;
        }
        catch (InvalidOperationException) { /* can't afford / not eligible - try the next candidate */ }
    }
    return false;
}

static bool TryPurchaseOne(GameState state, AbilityQueue queue, string playerId)
{
    var reserve = state.DiceIn(playerId, Zone.ReservePool).ToList();
    var candidates = state.Dice
        .Where(d => d.Zone == Zone.Unpurchased && d.CardId is not null)
        .Select(d => (Die: d, Card: state.CardCatalog[d.CardId!]))
        .Where(x => x.Card.CardType.IsCommunity() || x.Die.OwnerId == playerId)
        .Where(x => QueryEngine.CanPurchase(state, playerId, x.Card.Id))
        .OrderBy(x => (Environment.GetEnvironmentVariable("SIM_BUY") == "expensive" ? -1 : 1) * QueryEngine.GetPurchaseCost(state, x.Card, playerId))
        .ToList();

    foreach (var (die, card) in candidates)
    {
        // Put reserve dice that carry one of the card's required energy
        // symbols first, so SpendEnergy's stop-once-met scan is more
        // likely to have already covered every required type by the time
        // it hits `amountNeeded` - a heuristic, not a real allocator.
        var required = new HashSet<string>(card.EnergySymbolIds);
        var payers = reserve
            .OrderByDescending(x => (state.GetCurrentFace(x)?.Symbols.Any(s => required.Contains(s.SymbolId)) ?? false) ? 1 : 0)
            .Select(x => x.Id)
            .ToList();
        try
        {
            TurnEngine.Purchase(state, queue, die.Id, payers);
            Stats.Purchased(playerId, card.Name);
            return true;
        }
        catch (InvalidOperationException) { /* can't afford / type mismatch - try the next candidate */ }
    }
    return false;
}

// Active-player attack selection. Straight all-out-every-turn attacking
// leaves BOTH boards permanently empty (an unblocked attacker leaves the
// Attack Zone for good - rule 2.7.4.3.1), which never gives Armadillo's
// Defense buff or Owl's Foresight anything to matter against, since
// nobody is ever still there to block or protect. So the bot holds back
// its single best defender (highest Defense) as a home blocker whenever
// it has more than one eligible attacker - still an aggressive policy,
// just not a totally empty-board one.
static List<string> ChooseAttackers(GameState state)
{
    // Character face required defensively, not just as an optimization -
    // some cards (Owl's Cuttlefish) spin an already-fielded die to an
    // energy face mid-combat, which leaves it sitting in the Field Zone
    // but no longer a legal attacker at all (direct feedback from a
    // rejected declaration: CombatEngine's own eligibility check reads
    // the CURRENT face, not "was fielded successfully once").
    var fielded = state.DiceIn(state.ActivePlayerId, Zone.FieldZone)
        .Where(d => state.GetCurrentFace(d)?.Character is not null)
        .Where(d => !d.CombatFlags.Contains(CombatFlagKind.CantAttack) && !d.CombatFlags.Contains(CombatFlagKind.OnlyBlocker))
        .ToList();
    if (fielded.Count <= 1) return fielded.Select(d => d.Id).ToList();

    var holdBack = fielded.OrderByDescending(d => QueryEngine.GetDefense(state, d)).First();
    return fielded.Where(d => d.Id != holdBack.Id).Select(d => d.Id).ToList();
}

// Inactive-player blocking heuristic: for each lane, in order of biggest
// incoming attack first, offer up the best remaining idle Field Zone die
// as a blocker IF doing so is clearly worthwhile - it either eats the hit
// for free (its Defense covers the lane's whole Attack) or it kills the
// attacker(s) outright (its Attack covers their combined Defense). Chip
// damage that isn't worth losing a die over is left unblocked. This is
// what makes a Defense-stat champion (Armadillo) or a utility passive
// (Owl's Foresight) actually matter against a bot that would otherwise
// just race on offense.
static (CombatAssignment Assignment, List<string> BlockerIds) BuildSensibleBlocks(GameState state)
{
    var inactiveId = state.OpponentOf(state.ActivePlayerId);
    var assignment = new CombatAssignment();
    var blockerIds = new List<string>();
    var usedBlockers = new HashSet<string>();

    var lanesByThreat = state.DiceIn(state.ActivePlayerId, Zone.AttackZone)
        .GroupBy(d => d.Lane)
        .OrderByDescending(g => g.Sum(a => QueryEngine.GetAttack(state, a)))
        .ToList();
    // Same defensive Character-face filter as ChooseAttackers - a die
    // spun to an energy face while sitting in the Field Zone is no
    // longer a legal blocker either.
    var availableBlockers = state.DiceIn(inactiveId, Zone.FieldZone)
        .Where(d => state.GetCurrentFace(d)?.Character is not null)
        .ToList();

    foreach (var lane in lanesByThreat)
    {
        var attackers = lane.ToList();
        if (attackers.Count == 0) continue;
        var laneAttack = attackers.Sum(a => QueryEngine.GetAttack(state, a));
        var laneDefenseTotal = attackers.Sum(a => QueryEngine.GetDefense(state, a));

        var pool = availableBlockers
            .Where(b => !usedBlockers.Contains(b.Id) && !b.CombatFlags.Contains(CombatFlagKind.CantBlock))
            .OrderByDescending(b => QueryEngine.GetDefense(state, b))
            .ToList();
        if (pool.Count == 0) continue;

        var single = pool[0];
        var singleWorthwhile = QueryEngine.GetDefense(state, single) >= laneAttack
            || QueryEngine.GetAttack(state, single) >= laneDefenseTotal;

        if (singleWorthwhile)
        {
            usedBlockers.Add(single.Id);
            assignment.AssignBlocker(attackers[0].Id, single.Id);
            blockerIds.Add(single.Id);
            continue;
        }

        // A single blocker isn't enough - try gang-blocking with a second
        // idle defender (rule 2.7.2.2) IF that combination looks
        // worthwhile from CURRENT stats. Deliberately evaluated on
        // attack/defense as they stand BEFORE committing - a card like
        // Silverback (doubles its own Attack once 2+ blockers are
        // actually assigned) only reveals that punishment AFTER the
        // gang-block commits, same trap a real over-eager blocker would
        // walk into, not a special case this heuristic works around.
        if (pool.Count < 2) continue;
        var pair = pool.Take(2).ToList();
        var combinedDef = pair.Sum(b => QueryEngine.GetDefense(state, b));
        var combinedAtk = pair.Sum(b => QueryEngine.GetAttack(state, b));
        var pairWorthwhile = combinedDef >= laneAttack || combinedAtk >= laneDefenseTotal;
        if (!pairWorthwhile) continue;

        foreach (var b in pair)
        {
            usedBlockers.Add(b.Id);
            assignment.AssignBlocker(attackers[0].Id, b.Id);
            blockerIds.Add(b.Id);
        }
    }

    return (assignment, blockerIds);
}

// One-shot diagnostic dump for the "die X is not an eligible
// attacker/blocker" InvalidOperationException class - prints exactly
// what state each offered die id was actually in, plus the last few log
// lines, so the mismatch is visible instead of just "some die, sometime".
static bool MaybeDiagnose(GameState state, IReadOnlyList<string> offeredIds, string role)
{
    if (Environment.GetEnvironmentVariable("SIM_DEBUG_ERR") != "1") return false;
    Console.Error.WriteLine($"--- diagnosing a rejected {role} declaration ---");
    Console.Error.WriteLine($"Active player: {state.ActivePlayerId}, step: {state.CurrentStepId}");
    foreach (var id in offeredIds)
    {
        var die = state.Dice.FirstOrDefault(d => d.Id == id);
        if (die is null) { Console.Error.WriteLine($"  {id}: NOT FOUND in state.Dice"); continue; }
        var face = state.GetCurrentFace(die);
        Console.Error.WriteLine($"  {id}: zone={die.Zone} controller={die.ControllerId} faceIdx={die.CurrentFaceIndex} " +
            $"hasCharacterFace={face?.Character is not null} flags=[{string.Join(",", die.CombatFlags)}]");
    }
    Console.Error.WriteLine("Last 10 log lines:");
    foreach (var entry in state.Log.TakeLast(10))
        Console.Error.WriteLine($"  [{entry.PlayerId ?? "-"}] {entry.Text}");
    return false; // never actually swallow the exception - just observe it
}

// Great Horned Owl's once-per-turn Foresight: reroll one Reserve Pool
// die. The bot fires it at whichever reserve die is currently least
// useful (lowest energy pip count, and never a die already showing a
// Character face - no point rerolling away a Character you might field)
// right after Roll, hoping for something better before Main spends.
static void TryUseForesight(GameState state, AbilityQueue queue, IDiceRoller roller, Random rng)
{
    if (!TurnEngine.HasForesight(state, state.ActivePlayerId)) return;
    if (state.ForesightUsedThisTurn.Contains(state.ActivePlayerId)) return;

    var target = state.DiceIn(state.ActivePlayerId, Zone.ReservePool)
        .Where(d => state.GetCurrentFace(d)?.Character is null)
        .OrderBy(d => state.GetCurrentFace(d)?.SymbolCount ?? 0)
        .FirstOrDefault();
    if (target is null) return;

    try { TurnEngine.UseForesight(state, queue, roller, state.ActivePlayerId, target.Id); }
    catch (InvalidOperationException) { /* not actually eligible - skip */ }
}

sealed class RandomRoller(Random rng) : IDiceRoller
{
    public int Roll(DieDefinition die) => rng.Next(die.Faces.Count);
}

static class Stats
{
    static string[] _champ = new string[2]; // p1, p2 champion ids this game
    static readonly HashSet<(string P, string Card)> _fieldedThisGame = [];
    public static readonly Dictionary<(string Champ, string Card), int> Buys = [];
    public static readonly Dictionary<(string Champ, string Card), int> Fields = [];
    public static readonly Dictionary<(string Champ, string Card), int> GamesFielded = [];
    public static readonly Dictionary<(string Champ, string Card), int> WinsWhenFielded = [];
    public static readonly Dictionary<(string Champ, string Src), int> DamageDealt = [];
    public static readonly Dictionary<string, int> Games = [], Wins = [], TurnsSum = [];
    public static int SeatOneWins, Decided, Timeouts;
    public static readonly Dictionary<string, (long FieldSize, long Turns, long Attackers)> BoardSum = [];

    static string C(string pid) => _champ[pid == "p1" ? 0 : 1];
    static void Inc<K>(Dictionary<K, int> d, K k, int by = 1) where K : notnull => d[k] = d.GetValueOrDefault(k) + by;

    public static void Reset()
    {
        Buys.Clear(); Fields.Clear(); GamesFielded.Clear(); WinsWhenFielded.Clear(); DamageDealt.Clear();
        Games.Clear(); Wins.Clear(); TurnsSum.Clear(); BoardSum.Clear();
        SeatOneWins = Decided = Timeouts = 0;
    }
    public static void BeginGame(string one, string two) { _champ = [one, two]; _fieldedThisGame.Clear(); }
    public static void Purchased(string pid, string card) => Inc(Buys, (C(pid), card));
    public static void Fielded(string pid, string card, int level) { Inc(Fields, (C(pid), card)); _fieldedThisGame.Add((pid, card)); }
    public static void Damage(GameState s, (int P1, int P2) before, string src)
    {
        var d2 = before.P2 - s.PlayerTwo.Life; if (d2 > 0) Inc(DamageDealt, (C("p1"), src), d2);
        var d1 = before.P1 - s.PlayerOne.Life; if (d1 > 0) Inc(DamageDealt, (C("p2"), src), d1);
    }
    public static void Board(GameState s)
    {
        foreach (var pid in new[] { "p1", "p2" })
        {
            var c = C(pid); var b = BoardSum.GetValueOrDefault(c);
            BoardSum[c] = (b.FieldSize + s.DiceIn(pid, Zone.FieldZone).Count() + s.DiceIn(pid, Zone.AttackZone).Count(), b.Turns + 1, b.Attackers + s.DiceIn(pid, Zone.AttackZone).Count());
        }
    }
    public static void EndGame(string winner, string one, string two, int turns)
    {
        Inc(Games, one); Inc(Games, two); Inc(TurnsSum, one, turns); Inc(TurnsSum, two, turns);
        if (winner == "__timeout__" || winner == "__draw__") { Timeouts++; }
        else { Decided++; if (winner == one && one != two) SeatOneWins++; Inc(Wins, winner); }
        foreach (var (pid, card) in _fieldedThisGame)
        {
            var c = C(pid);
            Inc(GamesFielded, (c, card));
            if (winner == c) Inc(WinsWhenFielded, (c, card));
        }
    }
    public static void Report(Dictionary<string, string> names)
    {
        Console.WriteLine($"=== STATS === seat-one (goes first) wins: {SeatOneWins}/{Decided} decided ({100.0 * SeatOneWins / Math.Max(1, Decided):F1}%), turn-cap games: {Timeouts}");
        foreach (var champ in Games.Keys)
        {
            var games = Games[champ];
            Console.WriteLine($"--- {names[champ]} ({games} games, win {100.0 * Wins.GetValueOrDefault(champ) / games:F1}%) ---");
            var b = BoardSum[champ];
            Console.WriteLine($"  avg board (field+attack) after combat: {(double)b.FieldSize / b.Turns:F2}");
            var totalDmg = DamageDealt.Where(k => k.Key.Champ == champ).Sum(k => k.Value);
            foreach (var kv in DamageDealt.Where(k => k.Key.Champ == champ).OrderByDescending(k => k.Value))
                Console.WriteLine($"  dmg via {kv.Key.Src}: {(double)kv.Value / games:F2}/game ({100.0 * kv.Value / totalDmg:F0}%)");
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
