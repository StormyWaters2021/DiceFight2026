using DiceFight.V2;

namespace DiceFight.Api.Recording;

// A short fingerprint of everything a move can change: step, whose turn and
// priority, life, and every die's zone, controller, face, damage and lane.
// Recorded after each action so a replay can say exactly where it drifted.
public static class StateChecksum
{
    public static string Of(GameState state)
    {
        var text = new System.Text.StringBuilder();
        text.Append(state.CurrentStepId).Append('|').Append(state.ActivePlayerId).Append('|').Append(state.PriorityPlayerId)
            .Append('|').Append(state.PlayerOne.Life).Append('|').Append(state.PlayerTwo.Life)
            .Append('|').Append(state.PendingChoice?.Description);
        foreach (var die in state.Dice.OrderBy(d => d.Id, StringComparer.Ordinal))
            text.Append('|').Append(die.Id).Append(':').Append(die.Zone).Append(':').Append(die.ControllerId)
                .Append(':').Append(die.CurrentFaceIndex).Append(':').Append(die.Damage).Append(':').Append(die.Lane);

        // FNV-1a, 32-bit - stable across processes, unlike string.GetHashCode.
        var hash = 2166136261;
        foreach (var ch in text.ToString()) hash = (hash ^ ch) * 16777619;
        return hash.ToString("x8");
    }
}
