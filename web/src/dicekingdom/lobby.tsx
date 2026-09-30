// Online games where the host picks only their own Champion (2026-09-30,
// user request: "you should be able to start an online game with an
// opponent without having to pick their faction" - it used to take
// picking both, starting, then copying the invite link from inside the
// match). The server holds a pending game until the invited player picks
// (V2GamesController Open/Lobby/Join); these are the three pieces both
// Dice Kingdom pages share: resolving an opened invite link, the host's
// waiting screen, and the invited player's Champion pick.
import { useEffect, useState } from "react";
import { api } from "./api";
import { CHAMPION_ICONS } from "./icons";
import { claimSeatFromUrl, nameClaimedSeat } from "./seats";
import type { GameState, LobbyStatus } from "./types";

/** Setup-screen value for Player 2's column: the opponent picks their own. */
export const OPPONENT_PICKS = "__opponent_picks__";

export interface ChampionChoice {
  id: string;
  energy: string;
}

export function championLabel(id: string): string {
  return id.replace(/([A-Z])/g, " $1").trim();
}

/** What an invite link in the URL leads to: a game to play, or a pick to make first. */
export type InviteResolution = { kind: "game"; game: GameState } | { kind: "pick"; lobby: LobbyStatus };

// Replaces the old "claim the seat, then GET the game" join: the game may
// not exist yet, if the host opened it with only their own Champion.
export async function resolveInvite(): Promise<InviteResolution | null> {
  const claim = claimSeatFromUrl();
  if (!claim) return null;
  const lobby = await api.getLobby(claim.gameId);
  if (lobby.yourPlayerId) nameClaimedSeat(claim.gameId, lobby.yourPlayerId);
  if (!lobby.started) return { kind: "pick", lobby };
  return { kind: "game", game: await api.getGame(claim.gameId) };
}

function ChampionGrid({
  champions,
  value,
  onPick,
}: {
  champions: ChampionChoice[];
  value: string | null;
  onPick: (id: string) => void;
}) {
  return (
    <div className="champ-pick">
      {champions.map((c) => {
        const Icon = CHAMPION_ICONS[c.id];
        return (
          <button
            key={c.id}
            type="button"
            className={`champ-opt${value === c.id ? " selected" : ""}`}
            style={{ ["--sel" as string]: `var(--${c.energy.toLowerCase()})`, color: `var(--${c.energy.toLowerCase()})` }}
            onClick={() => onPick(c.id)}
          >
            <Icon />
            <div className="cname" style={{ color: "var(--text-h)" }}>
              {championLabel(c.id)}
            </div>
          </button>
        );
      })}
    </div>
  );
}

/** Player 2's "let them choose" option on the setup screen. */
export function OpponentPicksOption({ selected, onPick }: { selected: boolean; onPick: () => void }) {
  return (
    <button
      type="button"
      className={`champ-opt dk-opponent-picks${selected ? " selected" : ""}`}
      style={{ ["--sel" as string]: "var(--text-h)" }}
      onClick={onPick}
    >
      <div className="cname" style={{ color: "var(--text-h)" }}>
        Opponent picks
      </div>
      <small>Send an invite link</small>
    </button>
  );
}

/** The host, after opening: the invite link, and a wait for the other player to pick. */
export function WaitingForOpponent({
  gameId,
  hostChampionId,
  link,
  onStarted,
  onCancel,
}: {
  gameId: string;
  hostChampionId: string;
  link: string | null;
  onStarted: (game: GameState) => void;
  onCancel: () => void;
}) {
  const [copied, setCopied] = useState<"copied" | "failed" | null>(null);

  useEffect(() => {
    let done = false;
    const timer = window.setInterval(async () => {
      try {
        const lobby = await api.getLobby(gameId);
        if (lobby.started && !done) {
          done = true;
          onStarted(await api.getGame(gameId));
        }
      } catch {
        // quiet - the next poll tries again
      }
    }, 1500);
    return () => {
      done = true;
      window.clearInterval(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gameId]);

  return (
    <div className="panel dk-lobby">
      <h3 style={{ margin: "0 0 6px" }}>Waiting for your opponent</h3>
      <p style={{ margin: "0 0 12px" }}>
        You're playing <b>{championLabel(hostChampionId)}</b>. Send this link - your opponent picks their own Champion,
        and the match starts as soon as they do.
      </p>
      {link && (
        <div className="dk-lobby-link">
          <input readOnly value={link} onFocus={(e) => e.currentTarget.select()} />
          <button
            type="button"
            className="btn"
            onClick={() =>
              navigator.clipboard?.writeText(link).then(
                () => setCopied("copied"),
                () => setCopied("failed"),
              )
            }
          >
            {copied === "copied" ? "Copied ✓" : copied === "failed" ? "Couldn't copy" : "Copy link"}
          </button>
        </div>
      )}
      <button type="button" className="btn dk-lobby-cancel" onClick={onCancel}>
        Cancel
      </button>
    </div>
  );
}

/** The invited player, before the game exists: see the host's pick, choose yours. */
export function PickYourChampion({
  lobby,
  champions,
  onJoined,
}: {
  lobby: LobbyStatus;
  champions: ChampionChoice[];
  onJoined: (game: GameState) => void;
}) {
  const [pick, setPick] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function join() {
    if (!pick) return;
    setBusy(true);
    setError(null);
    try {
      onJoined(await api.joinGame(lobby.gameId, pick));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      setBusy(false);
    }
  }

  return (
    <div className="panel dk-lobby">
      <h3 style={{ margin: "0 0 6px" }}>You've been invited</h3>
      <p style={{ margin: "0 0 12px" }}>
        Your opponent is playing <b>{championLabel(lobby.hostChampionId)}</b>. Pick your Champion to start the match.
      </p>
      {error && <p className="error">{error}</p>}
      <ChampionGrid champions={champions} value={pick} onPick={setPick} />
      <button type="button" className="btn" disabled={!pick || busy} onClick={join} style={{ marginTop: 14 }}>
        Start Match
      </button>
    </div>
  );
}
