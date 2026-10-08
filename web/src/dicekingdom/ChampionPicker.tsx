import { useState } from "react";
import { CHAMPION_ICONS } from "./icons";
import { championLabel, OpponentPicksOption, OPPONENT_PICKS, type ChampionChoice } from "./lobby";

// Keep the descriptions outside the page layouts. Power text follows
// DiceKingdomConfig.Champions and ChampionDef in the server rules project.
const CHAMPION_DETAILS: Record<string, { ability: string; playstyle: string }> = {
  Wolf: {
    ability: "Once per turn, give one of your creatures +3 attack for the turn.",
    playstyle: "Aggressive: push damage by strengthening a key attacker.",
  },
  Armadillo: {
    ability: "Once per turn, after blocks, prevent all combat damage to one of your creatures in combat this turn.",
    playstyle: "Defensive: protect a valuable creature during combat.",
  },
  GoldenEagle: {
    ability: "Once per turn, field one creature without paying its fielding cost.",
    playstyle: "Efficient: deploy creatures while conserving energy for other actions.",
  },
  GreatHornedOwl: {
    ability: "Once per turn, spin one of your creatures up one level or an opponent's creature down one level.",
    playstyle: "Tactical: manipulate creature levels to create an advantage.",
  },
};

export function ChampionPicker({ champions, value, onPick, allowOpponentPicks = false }: {
  champions: ChampionChoice[];
  value: string | null;
  onPick: (id: string) => void;
  allowOpponentPicks?: boolean;
}) {
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const detail = expandedId === value && value ? CHAMPION_DETAILS[value] : undefined;
  return (
    <div className="champion-picker">
      <div className="champ-pick">
        {allowOpponentPicks && (
          <OpponentPicksOption selected={value === OPPONENT_PICKS} onPick={() => { setExpandedId(null); onPick(OPPONENT_PICKS); }} />
        )}
        {champions.map((c) => {
          const Icon = CHAMPION_ICONS[c.id];
          const expanded = expandedId === c.id && value === c.id;
          return (
            <button key={c.id} type="button"
              className={`champ-opt${value === c.id ? " selected" : ""}`}
              style={{ ["--sel" as string]: `var(--${c.energy.toLowerCase()})`, color: `var(--${c.energy.toLowerCase()})` }}
              aria-expanded={expanded}
              onClick={() => { setExpandedId(expanded ? null : c.id); onPick(c.id); }}>
              <Icon />
              <div className="cname" style={{ color: "var(--text-h)" }}>{championLabel(c.id)}</div>
            </button>
          );
        })}
      </div>
      <div className={`champ-info-outer${detail ? " is-open" : ""}`}>
        <div className="champ-info-inner">
          {detail && (
            <div className="champ-info" role="region" aria-label={`${championLabel(value!)} information`}>
              <strong>{championLabel(value!)}</strong>
              <div className="champ-info-section"><b>Champion ability</b><p>{detail.ability}</p></div>
              <div className="champ-info-section"><b>Playstyle</b><p>{detail.playstyle}</p></div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
