using System.Net;
using System.Text;
using DiceFight.V2.Data;
using DiceFight.V2.Model;

// Renders the real DiceKingdomConfig (Champions, CharactersByChampion,
// ActionByChampion, Catalog, each Champion's Tardigrade pool) as one
// static HTML reference page - every die face (energy faces too, since
// 2026-09-30), every
// stat and every ability's RawText comes straight from the live CardDef
// records, not a hand-transcribed copy, so it can't silently drift from
// what a game actually plays like the way v3/CARD_INSPIRATION.md already
// has (that file is the pre-implementation brainstorm pass, explicitly
// "not decisions" - real stats moved on without it, several picks even
// changed animal). Re-run this after any card/champion edit:
//
//   dotnet run --project tools/CardCatalogDocs [output-path]
//
// output-path defaults to tools/CardCatalogDocs/dice-kingdom-cards.html;
// a GitHub-readable .md copy is written beside it.

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
  h3 { margin: 1.4rem 0 0.2rem; font-size: 1.05em; }
  .note { color: #555; font-size: 0.9em; margin: 0.2rem 0 0; }
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

    // The starting dice: not cards, so they only live on the ChampionDef.
    foreach (var pool in champion.TardigradePool)
    {
        html.AppendLine($"<h3>Tardigrade die &times;{pool.Count} <small>(starts in the Bag; free to field)</small></h3>");
        html.AppendLine("<table><thead><tr><th>Face</th><th>Count</th></tr></thead><tbody>");
        foreach (var (face, count) in GroupFaces(pool.Die))
            html.AppendLine($"<tr><td class=\"stats\">{face}</td><td class=\"cost\">{count}</td></tr>");
        html.AppendLine("</tbody></table>");
    }

    html.AppendLine("<h3>Characters</h3>");
    html.AppendLine("<table><thead><tr><th>Name</th><th>Cost</th><th>Die&nbsp;Limit</th><th>Keywords</th><th>Stats (fielding&nbsp;/&nbsp;ATK&nbsp;/&nbsp;DEF per level)</th><th>Energy faces</th><th>Ability text</th></tr></thead><tbody>");
    foreach (var cardId in DiceKingdomConfig.CharactersByChampion[champion.Id])
        AppendCardRow(html, catalog[cardId]);
    html.AppendLine("</tbody></table>");
}

html.AppendLine("<h2>Champion Basic Actions <small>(Global abilities, one per Champion)</small></h2>");
html.AppendLine("<table><thead><tr><th>Champion</th><th>Name</th><th>Cost</th><th>Die&nbsp;Limit</th><th>Die faces</th><th>Ability text</th></tr></thead><tbody>");
foreach (var (championId, cardId) in DiceKingdomConfig.ActionByChampion)
{
    var card = catalog[cardId];
    html.AppendLine("<tr>");
    html.AppendLine($"<td>{Enc(config.Champions.First(c => c.Id == championId).Name)}</td>");
    html.AppendLine($"<td>{Enc(card.Name)}</td>");
    html.AppendLine($"<td class=\"cost\">{card.PurchaseCost}</td>");
    html.AppendLine($"<td class=\"cost\">{card.DieLimit}</td>");
    html.AppendLine($"<td class=\"stats\">{string.Join("<br>", GroupFaces(card.Die).Select(g => g.Count > 1 ? $"{g.Face} &times;{g.Count}" : g.Face))}</td>");
    html.AppendLine($"<td>{Enc(card.RawText)}</td>");
    html.AppendLine("</tr>");
}
html.AppendLine("</tbody></table>");
html.AppendLine("<p class=\"note\">Either player may buy either Basic Action; each die's energy faces show its Champion's type.</p>");

html.AppendLine("</body></html>");

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath, html.ToString());
Console.WriteLine($"Wrote {catalog.Count} cards across {config.Champions.Count} Champions to {outPath}");

// The same reference as Markdown beside the HTML, so it reads on GitHub
// (which shows .html files as source). Added 2026-09-30.
var mdPath = Path.ChangeExtension(outPath, ".md");
File.WriteAllText(mdPath, BuildMarkdown());
Console.WriteLine($"Wrote {mdPath}");

string BuildMarkdown()
{
    var md = new StringBuilder();
    md.AppendLine("# Dice Kingdom — Card Reference");
    md.AppendLine();
    md.AppendLine($"_Generated {DateTime.UtcNow:yyyy-MM-dd} from `src/DiceFight.V2/Data/DiceKingdomConfig.cs` by `tools/CardCatalogDocs` — " +
        "every field is read straight from the live CardDef/ChampionDef records. Re-run the tool after any card change; don't hand-edit this file._");
    foreach (var champion in config.Champions)
    {
        md.AppendLine();
        md.AppendLine($"## {champion.Name} ({champion.EnergySymbolId})");
        md.AppendLine();
        md.AppendLine($"_Passive: {Md(DescribePassive(champion))}_");
        foreach (var pool in champion.TardigradePool)
        {
            md.AppendLine();
            md.AppendLine($"### Tardigrade die ×{pool.Count} (starts in the Bag; free to field)");
            md.AppendLine();
            md.AppendLine("| Face | Count |");
            md.AppendLine("|---|:-:|");
            foreach (var (face, count) in GroupFaces(pool.Die))
                md.AppendLine($"| {face} | {count} |");
        }
        md.AppendLine();
        md.AppendLine("### Characters");
        md.AppendLine();
        md.AppendLine("| Name | Cost | Die Limit | Keywords | Stats (fielding / ATK / DEF per level) | Energy faces | Ability text |");
        md.AppendLine("|---|:-:|:-:|---|---|---|---|");
        foreach (var cardId in DiceKingdomConfig.CharactersByChampion[champion.Id])
        {
            var card = catalog[cardId];
            var name = card.Subtitle is { } sub ? $"{Md(card.Name)} _({Md(sub)})_" : Md(card.Name);
            var energy = string.Join("<br>", card.Die.Faces.Where(f => f.Kind == FaceKind.EnergyFace).Select(DescribeEnergy));
            md.AppendLine($"| {name} | {card.PurchaseCost} | {card.DieLimit} | {string.Join(", ", card.Keywords.Select(Md))} | {DescribeLevels(card)} | {energy} | {Md(card.RawText)} |");
        }
    }
    md.AppendLine();
    md.AppendLine("## Champion Basic Actions (Global abilities, one per Champion)");
    md.AppendLine();
    md.AppendLine("| Champion | Name | Cost | Die Limit | Die faces | Ability text |");
    md.AppendLine("|---|---|:-:|:-:|---|---|");
    foreach (var (championId, cardId) in DiceKingdomConfig.ActionByChampion)
    {
        var card = catalog[cardId];
        var faces = string.Join("<br>", GroupFaces(card.Die).Select(g => g.Count > 1 ? $"{g.Face} ×{g.Count}" : g.Face));
        md.AppendLine($"| {Md(config.Champions.First(c => c.Id == championId).Name)} | {Md(card.Name)} | {card.PurchaseCost} | {card.DieLimit} | {faces} | {Md(card.RawText)} |");
    }
    md.AppendLine();
    md.AppendLine("Either player may buy either Basic Action; each die's energy faces show its Champion's type.");
    return md.ToString();
}

// Text for a Markdown table cell: pipes escaped, line breaks as <br>.
static string Md(string s) => s.Replace("|", "\\|").Replace("\r\n", "<br>").Replace("\n", "<br>");

void AppendCardRow(StringBuilder sb, CardDef card)
{
    sb.AppendLine("<tr>");
    var displayName = card.Subtitle is { } sub ? $"{card.Name} <i>({Enc(sub)})</i>" : Enc(card.Name);
    sb.AppendLine($"<td>{displayName}</td>");
    sb.AppendLine($"<td class=\"cost\">{card.PurchaseCost}</td>");
    sb.AppendLine($"<td class=\"cost\">{card.DieLimit}</td>");
    sb.AppendLine($"<td>{string.Join(" ", card.Keywords.Select(k => $"<span class=\"keyword\">{Enc(k)}</span>"))}</td>");
    sb.AppendLine($"<td class=\"stats\">{DescribeLevels(card)}</td>");
    sb.AppendLine($"<td class=\"stats\">{string.Join("<br>", card.Die.Faces.Where(f => f.Kind == FaceKind.EnergyFace).Select(DescribeEnergy))}</td>");
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

// One face, as it reads on the die: a creature face's level and stats (plus
// any energy printed on the same face - Dice Kingdom's hybrid Tardigrade
// faces carry both), a bare energy face, or an action face.
static string DescribeFace(Face f)
{
    if (f.Kind == FaceKind.ActionFace) return "Action";
    var energy = f.Symbols.Count > 0 ? DescribeEnergy(f) : null;
    if (f.Character is not { } c) return energy ?? "blank";
    var stats = $"L{c.Level}: {c.FieldingCost}&#9889; / {c.Attack}A / {c.Defense}D";
    return energy is null ? stats : $"{stats} + {energy}";
}

static string DescribeEnergy(Face f) => string.Join(" + ", f.Symbols.Select(s => $"{s.Count} {Enc(s.SymbolId)}"));

// Identical faces collapsed with a count, in the die's own face order.
static List<(string Face, int Count)> GroupFaces(DieDefinition die) =>
    die.Faces.Select(DescribeFace).GroupBy(s => s).Select(g => (g.Key, g.Count())).ToList();

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
