import type { ReactElement, ReactNode } from "react";
import wolfChampionArt from "./assets/wolf-champion.jpg";
import tardigradeSketch from "./assets/tardigrade-sketch.jpg";
import barnSwallowDuck from "./assets/barn-swallow-duck.jpg";

// Hand-drawn icon set for Dice Kingdom - the same SVG concepts already
// designed and reviewed as Claude artifacts this session (the energy-pip
// icon sheet and the 45-animal avatar sheet), ported into the real app.
// Every icon is a single <path>-based glyph using currentColor, so a
// wrapping element's `color` sets the badge's hue - see dicekingdom.css's
// `.pip`/`.avatar` classes for the accent tokens.

type IconProps = { size?: number };

function Svg({ size = 24, children }: IconProps & { children: ReactNode }) {
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" aria-hidden="true">
      {children}
    </svg>
  );
}

// ---- Energy glyphs (Claw / Shell / Wing / Eye / Wild) ----
// currentColor for the main shape, a fixed warm cutout tone for the inner
// detail (seam/pupil) - matches the energy-pip-icons artifact exactly.
const CUTOUT = "#241B12";

export function ClawIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g transform="translate(32,33) rotate(35)" fill="currentColor">
        <path d="M-17,-19 C-14,-13 -14,13 -17,19 C-20,13 -20,-13 -17,-19 Z" />
        <path d="M0,-22 C3,-15 3,15 0,22 C-3,15 -3,-15 0,-22 Z" />
        <path d="M17,-19 C20,-13 20,13 17,19 C14,13 14,-13 17,-19 Z" />
      </g>
    </Svg>
  );
}

export function ShellIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M8,42 C7,24 17,9 32,9 C47,9 57,24 56,42 C53,48 48,40 44,47 C40,40 36,48 32,41 C28,48 24,40 20,47 C16,40 11,48 8,42 Z"
      />
      <path fill="none" stroke={CUTOUT} strokeWidth={3} strokeLinecap="round" d="M32,14 C30,22 30,32 32,39" />
    </Svg>
  );
}

export function WingIcon(props: IconProps) {
  return (
    <Svg {...props}>
      {/* Scaled to 75% about center (2026-09-10) - direct feedback: "the
          circle border for Wing energy" looked "a little off" (only
          Wing, not Claw). The un-scaled path's own corners reached
          ~34 units from center, past the ~32-unit inscribed circle
          every other energy glyph stays within (Claw/Shell/Eye all
          land around 25-26) - just enough for the outline filter's
          dilate to poke a sliver past the badge's circular edge. */}
      <g transform="translate(32,32) scale(0.75) translate(-32,-32)">
        <path
          fill="currentColor"
          d="M8,54 C10,40 16,24 30,14 C38,9 46,7 56,8 C50,12 45,15 41,21 C46,20 51,20 55,23 C48,26 42,28 38,33 C44,33 49,35 52,38 C44,41 37,42 33,47 C29,51 23,53 18,55 C14,56 10,56 8,54 Z"
        />
      </g>
    </Svg>
  );
}

export function EyeIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M4,32 Q32,6 60,32 Q32,58 4,32 Z" />
      <ellipse cx={32} cy={32} rx={4.4} ry={15} fill={CUTOUT} />
      <circle cx={27} cy={21} r={3} fill="currentColor" opacity={0.55} />
    </Svg>
  );
}

// Surge (the Tardigrade die's sixth face) provides Wild energy - any of
// the four types. No printed animal for "any", so this is a plain glyph
// rather than a fifth creature, same role as v1's WILD_ENERGY_ICON "?".
export function WildIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <text x={32} y={44} fontSize={34} fontWeight={700} textAnchor="middle" fill="currentColor">
        ?
      </text>
    </Svg>
  );
}

export const ENERGY_ICONS: Record<string, (p: IconProps) => ReactElement> = {
  Claw: ClawIcon,
  Shell: ShellIcon,
  Wing: WingIcon,
  Eye: EyeIcon,
  Wild: WildIcon,
};

// ---- Small UI glyphs (How to Play / Settings) - plain chrome, not game
// content, so these stay simple rather than earning a full hand-drawn
// treatment like the energy/animal icons above. ----
export function HelpIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <circle cx={32} cy={32} r={28} fill="none" stroke="currentColor" strokeWidth={5} />
      <text x={32} y={44} fontSize={32} fontWeight={700} textAnchor="middle" fill="currentColor">
        ?
      </text>
    </Svg>
  );
}

// A "sliders" glyph, not a literal gear - direct feedback asked for "a
// gear for settings," but a hand-drawn gear tooth shape is fussy at
// this size; this reads as "settings" just as clearly and is far
// simpler to keep crisp small.
export function SettingsIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g stroke="currentColor" strokeWidth={5} strokeLinecap="round">
        <line x1={10} y1={18} x2={54} y2={18} />
        <line x1={10} y1={32} x2={54} y2={32} />
        <line x1={10} y1={46} x2={54} y2={46} />
      </g>
      <circle cx={22} cy={18} r={5.5} fill="currentColor" />
      <circle cx={42} cy={32} r={5.5} fill="currentColor" />
      <circle cx={26} cy={46} r={5.5} fill="currentColor" />
    </Svg>
  );
}

// "Variant A" from the corner-badge mockup (2026-09-08, compared
// alongside a neutral-circle/colored-symbol alternative): the energy
// type's own color as a circle's FILL, its icon in white/cream on top -
// direct feedback picked this one specifically because it "reads faster
// and stays legible even shrunk small," unlike a bare colored icon with
// no backing shape. One shared badge for every place that shows an
// energy type - a die's own rolled amount, a printed cost, a popover
// detail - each just asking for whatever size that spot needs, so the
// look stays the same everywhere instead of drifting per call site.
export function EnergyBadge({ type, size = 16 }: { type: string; size?: number }) {
  const Icon = ENERGY_ICONS[type];
  if (!Icon) return <span style={{ fontSize: size * 0.6 }}>{type}</span>;
  const cssVar = type === "Wild" ? "var(--wild)" : `var(--${type.toLowerCase()})`;
  return (
    <span className="energy-badge" style={{ width: size, height: size, background: cssVar }}>
      <Icon size={Math.round(size * 0.62)} />
    </span>
  );
}

// ---- Champion avatars (no energy tie-in - see v3/DESIGN_NOTES.md on why
// avatars are deliberately orthogonal to energy color) ----

// The first real piece of fan art in the set (2026-09-06, drawn by the
// user's kid) - a raster image rather than a currentColor SVG glyph like
// every other icon here, so it can't pick up an accent tint the way
// those do. Same IconProps signature as the rest (just `size`) so it
// drops into CHAMPION_ICONS/callers without any special-casing. More of
// these are expected to replace the placeholder SVG glyphs over time.
export function WolfIcon({ size = 24 }: IconProps) {
  return (
    <img
      src={wolfChampionArt}
      alt="Wolf"
      width={size}
      height={size}
      style={{ objectFit: "contain", borderRadius: "20%", background: "#fff" }}
    />
  );
}

// A plain currentColor glyph in the same minimal style as the other
// three Champions - kept alongside WolfIcon (the real photo) for
// whatever small/tinted context a flat raster image doesn't suit (the
// photo can't pick up an accent color the way this can, and doesn't
// shrink to a tiny badge as cleanly as a few paths do).
export function WolfGlyphIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M14,14 L26,24 L32,18 L38,24 L50,14 L46,32 L52,40 L40,42 L32,56 L24,42 L12,40 L18,32 Z"
      />
    </Svg>
  );
}

export function ArmadilloIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        d="M10,48 C10,32 20,20 32,20 C44,20 54,32 54,48 Z"
        fill="none"
        stroke="currentColor"
        strokeWidth={4}
      />
      <line x1={10} y1={48} x2={54} y2={48} stroke="currentColor" strokeWidth={4} />
    </Svg>
  );
}

export function GoldenEagleIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M14,26 C22,20 34,20 40,26 C34,26 26,30 20,38 C16,34 14,30 14,26 Z" />
    </Svg>
  );
}

export function GreatHornedOwlIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <polygon points="18,26 24,10 30,26" fill="currentColor" />
      <polygon points="34,26 40,10 46,26" fill="currentColor" />
      <path d="M20,28 C20,20 44,20 44,28 C44,36 20,36 20,28 Z" fill="currentColor" />
    </Svg>
  );
}

export const CHAMPION_ICONS: Record<string, (p: IconProps) => ReactElement> = {
  Wolf: WolfIcon,
  Armadillo: ArmadilloIcon,
  GoldenEagle: GoldenEagleIcon,
  GreatHornedOwl: GreatHornedOwlIcon,
};

// A generic glyph for the basic pool creature - direct feedback
// (2026-09-05): "I don't really know which dice are Tardigrades and
// which one is a Pangolin" - every die's cube face now shows SOME
// identity glyph regardless of which face it's resting on (see
// dieFaces.ts's `avatar` field), and a Tardigrade has no CardId/card
// avatar of its own to use for that. Not meant to be a scientifically
// accurate tardigrade, just a simple, at-a-glance "this is the basic
// creature, not someone's Character" silhouette in the same plain-path
// style as the rest of this file.
// Side profile, not the original front-on blob-plus-four-dots - direct
// feedback (2026-09-08): "I think that's more recognizable" at the tiny
// sizes this actually renders at. A rounded snout at the front, a
// plump tapering body, four stubby legs trailing along the bottom -
// the classic "water bear" silhouette, simplified down to what still
// reads at 15-20px.
export function TardigradeIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M14,30 C14,20 22,14 34,14 C46,14 54,21 54,30 C54,39 46,44 34,44 C20,44 14,40 14,30 Z"
      />
      <circle cx={13} cy={28} r={6} fill="currentColor" />
      <g fill="currentColor">
        <rect x={18} y={42} width={5} height={10} rx={2.5} />
        <rect x={28} y={43} width={5} height={10} rx={2.5} />
        <rect x={38} y={43} width={5} height={10} rx={2.5} />
        <rect x={47} y={41} width={5} height={10} rx={2.5} />
      </g>
    </Svg>
  );
}

// A real sketch, kept alongside the vector TardigradeIcon rather than
// replacing it - direct feedback pattern (2026-09-08): the vector glyph
// is what actually renders at die-cube sizes (15-20px, where a scanned
// pencil drawing would just look like noise), but a bigger context - a
// Tardigrade's own info popover (DieTile's own "click for full
// information", added the same day) - has the room for the real thing,
// same "bigger image" rule the user gave for a Champion portrait.
export function TardigradePhotoIcon({ size = 24 }: IconProps) {
  return (
    <img
      src={tardigradeSketch}
      alt="Tardigrade"
      width={size}
      height={size}
      style={{ objectFit: "contain", borderRadius: "20%", background: "#fff" }}
    />
  );
}

// ---- Character avatars (the 8-card v3 pool) ----

const CREAM = "#FFF8EC";

function HoneyBadgerIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <polygon points="24,17 32,21.5 32,30.5 24,35 16,30.5 16,21.5" />
        <polygon points="40,17 48,21.5 48,30.5 40,35 32,30.5 32,21.5" />
        <polygon points="32,32 40,36.5 40,45.5 32,50 24,45.5 24,36.5" />
      </g>
    </Svg>
  );
}

function WolverineIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <polygon points="12,50 32,14 52,50" fill="currentColor" />
      <polygon points="26,26 32,14 38,26 34,23 32,25 30,23" fill={CREAM} />
    </Svg>
  );
}

function HippopotamusIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path d="M10,26 C10,20 54,20 54,26 C54,40 10,40 10,26 Z" fill="currentColor" />
      <polygon points="16,26 21,26 18,34" fill={CREAM} />
      <polygon points="48,26 43,26 46,34" fill={CREAM} />
    </Svg>
  );
}

function MuskOxIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <circle cx={32} cy={32} r={7} fill="none" stroke="currentColor" strokeWidth={2.4} />
      <g stroke="currentColor" strokeWidth={3} strokeLinecap="round">
        <path d="M32,18 L32,10" />
        <path d="M32,46 L32,54" />
        <path d="M18,32 L10,32" />
        <path d="M46,32 L54,32" />
        <path d="M22,22 L16,16" />
        <path d="M42,22 L48,16" />
        <path d="M22,42 L16,48" />
        <path d="M42,42 L48,48" />
      </g>
    </Svg>
  );
}

function OspreyIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path d="M18,30 C22,24 32,22 40,26 C34,28 26,30 18,30 Z" fill="currentColor" />
      <path d="M18,30 L12,26 L13,33 Z" fill="currentColor" />
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={3.4}
        strokeLinecap="round"
        d="M22,30 C22,38 26,42 22,48 M30,29 C31,37 35,41 32,48"
      />
    </Svg>
  );
}

// A real drawing, not the original nest-glyph - direct feedback
// (2026-09-08): the user's own example of what a house-drawn animal
// picture could replace ("if I upload a duck, you could swap out the
// barn swallow"), then an actual duck drawing arrived a few messages
// later. Same img-based pattern as WolfIcon.
function BarnSwallowIcon({ size = 24 }: IconProps) {
  return (
    <img
      src={barnSwallowDuck}
      alt="Barn Swallow"
      width={size}
      height={size}
      style={{ objectFit: "contain", borderRadius: "20%", background: "#fff" }}
    />
  );
}

function BarnOwlIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path d="M32,48 C14,34 14,18 32,20 C50,18 50,34 32,48 Z" fill="currentColor" />
    </Svg>
  );
}

function HyenaIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <polygon points="14,32 40,26 40,38" fill="currentColor" />
      <g fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
        <path d="M44,26 C48,24 50,20 50,16" />
        <path d="M46,32 C50,31 53,28 54,24" />
        <path d="M44,38 C48,39 51,42 52,46" />
      </g>
    </Svg>
  );
}

// The remaining 24 of the 32-card roster (2026-09-05) - ported from the
// "Forty-five personal marks" avatar-sheet artifact (the same one the
// first 8 above already came from), picking out the ones matching this
// roster's actual names; the sheet's other marks (Rhino, Wolf, Snow
// Leopard, etc.) were earlier brainstorm alternates for animals that
// aren't in the final 32 and are left unported. Box Turtle isn't on
// that sheet at all - drawn fresh below, in the same plain-path style,
// since Snapping Turtle already claimed the sheet's one turtle concept
// (a snapped twig, for its aggressive bite).

function GrizzlyBearIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path d="M12,26 C16,18 28,15 38,20 C30,23 20,26 12,26 Z" fill="currentColor" />
      <path d="M12,26 L5,20 L7,28 Z" fill="currentColor" />
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={4}
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12,42 L19,35 L26,42 L33,35 L40,42 L47,35 L54,42"
      />
    </Svg>
  );
}

function OrcaIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M18,20 C30,16 46,22 48,34 C42,40 30,42 22,36 C26,34 28,28 24,24 C22,26 20,24 18,20 Z"
      />
    </Svg>
  );
}

function PeregrineFalconIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M14,50 C22,34 34,20 50,12 C44,22 38,32 34,40 C30,46 22,50 14,50 Z"
      />
    </Svg>
  );
}

function TigerIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <rect x={12} y={20} width={40} height={24} rx={10} fill="currentColor" />
      <g stroke={CREAM} strokeWidth={3.4} strokeLinecap="round">
        <path d="M20,22 C24,28 24,36 20,42" />
        <path d="M32,20 C36,28 36,36 32,44" />
        <path d="M44,22 C48,28 48,36 44,42" />
      </g>
    </Svg>
  );
}

function StoatIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M14,18 C26,14 38,22 34,34 C31,42 22,44 16,40 C22,40 28,36 28,30 C28,22 20,20 14,22 Z"
      />
      <circle cx={16} cy={40} r={5} fill="currentColor" />
    </Svg>
  );
}

function CapeBuffaloIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M32,26 C26,26 22,20 14,20 C20,26 24,30 24,34 C24,38 20,44 16,48 C24,46 30,38 32,32 C34,38 40,46 48,48 C44,44 40,38 40,34 C40,30 44,26 50,20 C42,20 38,26 32,26 Z"
      />
    </Svg>
  );
}

function PangolinIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M42,14 C24,14 14,26 16,38 C17,46 24,52 34,50 C28,48 22,42 24,34 C26,26 34,22 42,24 C36,20 38,16 42,14 Z"
      />
      <g fill="none" stroke={CREAM} strokeWidth={2}>
        <path d="M22,32 C26,30 30,30 33,32" />
        <path d="M20,38 C25,36 30,36 33,39" />
      </g>
    </Svg>
  );
}

function HermitCrabIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={3}
        d="M20,44 C14,34 18,18 34,16 C48,15 52,28 46,38 C40,47 26,48 20,44 Z"
      />
      <path d="M16,46 L8,42 M16,48 L8,52" stroke="currentColor" strokeWidth={3} strokeLinecap="round" />
    </Svg>
  );
}

function OpossumIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={32} cy={34} rx={18} ry={9} fill="currentColor" />
      <g stroke="currentColor" strokeWidth={3} strokeLinecap="round">
        <path d="M18,27 L14,16" />
        <path d="M26,25 L24,14" />
        <path d="M38,25 L40,14" />
        <path d="M46,27 L50,16" />
      </g>
      <g stroke="currentColor" strokeWidth={2} strokeLinecap="round">
        <path d="M27,32 L31,36 M31,32 L27,36" />
        <path d="M33,32 L37,36 M37,32 L33,36" />
      </g>
    </Svg>
  );
}

function QueenTermiteIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M32,10 C36,24 42,38 46,52 L18,52 C22,38 28,24 32,10 Z" />
      <g stroke={CREAM} strokeWidth={2}>
        <path d="M23,32 L41,32" />
        <path d="M21,42 L43,42" />
      </g>
    </Svg>
  );
}

function SnappingTurtleIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={4.5}
        strokeLinecap="round"
        d="M12,44 L26,30 L22,24 L38,20 L52,10"
      />
    </Svg>
  );
}

// Not on the source avatar sheet - Snapping Turtle already claimed its
// one turtle concept (a snapped twig). A domed, hinge-lined shell
// stands in for Box Turtle's own defining trait instead: a hinged
// plastron that lets it seal shut completely, unlike a snapper's.
function BoxTurtleIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M12,38 C12,24 20,16 32,16 C44,16 52,24 52,38 C52,42 44,44 32,44 C20,44 12,42 12,38 Z"
      />
      <line x1={32} y1={20} x2={32} y2={42} stroke={CREAM} strokeWidth={1.6} />
      <line x1={18} y1={34} x2={46} y2={34} stroke={CREAM} strokeWidth={1.6} />
    </Svg>
  );
}

function HummingbirdIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M32,50 C24,36 24,22 32,12 C40,22 40,36 32,50 Z" />
      <g fill="currentColor">
        <polygon points="32,12 26,4 32,7 38,4" />
        <polygon points="20,18 12,14 18,20 12,24" />
        <polygon points="44,18 52,14 46,20 52,24" />
      </g>
    </Svg>
  );
}

function MountainGoatIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M10,50 L10,38 L22,38 L22,28 L34,28 L34,18 L46,18 L46,10 L54,10 L54,50 Z" />
    </Svg>
  );
}

function MonarchButterflyIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M32,10 C42,14 46,26 40,40 C36,50 28,50 24,40 C18,26 22,14 32,10 Z" />
      <line x1={32} y1={14} x2={32} y2={46} stroke={CREAM} strokeWidth={1.6} />
    </Svg>
  );
}

function HomingPigeonIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={32} cy={32} rx={16} ry={8} fill="none" stroke="currentColor" strokeWidth={4} />
      <rect x={27} y={40} width={10} height={8} rx={1.5} fill="currentColor" />
    </Svg>
  );
}

function GreyhoundIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <line x1={16} y1={12} x2={16} y2={52} stroke="currentColor" strokeWidth={3} />
      <path fill="currentColor" d="M16,16 L44,16 L36,26 L44,36 L16,36 Z" />
      <g fill={CREAM}>
        <rect x={19} y={18} width={6} height={6} />
        <rect x={31} y={18} width={6} height={6} />
        <rect x={25} y={24} width={6} height={6} />
        <rect x={19} y={30} width={6} height={4} />
      </g>
    </Svg>
  );
}

function AlbatrossIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M22,26 C26,22 38,22 42,26 C38,24 26,24 22,26 Z" />
      <g fill="none" stroke="currentColor" strokeWidth={2.6} strokeLinecap="round">
        <path d="M10,40 C18,36 26,44 34,40" />
        <path d="M28,46 C36,42 44,50 52,46" />
      </g>
    </Svg>
  );
}

function AnglerfishIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="none" stroke="currentColor" strokeWidth={2.6} strokeLinecap="round" d="M18,46 C18,30 24,20 30,14" />
      <circle cx={30} cy={12} r={5} fill="currentColor" />
      <g stroke="currentColor" strokeWidth={1.6} strokeLinecap="round">
        <path d="M30,4 L30,1" />
        <path d="M22,8 L19,6" />
        <path d="M38,8 L41,6" />
      </g>
    </Svg>
  );
}

function CowbirdIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <ellipse cx={18} cy={34} rx={4} ry={6} transform="rotate(-15 18 34)" />
        <polygon points="14,20 18,12 22,20" />
        <polygon points="6,24 10,16 14,24" />
        <polygon points="22,24 26,16 30,24" />
      </g>
      <g fill="currentColor">
        <ellipse cx={44} cy={30} rx={6} ry={9} transform="rotate(10 44 30)" />
        <ellipse cx={52} cy={32} rx={6} ry={9} transform="rotate(10 52 32)" />
      </g>
    </Svg>
  );
}

function MagpieIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M32,10 C40,20 42,36 34,54 C30,36 30,20 32,10 Z" />
      <line x1={32} y1={14} x2={32} y2={50} stroke={CREAM} strokeWidth={1.4} />
      <g stroke={CREAM} strokeWidth={1.2}>
        <path d="M32,22 L26,26" />
        <path d="M32,22 L38,26" />
        <path d="M32,32 L25,36" />
        <path d="M32,32 L39,36" />
      </g>
    </Svg>
  );
}

function RavenIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <polygon points="32,16 42,32 32,48 22,32" fill="currentColor" />
      <g stroke="currentColor" strokeWidth={2} strokeLinecap="round">
        <path d="M46,14 L50,10" />
        <path d="M50,14 L46,10" />
      </g>
    </Svg>
  );
}

function ElephantIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={5}
        strokeLinecap="round"
        d="M24,14 C40,14 44,24 38,32 C32,40 40,42 34,48"
      />
      <circle cx={34} cy={54} r={3} fill="currentColor" />
    </Svg>
  );
}

function FoxIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M14,20 C30,18 46,28 46,42 C36,44 24,40 20,30 C18,26 16,22 14,20 Z"
      />
      <path fill={CREAM} d="M46,42 C40,44 34,42 30,38 C36,36 42,38 46,42 Z" />
    </Svg>
  );
}

// ---- Mobile refresh (2026-09) chrome glyphs ----
// A second, unrelated icon language from the animal/energy set above -
// stroke-based on a 24-unit viewBox, straight from the design handoff's
// literal paths (design_handoff_dice_kingdom_mobile/README.md), not
// reproduced by eye. Kept separate from Svg/ENERGY_ICONS/CHARACTER_ICONS
// rather than forced into that 64-unit fill-based style, since these
// really are a different visual language (phase-rail pills, not game
// pieces) - the handoff itself draws this distinction.
function StrokeSvg({ size = 17, strokeWidth = 1.9, children }: IconProps & { strokeWidth?: number; children: ReactNode }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {children}
    </svg>
  );
}

export type PhaseKey = "clear" | "roll" | "main" | "attack" | "cleanup";

const PHASE_PATHS: Record<PhaseKey, string> = {
  clear: "M12 3v10M8 9l4 4 4-4M4 15v5h16v-5",
  roll: "M4 4h16v16H4zM9 9h0.01M15 15h0.01",
  main: "M12 5v14M5 12h14",
  attack: "M5 4l5 16M11 4l5 16M17 5l4 12",
  cleanup: "M20 12a8 8 0 1 1-2.4-5.7M20 4v4h-4",
};

export function PhaseIcon({ phase, size = 17 }: { phase: PhaseKey; size?: number }) {
  return (
    <StrokeSvg size={size}>
      <path d={PHASE_PATHS[phase]} />
    </StrokeSvg>
  );
}

export function ChevronDownIcon({ size = 10 }: IconProps) {
  return (
    <StrokeSvg size={size} strokeWidth={2.6}>
      <path d="M6 9l6 6 6-6" />
    </StrokeSvg>
  );
}

export function ArrowRightIcon({ size = 14 }: IconProps) {
  return (
    <StrokeSvg size={size} strokeWidth={2}>
      <path d="M5 12h14M14 7l5 5-5 5" />
    </StrokeSvg>
  );
}

// Three cards added to the roster after the original 32-card sheet
// (DiceKingdomConfig.cs's own Mongoose/Swift/Cuttlefish remarks -
// extra cost-curve entries per type) with no avatar drawn for them yet -
// real bug, direct feedback (2026-09-13): all three silently fell back
// to TardigradeIcon (CHARACTER_ICONS had no "DK-CLAW-09"/"DK-WING-09"/
// "DK-EYE-09" entry at all), most visibly "Mongoose looks like a
// Tardigrade" in the roster/buy strip.

function MongooseIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M8,36 C8,28 16,24 26,24 C36,24 46,28 52,32 C46,32 38,30 30,30 C34,34 34,40 28,44 C24,47 16,47 10,44 C15,43 19,40 19,36 C15,38 11,38 8,36 Z"
      />
      <polygon points="8,36 2,32 4,40" fill="currentColor" />
    </Svg>
  );
}

function SwiftIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="currentColor"
        d="M32,30 C24,18 12,14 4,16 C12,20 18,26 20,32 C12,32 4,36 0,42 C10,42 18,40 24,34 C26,40 30,46 38,50 C36,42 34,36 34,30 C40,34 48,36 56,34 C48,30 40,28 32,30 Z"
      />
    </Svg>
  );
}

// Head-on with the frill fanned out - the threat display Intimidate is.
function FrilledLizardIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M32,42 L5,30 C6,14 20,6 32,6 C44,6 58,14 59,30 Z" />
      <g stroke={CREAM} strokeWidth={1.8} strokeLinecap="round" opacity={0.75}>
        <path d="M32,40 L11,24" />
        <path d="M32,40 L20,13" />
        <path d="M32,40 L32,10" />
        <path d="M32,40 L44,13" />
        <path d="M32,40 L53,24" />
      </g>
      <ellipse cx={32} cy={44} rx={9} ry={8} fill="currentColor" />
      <path d="M26,49 Q32,54 38,49" stroke={CREAM} strokeWidth={2} fill="none" strokeLinecap="round" />
      <circle cx={28} cy={41} r={1.8} fill={CREAM} />
      <circle cx={36} cy={41} r={1.8} fill={CREAM} />
    </Svg>
  );
}

// Gliding, membrane spread, seen from below.
function FlyingSquirrelIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M10,22 C20,26 44,26 54,22 L50,42 C42,38 22,38 14,42 Z" />
      <circle cx={32} cy={20} r={7} fill="currentColor" />
      <path fill="currentColor" d="M26,15 L25,8 L30,13 Z M38,15 L39,8 L34,13 Z" />
      <ellipse cx={32} cy={50} rx={5} ry={10} fill="currentColor" />
      <circle cx={29.5} cy={19} r={1.6} fill={CREAM} />
      <circle cx={34.5} cy={19} r={1.6} fill={CREAM} />
    </Svg>
  );
}

// Side-on, a jolt arcing off its back - every action used zaps something.
function ElectricEelIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={7}
        strokeLinecap="round"
        d="M8,44 C16,34 24,50 32,40 C40,30 48,46 56,36"
      />
      <circle cx={10} cy={42} r={1.4} fill={CREAM} />
      <path fill="currentColor" d="M34,8 L26,22 L32,22 L28,32 L40,17 L34,17 L38,8 Z" />
    </Svg>
  );
}

// Curled tail, crest and a swivelling eye - blends in, slips past.
function ChameleonIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M8,34 C8,22 20,16 34,18 C44,19 52,24 54,32 C48,38 36,40 26,38 C18,37 12,38 8,34 Z" />
      <path fill="currentColor" d="M8,34 L4,26 L14,24 Z" />
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={4}
        strokeLinecap="round"
        d="M52,32 C60,38 58,52 48,52 C41,52 40,44 46,43 C50,42 51,47 48,48"
      />
      <g stroke="currentColor" strokeWidth={3} strokeLinecap="round">
        <path d="M22,38 L20,48" />
        <path d="M38,39 L40,48" />
      </g>
      <circle cx={16} cy={27} r={3.6} fill={CREAM} />
      <circle cx={16} cy={27} r={1.6} fill="currentColor" />
    </Svg>
  );
}

// Glowing tail end and a pair of wings - flashes on a double energy roll.
function FireflyIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={32} cy={24} rx={6} ry={8} fill="currentColor" />
      <ellipse cx={32} cy={42} rx={8} ry={10} fill="currentColor" />
      <circle cx={32} cy={44} r={5} fill={CREAM} />
      <path fill="currentColor" opacity={0.7} d="M26,28 C14,20 8,30 14,36 C18,38 24,34 26,32 Z M38,28 C50,20 56,30 50,36 C46,38 40,34 38,32 Z" />
      <g stroke="currentColor" strokeWidth={2} strokeLinecap="round">
        <path d="M30,17 L26,9" />
        <path d="M34,17 L38,9" />
      </g>
    </Svg>
  );
}

// A looping leech with its sucker down - drains whatever it touches.
function LeechIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth={9}
        strokeLinecap="round"
        d="M12,46 C12,30 26,22 32,32 C38,42 52,34 52,20"
      />
      <circle cx={12} cy={48} r={6} fill="currentColor" />
      <circle cx={12} cy={48} r={2.5} fill={CREAM} />
    </Svg>
  );
}

// Upright on sentry duty.
function MeerkatIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={32} cy={14} rx={7} ry={8} fill="currentColor" />
      <path fill="currentColor" d="M24,22 C22,34 22,46 24,56 L40,56 C42,46 42,34 40,22 Z" />
      <g stroke="currentColor" strokeWidth={3} strokeLinecap="round">
        <path d="M25,30 L21,36" />
        <path d="M39,30 L43,36" />
      </g>
      <circle cx={29} cy={13} r={1.5} fill={CREAM} />
      <circle cx={35} cy={13} r={1.5} fill={CREAM} />
    </Svg>
  );
}

// Three-part body, mandibles open.
function ArmyAntIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <circle cx={16} cy={30} r={7} fill="currentColor" />
      <ellipse cx={30} cy={32} rx={6} ry={5} fill="currentColor" />
      <ellipse cx={46} cy={34} rx={11} ry={8} fill="currentColor" />
      <g stroke="currentColor" strokeWidth={2.4} strokeLinecap="round" fill="none">
        <path d="M10,26 L4,20 M10,33 L3,36" />
        <path d="M27,36 L22,48 M30,37 L30,50 M33,36 L38,48" />
      </g>
    </Svg>
  );
}

// Spitting a jet up and over.
function ArcherfishIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M10,44 C18,34 34,32 44,40 L54,32 L52,46 L54,58 L44,50 C34,56 18,54 10,44 Z" />
      <circle cx={18} cy={42} r={2} fill={CREAM} />
      <g fill="currentColor">
        <circle cx={14} cy={30} r={2.2} />
        <circle cx={20} cy={20} r={2.2} />
        <circle cx={30} cy={13} r={2.2} />
        <circle cx={42} cy={10} r={2.2} />
      </g>
    </Svg>
  );
}

// Beetle with its spray going off behind it.
function BombardierBeetleIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={26} cy={34} rx={14} ry={11} fill="currentColor" />
      <circle cx={10} cy={34} r={5} fill="currentColor" />
      <path d="M26,23 L26,45" stroke={CREAM} strokeWidth={1.6} />
      <g stroke="currentColor" strokeWidth={2.4} strokeLinecap="round">
        <path d="M44,30 L56,24" />
        <path d="M44,34 L58,34" />
        <path d="M44,38 L56,44" />
      </g>
    </Svg>
  );
}

// Long lizard, forked tongue out.
function KomodoDragonIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <path fill="currentColor" d="M6,32 C10,26 18,26 24,28 L46,28 C52,28 58,32 62,38 C54,36 50,36 46,38 L24,38 C18,40 10,38 6,32 Z" />
      <g stroke="currentColor" strokeWidth={3} strokeLinecap="round">
        <path d="M26,38 L22,48 M40,38 L44,48 M28,28 L24,20 M40,28 L44,20" />
      </g>
      <path d="M6,32 L0,30 M6,32 L0,34" stroke="currentColor" strokeWidth={1.6} strokeLinecap="round" />
      <circle cx={12} cy={30} r={1.6} fill={CREAM} />
    </Svg>
  );
}

function CuttlefishIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <ellipse cx={30} cy={26} rx={14} ry={10} fill="currentColor" />
      <g stroke="currentColor" strokeWidth={2.4} strokeLinecap="round" fill="none">
        <path d="M20,34 C18,42 16,48 14,52" />
        <path d="M26,36 C25,43 24,48 23,52" />
        <path d="M34,36 C35,43 36,48 37,52" />
        <path d="M40,34 C42,42 44,48 46,52" />
      </g>
      <circle cx={35} cy={24} r={2.2} fill={CREAM} />
    </Svg>
  );
}

// --- Basic Actions (2026-09-26): plants, not animals - "to differentiate
// these as actions with no stats, let's give them a plant or nature
// theme." Same flat 64x64 silhouette style as the creatures. ---

// Anger Issues - a thistle: prickly, and it bites back.
function ThistleIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <rect x={30} y={34} width={4} height={24} rx={2} />
        <path d="M32,50 C24,48 18,42 16,36 C23,38 28,42 32,46 Z" />
        <path d="M32,44 C40,42 46,36 48,30 C41,32 36,36 32,40 Z" />
        <ellipse cx={32} cy={30} rx={9} ry={7} />
        <path d="M22,22 L27,26 L25,14 L30,24 L32,8 L34,24 L39,14 L37,26 L42,22 L38,28 L26,28 Z" />
      </g>
      <path d="M26,30 L38,30 M27,34 L37,34" stroke={CREAM} strokeWidth={1.6} strokeLinecap="round" />
    </Svg>
  );
}

// Archnemesis - a Venus flytrap: two jaws closing on each other, the
// "deal damage to each other" of the card.
function VenusFlytrapIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <rect x={30} y={36} width={4} height={22} rx={2} />
        <path d="M32,50 C25,48 20,44 18,39 C24,40 29,43 32,47 Z" />
        <path d="M31,34 C24,34 16,28 14,18 C22,18 29,24 31,34 Z" />
        <path d="M33,34 C40,34 48,28 50,18 C42,18 35,24 33,34 Z" />
      </g>
      <g stroke={CREAM} strokeWidth={1.6} strokeLinecap="round">
        <path d="M16,19 L13,15 M20,19 L18,14 M24,21 L23,16" />
        <path d="M48,19 L51,15 M44,19 L46,14 M40,21 L41,16" />
      </g>
    </Svg>
  );
}

// Distraction - a bright flower, the lure you can't look away from.
function FlowerIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <rect x={30} y={34} width={4} height={24} rx={2} />
        <path d="M32,52 C26,50 22,46 21,41 C26,42 30,45 32,49 Z" />
        {[0, 72, 144, 216, 288].map((deg) => (
          <ellipse key={deg} cx={32} cy={13} rx={7} ry={10} transform={`rotate(${deg} 32 24)`} />
        ))}
      </g>
      <circle cx={32} cy={24} r={6} fill={CREAM} />
    </Svg>
  );
}

// Resurrection - a seedling pushing back up out of the soil.
function SeedlingIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <path d="M10,54 C16,46 48,46 54,54 Z" />
        <rect x={30} y={26} width={4} height={24} rx={2} />
        <path d="M32,30 C30,20 22,14 10,16 C12,27 21,32 32,30 Z" />
        <path d="M32,26 C34,15 43,9 55,11 C53,22 44,28 32,26 Z" />
      </g>
      <path d="M31,29 C25,25 19,21 14,19 M33,25 C40,20 46,15 51,13" stroke={CREAM} strokeWidth={1.4} fill="none" strokeLinecap="round" />
    </Svg>
  );
}

// Mutation - a mushroom: the strange thing that grows overnight.
function MushroomIcon(props: IconProps) {
  return (
    <Svg {...props}>
      <g fill="currentColor">
        <path d="M8,34 C8,18 20,10 32,10 C44,10 56,18 56,34 Z" />
        <path d="M25,34 L39,34 L41,54 C41,57 23,57 23,54 Z" />
      </g>
      <g fill={CREAM}>
        <circle cx={22} cy={24} r={4} />
        <circle cx={36} cy={18} r={3} />
        <circle cx={44} cy={28} r={3.5} />
        <circle cx={30} cy={29} r={2.2} />
      </g>
    </Svg>
  );
}

export const CHARACTER_ICONS: Record<string, (p: IconProps) => ReactElement> = {
  "DK-ACT-01": ThistleIcon,
  "DK-ACT-02": FlowerIcon,
  "DK-ACT-03": SeedlingIcon,
  "DK-ACT-04": MushroomIcon,
  "DK-ACT-05": VenusFlytrapIcon,
  "DK-CLAW-01": HoneyBadgerIcon,
  "DK-CLAW-02": WolverineIcon,
  "DK-CLAW-03": GrizzlyBearIcon,
  "DK-CLAW-04": OrcaIcon,
  "DK-CLAW-05": PeregrineFalconIcon,
  "DK-CLAW-06": TigerIcon,
  "DK-CLAW-07": StoatIcon,
  "DK-CLAW-08": CapeBuffaloIcon,
  "DK-CLAW-09": MongooseIcon,
  "DK-CLAW-10": ElectricEelIcon,
  "DK-CLAW-11": ArmyAntIcon,
  "DK-CLAW-12": KomodoDragonIcon,
  "DK-SHELL-01": HippopotamusIcon,
  "DK-SHELL-02": MuskOxIcon,
  "DK-SHELL-03": PangolinIcon,
  "DK-SHELL-04": HermitCrabIcon,
  "DK-SHELL-05": OpossumIcon,
  "DK-SHELL-06": QueenTermiteIcon,
  "DK-SHELL-07": SnappingTurtleIcon,
  "DK-SHELL-08": BoxTurtleIcon,
  "DK-SHELL-09": LeechIcon,
  "DK-SHELL-10": MeerkatIcon,
  "DK-SHELL-11": BombardierBeetleIcon,
  "DK-WING-01": OspreyIcon,
  "DK-WING-02": BarnSwallowIcon,
  "DK-WING-03": HummingbirdIcon,
  "DK-WING-04": MountainGoatIcon,
  "DK-WING-05": MonarchButterflyIcon,
  "DK-WING-06": HomingPigeonIcon,
  "DK-WING-07": GreyhoundIcon,
  "DK-WING-08": AlbatrossIcon,
  "DK-WING-09": SwiftIcon,
  "DK-WING-10": FlyingSquirrelIcon,
  "DK-WING-11": FireflyIcon,
  "DK-WING-12": ArcherfishIcon,
  "DK-EYE-01": BarnOwlIcon,
  "DK-EYE-02": HyenaIcon,
  "DK-EYE-03": AnglerfishIcon,
  "DK-EYE-04": CowbirdIcon,
  "DK-EYE-05": MagpieIcon,
  "DK-EYE-06": RavenIcon,
  "DK-EYE-07": ElephantIcon,
  "DK-EYE-08": FoxIcon,
  "DK-EYE-09": CuttlefishIcon,
  "DK-EYE-10": FrilledLizardIcon,
  "DK-EYE-11": ChameleonIcon,
};
