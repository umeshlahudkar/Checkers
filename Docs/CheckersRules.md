# Checkers Rule Variants

This document describes the 9 checkers/draughts variants implemented in this project, the specific
rules of each, and known caveats/limitations in how they're implemented. All 9 have been audited
individually against detailed, sourced rule descriptions and corrected in code. Everything below
was verified by reading and tracing the engine's code — **it has not been runtime-tested by
actually playing matches in Unity**, since that isn't possible in the environment these audits were
done in.

A later, independent second-pass audit — this one cross-checking each ruleset's actual field values
against external sources (Wikipedia, federation rulebooks, specialized draughts-variant sites)
rather than relying on the original pass's own notes — found several real discrepancies the first
pass missed. Five have since been fixed (see "Second independent audit" near the end of this
document for what changed and why); one AI-search-quality gap (Russian) and one genuinely unresolved
rules question (Turkish's mid-chain promotion timing, where sources disagree) remain open.

A third pass — this one documentation-driven, expanding **all nine** sections to spell out their
movement/capture/win/draw rules in full and checking each claim against the assets and code — turned
up four further open **rule** discrepancies that neither audit caught, all still unfixed as of this
writing. Five variants came back completely clean (Brazilian, Italian, Spanish, Canadian, Pool
Checkers), and two of them strengthened existing findings: Brazilian and Pool Checkers each
independently corroborated a fix the second audit had applied, while Italian's weakly-evidenced
`deferCaptureRemoval` fix went uncorroborated but was shown to be behaviorally inert, so it no longer
matters either way. A consolidated list of everything still open is at the end of this document.

That pass also found a **cosmetic implementation defect** in board orientation: the
`DarkSquareBottomRight` flag recolors squares but never moves the pieces, so it inverts Italian's
board rather than correcting it, and cannot express Spanish's mirrored board at all. It affects no
outcomes — a mirrored checkers board is an isomorphic game — but it means two variants render
unlike themselves. See the cross-cutting notes for the trace.

- **International never sets `firstMoveColor`, though White is supposed to move first.** Same class
  of bug as the Brazilian and Turkish findings below.
- **Russian never sets `firstMoveColor` either** — White moves first there too. Third instance of
  that same bug class.
- **Turkish sets `menCaptureBackward: 1`, but men should not capture backward** — per the FMJD's own
  rules PDF among others.
- **Russian sets `deferCaptureRemoval: 0`, but Russian draughts uses delayed removal** (the *Turkish
  strike* rule). This one also partly undercuts the second audit's reasoning, which treated Russian's
  immediate removal as a correct baseline while fixing Pool Checkers against it — see Russian's
  caveats.

That pass also re-opened the Turkish mid-chain promotion question in a new way: the FMJD PDF
describes a *third* behavior (`EndsTurnOnPromotion`) that neither earlier audit considered, leaving
the current setting the least-supported of three candidates. And it confirmed one *non*-issue worth
recording so it isn't re-flagged later: American's immediate-removal setting is behaviorally
identical to deferred removal for that variant, for reasons given in its section.

A cross-cutting gap the same pass made explicit: **draw conditions are barely modeled anywhere.**
Every variant relies on the single generic `noProgressMoveLimit` counter. Draw by mutual agreement
and draw by threefold repetition are absent engine-wide (no draw-offer path, no position history),
as are all the variant-specific endgame draws — International's king-count rules and Turkish's
1-vs-1 rule. See each variant's "Draw conditions" for specifics.

**Update — a later implementation pass closed most of this.** Mutual agreement, threefold
repetition (all 8 variants that call for it), International/Canadian's king-count endgame draws, and
Turkish's 1-vs-1 rule are now all implemented in `GameManager.cs`. One item remains open by design —
Italian's "no forceable win" rule needs real endgame theory/a tablebase judgment, not a counter, and
was left unimplemented rather than faked. One item remains open by omission — the mutual-agreement
offer's UI (an "Offer Draw" button and an Accept/Decline popup prefab) still needs to be wired up in
the Unity Editor; the RPC/negotiation logic behind it is done. See "Draw rules implementation
status" near the end of this document for the full per-variant accounting.

Each variant is a `RuleSetSO` asset under `Assets/Script/Gameplay/RuleSets/`, selectable from the
mode-selection screen's ruleset carousel. The engine that reads these assets lives mainly in
`MoveGenerator.cs` (live gameplay) and its pure-data mirror `BoardState.cs` (used by the bot's
search) — every variant runs through the exact same code, parameterized entirely by the asset's
field values.

## Quick reference

| Variant | Board | Pieces | Movement | Flying Kings | Men Capture Backward | Mandatory Max | Tiebreak(s) | Promotion Timing | Removal Timing |
|---|---|---|---|---|---|---|---|---|---|
| American | 8×8 | 12 | Diagonal | No | No | No | — | Ends turn | Immediate |
| International | 10×10 | 20 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Russian | 8×8 | 12 | Diagonal | Yes | Yes | No | — | Continue as King | Immediate‡ |
| Brazilian | 8×8 | 12 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Italian | 8×8 | 12 | Diagonal | No | No | Yes | King mover → Most Kings → First King soonest | Ends turn | No-removal |
| Spanish | 8×8 | 12 | Diagonal | Yes | No | Yes | Most Kings captured | Deferred | No-removal |
| Canadian | 12×12 | 30 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Pool Checkers | 8×8 | 12 | Diagonal | Yes | Yes | No | — | Deferred | No-removal |
| Turkish (Dama) | 8×8 | 16 | Orthogonal | Yes | Yes‡ | Yes | — | Deferred§ | Immediate |

"Deferred" promotion = a piece that reaches the back row mid-capture, with a further legal capture
still available, keeps playing as a man and only crowns once the chain truly ends there.
"No-removal" = captured pieces stay on the board (blocking, not recapturable) until the whole
capture turn finishes, rather than disappearing the instant they're jumped.

This table describes **what the engine currently does**, which is not always what the variant's
published rules say — see each section's caveats.

§ — flagged by the second independent audit as genuinely unresolved (sources disagree on Turkish's
promotion timing), and since widened to a three-way split by a later documentation pass. See
"Second independent audit" near the end of this document for specifics and for the five findings
from that audit that have since been fixed (no longer marked here).

‡ — believed **wrong**, open and unfixed; see the relevant variant's caveats. Turkish men should not
capture backward, and Russian should use delayed (not immediate) removal.

---

## American Checkers (English Draughts)

Also known as **Straight Checkers** (North America, to distinguish it from Pool Checkers and the
unrelated Chinese Checkers), **English Draughts** (UK), **Dams** (historical Scots), and
**Dama/Dames/Damas** in continental Europe. "Checkers" names the checkered board; "draughts" comes
from the old verb meaning to draw/move a piece. Every one of those names refers to this same
variant.

### Board and setup

- 8×8 board = 64 squares, but play happens **only on the 32 dark squares** — a piece never occupies
  or crosses a light square.
- The board is oriented so each player has a dark square in their nearest bottom-left corner.
  (Engine default; American does not set `DarkSquareBottomRight`, unlike Italian.)
- 12 pieces per side, on the dark squares of the 3 rows closest to each player.
- Only one piece per square; no stacking.

### Movement

- **Men** move one square diagonally forward into an empty dark square. Never backward, never
  sideways.
- **Kings** move one square diagonally *either* forward or backward. They do **not** fly — a king
  covers exactly one square per step, same as a man, just in four directions instead of two.
- One piece moves per turn, the sole exception being a piece continuing a multi-jump.

### Capturing

- **The jump.** If an enemy piece sits diagonally adjacent and the square directly beyond it (same
  diagonal) is empty, you jump over it and land there.
- **Mandatory.** If any jump exists anywhere on the board, a quiet move is illegal — you must jump.
- **Free choice of jump.** When several pieces can jump, or one piece has several jump paths, you
  pick freely. There is *no* longest-sequence requirement: taking fewer pieces is legal.
  (`mustCaptureMaximum: 0`, so `MoveGenerator.ApplyMandatoryCaptureTiers` returns every candidate
  untouched — none of the tiebreak tiers used by Italian/Spanish/Brazilian apply here.)
- **Multi-jumps.** If the landing square offers another jump for that same piece, it must keep
  jumping, repeating until no further jump is available to it.
- **Men never capture backward** — an enemy piece sitting open on the diagonal behind a man is
  simply safe from it (`menCaptureBackward: 0`).
- **Kings capture in all four diagonal directions** and may freely mix forward and backward hops
  inside one multi-jump chain.
- **No piece is jumped twice** in a single turn.

### Kings (promotion)

- A man reaching any square of the opponent's back row (the King's Row) is crowned immediately.
- **The crowning ends the turn on the spot.** Even if the newly-made king has a backward jump
  available from that square, it cannot take it this turn — it must wait until its next turn to use
  king powers. This is `midChainPromotionRule: 1` (`MidChainPromotionRule.EndsTurnOnPromotion`),
  shared only with Italian.

### Win conditions

A player wins the moment either holds:

1. **Total elimination** — the opponent has no pieces left on the board.
2. **Total immobilization (blockade)** — the opponent still has pieces, but it is their turn and
   they have zero legal moves; every piece is blocked by other pieces or by the board edge.

Both funnel through the same check: `GameManager.StartTurn` calls `Player.CanPlay()` at the start of
each turn and, when it returns false, ends the match with reason `"no legal moves left"` — an
empty board is just the degenerate case of having no moves.

### Draw conditions

Real American checkers recognizes three draws; **the engine now implements all three**, one of them
approximately.

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree neither can force a win | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring (see "Draw rules implementation status") |
| 40-move / no-progress rule | 40 consecutive moves *by each player* with no capture and no man advanced | **Implemented, but counted differently** — see below |
| Threefold repetition | Same position, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw`, a position hash checked every turn (`ThreefoldRepetitionEnabled: true`) |

The one that does run is the no-progress rule: `AmericanRules.asset` sets `noProgressMoveLimit: 40`,
and `GameManager.SwitchTurn` increments `movesWithoutProgress` on every turn that made no progress,
declaring a draw with reason `"no progress for too long"` once it reaches the limit. Two deliberate
differences from the textbook rule are worth knowing:

- **It counts plies, not move-pairs.** 40 here means 40 individual turns total (20 per side), where
  the traditional 40-move rule means 40 by *each* player. The engine's limit is therefore roughly
  half as patient.
- **"Progress" means a capture or a promotion, not a man advancing.** `Player.cs:304` passes
  `hasDeleted || justPromoted` as the progress flag, so a plain man advance does not reset the
  counter even though the classic rule treats it as progress. In practice this only bites in long
  king-vs-king endings where men still exist but never move.

A timed-out turn (turn-timer modes only) neither advances nor resets the counter — it is carried
over unchanged, since no move was played.

**Caveats:** none known. Fully audited; two real bugs were found and fixed during that audit
(mandatory capture wasn't actually enforced over quiet moves, and mid-chain promotion was letting
the newly-crowned piece keep capturing with backward powers in the same turn).

One point where American's configuration *looks* like it contradicts the published rules but does
not, traced during this pass: most descriptions of American checkers say captured pieces are lifted
only once the whole jumping turn ends, whereas `AmericanRules.asset` sets `deferCaptureRemoval: 0`
(immediate removal). The two are **behaviorally identical for this variant**, because American's
kings don't fly. Every hop moves the mover exactly ±2 rows and ±2 columns, so `(row % 2, col % 2)`
is invariant across an entire chain, while every captured square sits at ±1 on both axes — the
opposite parity. A captured piece's square can therefore never be a landing square later in the same
chain. Its only other possible role is as a later hop's midpoint, and there both settings refuse the
jump anyway (immediate removal leaves nothing to jump over; deferred removal leaves a corpse that
`MoveGenerator.SearchCaptures` skips via `middlePiece.IsCaptured`). The distinction only becomes
observable with flying kings, which is exactly why it was a real bug for Pool Checkers (see the
second independent audit below) and is a non-issue here.

## International Draughts

Also called **International Checkers** or **Polish draughts** — the globally standardized variant,
governed by the FMJD, and the one played competitively across Europe, Africa, and South America. Its
rules diverge from American on almost every axis that matters: bigger board, backward captures for
men, flying kings, forced-maximum captures, and end-of-turn removal.

### Board and setup

- 10×10 board = 100 squares, using only the **50 dark squares**.
- Oriented so each player's nearest bottom-left corner square is dark. This is the engine's shared
  default (`DarkSquareBottomRight` is unset here — only Italian flips it), so no special handling is
  needed.
- 20 pieces per side, filling the dark squares of the first 4 rows (4 rows × 5 dark squares = 20).
- **White moves first** — the reverse of American. See the caveat below; this is *not* currently
  modeled.

### Movement

- **Men** move one square diagonally forward only, never backward. (Backward *capturing* is a
  separate matter — see below.)
- **Flying kings** (`flyingKings: 1`): a king slides any number of empty squares along a diagonal,
  forward or backward, like a chess bishop.

### Capturing

- **Mandatory**, as everywhere.
- **Men capture backward.** A man may not *move* backward, but it may jump an adjacent enemy piece
  in any of the four diagonal directions, landing on the empty square beyond
  (`menCaptureBackward: 1`). This is one of the sharpest tactical differences from American.
- **Flying king captures.** A king may jump an enemy piece from arbitrary distance provided every
  square between them is empty, and it may land on **any** empty square beyond the captured piece,
  not just the one immediately behind. Both halves are explicit in
  `MoveGenerator.SearchCaptures`: a `flying` piece walks outward through `IsPassable` squares to
  find its victim ([MoveGenerator.cs:343](Assets/Script/Gameplay/MoveGenerator.cs:343)), then the
  landing loop keeps advancing past the victim, recursing from each landing square in turn, until it
  hits an obstruction or the edge ([MoveGenerator.cs:373](Assets/Script/Gameplay/MoveGenerator.cs:373)).
  Non-flying pieces `break` after the single landing square.
- **Maximum capture rule (quantity only).** With several capture paths available, the one taking the
  most pieces is compulsory — a 3-piece path forbids a 2-piece path *even if the shorter one takes
  kings*. `mustCaptureMaximum: 1` with all three `prefer*` tiebreak flags at `0`, so
  `ApplyMandatoryCaptureTiers` filters on count and then stops; quality is never consulted. (Compare
  Italian, which layers three further tiers on top, and Spanish, which adds one.)
- **No in-flight removal ("the Turk's stroke").** Captured pieces stay on the board until the whole
  turn completes: they still block squares, and the same piece can never be jumped twice
  (`deferCaptureRemoval: 1`). Because kings fly here, this is load-bearing rather than cosmetic — a
  corpse left standing can block a king's ray for the rest of the turn, which is precisely the
  tactical point of the rule.

### Kings (promotion)

- A man crowns **only if it finishes its turn** on the opponent's back row. Passing *through* the
  back row during a multi-jump and landing elsewhere leaves it a man
  (`midChainPromotionRule: 0` = `MidChainPromotionRule.DeferUntilChainEnds`).
- A piece that lands on the back row with a further legal capture still available must play that
  capture, as a man, and only crowns if the chain genuinely ends there.

### Win conditions

1. **Total elimination** — the opponent has no pieces left.
2. **Total immobilization** — it's the opponent's turn and they have no legal move.

Same single code path as every other variant: `Player.CanPlay()` checked from
`GameManager.StartTurn`, ending the match with reason `"no legal moves left"`.

### Draw conditions

International's official draw rules are the most elaborate of any variant here, and **all four are
now implemented** as simplified stand-ins (not full endgame theory, but tied to the specific
material/position each rule actually names).

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| Threefold repetition | Same position, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |
| 3 kings vs 1 king | Draw after 16 moves if no win is forced | **Implemented as a move counter** — `GameManager.CheckMaterialDraw`, gated on exact material (`ThreeVsOneKingDrawLimit: 16`), not a forced-win judgment |
| 2 kings, or king + man, vs 1 king | Draw after 5 moves | **Implemented as a move counter** — same mechanism (`TwoVsOneKingDrawLimit: 5`) |

`InternationalRules.asset` used to set `noProgressMoveLimit: 25` as a rough stand-in for the two
king-count rules above, back when neither they nor threefold repetition existed. Now that both are
properly implemented, that stand-in has been turned off (`noProgressMoveLimit: 0`) rather than left
running alongside its own replacement — a finite board has finitely many positions, so a shuffle
making no progress is now guaranteed to eventually trip the threefold-repetition check on its own,
without needing a second, cruder counter layered on top of it.

**Caveats:** one open discrepancy, plus one previously-documented gap that is fixed.

- **`firstMoveColor` is unset; it should be `White`.** Found while documenting the rules above, not
  by either earlier audit — `InternationalRules.asset` has no `firstMoveColor` line at all, so it
  falls back to `PieceType.None` and `GameManager.DetermineFirstTurnPlayer` simply hands the first
  turn to player 1 regardless of color. FMJD Annex 1 and the general rule descriptions agree White
  opens in international draughts. This is the exact same class of bug the second independent audit
  found and fixed for Brazilian and Turkish — International was assumed to be in the "doesn't care"
  group at that time and was never rechecked. Since piece color is player-selectable in offline
  modes, a player who picks Black currently still moves first. The fix is the same one-liner applied
  to the five sibling assets (`firstMoveColor: 1`); it has **not** been applied here, since this pass
  was scoped to documentation.
- ~~**No-removal live-play gap**~~ — **Fixed.** A captured piece is now only marked and shrunk to
  half-scale at hop time (`Piece.MarkCaptured`), keeping its square occupied — and therefore still
  blocking a flying king's path — until the whole capture turn ends, at which point every piece
  marked this turn is actually destroyed (`Player.DestroyPieceAt`, called a second time per piece
  from the end-of-chain sweep) right before the turn switches. See the cross-cutting notes below for
  detail shared with Brazilian/Spanish/Canadian.

## Russian Checkers (Shashki)

**Shashki** (русские шашки) — American's compact 8×8 footprint married to International's long-range
mechanics, minus the maximum-capture constraint. Its signature is mid-turn promotion: a piece can
change what it *is* partway through a single move.

### Board and setup

- 8×8 board, 32 dark squares only, 12 pieces per side on the nearest 3 rows.
- Oriented with a dark square in each player's nearest bottom-left corner — the engine's shared
  default, so no flag needed.
- **White moves first**, the reverse of American. See the caveats — not currently modeled.

### Movement

- **Men** move one square diagonally forward only; never backward, never sideways.
- **Flying kings** (`flyingKings: 1`): a crowned piece (*damka*) slides any number of empty squares
  along a diagonal in either direction, like a chess bishop.

### Capturing

- **Mandatory but not maximum.** Capture is compulsory, but among available paths you choose freely —
  there is no obligation to take the longest line (`mustCaptureMaximum: 0`, so
  `ApplyMandatoryCaptureTiers` returns every candidate untouched). This is the sharpest divergence
  from International, which shares nearly everything else.
- **Men capture backward** (`menCaptureBackward: 1`) — a man may not *move* backward, but may jump
  in any diagonal direction.
- **Long-range king jumps:** a king glides across empty squares to reach a distant victim and may
  land on any empty square beyond it.
- **Mid-jump instant promotion.** A man landing on the back row mid-chain crowns *immediately* and
  must keep capturing that same turn with its new king powers
  (`midChainPromotionRule: 2` = `MidChainPromotionRule.ContinueAsKing` — the only ruleset here that
  uses it). The genuinely new power is the king's long-range flying capture; backward capture isn't
  it, since Russian men already had that.
- **Removal timing:** the engine removes captured pieces immediately; published rules say they stay
  until the turn ends. See the caveats — this is an open discrepancy.

### Win conditions

1. **Total elimination** — all 12 enemy pieces captured.
2. **Total immobilization** — the opponent has zero legal moves on their turn.

Same shared path as every variant: `Player.CanPlay()` from `GameManager.StartTurn`, ending with
reason `"no legal moves left"`.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| 25-move rule | 25 consecutive moves using only kings, no capture and no man advanced | **Approximated** — generic counter at `noProgressMoveLimit: 30` |
| Threefold repetition | Same layout, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |

The generic counter is a loose stand-in rather than the real rule: it is set to 30 (not 25), counts
plies rather than move-pairs, treats captures and promotions as the only progress
([Player.cs:304](Assets/Script/Gameplay/Player.cs:304)), and — unlike the published rule — never
checks the "using only kings" precondition, so it can fire in positions where the real 25-move rule
would not apply at all.

**Caveats:** two open discrepancies found by a later documentation pass, plus the previously-known
AI-quality gap.

- **Open — `deferCaptureRemoval` should probably be `1`, not `0`.** The asset configures immediate
  removal, but multiple sources describe Russian draughts as using delayed removal: jumped pieces
  stay on the board until the whole turn completes, and the same piece can never be jumped twice.
  This is the well-known *Turkish strike* (турецкий удар) situation, where a flying king's path stays
  blocked by a piece it already captured earlier in the same chain. Because Russian has flying kings,
  the difference is behaviorally real — exactly the reasoning the second independent audit used to
  fix Pool Checkers. **This finding partly undercuts that audit's premise:** it justified Pool
  Checkers' old setting as "matching Russian" and corrected only Pool Checkers, treating Russian's
  immediate removal as the correct baseline. If Russian is deferred too, that baseline was wrong and
  Pool Checkers happened to get the right fix for a partly-wrong reason. Not applied here — and worth
  noting it would be the first pairing of `ContinueAsKing` with `deferCaptureRemoval: 1` anywhere in
  the project, a combination nothing has exercised yet, so it deserves actual play-testing rather
  than a blind flag flip.
- **Open — `firstMoveColor` is unset; it should be `White`.** `RussianRules.asset` has no
  `firstMoveColor` line, so it falls back to `PieceType.None` and player 1 opens regardless of color.
  Sources agree White moves first in Russian draughts. Third instance of this same bug class, after
  Brazilian and Turkish (both fixed) and International (open). Since color is player-selectable
  offline, a player who picks Black currently still moves first.
- **Known — the bot's minimax search never models mid-chain promotion.** From the second independent
  audit: `AIRules` has no `MidChainPromotionRule` field, so a piece that would crown partway through
  a hypothetical search line keeps man-only movement for the rest of that search. Doesn't produce
  illegal moves — live execution crowns for real, hop-by-hop, independent of the search — but makes
  the bot slightly blind to king-power lines specific to Russian's `ContinueAsKing` rule. This is the
  variant where that gap costs the most, since `ContinueAsKing` is unique to it.

## Brazilian Checkers

Natively **Jogo de Damas** — International draughts' rulebook on American's 8×8 board. Structurally
it is International scaled down, not a variant with rules of its own: every mechanic below is
inherited, and the only differences from the 10×10 game are board size, piece count, and the draw
counter.

### Board and setup

- 8×8 board, 32 dark squares only, 12 pieces per side on the nearest 3 rows.
- Oriented with a dark square in each player's nearest bottom-left corner — the engine's shared
  default.
- **White moves first** (`firstMoveColor: 1`, fixed by the second independent audit).

### Movement

- **Men** move one square diagonally forward only, never backward.
- **Flying kings** (`flyingKings: 1`): slide any number of empty squares along a diagonal in either
  direction, like a chess bishop.

### Capturing

- **Mandatory**, always.
- **Men capture backward** (`menCaptureBackward: 1`) — forward-only movement, all-directions
  capture.
- **Long-range king jumps**, landing on any empty square beyond the victim — the same flying
  machinery described under International.
- **Maximum capture, quantity only.** The longest path is compulsory, and *nothing else* breaks
  ties. Worth spelling out, because this collapses two separate tiers the engine supports and
  Brazilian deliberately doesn't use:
  - A king's sequence does **not** outrank a man's sequence of equal length — either may be played
    (`preferKingMover: 0`).
  - A path capturing more *kings* does **not** outrank an equal-length path capturing fewer
    (`preferKingCaptures: 0`). Raw piece count is the only criterion.

  Both flags are off, so `ApplyMandatoryCaptureTiers` filters on count and stops. Contrast Italian,
  which uses all four tiers, and Spanish, which uses the second of these two.
- **Delayed removal** (`deferCaptureRemoval: 1`): jumped pieces stay on the board, still blocking
  their squares, until the turn completes; the same piece can never be jumped twice. Load-bearing
  here, as in International, because kings fly.

### Kings (promotion)

- A man crowns **only if it finishes its turn** on the opponent's back row. Jumping through the back
  row mid-chain and landing elsewhere leaves it a man
  (`midChainPromotionRule: 0` = `MidChainPromotionRule.DeferUntilChainEnds`).

### Win conditions

1. **Total elimination** — all 12 enemy pieces captured.
2. **Total immobilization** — the opponent has zero legal moves on their turn.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| 20-move rule | 20 consecutive moves using only kings, no capture and no man advanced | **Approximated** — generic counter at `noProgressMoveLimit: 25` |
| Threefold repetition | Same layout, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |

Same limitations as everywhere else: the counter is set to 25 rather than 20, counts plies rather
than move-pairs, and never checks the "using only kings" precondition
([Player.cs:304](Assets/Script/Gameplay/Player.cs:304)).

**Caveats:** none known — and unlike American/International/Turkish/Russian, the documentation pass
that expanded this section found **no new discrepancies**: every field matches the published rules,
including both mandatory-capture tiers being correctly disabled. The no-removal live-play fix
described under International applies here too (inherited, not variant-specific). The second
independent audit (see that section near the end) found and fixed two issues not caught by the
original per-variant pass:
- `firstMoveColor` was unset — real Brazilian draughts has White (the light pieces) always moving
  first, same class of rule as Italian/Spanish/Canadian's fix, just missed for this variant.
  `BrazilianRules.asset` now sets `firstMoveColor: 1` (White).
- `preferKingCaptures` was set, adding a spurious "most Kings captured" tiebreak (copied from
  Spanish/Italian) on top of mandatory-maximum — the IDF's own rulebook states Brazilian captures
  tie purely on quantity, "regardless of quality." `BrazilianRules.asset` now sets
  `preferKingCaptures: 0`, and its in-app rules text (which used to describe a "King Priority" tier)
  was updated to match.

## Italian Checkers (Dama Italiana)

**Dama Italiana** — American's board and short-range kings, plus the most formalized capture
hierarchy of any variant here. Its two signatures are the king immunity shield (men cannot touch
kings at all) and a four-law priority cascade that removes almost all player choice from capturing.

### Board and setup

- 8×8 board, 32 dark squares only, 12 pieces per side on the nearest 3 rows.
- **The cantone (bottom-right rule):** the board is rotated so each player has a dark square in
  their nearest **bottom-right** corner — the opposite of American — and that dark corner is a
  playing square. This is the only ruleset setting `darkSquareBottomRight: 1`, **and that flag does
  not correctly express the rule**: it recolors squares without moving the pieces, so Italian
  currently renders with its pieces on the light-colored squares and a dark *unplayable* corner —
  the inverse of the intended look. See the cross-cutting notes for the full trace. Cosmetic only:
  it does not affect legality, since a mirrored checkers board is an isomorphic game.
- **White always moves first** (`firstMoveColor: 1`).

### Movement

- **Men (*pedine*)** move one square diagonally forward only; never backward.
- **Short-range kings** (`flyingKings: 0`) — a *dama* moves and captures exactly one square
  diagonally, forward or backward. No sliding, unlike International/Russian/Brazilian.

### Capturing (the hierarchy)

Capturing is mandatory, and men capture **diagonally forward only** (`menCaptureBackward: 0`). When
several paths exist, four laws are applied in strict order, each narrowing the survivors from the
last — implemented as the tier cascade in `MoveGenerator.ApplyMandatoryCaptureTiers`:

| # | Law | Field |
|---|---|---|
| 1 | **Maximum Quantity** — take the path capturing the most pieces | `mustCaptureMaximum: 1` |
| 2 | **King Execution** — among ties, a king's sequence must be played over a man's | `preferKingMover: 1` |
| 3 | **Target Quality** — among what remains, take the path capturing the most kings | `preferKingCaptures: 1` |
| 4 | **First King Eaten** — among what still remains, take the path capturing a king earliest | `preferEarlierKingCapture: 1` |

Italian is the only ruleset that enables all four. (Spanish uses laws 1 and 3; Brazilian and
International use law 1 alone.)

- **The King Immunity Shield** (`menCannotCaptureKings: 1`): a man can never jump or capture a king
  under any circumstance — only a king may capture a king. Enforced both in capture legality
  ([MoveGenerator.cs:362](Assets/Script/Gameplay/MoveGenerator.cs:362)) and in the AI's "is this
  piece safe" heuristic, so the bot correctly treats an enemy man as no threat to its kings.
- **Delayed removal** (`deferCaptureRemoval: 1`): captured pieces stay on the board until the turn
  ends. See the caveats — this setting is behaviorally inert for Italian.

### Kings (promotion)

- A man reaching the opponent's back row crowns immediately, and **its turn ends right there**
  (`midChainPromotionRule: 1` = `MidChainPromotionRule.EndsTurnOnPromotion`) — same as American, and
  shared only with it.

### Win conditions

1. **Total elimination** — all 12 enemy pieces captured.
2. **Total immobilization** — the opponent has no legal move on their turn.

### Draw conditions

Italian's published draw rules are unusual here in that **neither is a move counter**:

| Rule | Real game | This engine |
|---|---|---|
| No forceable win | Remaining material means neither side can engineer a finishing sequence | **Not implemented, and out of scope** — requires real endgame theory/a tablebase judgment, not a counter; a later pass that implemented every other variant's draw rules deliberately left this one unimplemented rather than fake it with a counter |
| Mutual agreement | Both players agree the position is unbreakable | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |

This makes Italian the one variant where the engine's draw behavior is not merely an approximation
but an *addition*: `noProgressMoveLimit: 40` (the highest of any ruleset, tied with American) will
declare a draw after 40 no-progress plies even though nothing in Italian's own rules calls for a
move limit. In practice it is a reasonable safety valve — short-range kings plus no-removal make
endless shuffling entirely possible, and the counter is a crude proxy for exactly the "no forceable
win" judgment the real rules ask a human to make — but it can fire in positions where the published
rules would let play continue.

**Caveats:** none known, and the documentation pass that expanded this section found no new
discrepancies — every field matches the published rules, including all four priority laws in the
correct order. Two notes on the *evidence* behind existing settings, though:

- **`deferCaptureRemoval: 1` is behaviorally inert here** — and it remains single-sourced. The
  second independent audit set it on one source (mindsports.nl) and filed it under "Likely" rather
  than "Confirmed"; nothing in this pass corroborated it either way, since the rule description used
  did not mention removal timing at all. That matters less than it might, because the parity
  argument given in American's section applies verbatim to Italian: with non-flying kings every hop
  moves the piece exactly ±2 rows and ±2 columns, so `(row % 2, col % 2)` is invariant across the
  chain while captured squares sit at the opposite parity — a captured square can never be a landing
  square, and as a later hop's midpoint both settings refuse the jump anyway. **Immediate and
  deferred removal produce identical legal moves for Italian.** The low-confidence fix therefore
  carries no risk, and equally no benefit; it is not worth further sourcing effort.
- **`midChainPromotionRule: 1` was likewise not re-confirmed** by this pass — the description used
  was silent on mid-chain promotion. It dates from the original per-variant audit and stands
  unchallenged, not re-verified.

All three items below were found and fixed by earlier passes:
- **Board orientation ("cantone").** `RuleSetSO`/`IRuleSet` now expose a `DarkSquareBottomRight`
  flag (`ItalianRules.asset` is the only ruleset with it set); `BoardGenerator.GenerateBoard`
  flips every square's color when it's set, landing a dark square in the bottom-right corner
  instead of the shared default's light one. Purely cosmetic, doesn't affect legality.
- **"White always moves first."** `RuleSetSO`/`IRuleSet` now expose a `FirstMoveColor`
  (`PieceType.None` for every ruleset that doesn't care, which keeps the previous "player 1 always
  opens" behavior unchanged). Italian's is `White`; `GameManager.StartFirstTurn` now picks whichever
  seat currently holds that color instead of hardcoding player 1.
- **No-removal, found by the second independent audit** (see that section near the end):
  `deferCaptureRemoval` used to be `0` (immediate removal), but mindsports.nl's Dama Italiana rules
  state a multiple capture "must be completed before the captured pieces are removed from the
  board" — the same no-removal semantics as International/Brazilian/Spanish/Canadian.
  `ItalianRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text was updated to
  mention the rule.

## Spanish Checkers (Damas Españolas)

**Dama Española** — traditionally played across the Iberian Peninsula, North Africa, and parts of
South America. Same 12-piece 8×8 footprint as American, but with flying kings, a mirrored board, and
men strictly forbidden from capturing backward.

### Board and setup

- 8×8 board, 32 playing squares, 12 pieces per side on the nearest 3 rows.
- **The mirrored board:** Spanish is conventionally described as playing on the *light* squares, with
  a light square in each player's nearest **bottom-right** corner — the mirror of American's
  arrangement. This is **not modeled**; see the caveats. It has no effect on outcomes (a left-right
  mirror of a checkers position is an isomorphic game), but the board does not look the way the
  convention describes.
- **White always moves first** (`firstMoveColor: 1`).

### Movement

- **Men** move one square diagonally forward only; never backward.
- **Flying kings** (`flyingKings: 1`): slide any number of empty squares along a diagonal in either
  direction, like a chess bishop.

### Capturing

- **Mandatory**, always.
- **No backward capture by men** (`menCaptureBackward: 0`) — unlike Russian/International/Brazilian,
  a Spanish man jumps diagonally forward only. Together with flying kings, this is what gives the
  variant its character: weak men, dominant kings.
- **Long-range king jumps**, landing on any empty square beyond the victim.
- **Two-law priority cascade**, applied in order:
  1. **Maximum Quantity** — take the path capturing the most pieces (`mustCaptureMaximum: 1`).
  2. **Quality tiebreak** — among equal-length paths, take the one capturing the most kings
     (`preferKingCaptures: 1`).

  Spanish sits between Brazilian (law 1 only) and Italian (all four): `preferKingMover` and
  `preferEarlierKingCapture` are both `0`, so a king's sequence does not outrank a man's, and
  *when* a king is taken in the sequence is irrelevant.
- **Delayed removal** (`deferCaptureRemoval: 1`): captured pieces stay on the board, still blocking
  their squares, until the turn ends. Load-bearing here, as in International, because kings fly.

### Kings (promotion)

- A man crowns **only if it finishes its turn** on the opponent's back row; jumping through mid-chain
  and landing elsewhere leaves it a man
  (`midChainPromotionRule: 0` = `MidChainPromotionRule.DeferUntilChainEnds`).

### Win conditions

1. **Total elimination** — all 12 enemy pieces captured.
2. **Total immobilization** — the opponent has zero legal moves on their turn.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| Threefold repetition | Same layout, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |

The published rules used here list **no move-limit draw at all** — `noProgressMoveLimit` used to be
set to 30 anyway, as a pragmatic safety valve against endless king shuffling, but that made Spanish's
draw behavior an *addition* rather than an approximation, capable of ending a game the real rules
would let continue. Now that threefold repetition is properly implemented and already guarantees a
no-progress shuffle can't run forever on its own, the redundant counter has been turned off
(`noProgressMoveLimit: 0`).

**Caveats:** every *rule* field matches the published rules — the documentation pass found no
discrepancy in movement, capturing, promotion, or win conditions. The board-orientation modeling,
however, is weaker than this document previously claimed:

- **The mirrored board is not modeled, and the earlier reasoning for that was wrong.** This document
  used to state that Spanish's "bottom-right corner convention is *white*, which was already the
  engine's shared default, so `DarkSquareBottomRight` didn't need to be set." That matched on the
  *color word* without checking whether that white square is one you play on. The engine's default
  puts a light square in the bottom-right corner that is **not** a playing square; Spanish's
  convention wants a light square there that **is**. Those are opposite arrangements that happen to
  share a color name.
- **Consequence:** none for gameplay. Checkers is symmetric under left-right reflection, and
  mirroring maps one square-parity class onto the other, so the mirrored and unmirrored boards are
  isomorphic — identical game trees under relabeling. The cost is purely visual/notational: the
  board doesn't match the convention players of this variant expect.
- **The flag that would express this is itself broken** — see the cross-cutting notes below. Setting
  `DarkSquareBottomRight` would not actually fix Spanish, because that flag only recolors squares
  and never moves the pieces.

The no-removal live-play gap is fixed (see under International), and "White always moves first" is
fixed (see under Italian).

## Canadian Checkers (Grand jeu de dames)

**Grand jeu de dames** — International draughts' rulebook scaled up to 12×12, developed by French
settlers in Canada and still played in Quebec. Like Brazilian, it is International with the board
resized rather than a variant with mechanics of its own; unlike Brazilian, it scales *up*.

### Board and setup

- 12×12 board = 144 squares, using only the **72 dark squares**.
- Oriented with a dark square in each player's nearest **bottom-left** corner — which the engine
  gets right by default. Worth stating explicitly given the orientation defect affecting
  Italian/Spanish: for a 12×12 board, the bottom-left square (11,0) has parity
  `(11 + 0) % 2 = 1`, so it is both a playing square and rendered dark, exactly as the convention
  requires. Canadian needs no orientation flag and is unaffected by that defect.
- 30 pieces per side on the nearest 5 rows (5 rows × 6 dark squares per 12-wide row = 30), leaving
  the middle two rows empty at setup.
- **White moves first** (`firstMoveColor: 1`), matching International.

### Movement

- **Men** move one square diagonally forward only, never backward.
- **Flying kings** (`flyingKings: 1`): slide any number of empty squares along a diagonal in either
  direction, like a chess bishop.

### Capturing

- **Mandatory**, always.
- **Men capture backward** (`menCaptureBackward: 1`) — forward-only movement, all-directions
  capture.
- **Long-range king jumps**, landing on any empty square beyond the victim.
- **Maximum capture, quantity only** (`mustCaptureMaximum: 1`, all three `prefer*` tiebreak flags at
  `0`) — the longest path is compulsory and nothing else breaks ties, identical to International and
  Brazilian.
- **Delayed removal** (`deferCaptureRemoval: 1`): jumped pieces stay on the board, still blocking
  their squares, until the turn completes; the same piece can never be jumped twice. Load-bearing,
  since kings fly.

### Kings (promotion)

- A man crowns **only if it finishes its turn** on the opponent's back row; jumping through the back
  row mid-chain and landing elsewhere leaves it a man
  (`midChainPromotionRule: 0` = `MidChainPromotionRule.DeferUntilChainEnds`).

### Win conditions

1. **Total elimination** — all 30 enemy pieces captured.
2. **Total immobilization** — the opponent has zero legal moves on their turn.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| Threefold repetition | Same layout, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |

The description used here lists no move-limit rule, and `noProgressMoveLimit` is now `0` (disabled)
here too, for the same reason as Spanish and International: threefold repetition already guarantees
a no-progress shuffle can't run forever, so the old safety-valve counter (it used to be 35, making
Canadian's draw behavior an *addition* rather than an approximation) is redundant rather than needed.
As International's scaled-up sibling, Canadian also inherits International's material-specific
endgame draws (the king-count rules), now implemented too, the same way as International:
`GameManager.CheckMaterialDraw` with `ThreeVsOneKingDrawLimit: 16`/`TwoVsOneKingDrawLimit: 5` set on
`CanadianRules.asset`.

**Caveats:** none known, and the documentation pass that expanded this section found **no
discrepancies of any kind** — every rule field matches, and unlike Italian and Spanish, its board
orientation is correct as shipped. The no-removal live-play gap is fixed (see under International),
and "White always moves first" is fixed (see under Italian) — `CanadianRules.asset` sets
`FirstMoveColor: White`.

## Pool Checkers

Historically popular in the American South. American's board and setup, but with International's
flying kings and backward-capturing men grafted on — and then, unlike International or Brazilian, the
maximum-capture rule thrown out entirely, giving the player free choice of capture path.

### Board and setup

- 8×8 board, 32 dark squares only, 12 pieces per side on the nearest 3 rows — identical footprint to
  American.
- Oriented with a dark square in each player's nearest bottom-left corner — the engine's default,
  correct as shipped (same as American and Canadian; unaffected by the orientation defect that hits
  Italian and Spanish).
- **Black moves first**, matching American rather than the White-first continental variants.
  `firstMoveColor: 2` — and `PieceType` is `None = 0, White = 1, Black = 2`
  ([Piece.cs:231](Assets/Script/Gameplay/Piece.cs:231)), so `2` is indeed Black. This is the only
  ruleset in the project that names Black.

### Movement

- **Men** move one square diagonally forward only, never backward.
- **Flying kings** (`flyingKings: 1`): slide any number of empty squares along a diagonal in either
  direction, like a chess bishop.

### Capturing

- **Mandatory but never maximum.** Capture is compulsory, but path choice is entirely free — a
  1-piece capture may be played over an available 3-piece capture (`mustCaptureMaximum: 0`, with all
  three `prefer*` tiebreak flags also `0`, so `ApplyMandatoryCaptureTiers` returns every candidate
  untouched). Pool Checkers shares this only with American and Russian.
- **Men capture backward** (`menCaptureBackward: 1`).
- **Long-range king jumps**, landing on any empty square beyond the victim.
- **Delayed removal** (`deferCaptureRemoval: 1`): jumped pieces stay on the board, still blocking
  their squares, until the turn completes; the same piece can never be jumped twice. Load-bearing
  here, since kings fly.

### Kings (promotion)

- A man crowns **only if it finishes its turn** on the back row; a piece landing there mid-chain with
  a further legal capture keeps playing as a man
  (`midChainPromotionRule: 0` = `MidChainPromotionRule.DeferUntilChainEnds`).
- **This is now the sole mechanical difference from Russian.** Both share the 8×8 board, flying
  kings, backward-capturing men, free capture choice, and — pending the open Russian fix — delayed
  removal. Only the promotion rule genuinely separates them: Russian crowns mid-chain and continues
  with king powers (`ContinueAsKing`); Pool Checkers does not crown until the chain ends.

### Win conditions

1. **Total elimination** — all 12 enemy pieces captured.
2. **Total immobilization** — the opponent has zero legal moves on their turn.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree to a tie | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| Threefold repetition | Same layout, same side to move, three times | **Implemented** — `GameManager.CheckRepetitionDraw` |

`noProgressMoveLimit` is now `0` (disabled) here too, for the same reason as Spanish/Canadian/
International — it used to be 30, making Pool Checkers a fourth variant (after Italian/Spanish/
Canadian) whose draw behavior was an *addition* to the published rules rather than an approximation
of them; threefold repetition alone already guarantees the same safety without it.

**Caveats:** none known, and the documentation pass that expanded this section found no
discrepancies — every field matches, including board orientation. That pass also **independently
corroborated the second audit's `deferCaptureRemoval: 1` fix** (a fourth source agreeing captures are
delayed here), and, taken together with the same pass's Russian finding, resolved how these two
variants relate: both use delayed removal, so they agree rather than differ, and it is Russian's
asset that is still wrong. See Russian's caveats.

"Black moves first" is fixed — `PoolCheckersRules.asset` sets
`FirstMoveColor: Black` (same mechanism as Italian/Spanish/Canadian's `White`, just the other
color). The second independent audit (see that section near the end) also found and fixed a
removal-timing bug: `deferCaptureRemoval` was `0` (immediate removal), but three independent
sources (Wikipedia, boardgamecentral.com, gambiter.com) agree captured pieces "are not removed
until all jumps are completed" — real Pool Checkers is no-removal, despite otherwise resembling
Russian. `PoolCheckersRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text was
updated to match. (A later documentation pass found the "despite otherwise resembling Russian"
framing was probably backwards — Russian appears to use delayed removal as well, meaning the two
variants agree and it's Russian's setting that's still wrong. The fix applied here was right
regardless; only the stated reasoning was off. See Russian's caveats.)

## Turkish Checkers (Dama)

Natively **Türk Daması**, or just **Dama**. The one variant here that abandons diagonal movement
entirely: play runs along ranks and files (orthogonally) across **all 64 squares**, so square color
is decorative rather than structural.

### Board and setup

- 8×8 board, **every square used**, not just the dark ones (`piecesOnAllSquares: 1`).
- 16 pieces per side, in two full rows of 8 — the **2nd and 3rd rows** from each player's edge
  (`pieceRowsPerSide: 2` with `leaveBackRowEmpty: 1`).
- The **back row (King's Row) starts empty**, as do the two middle rows — four empty ranks in total
  at setup.
- **White moves first** (`firstMoveColor: 1`, fixed by the second independent audit).

### Movement

- **Orthogonal only** (`movementScheme: 1`) — no diagonals exist in this variant.
- **Men** move exactly one square forward, left, or right. Never backward.
- **The Dama (king)** slides any number of empty squares forward, backward, left, or right in a
  straight line — a chess rook (`flyingKings: 1`).

Both fall out of `MoveGenerator.GetMoveDirections`, which drops only the straight-backward direction
for a non-king; under the orthogonal direction set that leaves forward plus both sideways
([MoveGenerator.cs:52](Assets/Script/Gameplay/MoveGenerator.cs:52)).

### Capturing

- **Mandatory and maximum** — capture is compulsory, and among available paths the one taking the
  most pieces must be played (`mustCaptureMaximum: 1`, no further tiebreak tiers).
- **Jump mechanic:** hop orthogonally over an adjacent enemy piece into the empty square directly
  beyond it.
- **Long-range Dama captures:** a king glides across any number of empty squares to reach a distant
  victim and may land on **any** empty square beyond it, same flying machinery International uses.
- **Immediate removal ("sweep rule")** (`deferCaptureRemoval: 0`): captured pieces come off the
  board the instant they're jumped, which genuinely changes the board mid-turn and can open fresh
  squares that extend the same chain. Unlike American — where immediate vs. deferred removal is
  provably equivalent — this matters here, because flying kings make vacated squares reachable.
- **No 180-degree turns:** within one multi-jump, hop N+1 cannot be the exact reverse of hop N, since
  that would send the piece straight back through the square it just left
  (`forbidImmediateReversal: 1`, enforced at
  [MoveGenerator.cs:332](Assets/Script/Gameplay/MoveGenerator.cs:332)).
- **Men capturing backward:** the engine currently allows it; published rules say it should not.
  See the caveats below — this is an open discrepancy.

### Kings (promotion)

- A man reaching the opponent's back row is promoted to a **Dama**, gaining rook-like movement.
- **What happens mid-chain is the one genuinely unresolved question in this document.** The engine
  uses `midChainPromotionRule: 0` (`DeferUntilChainEnds`): a man landing on the back row with a
  further legal capture keeps playing as a man and only crowns if the chain ends there. Sources split
  three ways on whether that is right — see the caveats below for the full breakdown.

### Win conditions

1. **Total elimination** — all 16 enemy pieces captured.
2. **Total immobilization** — the opponent has no legal move on their turn.
3. **Total dominance (*fish-tail* / *kedi köşesi*)** — reducing the opponent to a single regular man
   while you still hold at least one Dama wins instantly, since a lone man can never escape a king.
   This one is real and implemented: `singleManLosesToKing: 1`, checked every turn transition by
   `GameManager.TryEndGameOnSingleManVsKing`, ending the match with reason
   `"reduced to a single man against a Dama"`. It's the only variant here that sets the flag.

### Draw conditions

| Rule | Real game | This engine |
|---|---|---|
| Mutual agreement | Both players agree neither can force a win | **Implemented** — `GameManager.OfferDraw`/`ReceiveDrawOffer`/`RespondToDrawOffer`. RPC/negotiation logic is done; the "Offer Draw" button and Accept/Decline popup prefab still need Editor wiring |
| 1 vs. 1 | Exactly one piece against one piece is an automatic draw | **Implemented** — `GameManager.TryEndGameOnOneVsOneDraw` (`OneVsOneIsDraw: true`), checked every turn transition right after the single-man-vs-Dama win check |

The generic no-progress rule used to fill in for the missing 1-vs-1 rule (`noProgressMoveLimit: 30`,
grinding a Dama-vs-Dama ending out to a draw eventually rather than immediately); with the real rule
now implemented and threefold repetition already guaranteeing termination for anything else, the
counter has been turned off (`noProgressMoveLimit: 0`) rather than kept running redundantly alongside
both. The asymmetry the old missing 1-vs-1 rule used to create is resolved the same way either way:
`TryEndGameOnOneVsOneDraw` runs *after* `TryEndGameOnSingleManVsKing`, so a lone man against a Dama
still ends instantly in a win (rule 3 above) rather than ever reaching this draw.

**Caveats:** this variant received the most correction passes in the original audit (setup bug,
mandatory capture, removal timing, the 180°-turn rule, and the single-man-vs-Dama win condition were
all found missing or wrong and then fixed), and the second independent audit still found that
confidence overstated. A later documentation pass found more:

- **Fixed:** `firstMoveColor` was unset, but three independent sources (Wikipedia, gambiter.com,
  mindsports.nl) agree White always moves first in Turkish draughts. Since piece color is
  player-selectable in offline modes, a player who picked Black used to still move first.
  `TurkishRules.asset` now sets `firstMoveColor: 1` (White).
- **Open — `menCaptureBackward` should almost certainly be `0`, not `1`.** The asset currently lets a
  man capture in all four orthogonal directions, and `GetCaptureDirections` duly returns the full
  direction set for any man when the flag is on
  ([MoveGenerator.cs:66](Assets/Script/Gameplay/MoveGenerator.cs:66)). But three sources — including
  the **FMJD's own Turkish draughts rules PDF**, i.e. the governing body — state men capture forward
  and sideways only, never backward. The fix is a one-liner (`menCaptureBackward: 0`), and the code
  already does the right thing with it: `GetCaptureDirections` falls through to `GetMoveDirections`,
  which for an orthogonal man yields exactly forward/left/right. Not applied, since this pass was
  scoped to documentation; the in-app `longDescription` (which explicitly advertises "capture in all
  four directions, including backward") would need updating alongside it.
- **Still unresolved, and now three-way — mid-chain promotion.** The setting is
  `midChainPromotionRule: 0` (`DeferUntilChainEnds`: keep capturing as a man, crown only if the chain
  ends on the back row). Sources split three ways rather than two:
  - `DeferUntilChainEnds` (current setting) — mindsports.nl, via a specific worked example. **One
    source.**
  - `ContinueAsKing` (crown immediately, keep capturing with king powers) — gambiter.com,
    draughts.github.io. **Two sources.**
  - `EndsTurnOnPromotion` (crown immediately, turn ends on the spot — American/Italian behavior) —
    the FMJD rules PDF and washburn.edu. **Two sources, one of them the governing body.**

  So the current setting is now the *least*-supported of the three options, and the best-credentialed
  single source points at a third answer neither earlier audit had considered. Still not something a
  code trace can settle, so it's left as-is and flagged — but if this is ever resolved by fiat, the
  FMJD reading is the one to reach for.

---

## Cross-cutting known limitations

Both issues that used to recur across several variants (rather than being fixed once per-variant)
are now fixed:

1. ~~**No-Removal live-play gap**~~ — **Fixed.** (International, Brazilian, Spanish, Canadian —
   everything with `DeferCaptureRemoval: true`.) Captured pieces used to be destroyed for real (and
   animate away) the instant each hop was played, instead of staying on the board as an obstacle
   until the whole turn ended. Now a captured piece is only marked (`Piece.MarkCaptured`) at hop
   time: it's flagged consumed, its button is disabled, and it shrinks to half-scale (DOTween
   `DOScale`) — but it keeps occupying its square (`GameplayController.occupancy`/`pieces` aren't
   cleared), so it still blocks a flying king's path and can't be captured a second time
   (`MoveGenerator.SearchCaptures`/`IsSafe` both skip a `Piece.IsCaptured` square). Once the whole
   capture chain ends, the existing `Player.DestroyPieceAt` RPC is re-sent for every piece marked
   that turn (tracked in a per-chain list) — since each is already flagged `IsCaptured`, this second
   call takes the real-destroy branch instead of marking it again, running board/list bookkeeping
   and the disappear animation right before the turn switches. No new RPC was needed: one existing
   RPC now branches on ruleset + already-marked state instead of always destroying immediately, so
   the next player's move generation always sees a fully "settled" board, exactly as strict
   no-removal rules require.

2. ~~**Turn order tied to piece color, and board-orientation cosmetics**~~ — **Fixed.** (Raised for
   Canadian, Pool Checkers, Italian, Spanish.) Several variants specify a fixed starting color
   ("White/Black always moves first") and/or a specific corner-square color convention; neither was
   modeled — this engine always started the turn with "player 1" regardless of chosen color, and
   board square coloring was one fixed global formula shared by every variant.

   Both are now driven by two new `RuleSetSO`/`IRuleSet` fields, each defaulting to "no change from
   previous behavior" so rulesets that don't need them are untouched. (At the time this fix shipped,
   American/International/Russian/Brazilian/Turkish were believed to be in that "untouched" group —
   the second independent audit below found Brazilian and Turkish actually do need `FirstMoveColor`
   set too; both have since been corrected, see that section. A later documentation pass found
   **International and Russian both need it as well** — White moves first in each — and those are
   still unfixed; see their caveats. American is the only variant now believed genuinely
   indifferent.)
   - `FirstMoveColor` (`PieceType`, default `None`): `GameManager.StartFirstTurn` now calls
     `DetermineFirstTurnPlayer`, which returns player 1 unchanged when this is `None`, or whichever
     seat currently holds the named color otherwise. Set to `White` on Italian/Spanish/Canadian and
     `Black` on Pool Checkers. Both players' `PieceType` are already resolved by the time this runs
     in every mode (`SetupLocalMatch`/`SpawnLocalPlayer` offline, `Player.Start`'s `OwnerActorNr`
     derivation online), so no extra network round-trip is needed - each client computes the same
     answer from state it already has locally.
   - `DarkSquareBottomRight` (`bool`, default `false`): `BoardGenerator.GenerateBoard` XORs this
     into its per-square coloring formula, flipping every square (not just the corner) when set, so
     the alternating pattern stays intact while landing the opposite color in the bottom-right
     corner. Set only on Italian (the only ruleset whose own description calls for a dark corner —
     Spanish/Pool Checkers/Canadian's own descriptions already match the engine's existing light-
     corner default, so they didn't need this flag, only `FirstMoveColor`).

     **Correction, from a later documentation pass — this flag does not do what the paragraph above
     claims.** It changes *square colors only*. Piece placement is hardcoded to the parity
     `(i + j) % 2 != 0` in `BoardGenerator.GeneratePieces`
     ([BoardGenerator.cs:128](Assets/Script/Gameplay/BoardGenerator.cs:128)) and never consults the
     flag, so the set of squares actually played on is the same for every diagonal ruleset. Setting
     the flag therefore recolors the board *underneath* unmoved pieces, which inverts the intended
     result rather than producing it:
     - For Italian on 8×8, piece squares (parity 1) evaluate to
       `isWhiteSquare = (false) != true = true` → the pieces end up sitting on the **light**-colored
       squares, when Italian plays on the dark ones.
     - The bottom-right corner (7,7) is parity 0, so it does turn dark as intended — but it is **not
       a playing square**, whereas Italian's *cantone* is by definition a playing square.

     So Italian currently renders as "pieces on light squares, with a dark unplayable corner," the
     photographic negative of its actual convention. Expressing either Italian's or Spanish's board
     properly requires flipping the **piece-placement parity**, not the square coloring — i.e. a
     change in `GeneratePieces`, with `GenerateBoard` following it rather than the reverse.

     **Severity: cosmetic only.** Checkers is symmetric under left-right reflection, and reflection
     maps one parity class onto the other, so every arrangement discussed here yields an isomorphic
     game — identical legal moves and outcomes under relabeling. Nothing here can produce a wrong
     result on the board; it only makes two variants look unlike themselves.

---

## Second independent audit

Everything above (including the two "Fixed" cross-cutting items just above) was produced by one
continuous editing/review pass, which mostly verified its own claims by re-reading its own code
changes. A separate, later pass deliberately did NOT trust that work — it re-derived each variant's
real rules from external sources (Wikipedia, mindsports.nl, the IDF's own rulebook,
boardgamecentral.com, gambiter.com, ludoteka.com, checkersonline.io) and re-traced the code
independently, specifically to catch anything the first pass got wrong or missed. It found six real
discrepancies plus one genuinely unresolved question, ranked by confidence below. All five rule
discrepancies (three "Confirmed", two "Likely") have since been fixed; only the AI-search gap and
the genuinely unresolved Turkish question remain open.

### Confirmed (multiple independent sources agree) — fixed

- **Pool Checkers: `deferCaptureRemoval` should be `1`, not `0`.** `PoolCheckersRules.asset` used to
  configure immediate removal (matching Russian). Three independent sources (Wikipedia,
  boardgamecentral.com, gambiter.com) all state captured pieces "are not removed until all jumps are
  completed" — real Pool Checkers is no-removal, like International/Brazilian/Spanish/Canadian. Since
  Pool Checkers has flying kings, immediate removal used to let a king's ray pass through a square
  vacated earlier in the same multi-jump turn, when it should stay blocked. **Fixed:**
  `PoolCheckersRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text (which used
  to say "Immediate Removal") was updated to describe the no-removal rule instead.

  **Since corroborated by a fourth source**, in the third pass. That pass also resolved the framing:
  this entry treated Pool Checkers as *differing* from Russian, but Russian appears to use delayed
  removal as well, so the two agree and it is Russian's asset that is still wrong. The fix applied
  here was correct; only the stated contrast was not. With removal timing settled the same way for
  both, mid-chain promotion is the sole mechanical difference left between the two variants.
- **Turkish: `firstMoveColor` was unset; should be `White`.** Three independent sources (Wikipedia,
  gambiter.com, mindsports.nl) agree White always moves first. Since piece color is player-selectable
  in offline modes (not tied to player number), a player who picked Black in a solo Turkish match used
  to still move first. **Fixed:** `TurkishRules.asset` now sets `firstMoveColor: 1` (White).
- **Brazilian: `firstMoveColor` was unset; should be `White` (the light pieces).** Wikipedia and
  checkersonline.io both state the light-piece player always opens. Same class of bug as Turkish's,
  above, and the same class of fix already applied to Italian/Spanish/Canadian/Pool Checkers.
  **Fixed:** `BrazilianRules.asset` now sets `firstMoveColor: 1` (White).

### Likely (single or moderate-confidence source) — fixed after a second opinion

- **Brazilian: `preferKingCaptures` was a spurious tiebreak.** It used to be set (adding a "most
  Kings captured" tier on top of mandatory-maximum), but the IDF's own rulebook states ties are
  broken purely on capture count, "regardless of quality" — i.e. no King-priority tier at all for
  Brazilian. This looked like Spanish's real tiebreak (which the audit *did* confirm as genuine for
  Spanish) leaking into Brazilian's configuration by copy/paste. Only one source was checked before
  fixing. **Fixed:** `BrazilianRules.asset` now sets `preferKingCaptures: 0`, and its in-app rules
  text (which used to describe a "King Priority" tier) was updated to match.

  **Since corroborated.** This was the weakest-evidenced of the fixes applied (one source, hence
  "Likely" rather than "Confirmed"). A later documentation pass independently described Brazilian's
  capture priority the same way — equal-length paths are freely chosen whether the mover is a king or
  a man, and a path taking more kings carries no precedence — confirming both `preferKingMover: 0`
  and `preferKingCaptures: 0`. This finding can now be treated as Confirmed rather than Likely.
- **Italian: `deferCaptureRemoval` needed to be `1`, not `0`.** mindsports.nl states a Dama Italiana
  multiple capture "must be completed before the captured pieces are removed from the board" — the
  same no-removal semantics as International/Brazilian/Spanish/Canadian, contradicting the previous
  immediate-removal setting. Only one source was found and corroborated before fixing (versus Pool
  Checkers' three), so this carried lower confidence than that finding despite the identical failure
  mode. **Fixed:** `ItalianRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text
  was updated to mention the no-removal rule.

  **Still single-sourced, but now known to be harmless either way.** A later documentation pass did
  not corroborate it (the rule description used was silent on removal timing), but did establish that
  the setting is *behaviorally inert* for Italian: with non-flying kings, the parity argument in
  American's section shows a captured square can never be a landing square, so immediate and deferred
  removal generate identical legal moves. Unlike Pool Checkers — where the identical-looking fix was
  load-bearing because kings fly — this one changes nothing on the board. Not worth chasing further
  sources for.

### Unresolved — sources disagree, not something a code trace can settle alone

- **Turkish's mid-chain promotion rule.** The current implementation (and this doc's description)
  has a promoting piece finish its capture chain as a man before crowning
  (`MidChainPromotionRule.DeferUntilChainEnds`, matching International/Pool Checkers) — this matches
  mindsports.nl's specific worked example. But two other sources (gambiter.com, draughts.github.io)
  describe Russian-style behavior instead: crown immediately and keep capturing with new king powers
  the same turn (`ContinueAsKing`). Genuinely conflicting testimony; flagged rather than guessed at.

  **Updated by a later documentation pass:** the split is three-way, not two-way. The FMJD's own
  Turkish draughts rules PDF (plus washburn.edu) describes a third behavior neither of the above —
  crown immediately *and end the turn on the spot*, i.e. `EndsTurnOnPromotion`, the American/Italian
  rule. That leaves the current setting supported by one source, `ContinueAsKing` by two, and
  `EndsTurnOnPromotion` by two including the sport's governing body. Still left as-is, but the
  FMJD reading now has the strongest claim. See Turkish's caveats for the full breakdown.

### Lower severity — doesn't affect legal play

- **Russian: the bot's minimax search (`BoardState.cs`) never models mid-chain promotion.** `AIRules`
  has no `MidChainPromotionRule` field, and the search's own mutate/recurse always copies the mover's
  pre-capture `IsKing` state unchanged through the rest of that search, so a hypothetical mid-search
  crowning never grants the extra flying-capture range a real king would have. `ApplyMove` only
  crowns once, at the very end of whichever sequence the search settled on. This degrades the bot's
  search quality specifically for Russian's `ContinueAsKing` rule (it can undervalue or miss lines
  that cross the promotion row mid-chain) but does **not** produce illegal moves on the actual board:
  live execution always crowns for real and re-queries legal continuations hop-by-hop, independent of
  whatever the search decided, for both human and bot turns.

---

## Open items

Everything still unresolved across all three passes, consolidated. Nothing here has been applied —
each earlier pass was scoped to its own findings, and the third pass was scoped to documentation.

### Rule discrepancies — one-line asset edits

| Variant | Field | Current | Should be | Confidence |
|---|---|---|---|---|
| International | `firstMoveColor` | *(unset)* | `1` (White) | High — FMJD rules; 5 sibling assets already carry this fix |
| Russian | `firstMoveColor` | *(unset)* | `1` (White) | High — same bug class, third instance |
| Turkish | `menCaptureBackward` | `1` | `0` | High — 3 sources incl. the FMJD rules PDF |

The first two are the same bug the second audit already fixed for Brazilian and Turkish; American is
now the only variant believed genuinely indifferent to starting color. The Turkish change also needs
its in-app `longDescription` updated, which currently advertises capturing "in all four directions,
including backward."

### Rule discrepancy — needs play-testing, not a blind flag flip

| Variant | Field | Current | Should be |
|---|---|---|---|
| Russian | `deferCaptureRemoval` | `0` | `1` |

Russian draughts uses delayed removal (the *Turkish strike* rule), corroborated indirectly by Pool
Checkers' matching setting. But applying it would create the **first pairing anywhere in the project
of `ContinueAsKing` with `deferCaptureRemoval: 1`** — mid-chain crowning while captured pieces still
occupy their squares. Nothing has exercised that combination, and Russian is also the variant where
the bot's unmodelled mid-chain promotion already bites hardest. Worth actually playing before
shipping.

### Genuinely unresolved — sources disagree

- **Turkish mid-chain promotion.** Three-way split: `DeferUntilChainEnds` (current, 1 source),
  `ContinueAsKing` (2 sources), `EndsTurnOnPromotion` (2 sources, including the FMJD). The current
  setting is the least-supported; the FMJD reading has the strongest claim if this is ever settled by
  fiat.

### Cosmetic — no effect on outcomes

- **`DarkSquareBottomRight` recolors squares without moving pieces.** Piece placement is hardcoded to
  parity `(i + j) % 2 != 0` in `GeneratePieces`, so the flag inverts Italian's board (pieces end up on
  light squares, with a dark *unplayable* corner) and cannot express Spanish's mirrored board at all.
  A correct fix flips piece-placement parity, with coloring following. Harmless to outcomes, since a
  mirrored checkers board is isomorphic.

### Draw rules implementation status

Everything below in this subsection used to describe an engine-wide gap ("draw conditions are barely
modeled anywhere"). A later implementation pass closed almost all of it — the per-variant "Draw
conditions" sections above are now current; this is the consolidated view.

**Implemented, code side done, all logic in `GameManager.cs`:**
- **Mutual agreement** — `OfferDraw()`/`ReceiveDrawOffer`/`RespondToDrawOffer`/`ReceiveDrawResponse`,
  for all 9 rulesets. The current-turn player offers (VsBot auto-declines locally, no real opponent
  to ask); the response reuses the existing `Draw(reason)` RPC to actually end the match, per this
  project's convention of piggybacking new sync behavior on an existing RPC rather than adding one.
- **Threefold repetition** — `CheckRepetitionDraw`, a cheap position hash (piece layout + side to
  move) checked every turn. Enabled (`ThreefoldRepetitionEnabled`) for all 9 rulesets except Italian,
  whose published rules don't call for it.
- **International/Canadian king-count draws** — `CheckMaterialDraw`, a move counter gated on exact
  material (3 Kings vs 1 King, or 2 Kings/King+man vs 1 King), reset whenever a capture changes that
  composition. A simplified stand-in in the same spirit as `noProgressMoveLimit` itself — a move
  counter tied to specific material, not a real forced-win judgment.
- **Turkish's 1-vs-1 draw** — `TryEndGameOnOneVsOneDraw`, checked every turn transition right after
  the existing single-man-vs-Dama win check (so that win still takes priority — see Turkish's Draw
  conditions above for why the ordering matters).

**Still open, by omission — needs Editor work, not more code:**
- The mutual-agreement offer's UI. `Assets/Script/UI/DrawOfferPage.cs` (the Accept/Decline popup) and
  `GamePage.OnOfferDrawButtonClick` exist and are wired to `GameManager.OfferDraw`, and
  `GamePageType.DrawOfferPage` is registered in code — but the actual `DrawOfferPage` prefab, its
  entry in `GamePageManager`'s `pageEntries` list, and an "Offer Draw" button on the `GamePage`
  prefab all still need to be created and wired in the Unity Editor before a player can actually
  trigger or see any of this in a build.

**Still open, by design — needs real endgame theory, not a counter:**
- **Italian's "no forceable win" rule.** Unlike every other draw rule above, this one asks a genuine
  endgame-theory question ("can either side engineer a finishing sequence from this exact material and
  position") that a move counter can't approximate the way `noProgressMoveLimit` approximates the
  other variants' move-limit rules. Deliberately left unimplemented rather than faked.

**Resolved by decision, not just code:** five variants' published rules have no generic move-limit
draw at all (International, Spanish, Canadian, Pool Checkers, Turkish) — the engine used to run
`noProgressMoveLimit` for them anyway, purely as a pragmatic safety valve (the same reasoning
`IRuleSet.cs` documents and `EngineBugs.md`'s M2 chose for a related question). Now that threefold
repetition is properly implemented for all five, that safety valve is provably redundant: a finite
board has finitely many positions, so a shuffle making no progress is *guaranteed* to eventually
repeat one three times on its own. `noProgressMoveLimit` is now `0` (disabled) on all five assets,
leaving threefold repetition (plus, where applicable, the material-specific rules) as the sole
automatic backstop — matching each variant's real published rules exactly, rather than adding
something they don't call for.

**Italian is the one deliberate exception.** It has neither a real move-limit rule nor threefold
repetition (only "no forceable win," which is out of scope above, and mutual agreement) — disabling
`noProgressMoveLimit` there as well would remove the *only* remaining automatic way a non-repeating
king shuffle can end, short of one player voluntarily offering a draw. `ItalianRules.asset` keeps
`noProgressMoveLimit: 40` for exactly this reason.

**American, Russian, and Brazilian are unaffected by any of this** — their `noProgressMoveLimit`
approximates a real, named rule in their own published rules (the 40-move, 25-move, and 20-move
rules respectively), not an engine addition, so it was never a candidate for disabling.

### AI-quality gap — no illegal moves

- **The bot's minimax search never models mid-chain promotion** (`AIRules` has no
  `MidChainPromotionRule` field). Costs the most in Russian, the only variant using `ContinueAsKing`.
