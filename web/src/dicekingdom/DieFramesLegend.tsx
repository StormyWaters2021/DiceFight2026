// The first-time legend for die status cues (Claude Design's "face frame"
// handoff, screen 2d, 2026-10-03): shown once, the first time any cue
// appears on the board, and reachable again from Help. The example dice
// are real DieCubes drawing real cues, so the legend can't drift from the
// board.
import { DieCube } from "./DieCube";
import type { CubeFace } from "./dieFaces";
import { TardigradeIcon } from "./icons";
import { CUE_COLORS, type TileCues } from "./statusCues";

const SEEN_KEY = "dk-die-frames-legend-seen";

export function legendSeen(): boolean {
  try {
    return localStorage.getItem(SEEN_KEY) === "1";
  } catch {
    return false;
  }
}

function markSeen() {
  try {
    localStorage.setItem(SEEN_KEY, "1");
  } catch {
    // Private window or blocked storage: it'll show again next time.
  }
}

const FACE: CubeFace = { kind: "character", level: 1, fieldingCost: 0, attack: 1, defense: 2, avatar: TardigradeIcon };
const NONE: TileCues = {
  frame: null, tab: null, tabEdge: "bottom", blanked: false, granted: null, deadly: false,
  intimidated: false, atk: null, def: null, word: null, any: true,
};
const amber = CUE_COLORS.combat;

const ROWS: { title: string; body: string; examples: TileCues[] }[] = [
  {
    title: "Amber border = a combat rule",
    body: "Solid: must. Dashed: can't. Double: unblockable. The tab on the lane edge says which.",
    examples: [
      { ...NONE, frame: { width: 2, style: "solid", color: amber }, tab: { glyph: "B!", fill: "solid", slash: false, color: amber } },
      { ...NONE, frame: { width: 2, style: "dashed", color: amber }, tab: { glyph: "B", fill: "outline", slash: true, color: amber } },
      { ...NONE, frame: { width: 3.5, style: "double", color: amber }, tab: { glyph: "»", fill: "solid", slash: false, color: amber } },
    ],
  },
  { title: "Struck-through art = no abilities", body: "Its text and keywords are off for now.", examples: [{ ...NONE, blanked: true }] },
  { title: "Red corner = KO'd at end of turn", body: "Whatever its damage, it's going.", examples: [{ ...NONE, deadly: true }] },
  { title: "Cream tab = a keyword it doesn't print", body: "Granted for this turn, e.g. +OC is Overcrush.", examples: [{ ...NONE, granted: "+OC" }] },
  {
    title: "Number chip = changed stat",
    body: "Cream is up, red is down. Tap the die for the breakdown.",
    examples: [{ ...NONE, def: "up" }, { ...NONE, def: "down" }],
  },
];

export function DieFramesLegend({ variant, onClose }: { variant: "mobile" | "desktop"; onClose: () => void }) {
  const close = () => {
    markSeen();
    onClose();
  };
  const body = (
    <>
      <h2 className="dk-legend-title">Reading a die's frame</h2>
      <p className="dk-legend-sub">Tap any die for what caused it and how long it lasts.</p>
      {ROWS.map((row) => (
        <div key={row.title} className="dk-legend-row">
          <div className="dk-legend-dice">
            {row.examples.map((cues, i) => (
              <DieCube key={i} faces={[FACE]} index={0} size={44} mine cues={cues} />
            ))}
          </div>
          <div className="dk-legend-text">
            <b>{row.title}</b>
            <span>{row.body}</span>
          </div>
        </div>
      ))}
      <button type="button" className="dk-legend-ok" onClick={close}>
        Got it
      </button>
    </>
  );
  return variant === "mobile" ? (
    <div className="dkm-overlay-backdrop" onClick={close}>
      <div className="dkm-sheet dk-legend" onClick={(e) => e.stopPropagation()}>
        <div className="dkm-sheet-handle" />
        {body}
      </div>
    </div>
  ) : (
    <div className="dk-legend-backdrop" onClick={close}>
      <div className="dk-legend dk-legend-panel" role="dialog" aria-label="Reading a die's frame" onClick={(e) => e.stopPropagation()}>
        {body}
      </div>
    </div>
  );
}
