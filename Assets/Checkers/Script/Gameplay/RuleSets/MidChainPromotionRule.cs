// What happens when a piece reaches the promotion row in the middle of a multi-jump capture
// sequence, rather than as the final landing square of its whole turn.
public enum MidChainPromotionRule
{
    // Promotion is deferred until the chain is confirmed complete: a piece with a further legal
    // capture from the promotion row (evaluated with its pre-promotion move set) must keep playing
    // that capture as a man; it only actually crowns once it truly has nowhere further to go from
    // there (International/Brazilian).
    DeferUntilChainEnds,

    // The piece crowns the instant it lands on the promotion row mid-chain, and its turn ends
    // immediately - it cannot use its new king powers (e.g. capturing backward) until the
    // following turn (American/Italian).
    EndsTurnOnPromotion,

    // The piece crowns the instant it lands on the promotion row mid-chain, and must immediately
    // continue capturing in the same turn using its new king powers, e.g. a longer flying-capture
    // range a mere man never had (Russian).
    ContinueAsKing
}
