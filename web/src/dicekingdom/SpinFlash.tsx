// The one status cue that moves (Claude Design's "face frame" handoff,
// screen 2c, 2026-10-03): when an EFFECT spins a die - Energy Drain,
// Cuttlefish, Mutation - a white ring fades out around it and a chip like
// "▼ L2 · Leech · Energy Drain" pops above it for 1.4s. Driven by the
// server's per-die LastSpin; a new seq means a new spin. Ongoing states
// never animate.
import { useEffect, useRef, useState } from "react";
import type { Die } from "./types";

const FLASH_MS = 1400;

// Just the effect ("Energy Drain"), not "Leech · Energy Drain": the chip is
// already twice a tile's width, and a die at the screen edge ran it off
// the page. The match log names the card.
function spinText(die: Die): string | null {
  const s = die.lastSpin;
  if (!s) return null;
  const cause = s.source.includes(" · ") ? s.source.slice(s.source.lastIndexOf(" · ") + 3) : s.source;
  if (s.toLevel === null) return `▼ energy · ${cause}`;
  const down = s.fromLevel !== null && s.toLevel < s.fromLevel;
  return `${down ? "▼" : "▲"} L${s.toLevel} · ${cause}`;
}

// Which spin this PAGE has already shown, per die - not per tile. A die
// often changes tiles right after it spins (a drained attacker goes back
// from its lane to the Field), and a page polling every 2s can first see
// the spin on the new tile. Per-tile memory missed 3 of 4 Energy Drain
// spins in a bot-played check (2026-10-04). A die first seen with a spin
// already on it (page load) is recorded without flashing.
const shownSeq = new Map<string, number>();

export function useSpinFlash(die: Die): { text: string; key: number } | null {
  const [flash, setFlash] = useState<{ text: string; key: number } | null>(null);
  const seq = die.lastSpin?.seq ?? null;
  const firstRender = useRef(true);
  useEffect(() => {
    const isFirst = firstRender.current;
    firstRender.current = false;
    const shown = shownSeq.get(die.id);
    // "Seen, and it had no spin yet" is recorded too (as 0), so the first
    // spin after that still flashes on a freshly mounted tile.
    shownSeq.set(die.id, seq ?? 0);
    if (seq === null || shown === seq) return;
    // Never seen this die before: it's arriving with old history, not a new spin.
    if (shown === undefined && isFirst) return;
    const text = spinText(die);
    if (!text) return;
    setFlash({ text, key: seq });
    const t = setTimeout(() => setFlash(null), FLASH_MS);
    return () => clearTimeout(t);
  }, [seq]); // eslint-disable-line react-hooks/exhaustive-deps
  return flash;
}

export function SpinFlash({ flash }: { flash: { text: string; key: number } | null }) {
  if (!flash) return null;
  return (
    <>
      <span key={`ring-${flash.key}`} className="dk-spin-ring" aria-hidden="true" />
      <span key={`chip-${flash.key}`} className="dk-spin-chip-wrap">
        <span className="dk-spin-chip">{flash.text}</span>
      </span>
    </>
  );
}
