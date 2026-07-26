# Checkers Rule Variants

This document describes the 9 checkers/draughts variants implemented in this project, the specific
rules of each, and known caveats/limitations in how they're implemented. All 9 have been audited
individually against detailed, sourced rule descriptions and corrected in code. Everything below
was verified by reading and tracing the engine's code — **it has not been runtime-tested by
actually playing matches in Unity**, since that isn't possible in the environment these audits were
done in.

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
| Russian | 8×8 | 12 | Diagonal | Yes | Yes | No | — | Continue as King | Immediate |
| Brazilian | 8×8 | 12 | Diagonal | Yes | Yes | Yes | Most Kings captured | Deferred | No-removal |
| Italian | 8×8 | 12 | Diagonal | No | No | Yes | King mover → Most Kings → First King soonest | Ends turn | Immediate |
| Spanish | 8×8 | 12 | Diagonal | Yes | No | Yes | Most Kings captured | Deferred | No-removal |
| Canadian | 12×12 | 30 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Pool Checkers | 8×8 | 12 | Diagonal | Yes | Yes | No | — | Deferred | Immediate |
| Turkish (Dama) | 8×8 | 16 | Orthogonal | Yes | Yes | Yes | — | Deferred | Immediate |

"Deferred" promotion = a piece that reaches the back row mid-capture, with a further legal capture
still available, keeps playing as a man and only crowns once the chain truly ends there.
"No-removal" = captured pieces stay on the board (blocking, not recapturable) until the whole
capture turn finishes, rather than disappearing the instant they're jumped.

---

## American Checkers (English Draughts)

- 8×8 board, 32 dark squares only, 12 pieces/side on the first 3 rows.
- Men move and capture diagonally forward only; kings move/capture one square, forward or backward
  (no flying).
- Capturing is mandatory whenever available, but there's no "longest sequence" requirement — any
  legal capture may be played.
- A piece that reaches the back row mid-capture crowns immediately and its turn ends right there —
  it can't use new king powers (e.g. capturing backward) until the following turn.
- Win by elimination or by leaving the opponent with no legal moves.

**Caveats:** none known. Fully audited; two real bugs were found and fixed during that audit
(mandatory capture wasn't actually enforced over quiet moves, and mid-chain promotion was letting
the newly-crowned piece keep capturing with backward powers in the same turn).

## International Draughts

- 10×10 board, 20 pieces/side on the first 4 rows.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Kings are flying kings: slide any distance along an empty diagonal, and can capture from a
  distance, landing on any empty square behind the captured piece.
- Mandatory maximum capture: if multiple capture paths exist, the one taking the most pieces must
  be played, regardless of whether they're men or kings.
- No-removal: a captured piece stays on the board until the whole turn finishes; you can't jump the
  same piece twice.
- A piece only becomes a king if it *finishes* its turn on the back row — jumping through the back
  row mid-chain and landing elsewhere leaves it a man.
- Win by elimination or blockade.

**Caveats:**
- **No-removal is only fully correct for the pre-move lookahead, not live play.** The engine
  correctly computes the full "what capture chains are possible from here" search treating an
  earlier-in-the-same-chain capture as still blocking the board (this drives the first-hop
  highlighting and the mandatory-maximum-length comparison correctly). But live gameplay actually
  destroys each captured piece for real the instant you click through that hop, before the next
  hop's continuation is computed. In the overwhelming majority of positions this makes no practical
  difference, but in the narrow case where a later hop's flight path would need to cross the exact
  square of an earlier-this-turn capture, live play won't block it the way strict no-removal rules
  require. Fully closing this would mean deferring actual piece destruction (and its disappear
  animation) until the whole turn ends — a real change to the capture/animation pipeline, not a
  rule tweak. Not built; flagged for a future pass if wanted.

## Russian Checkers (Shashki)

- 8×8 board, 12 pieces/side on the first 3 rows.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Flying kings, same as International.
- Capturing is mandatory, but *not* maximum — any legal capture path may be chosen freely.
- Captured pieces are removed from the board immediately as they're jumped, which can open up new
  paths mid-turn.
- A piece that reaches the back row mid-capture crowns *immediately* and must continue capturing
  the same turn using its new king powers (most notably: a king's long-range flying capture, which
  a mere man never had — Russian men already capture backward, so that isn't the "new power" here).
- Win by elimination or blockade.

**Caveats:** none known.

## Brazilian Checkers

- Effectively International draughts rules played on an 8×8 board (12 pieces/side, 3 rows) instead
  of 10×10.
- Same movement/capture/flying-king/no-removal/deferred-promotion rules as International.
- Extra tiebreak on top of mandatory-maximum: if two capture paths tie on the number of pieces
  taken, whichever captures the most Kings must be played.
- Win by elimination or blockade.

**Caveats:** same No-Removal live-play limitation as International (see above) — inherited, not
variant-specific.

## Italian Checkers (Dama Italiana)

- 8×8 board, 32 dark squares, 12 pieces/side on the first 3 rows.
- Men move diagonally forward only, and can never capture backward.
- Kings do **not** fly — they move/capture exactly one square diagonally, forward or backward.
- **Immunity:** a man can never capture a King at all; only an opposing King may capture a King.
  This is checked both in actual capture-legality and in the AI's "is this piece safe" heuristic
  (an enemy man poses no threat to a King).
- Capturing is mandatory, with a strict 4-tier tiebreak cascade applied in order whenever multiple
  paths are available:
  1. **Maximum Quantity** — the path capturing the most pieces.
  2. **King Dominance** — among paths tied on quantity, a King's capture sequence beats a man's.
  3. **Target Kings** — among what's left, whichever captures the most Kings.
  4. **First Blood** — among what's still left, whichever captures its first King soonest in the
     sequence.
- A piece that reaches the back row mid-capture crowns immediately and its turn ends right there,
  same as American.
- Win by elimination or blockade.

**Caveats:**
- **Board orientation ("cantone") not modeled.** Italian rules require the bottom-right corner
  square (the *cantone*) to be dark for both players, on a board rotated 90° from the American
  convention. This engine's board coloring is one fixed global formula shared by every variant, not
  something `RuleSetSO` can override per-ruleset — it was never made configurable. Purely cosmetic,
  doesn't affect legality.
- **"White always moves first" not enforced.** Turn order in this engine is always "player 1"
  (whoever is in the local/bottom seat), independent of which piece color they picked — color and
  turn order aren't linked anywhere in `GameManager`/`GameSettingsManager`. Never implemented for
  any variant; would mean changing how the starting player is decided at match setup.

## Spanish Checkers (Damas Españolas)

- 8×8 board, 32 dark squares, 12 pieces/side on the first 3 rows.
- Men move diagonally forward only, and cannot move or capture backward under any circumstance.
- Flying kings, same as International.
- Mandatory maximum capture, with a "most Kings captured" tiebreak when tied on quantity (same
  mechanism as Brazilian, but without Italian's extra King-mover/First-Blood tiers).
- No-removal, same as International: captured pieces stay on the board until the turn ends.
- A piece only becomes a king if it finishes its turn on the back row — jumping through mid-chain
  doesn't promote it.
- Win by elimination or blockade.

**Caveats:**
- Same No-Removal live-play limitation as International/Brazilian (see above).
- Same "board orientation" and "White always moves first" open items as Italian (see above) —
  Spanish rules specify the board is rotated so the bottom-right square is *white*, and White moves
  first; neither is modeled.

## Canadian Checkers (Grand jeu de dames)

- 12×12 board, 72 dark squares, 30 pieces/side on the first 5 rows from each edge, leaving the
  middle two rows empty.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Flying kings, mandatory maximum capture (no further tiebreak beyond quantity), no-removal,
  deferred promotion — otherwise identical in mechanics to International, just scaled up to a much
  larger board.
- Win by elimination or blockade.

**Caveats:**
- Same No-Removal live-play limitation as International (see above).
- Same "White always moves first" open item as Italian/Spanish (see above).

## Pool Checkers

- 8×8 board, 32 dark squares, 12 pieces/side on the first 3 rows — same footprint as American.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Flying kings, same as International/Russian.
- Capturing is mandatory, but *not* maximum — any legal capture path may be chosen freely (like
  Russian).
- Captured pieces are removed immediately as they're jumped (same as Russian) — this is the one
  place Pool Checkers and Russian actually agree.
- Unlike Russian, a piece that reaches the back row mid-capture with a further legal capture
  available does **not** promote — it keeps playing as a man, only crowning if the chain truly ends
  on the back row. (This is the one place Pool Checkers explicitly differs from Russian.)
- Win by elimination or blockade.

**Caveats:**
- **"Black moves first" not enforced** — same open item as the "White always moves first" cases
  above, just the other color.
- No No-Removal caveat here, since Pool Checkers uses immediate removal, not deferred.

## Turkish Checkers (Dama)

- 8×8 board, but **all 64 squares are used**, not just the dark ones — color doesn't matter for
  movement.
- 16 pieces/side, filling the 2nd and 3rd rows from each player's edge; **the very back row (1st
  row) is left empty** at setup.
- **Orthogonal movement only** — no diagonals at all. Men move one square forward, left, or right
  (never backward); they may *capture* in all four orthogonal directions including backward.
- Captures are jumps over an adjacent enemy piece in a straight line (forward/left/right/backward
  for capturing), landing in the empty square directly behind it.
- **No 180-degree turn:** within a single multi-jump turn, a piece cannot immediately reverse
  direction — hop N+1 can't be the exact opposite direction of hop N, since that would fly back
  through the square it just left.
- Mandatory maximum capture.
- Captured pieces are removed immediately as they're jumped, which can open up new paths mid-turn.
- The Dama (king) slides any number of empty squares in a straight line (forward/backward/
  left/right, like a rook), and can fly across empty squares to jump a distant enemy piece, landing
  on any empty square behind it.
- A piece that reaches the back row mid-capture, with a further legal capture available, does not
  promote yet — same deferred-promotion behavior as International/Pool Checkers, just orthogonal.
- Win by elimination/blockade, **or instantly** if one player is reduced to exactly one regular
  (non-king) piece while the other still has at least one Dama.

**Caveats:** none known — this variant received the most correction passes (setup bug, mandatory
capture, removal timing, the 180°-turn rule, and the single-man-vs-Dama win condition were all
found missing or wrong and then fixed), and every rule in the description above has now been
explicitly confirmed against a detailed source and checked against the code.

---

## Cross-cutting known limitations

Two issues recur across several variants rather than being fixed once per-variant:

1. **No-Removal live-play gap** (International, Brazilian, Spanish, Canadian — everything with
   `DeferCaptureRemoval: true`). The pre-move *lookahead* computation is correct; live turn-by-turn
   execution isn't fully faithful to "captured pieces stay on the board until the turn ends,"
   because pieces are actually destroyed (and animate away) the instant each hop is played, not
   deferred to end-of-turn. Fixing this fully means changing when `Piece.Destroy()` actually runs
   and how its disappear animation is sequenced — a pipeline change, not a rules tweak.

2. **Turn order tied to piece color, and board-orientation cosmetics** (raised for Canadian, Pool
   Checkers, Italian, Spanish). Several variants specify a fixed starting color ("White/Black
   always moves first") and/or a specific corner-square color convention. Neither is modeled: this
   engine always starts the turn with "player 1" regardless of chosen color, and board square
   coloring is one fixed global formula shared by every variant. Both are match-setup/cosmetic
   concerns rather than move-generation rules, and addressing the turn-order piece would touch the
   online color-assignment flow, which hasn't been audited.

Neither of these has been requested to be fixed as of this writing — they're documented here so
they aren't silently forgotten.
