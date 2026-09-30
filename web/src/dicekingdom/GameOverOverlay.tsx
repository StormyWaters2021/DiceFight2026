// Rule 2.9 - the game ends the moment a player's Life reaches 0 (added
// 2026-09-30: a playtest game used to run on at -5 life). Shared by both
// Dice Kingdom pages: who won, both final Life totals, and a way back to
// the setup screen. The board stays visible underneath.
import type { GameState } from "./types";

export function GameOverOverlay({ game, you, onNewGame }: { game: GameState; you: string | null; onNewGame: () => void }) {
  if (!game.gameOver) return null;
  const winner = game.winnerId === game.playerOne.id ? game.playerOne : game.winnerId === game.playerTwo.id ? game.playerTwo : null;
  const title = !winner ? "It's a tie" : you && winner.id === you ? "You win!" : `${winner.name} wins`;
  return (
    <div className="dk-gameover-backdrop" role="dialog" aria-label="Game over">
      <div className="dk-gameover">
        <p className="dk-gameover-eyebrow">Game over</p>
        <h2>{title}</h2>
        <p className="dk-gameover-life">
          {game.playerOne.name} {game.playerOne.life} · {game.playerTwo.name} {game.playerTwo.life}
        </p>
        <button type="button" className="btn" onClick={onNewGame}>
          New game
        </button>
      </div>
    </div>
  );
}
