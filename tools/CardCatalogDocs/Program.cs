using System.Net;
using System.Text;
using DiceFight.V2.Data;
using DiceFight.V2.Model;

// Renders the real DiceKingdomConfig (Champions, CharactersByChampion,
// ActionByChampion, Catalog) as one static HTML reference page - every
// stat and every ability's RawText comes straight from the live CardDef
// records, not a hand-transcribed copy, so it can't silently drift from
// what a game actually plays like the way v3/CARD_INSPIRATION.md already
// has (that file is the pre-implementation brainstorm pass, explicitly
// "not decisions" - real stats moved on without it, several picks even
// changed animal). Re-run this after any card/champion edit:
//
//   dotnet run --project tools/CardCatalogDocs [output-path]
//
// output-path defaults to tools/CardCatalogDocs/dice-kingdom-cards.html.

var outPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(FindRepoRoot(), "tools", "CardCatalogDocs", "dice-kingdom-cards.html");

// Walks up from wherever the build output landed (AppContext.BaseDirectory
// is .../tools/CardCatalogDocs/bin/Debug/net10.0/, nowhere useful to
// default INTO) looking for the solution file, so the default output path
// is stable regardless of the working directory `dotnet run` was invoked
// from.
static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DiceFight.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}

var config = DiceKingdomConfig.Config;
var catalog = DiceKingdomConfig.Catalog;

var html = new StringBuilder();
html.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Dice Kingdom - Card Reference</title>");
html.AppendLine("""
<style>
  body { font: 15px/1.5 -apple-system, Segoe UI, sans-serif; max-width: 980px; margin: 2rem auto; padding: 0 1rem; color: #1a1a1a; }
  h1 { margin-bottom: 0.2rem; }
  .generated { color: #666; font-size: 0.9em; margin-bottom: 2rem; }
  h2 { border-bottom: 2px solid #333; padding-bottom: 0.3rem; margin-top: 2.5rem; }
  .passive { font-style: italic; color: #444; margin-top: -0.3rem; }
  table { border-collapse: collapse; width: 100%; margin: 1rem 0 2rem; }
  th, td { border: 1px solid #ccc; padding: 0.5rem 0.6rem; text-align: left; vertical-align: top; }
  th { background: #f0f0f0; }
  tr:nth-child(even) { background: #fafafa; }
  .stats { font-family: ui-monospace, Menlo, monospace; font-size: 0.9em; white-space: nowrap; }
  .keyword { display: inline-block; background: #e8e8f8; border-radius: 4px; padding: 0 0.4em; margin: 1px; font-size: 0.85em; }
  .cost { text-align: center; font-weight: 600; }
</style>
""");
html.AppendLine("</head><body>");
html.AppendLine("<h1>Dice Kingdom &mdash; Card Reference</h1>");
html.AppendLine($"<p class=\"generated\">Generated {DateTime.UtcNow:yyyy-MM-dd} from <code>src/DiceFight.V2/Data/DiceKingdomConfig.cs</code> " +
    "by <code>tools/CardCatalogDocs</code> &mdash; every field below is read straight from the live CardDef/ChampionDef records, " +
    "not hand-copied. Re-run the tool after any card change; don't hand-edit this file.</p>");

foreach (var champion in config.Champions)
{
    html.AppendLine($"<h2>{Enc(champion.Name)} <small>({Enc(champion.EnergySymbolId)})</small></h2>");
    html.AppendLine($"<p class=\"passive\">Passive: {Enc(DescribePassive(champion))}</p>");

    html.AppendLine("<table><thead><tr><th>Name</th><th>Cost</th><th>Die&nbsp;Limit</th><th>Keywords</th><th>Stats (fielding&nbsp;/&nbsp;ATK&nbsp;/&nbsp;DEF per level)</th><th>Ability text</th></tr></thead><tbody>");
    foreach (var cardId in DiceKingdomConfig.CharactersByChampion[champion.Id])
        AppendCardRow(html, catalog[cardId]);
    html.AppendLine("</tbody></table>");
}

html.AppendLine("<h2>Champion Basic Actions <small>(Global abilities, one per Champion)</small></h2>");
html.AppendLine("<table><thead><tr><th>Champion</th><th>Name</th><th>Cost</th><th>Die&nbsp;Limit</th><th>Ability text</th></tr></thead><tbody>");
foreach (var (championId, cardId) in DiceKingdomConfig.ActionByChampion)
{
    var card = catalog[cardId];
    html.AppendLine("<tr>");
    html.AppendLine($"<td>{Enc(config.Champions.First(c => c.Id == championId).Name)}</td>");
    html.AppendLine($"<td>{Enc(card.Name)}</td>");
    html.AppendLine($"<td class=\"cost\">{card.PurchaseCost}</td>");
    html.AppendLine($"<td class=\"cost\">{card.DieLimit}</td>");
    html.AppendLine($"<td>{Enc(card.RawText)}</td>");
    html.AppendLine("</tr>");
}
html.AppendLine("</tbody></table>");

html.AppendLine("</body></html>");

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath, html.ToString());
Console.WriteLine($"Wrote {catalog.Count} cards across {config.Champions.Count} Champions to {outPath}");

void AppendCardRow(StringBuilder sb, CardDef card)
{
    sb.AppendLine("<tr>");
    var displayName = card.Subtitle is { } sub ? $"{card.Name} <i>({Enc(sub)})</i>" : Enc(card.Name);
    sb.AppendLine($"<td>{displayName}</td>");
    sb.AppendLine($"<td class=\"cost\">{card.PurchaseCost}</td>");
    sb.AppendLine($"<td class=\"cost\">{card.DieLimit}</td>");
    sb.AppendLine($"<td>{string.Join(" ", card.Keywords.Select(k => $"<span class=\"keyword\">{Enc(k)}</span>"))}</td>");
    sb.AppendLine($"<td class=\"stats\">{DescribeLevels(card)}</td>");
    sb.AppendLine($"<td>{Enc(card.RawText)}</td>");
    sb.AppendLine("</tr>");
}

static string DescribeLevels(CardDef card)
{
    var levels = card.Die.Faces
        .Where(f => f.Character is not null)
        .Select(f => f.Character!)
        .OrderBy(c => c.Level)
        .ToList();
    if (levels.Count == 0) return "<em>no character face</em>";
    return string.Join("<br>", levels.Select(c => $"L{c.Level}: {c.FieldingCost}&#9889; / {c.Attack}A / {c.Defense}D"));
}

static string DescribePassive(ChampionDef c) => c.PassiveKind switch
{
    ChampionPassiveKind.AttackBuff => $"+{c.Amount} ATK to all your dice",
    ChampionPassiveKind.DefenseBuff => $"+{c.Amount} DEF to all your dice",
    ChampionPassiveKind.FieldingCostDiscount => $"-{c.Amount} Fielding cost for your dice",
    ChampionPassiveKind.PurchaseCostDiscount => $"-{c.Amount} Purchase cost",
    ChampionPassiveKind.Foresight => "Foresight - once per turn during your Main Step, reroll one die in your Reserve Pool",
    _ => c.PassiveKind.ToString(),
};

static string Enc(string s) => WebUtility.HtmlEncode(s);
