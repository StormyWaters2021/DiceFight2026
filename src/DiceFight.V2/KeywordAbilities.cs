using DiceFight.V2.Model;
using DiceFight.V2.Model.Effects;

namespace DiceFight.V2;

// Keywords whose rules text IS an ability (2026-10-03, Dice Kingdom). A
// card just lists the keyword, the same as Deadly or Overcrush, and the
// engine supplies the ability - so the card text stays one word, a
// granted keyword (GrantTag, TagAura) works with no extra authoring, and
// blanking switches it off through the same AbilitiesOf path as printed
// text.
public static class KeywordAbilities
{
    // Dice Masters' Intimidate: "When fielded, remove target opposing
    // character die from the Field Zone until end of turn." The die waits
    // in Zone.Intimidated - not active, so it can't block, can't be
    // targeted and its "while active" text is off - and TurnEngine.CleanUp
    // returns it on the same face. Only the Field Zone: fielding happens
    // in Main, when the Attack Zone is empty anyway.
    public static readonly TriggeredAbility Intimidate = new(TriggerKind.DieFielded,
        new MoveDie(new TargetFilter(Ownership: TargetOwnership.Opposing, Zones: [Zone.FieldZone],
            Prompt: "Intimidate - choose an opposing creature to remove from the Field until end of turn."), Zone.Intimidated));

    // Dice Masters' Infiltrate: "When this character die is unblocked,
    // you may return this die to the Field Zone and it deals your opponent
    // 1 damage." Not an EventBus trigger - no event means "unblocked" - so
    // CombatEngine.DeclareBlockers queues this for each unblocked
    // Infiltrate attacker, after the On Block abilities. InZone guards an
    // attacker something else already removed. Back on the Field it is
    // still a normal active die (targetable, in the post-block window
    // too); what it skips is combat damage and the unblocked attacker's
    // trip Out of Play.
    public static readonly MayPay InfiltrateChoice = new(Cost: null,
        Then: new Sequence([
            new MoveDie(new TargetFilter(Self: true), Zone.FieldZone),
            new DealDamage(new Fixed(1), new TargetFilter(Kind: TargetKind.Player, Ownership: TargetOwnership.Opposing)),
        ]),
        Prompt: "Infiltrate - return it to the Field and deal 1 damage to the opponent (instead of its full attack)?");

    public static readonly EffectNode InfiltrateOffer = new Conditional(new InZone(Zone.AttackZone), InfiltrateChoice);

    public static IEnumerable<TriggeredAbility> For(IReadOnlySet<string> keywords)
    {
        if (keywords.Contains("Intimidate")) yield return Intimidate;
    }
}
