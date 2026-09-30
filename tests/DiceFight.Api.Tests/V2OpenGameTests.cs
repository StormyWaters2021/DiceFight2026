using DiceFight.Api;
using DiceFight.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiceFight.Api.Tests;

// Open games (2026-09-30): the host picks only their own Champion and gets
// an invite link straight away; the invited player picks theirs to start.
public class V2OpenGameTests
{
    private static V2GamesController As(V2GameStore store, string? token, string method = "POST")
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        if (token is not null) http.Request.Headers["X-Seat-Token"] = token;
        return new V2GamesController(store) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    private static T Ok<T>(ActionResult<T> result) => (T)((OkObjectResult)result.Result!).Value!;

    [Fact]
    public void Host_Opens_Guest_Joins_And_The_Same_Id_And_Seats_Carry_Into_The_Game()
    {
        var store = new V2GameStore();
        var opened = Ok(As(store, null).Open(new OpenV2GameRequest("Armadillo")));
        var host = opened.Seats.Single(s => s.PlayerId == "teamA");
        var guest = opened.Seats.Single(s => s.PlayerId == "teamB");

        var waiting = Ok(As(store, host.Token, "GET").Lobby(opened.GameId));
        Assert.False(waiting.Started);
        Assert.Equal("Armadillo", waiting.HostChampionId);
        Assert.Equal("teamA", waiting.YourPlayerId);
        Assert.Equal("teamB", Ok(As(store, guest.Token, "GET").Lobby(opened.GameId)).YourPlayerId);

        var game = Ok(As(store, guest.Token).Join(opened.GameId, new JoinV2GameRequest("GoldenEagle")));

        Assert.Equal(opened.GameId, game.GameId);
        Assert.Equal("teamB", game.YourPlayerId);
        Assert.Equal("Armadillo", game.PlayerOne.Champion!.Id);
        Assert.Equal("GoldenEagle", game.PlayerTwo.Champion!.Id);
        Assert.True(Ok(As(store, host.Token, "GET").Lobby(opened.GameId)).Started);
        // The host's original token now plays the real game.
        Assert.Equal("teamA", Ok(As(store, host.Token, "GET").Get(opened.GameId)).YourPlayerId);
    }

    [Fact]
    public void Only_The_Invited_Seat_Can_Join_And_Only_Once()
    {
        var store = new V2GameStore();
        var opened = Ok(As(store, null).Open(new OpenV2GameRequest("Wolf")));
        var host = opened.Seats.Single(s => s.PlayerId == "teamA");
        var guest = opened.Seats.Single(s => s.PlayerId == "teamB");

        Assert.Throws<SeatRequiredException>(() => As(store, host.Token).Join(opened.GameId, new JoinV2GameRequest("Wolf")));
        As(store, guest.Token).Join(opened.GameId, new JoinV2GameRequest("Wolf"));
        Assert.Throws<InvalidOperationException>(() => As(store, guest.Token).Join(opened.GameId, new JoinV2GameRequest("Owl")));
    }

    [Fact]
    public void Opening_With_An_Unknown_Champion_Fails()
    {
        Assert.Throws<InvalidOperationException>(() => As(new V2GameStore(), null).Open(new OpenV2GameRequest("Dragon")));
    }
}
