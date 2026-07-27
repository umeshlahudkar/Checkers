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
| Brazilian | 8×8 | 12 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Italian | 8×8 | 12 | Diagonal | No | No | Yes | King mover → Most Kings → First King soonest | Ends turn | No-removal |
| Spanish | 8×8 | 12 | Diagonal | Yes | No | Yes | Most Kings captured | Deferred | No-removal |
| Canadian | 12×12 | 30 | Diagonal | Yes | Yes | Yes | — | Deferred | No-removal |
| Pool Checkers | 8×8 | 12 | Diagonal | Yes | Yes | No | — | Deferred | No-removal |
| Turkish (Dama) | 8×8 | 16 | Orthogonal | Yes | Yes | Yes | — | Deferred§ | Immediate |

"Deferred" promotion = a piece that reaches the back row mid-capture, with a further legal capture
still available, keeps playing as a man and only crowns once the chain truly ends there.
"No-removal" = captured pieces stay on the board (blocking, not recapturable) until the whole
capture turn finishes, rather than disappearing the instant they're jumped.

§ — flagged by the second independent audit as genuinely unresolved (sources disagree on Turkish's
promotion timing). See "Second independent audit" near the end of this document for specifics and
for the five findings from that audit that have since been fixed (no longer marked here).

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

**Caveats:** none known. The no-removal live-play gap that used to be documented here was fixed: a
captured piece is now only marked captured and shrunk to half-scale at hop time
(`Piece.MarkCaptured`), keeping its square occupied — and therefore still blocking a flying king's
path — until the whole capture turn ends, at which point every piece marked this turn is actually
destroyed (`Player.DestroyPieceAt`, called a second time per piece from the end-of-chain sweep)
right before the turn switches. See the cross-cutting notes below for detail shared with
Brazilian/Spanish/Canadian.

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

**Caveats:** live-play mechanics confirmed correct. One AI-quality-only gap found in the second
independent audit (see that section near the end) — the bot's minimax search never models mid-chain
promotion, so a piece that would crown partway through a hypothetical search line is still evaluated
with man-only movement for the rest of that search. Doesn't produce illegal moves (live execution
crowns for real, hop-by-hop, independent of the search), just makes the bot's search slightly blind
to king-power lines that are specific to Russian's `ContinueAsKing` rule.

## Brazilian Checkers

- Effectively International draughts rules played on an 8×8 board (12 pieces/side, 3 rows) instead
  of 10×10.
- Same movement/capture/flying-king/no-removal/deferred-promotion rules as International.
- Mandatory maximum capture, with no further tiebreak beyond quantity: if multiple paths tie on the
  number of pieces taken, any of them may be played, regardless of whether the pieces are men or
  Kings.
- Win by elimination or blockade.

**Caveats:** none known. The no-removal live-play fix described under International applies here
too (inherited, not variant-specific). The second independent audit (see that section near the end)
found and fixed two issues not caught by the original per-variant pass:
- `firstMoveColor` was unset — real Brazilian draughts has White (the light pieces) always moving
  first, same class of rule as Italian/Spanish/Canadian's fix, just missed for this variant.
  `BrazilianRules.asset` now sets `firstMoveColor: 1` (White).
- `preferKingCaptures` was set, adding a spurious "most Kings captured" tiebreak (copied from
  Spanish/Italian) on top of mandatory-maximum — the IDF's own rulebook states Brazilian captures
  tie purely on quantity, "regardless of quality." `BrazilianRules.asset` now sets
  `preferKingCaptures: 0`, and its in-app rules text (which used to describe a "King Priority" tier)
  was updated to match.

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
- No-removal: a captured piece stays on the board until the whole turn finishes; you can't jump the
  same piece twice.
- A piece that reaches the back row mid-capture crowns immediately and its turn ends right there,
  same as American.
- Win by elimination or blockade.

**Caveats:** none known. All three open items below were fixed:
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

- 8×8 board, 32 dark squares, 12 pieces/side on the first 3 rows.
- Men move diagonally forward only, and cannot move or capture backward under any circumstance.
- Flying kings, same as International.
- Mandatory maximum capture, with a "most Kings captured" tiebreak when tied on quantity (same
  mechanism as Brazilian, but without Italian's extra King-mover/First-Blood tiers).
- No-removal, same as International: captured pieces stay on the board until the turn ends.
- A piece only becomes a king if it finishes its turn on the back row — jumping through mid-chain
  doesn't promote it.
- Win by elimination or blockade.

**Caveats:** none known. The no-removal live-play gap is fixed (see under International), and
"White always moves first" is fixed (see under Italian) — `SpanishRules.asset` sets
`FirstMoveColor: White`. Its own bottom-right corner convention is *white*, which was already the
engine's shared default, so `DarkSquareBottomRight` didn't need to be set here.

## Canadian Checkers (Grand jeu de dames)

- 12×12 board, 72 dark squares, 30 pieces/side on the first 5 rows from each edge, leaving the
  middle two rows empty.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Flying kings, mandatory maximum capture (no further tiebreak beyond quantity), no-removal,
  deferred promotion — otherwise identical in mechanics to International, just scaled up to a much
  larger board.
- Win by elimination or blockade.

**Caveats:** none known. The no-removal live-play gap is fixed (see under International), and
"White always moves first" is fixed (see under Italian) — `CanadianRules.asset` sets
`FirstMoveColor: White`.

## Pool Checkers

- 8×8 board, 32 dark squares, 12 pieces/side on the first 3 rows — same footprint as American.
- Men move diagonally forward only, but capture in any diagonal direction (backward included).
- Flying kings, same as International/Russian.
- Capturing is mandatory, but *not* maximum — any legal capture path may be chosen freely (like
  Russian).
- No-removal: a captured piece stays on the board until the whole turn finishes; you can't jump the
  same piece twice. (Originally implemented as immediate removal, matching Russian — corrected by
  the second independent audit below; unlike Russian, in real Pool Checkers this is deferred.)
- Unlike Russian, a piece that reaches the back row mid-capture with a further legal capture
  available does **not** promote — it keeps playing as a man, only crowning if the chain truly ends
  on the back row. (This is the one place Pool Checkers explicitly differs from Russian.)
- Win by elimination or blockade.

**Caveats:** none known. "Black moves first" is fixed — `PoolCheckersRules.asset` sets
`FirstMoveColor: Black` (same mechanism as Italian/Spanish/Canadian's `White`, just the other
color). The second independent audit (see that section near the end) also found and fixed a
removal-timing bug: `deferCaptureRemoval` was `0` (immediate removal), but three independent
sources (Wikipedia, boardgamecentral.com, gambiter.com) agree captured pieces "are not removed
until all jumps are completed" — real Pool Checkers is no-removal, despite otherwise resembling
Russian. `PoolCheckersRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text was
updated to match.

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

**Caveats:** this variant received the most correction passes in the original audit (setup bug,
mandatory capture, removal timing, the 180°-turn rule, and the single-man-vs-Dama win condition were
all found missing or wrong and then fixed) — but the second independent audit (see that section near
the end) found that confidence was overstated:
- **Fixed:** `firstMoveColor` was unset, but three independent sources (Wikipedia, gambiter.com,
  mindsports.nl) agree White always moves first in Turkish draughts. Since piece color is
  player-selectable in offline modes, a player who picked Black used to still move first.
  `TurkishRules.asset` now sets `firstMoveColor: 1` (White).
- **Still unresolved:** the mid-chain promotion rule above (`DeferUntilChainEnds`, matching
  International/Pool Checkers) is contradicted by two of the three sources checked (which describe
  Russian-style `ContinueAsKing` instead) and confirmed by only one (mindsports.nl, via a specific
  worked example). Genuinely unresolved — sources disagree, and this isn't something a code trace
  alone can settle. Left as-is.

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
   set too; both have since been corrected, see that section.)
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
- **Italian: `deferCaptureRemoval` needed to be `1`, not `0`.** mindsports.nl states a Dama Italiana
  multiple capture "must be completed before the captured pieces are removed from the board" — the
  same no-removal semantics as International/Brazilian/Spanish/Canadian, contradicting the previous
  immediate-removal setting. Only one source was found and corroborated before fixing (versus Pool
  Checkers' three), so this carried lower confidence than that finding despite the identical failure
  mode. **Fixed:** `ItalianRules.asset` now sets `deferCaptureRemoval: 1`, and its in-app rules text
  was updated to mention the no-removal rule.

### Unresolved — sources disagree, not something a code trace can settle alone

- **Turkish's mid-chain promotion rule.** The current implementation (and this doc's description)
  has a promoting piece finish its capture chain as a man before crowning
  (`MidChainPromotionRule.DeferUntilChainEnds`, matching International/Pool Checkers) — this matches
  mindsports.nl's specific worked example. But two other sources (gambiter.com, draughts.github.io)
  describe Russian-style behavior instead: crown immediately and keep capturing with new king powers
  the same turn (`ContinueAsKing`). Genuinely conflicting testimony; flagged rather than guessed at.

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
