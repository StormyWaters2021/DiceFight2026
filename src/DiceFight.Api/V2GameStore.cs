using System.Collections.Concurrent;
using DiceFight.V2;

namespace DiceFight.Api;

// v2 counterpart to GameStore.cs - kept as a genuinely separate class
// (not a shared generic store) per the mellow-sparking-comet plan's own
// reasoning: "keeps v1 untouched" matters more here than avoiding a
// little duplication. In-memory only, same caveat as GameStore.
public sealed class V2GameStore
{
    private readonly ConcurrentDictionary<string, V2GameSession> _games = new();

    public V2GameSession Create(GameState state)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var session = new V2GameSession
        {
            Id = id,
            State = state,
            Seats =
            [
                new Seat(state.PlayerOne.Id, GameSession.NewToken()),
                new Seat(state.PlayerTwo.Id, GameSession.NewToken()),
            ],
        };
        _games[id] = session;
        return session;
    }

    public V2GameSession GetSession(string gameId) =>
        _games.TryGetValue(gameId, out var session)
            ? session
            : throw new KeyNotFoundException($"No V2 game with id '{gameId}'.");

    // --- Open games (2026-09-30): the host picks only their own Champion
    // and gets an invite link straight away; the game itself is built when
    // the invited player picks theirs (V2GamesController.Join). The lobby's
    // id and seat tokens carry over unchanged, so the link keeps working.

    private readonly ConcurrentDictionary<string, V2Lobby> _lobbies = new();

    public V2Lobby OpenLobby(string hostChampionId, string hostSeatId, string guestSeatId)
    {
        var lobby = new V2Lobby
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            HostChampionId = hostChampionId,
            Seats = [new Seat(hostSeatId, GameSession.NewToken()), new Seat(guestSeatId, GameSession.NewToken())],
        };
        _lobbies[lobby.Id] = lobby;
        return lobby;
    }

    public V2Lobby? GetLobby(string gameId) => _lobbies.TryGetValue(gameId, out var lobby) ? lobby : null;

    public bool HasGame(string gameId) => _games.ContainsKey(gameId);

    // Builds the real game under the lobby's id and seats, exactly once -
    // a second join (a double tap, two tabs) finds the lobby already gone.
    public V2GameSession StartLobby(V2Lobby lobby, GameState state)
    {
        if (!_lobbies.TryRemove(lobby.Id, out _))
            throw new InvalidOperationException("That game has already started.");
        var session = new V2GameSession { Id = lobby.Id, State = state, Seats = lobby.Seats };
        _games[lobby.Id] = session;
        return session;
    }
}

public sealed class V2Lobby
{
    public required string Id { get; init; }
    public required string HostChampionId { get; init; }
    public required IReadOnlyList<Seat> Seats { get; init; }
}
