namespace DiceFight.V2.Bot;

// One move the computer opponent wants to make next - exactly one engine
// call's worth (the same granularity as one API endpoint), so the web
// client can pace/animate each one and the simulator can apply them in a
// loop. DiceKingdomBot.Decide produces these; BotDriver (simulator, tests)
// and the web client (via GET .../bot-decision, api.ts) carry them out.
public enum BotActionKind
{
    ClearAndDraw,
    Roll,
    Reroll,             // DieIds - the one reroll decision (rule 2.4.3); advances to Main
    FinishRoll,         // no reroll; advances to Main
    Foresight,          // DieId - Great Horned Owl's passive
    Field,              // DieId + EnergyDieIds
    Purchase,           // DieId (an Unpurchased die) + EnergyDieIds
    UseAction,          // DieId - an action die showing an action face
    UseGlobal,          // CardId + AbilityIndex + EnergyDieIds
    Pass,               // priority pass; SkipAttack only matters for the Active player in Main
    DeclareAttackers,   // AttackerLanes
    DeclareBlockers,    // Blocks
    ResolvePendingChoice, // DieIds (candidate ids - may include a player id)
    CleanUp,
}

public sealed record BotDecision(BotActionKind Kind, string Reason)
{
    public string? DieId { get; init; }
    public IReadOnlyList<string> DieIds { get; init; } = [];
    public IReadOnlyList<string> EnergyDieIds { get; init; } = [];
    public string? CardId { get; init; }
    public int AbilityIndex { get; init; }
    public bool SkipAttack { get; init; }
    public IReadOnlyDictionary<string, int> AttackerLanes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<(string AttackerId, string BlockerId)> Blocks { get; init; } = [];
}

// Knobs for a future per-Champion play style (user, 2026-09-28: "a bot
// running Wolf may act more aggressively" - not urgent while testing with
// Champion powers off). Only Default exists today; every policy decision
// that is a matter of taste rather than arithmetic reads one of these.
public sealed record BotPersona(
    // How readily an attacker swings into a board that could kill it:
    // 0 = only when no blocker can kill it, 1 = accepts even trades,
    // 2 = swings into bad trades too.
    double Aggression = 1.0,
    // How many points of incoming damage a chump block (losing the
    // blocker) is worth at full life; scales up as life drops.
    double ChumpThreshold = 6.0)
{
    public static readonly BotPersona Default = new();
}
