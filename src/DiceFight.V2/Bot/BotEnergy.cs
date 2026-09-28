using DiceFight.V2.Model;

namespace DiceFight.V2.Bot;

// Which Reserve Pool dice to offer as payment, in what order - a port of
// web/src/dicekingdom/bot.ts's pickEnergy (which the human auto-pay still
// uses), extended with a "worth keeping" cost per die.
//
// TurnEngine.SpendEnergy spends dice in the order offered and stops once
// the cost is met; only the LAST die can be overspent, and it then spins
// down to a lower face. So this tries every subset + last-die choice that
// exactly reaches the cost and keeps the cheapest by, in order:
//   1. fewest Wild pips spent - a Wild pays for anything, so it's the last
//      to go (direct feedback 2026-09-26);
//   2. least "keep value" spent - a Tardigrade showing a creature face
//      could still be fielded instead, a bare energy face can't;
//   3. the most useful overspend leftover (bot.ts's scoring);
//   4. fewest dice.
public static class BotEnergy
{
    public static int Pips(GameState state, DieInstance die) => state.GetCurrentFace(die)?.SymbolCount ?? 0;

    public static bool IsWild(GameState state, DieInstance die) =>
        state.GetCurrentFace(die)?.Symbols.Any(s => state.Config.EnergySymbols.Any(e => e.IsWild && e.Id == s.SymbolId)) ?? false;

    public static bool Pays(GameState state, DieInstance die, string symbolId) =>
        state.GetCurrentFace(die)?.Symbols.Any(s => s.SymbolId == symbolId) ?? false;

    // What the die is still worth if NOT spent: a Tardigrade on a
    // creature face could be fielded; a bare energy face has no other use.
    private static double KeepValue(GameState state, DieInstance die) =>
        state.GetCurrentFace(die)?.Character is { } c ? 0.5 + 0.5 * (c.Attack + c.Defense) / 2.0 : 0;

    public static IReadOnlyList<string>? Pick(GameState state, IReadOnlyList<DieInstance> pool, int cost, string? requiredSymbolId)
    {
        if (cost <= 0) return [];
        var dice = pool.Where(d => Pips(state, d) > 0).OrderBy(d => Pips(state, d)).ToList();
        bool Matches(DieInstance d) => requiredSymbolId is null || Pays(state, d, requiredSymbolId) || IsWild(state, d);
        if (dice.Count > 14) return Greedy(state, dice, cost, requiredSymbolId);

        (List<DieInstance> Order, double Score)? best = null;
        for (var mask = 1; mask < 1 << dice.Count; mask++)
        {
            var members = dice.Where((_, i) => (mask & (1 << i)) != 0).ToList();
            var sum = members.Sum(d => Pips(state, d));
            if (sum < cost || !members.Any(Matches)) continue;
            foreach (var last in members)
            {
                if (sum - Pips(state, last) >= cost) continue; // engine would stop before reaching `last`
                var overspend = sum - cost;
                var isTardigrade = last.CardId is null;
                var leftoverScore = overspend == 0 ? 4
                    : isTardigrade && Pips(state, last) == 2 && overspend == 1 ? 3
                    : !isTardigrade && Pips(state, last) == 2 && overspend == 1 ? 2
                    : 0;
                var wilds = members.Where(d => IsWild(state, d)).Sum(d => Pips(state, d))
                    - (IsWild(state, last) ? overspend : 0);
                var kept = members.Sum(d => KeepValue(state, d));
                var score = wilds * 100 + kept * 10 - leftoverScore + members.Count * 0.1;
                if (best is null || score < best.Value.Score)
                    best = ([.. members.Where(d => d != last), last], score);
            }
        }
        return best?.Order.Select(d => d.Id).ToList();
    }

    private static IReadOnlyList<string>? Greedy(GameState state, List<DieInstance> dice, int cost, string? requiredSymbolId)
    {
        var rest = dice.ToList();
        var picked = new List<string>();
        var total = 0;
        if (requiredSymbolId is not null)
        {
            var first = rest.FirstOrDefault(d => Pays(state, d, requiredSymbolId)) ?? rest.FirstOrDefault(d => IsWild(state, d));
            if (first is null) return null;
            picked.Add(first.Id);
            total += Pips(state, first);
            rest.Remove(first);
        }
        foreach (var d in rest.OrderBy(d => IsWild(state, d)))
        {
            if (total >= cost) break;
            picked.Add(d.Id);
            total += Pips(state, d);
        }
        return total >= cost ? picked : null;
    }
}
