# Checkers Engine — Bug Audit

This document is the engine-side counterpart to [`CheckersRules.md`](CheckersRules.md). That file
tracks whether each ruleset's *asset field values* match the variant's published rules. This one
tracks whether the *code* that executes those rules — move generation, capture-chain execution,
turn/match lifecycle, AI, and networking — does what it's supposed to, independent of any specific
ruleset's field values.

**Methodology:** a fan-out of specialized agents each audited one ruleset (asset fields traced
through `MoveGenerator.cs` to confirm actual *behavior*, not just that a field "looks right") or one
engine dimension (move generation, AI/live parity, capture-chain execution, match lifecycle, board
setup, networking), reading the relevant files in full rather than sampling. Every raw finding was
then independently re-verified by a second, adversarially-skeptical pass whose default posture was
to refute it — checking for guards elsewhere, unreachable flag combinations, and whether the shipped
`.asset` files can actually exercise the claimed failure. 46 raw findings were produced; 43 survived
verification, 3 were refuted and are not included below.

This is a **static code review only** — no live Unity session, no executed repros. Every failure
scenario below is a hand-traced code path anchored to cited line numbers, not an observed bug report.
See [Coverage and limitations](#coverage-and-limitations) for what was explicitly out of scope.

## Executive summary

Move and capture generation itself (`MoveGenerator.cs`, mirrored in `BoardState.cs` for the bot) is
fundamentally sound — mandatory-capture enforcement, tier cascades, board setup, and win-condition
detection were traced field-by-field across all 9 rulesets and held up far more often than not. The
real defects cluster in three places instead:

1. **The capture-chain execution layer** in `Player.cs`, where a well-intentioned "destroy early"
   optimization and a missing turn-timer guard can corrupt board state or strand pieces permanently.
2. **Player-identity assumptions baked into `GameManager.cs`**, where a hardcoded
   Black=player1/White=player2 mapping — true only in Multiplayer — silently inverts outcomes and
   swaps UI in offline modes where color is randomly assigned.
3. **Cosmetic and documentation drift** — every ruleset's in-app rules text overpromises (none
   mention their own draw condition), and a couple of code comments describe behavior that no longer
   matches the code they're attached to.

The single most important fix is **C2** (early-destroy corrupting a live capture chain) — it can
silently hand a player an illegal extra capture in ordinary play, not just degrade AI quality or
text. Close behind is **C1** (the hardcoded color mapping), which inverts match outcomes in offline
play roughly half the time for any ruleset using `SingleManLosesToKing`, and swaps the pieces-left UI
for every offline match regardless of ruleset.

---

## Critical

### C1 — Turkish's instant-win check silently swaps winner and loser in offline play

**File:** [GameManager.cs:454-483](../Assets/Script/Gameplay/GameManager.cs) (`TryEndGameOnSingleManVsKing` / `GetSingleManVsKingWinner`)

**What it is:** The Turkish `singleManLosesToKing` instant win/loss check hardcodes Black=player 1,
White=player 2:

```csharp
int winner = GetSingleManVsKingWinner(gameplayController.blackPieces, gameplayController.whitePieces, 2);
if (winner == 0)
{
    winner = GetSingleManVsKingWinner(gameplayController.whitePieces, gameplayController.blackPieces, 1);
}
```

That mapping is only actually true in Multiplayer (`Player.cs:78`). In offline VsBot/VsPlayer, the
local player's color is assigned via `Random.Range(1,3)`, so player 1 is White in roughly half of
matches.

**Failure scenario:** Offline Turkish match; the random roll makes player 1 White. White is reduced
to a single uncrowned man while Black (player 2) still holds a Dama. `GetSingleManVsKingWinner`
returns `winner = 1` — the side that just *lost* the material race — and `GameManager.GameOver(1, ...)`
awards **the losing player** the win.

**Fix:** Resolve the winner from actual `PieceType` ownership per player (look up which `Player`
instance currently holds Black/White) rather than a fixed player-number-to-color mapping.

### C2 — Early-destroy optimization lets a flying king play an illegal extra capture

**Files:** [Player.cs:219-228](../Assets/Script/Gameplay/Player.cs) (early-destroy shortcut),
[Player.cs:257](../Assets/Script/Gameplay/Player.cs) (authoritative recheck),
[MoveGenerator.cs:553-557](../Assets/Script/Gameplay/MoveGenerator.cs) (`IsPassable`)

**What it is:** For `DeferCaptureRemoval` rulesets, a single (non-chained) capture is handled as a
special case meant purely to save an animation frame:

```csharp
if (hasDeleted && capturedThisChain.Count == 1 && ServiceLocator.Get<GameManager>().RuleSet.DeferCaptureRemoval)
{
    Piece movedPiece = ServiceLocator.Get<GameplayController>().pieces[block.Row_ID, block.Coloum_ID];
    if (!WouldChainContinue(movedPiece))
    {
        Piece justCapturedPiece = ServiceLocator.Get<GameplayController>().pieces[capturedPosition.row_ID, capturedPosition.col_ID];
        thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, capturedPosition.row_ID, capturedPosition.col_ID);
        capturedThisChain.Remove(justCapturedPiece);
    }
}
```

`WouldChainContinue` runs `CanPieceKill` **immediately**, before the captured piece's disappearance
animation or the 0.5s settle wait. If it says no further capture exists, the piece is destroyed for
real right then — `DestroyPieceAt` takes the true-destroy branch because `IsCaptured` is already set
from the first mark. But the chain's *actual*, authoritative continuation check runs 0.5 seconds
later, at line 257 (`canContinue = ... CanPieceKill(selectedPiece)`), against a board where that
square is now genuinely empty rather than still "captured but blocking." For a flying king, a square
that was closed a moment ago can now be an open lane.

**Concrete failure scenario (Brazilian):** Black king at (5,2); White men at (4,3) and (6,1); (7,0)
empty. The king captures the man at (4,3), landing on (3,4). At that instant, the man still sitting
at (6,1) blocks the ray, so `WouldChainContinue` finds no follow-up and the captured piece at (4,3)
is destroyed for real. 0.5 seconds later, `CanPieceKill` re-runs on a board where (4,3) is truly
empty — the ray (3,4) → (4,3) → (5,2) → (6,1) is now open, and the king is forced into a second,
rules-illegal capture. The player sees a bogus "DOUBLE KILL!" and ends the turn a piece ahead of
what the actual rules allow.

**Affected rulesets:** International, Brazilian, Spanish, Canadian, Pool Checkers — the 5 rulesets
combining `flyingKings:1` with `deferCaptureRemoval:1`. **Not reachable** for Italian (no flying
kings — see the parity argument in `CheckersRules.md`'s American section, which applies here too)
or for any man's capture (parity prevents a man's landing square from ever coinciding with a square
it could later need to fly back through).

**Fix:** Fold the early-destroy branch into the same end-of-chain sweep that already exists at
[Player.cs:282-288](../Assets/Script/Gameplay/Player.cs) instead of running it mid-turn — i.e. always
wait for the chain to genuinely end before performing a real `Destroy()`. Alternatively, re-run
`WouldChainContinue` against a board state where the piece is still logically "captured but
blocking" rather than physically removed.

---

## High

### H1 — Clicking a different piece mid-capture-chain silently abandons the mandatory continuation

**Files:** [HumanPlayer.cs:119-146](../Assets/Script/Gameplay/HumanPlayer.cs) (`OnHighlightedPieceClick`),
[Player.cs:116-117](../Assets/Script/Gameplay/Player.cs) (`movablePieces` populated once per turn)

`movablePieces` is snapshotted once at turn start and never refreshed while a chain is in progress.
`Piece.button.interactable` is never disabled for non-selected pieces mid-chain, and `Piece.OnClick`
only gates on whose turn it is. So clicking a different piece that was also in the original
`movablePieces` list calls `SelectPieceForNewMove`, silently dropping the in-progress chain instead
of going through `ContinueAfterKill`.

**Failure scenario:** A Russian turn (`mustCaptureMaximum:0`, so multiple independent captures can
be available). Pieces X and Y both have legal captures. The player starts capturing with X; while
X's continuation is highlighted, they click Y instead. X's chain is abandoned mid-way — an illegal
partial capture is left standing on the board — and Y's separate capture begins.

**Fix:** Track a "chain in progress" flag on `Player`/`HumanPlayer`; gate `OnHighlightedPieceClick`/
`Piece.OnClick` on it so only the currently-chaining piece (or its legal continuation targets) can be
clicked until the chain naturally ends.

### H2 — Turkish's "no 180° reversal" rule isn't enforced across real hop boundaries

**File:** [MoveGenerator.cs:300-336](../Assets/Script/Gameplay/MoveGenerator.cs)
(`GetLegalContinuations`, reversal check)

The no-immediate-reversal check only applies *within one* `FindCaptureSequences`/`SearchCaptures`
recursion call. `GetLegalContinuations` restarts that recursion fresh at the piece's new square with
`lastDirection = null` every time a real hop is actually played — so the direction of the hop that
was just played is never compared against the direction offered for the next hop.

**Failure scenario:** A Turkish Dama's first real hop moves left. `GetLegalContinuations` for the
*next* hop has no memory of that direction and can legally offer — or, if it's the only remaining
option, force — a capture straight back through the square the piece just vacated, exactly what the
rule forbids.

**Fix:** Thread the direction of the just-played hop into the `FindCaptureSequences`/`SearchCaptures`
call inside `GetLegalContinuations`, instead of resetting it to `null` on every real hop.

### H3 — A turn timeout mid-capture-chain permanently strands a captured piece

**Files:** [Player.cs:401-426](../Assets/Script/Gameplay/Player.cs) (`DestroyPieceAt`),
[Player.cs:282-288](../Assets/Script/Gameplay/Player.cs) (end-of-chain sweep),
[GameManager.cs:311-330](../Assets/Script/Gameplay/GameManager.cs) (`ChangeTurn`)

For `DeferCaptureRemoval` rulesets, a piece captured mid-chain is only truly destroyed by the
end-of-chain sweep — which only runs if the capture coroutine reaches the natural end of the chain.
If the turn timer expires while the human is deciding on the *next* forced hop, no coroutine is
running at all in that window. `ChangeTurn` (fired via `HandleTurnMissCount`) resets highlight state
but never touches `capturedThisChain`; the next call to `SelectPieceForNewMove` silently clears the
list, discarding the only reference to the stranded piece(s).

**Failure scenario:** Vs Player or Multiplayer (both ship `enableTurnTimer:1`), any of
International/Brazilian/Spanish/Canadian/Italian/Pool Checkers. The player starts a 3-hop capture,
takes too long deciding on hop 2, and the timer fires. The hop-1 victim stays on the board forever —
visually half-scaled, still occupying its square, still counted in `whitePieces`/`blackPieces` —
corrupting piece counts and permanently blocking that square for the rest of the match.

**Fix:** On timeout (or any turn change), run the same destroy sweep over any player's non-empty
`capturedThisChain` before clearing it.

### H4 — Remaining-piece-count helper has the same hardcoded color mapping as C1

**File:** [GameManager.cs:650-655](../Assets/Script/Gameplay/GameManager.cs) (`GetRemainingPieceCount`)

Same fixed Black=player1/White=player2 assumption as C1, applied to the pieces-left UI. Feeds
`InitPiecesLeft`/`UpdatePiecesLeft` (`GameManager.cs:217,418`) and `LocalPiecesLeft`/
`OpponentPiecesLeft` on every result screen (`GameManager.cs:518-519,550-551,613-614`);
`Piece.cs:110`'s live per-capture update has the identical hardcoded ordering.

**Failure scenario:** Any offline match where the random roll makes player 1 White — both players'
"pieces left" figures are swapped for the entire match, live and on the end screen. Unlike C1, this
fires on *every* affected match regardless of ruleset, not just Turkish's `SingleManLosesToKing`.

**Fix:** Same as C1 — resolve remaining counts from actual `PieceType` ownership.

### H5 — The turn timer keeps counting through a move's commit animation

**Files:** [TimerController.cs:40-59](../Assets/Script/Gameplay/TimerController.cs),
[Player.cs:182-307](../Assets/Script/Gameplay/Player.cs) (`HandlePieceMovementAndPieceDelete`)

The timer ticks continuously through a move's commit coroutine (0.5s+ per hop, longer for
multi-hops) with no pause and no turn-generation guard anywhere in that coroutine — unlike
`HumanPlayer.ShowHintRoutine` and `BotPlayer.PlayAITurn`, which both explicitly re-check for exactly
this "turn moved on while I was running" case.

**Failure scenario (VsPlayer/Multiplayer only — VsBot ships `enableTurnTimer:0`):** A player commits
a legal move with under 0.5s left on the clock. The timer hits zero and fires
`HandleTurnMissCount` → `ChangeTurn` before the move's own coroutine finishes and calls `SwitchTurn`.
Both act on the shared `currentTurn` with no ordering protection: the opponent's turn is silently
skipped, the player who moved in time is wrongly charged a miss, and turn state visibly bounces.

**Fix:** Have the commit coroutine check `IsMyTurn` (or a turn-generation token) immediately before
its final `SwitchTurn` call, and/or pause the timer the instant a legal move is committed rather than
only once `SwitchTurn` actually runs.

### H6 — Racing `ChangeTurn` RPCs can corrupt turn/miss-count/no-progress state

**File:** [GameManager.cs:264-330](../Assets/Script/Gameplay/GameManager.cs)
(`SwitchTurn`, `HandleTurnMissCount`, `ChangeTurn`)

`SwitchTurn` (fired locally by the mover) and `HandleTurnMissCount` (fired locally by the master
client) can both send a `ChangeTurn` RPC for the same transition, each built from its own possibly
stale local state. `ChangeTurn` carries no sequence number or idempotency check, so whichever RPC
lands second re-reads the already-advanced `currentTurn` and stomps the wrong player's
`TurnMissCount` — potentially reverting a legitimate no-progress reset.

**Failure scenario:** Multiplayer with the turn timer on. Player A captures at 14.8s of a 15s clock
(resetting `movesWithoutProgress` to 0) and fires `ChangeTurn` locally. Before that RPC arrives,
master client B's own timer independently crosses zero on stale local state and fires its own
`ChangeTurn` carrying B's outdated `movesWithoutProgress`. Whichever lands second corrupts the
other's counters.

**Fix:** Add a monotonic sequence number to `ChangeTurn`'s payload and ignore any RPC whose sequence
number doesn't match the expected next value.

### H7 — No gameplay RPC validates sender identity or turn authority

**Files:** [Player.cs:401-444](../Assets/Script/Gameplay/Player.cs)
(`DestroyPieceAt`, `CrownPieceAt`, `ReportChainLength`),
[GameManager.cs:311-536](../Assets/Script/Gameplay/GameManager.cs)
(`ChangeTurn`, `GameOver`, `Draw`)

Every board-mutating or match-ending RPC is a bare `[PunRPC]` with no check that the caller owns the
relevant `PhotonView`, is the player whose turn it currently is, or that the referenced move was
actually legal. The only gate anywhere is client-side UI (`Piece.OnClick`'s turn check) — trivially
bypassed by a modified client calling the RPC method directly.

**Failure scenario:** A cheating client calls `GameManager.GameOver(<attackerNumber>, "cheat")`
directly, or targets the opponent's `Player` PhotonView's `DestroyPieceAt`/`CrownPieceAt` with
arbitrary coordinates. Every other client accepts and applies it unconditionally.

**Fix:** Validate sender identity (`PhotonMessageInfo.Sender`) against the expected mover/owner and
re-derive legality server-authoritatively (or at minimum on the master client) before applying any
of these RPCs.

---

## Medium

### M1 — The bot discards its own searched capture sequence after the first hop

**File:** [BotPlayer.cs:61-100](../Assets/Script/Gameplay/BotPlayer.cs)
(`MakeMove`, `ContinueAfterKill`, `ChooseSafestOrFirst`)

`BotMinimax` scores whole multi-hop capture sequences, but `BotPlayer.MakeMove` only ever commits
`Landings[0]`/`Captured[0]` from the winning line. Every subsequent hop is independently re-derived
via `GetLegalContinuations` + `ChooseSafestOrFirst` — a first-safe-else-first heuristic with zero
knowledge of the sequence the search actually scored.

**Failure scenario:** Worst on American, Pool Checkers, and Russian (`mustCaptureMaximum:0` means no
length constraint keeps the heuristic honest) — the bot can finish a chain that captures strictly
fewer pieces than the line its own search evaluated, because `ChooseSafestOrFirst`'s adjacency-only
safety check has no concept of sequence length. On the other 6 rulesets (mandatory-maximum, but
incomplete tiebreak tiers), the divergence is a same-length but worse/different branch. Human hints
are unaffected — `HumanPlayer.ContinueAfterKill` hands hop 2+ back to the human rather than
auto-picking.

**Fix:** Store the full searched `CaptureSequence` on the bot's pending move and replay it
hop-by-hop, falling back to `ChooseSafestOrFirst` only if the stored sequence becomes invalid (e.g.
an external state change).

### M2 — No-progress draw counts plies, not move-pairs

**File:** [GameManager.cs:294-309](../Assets/Script/Gameplay/GameManager.cs) (`SwitchTurn`)

`movesWithoutProgress` increments once per **ply** (one call per completed move by either side, not
per move-pair), and resets only on a capture or promotion — never on a plain man advance. Combined
with each ruleset's `noProgressMoveLimit` (American/Italian 40, Canadian 35, Russian/Spanish/
Turkish/Pool Checkers 30, International/Brazilian 25), every variant's effective threshold is
roughly **half** its apparent "N-move" framing. Canadian, Pool Checkers, and Italian's published
rules define no move-limit draw at all — only mutual agreement/threefold repetition, neither of
which exists anywhere in the engine (see `CheckersRules.md`'s per-variant Draw sections and its
consolidated Open Items list).

**Failure scenario:** Any variant — both sides maneuver without a capture or promotion for the
threshold's half-count of moves-per-side (e.g. ~12–13 moves/side for International's 25-ply limit),
a routine occurrence in king endgames — and the match is silently drawn.

**Note:** `IRuleSet.cs:78-82` documents this as a deliberate simplified stand-in, and
`CheckersRules.md` already narrates the general discrepancy per-variant. This entry adds the specific
mechanism (ply-counting, not move-pair-counting) as a concrete, previously-undocumented detail.

**Fix:** Count move-pairs, not plies (increment once per two plies, or track per-color separately);
consider gating the draw on material (kings-only) for rulesets whose spec ties it to endgame
material; add a per-ruleset flag to disable the no-progress draw for rulesets that don't define one.

### M3 — `IsSafe` ignores `MenCaptureBackward`

**File:** [MoveGenerator.cs:522-526](../Assets/Script/Gameplay/MoveGenerator.cs)
(mirrored at `BoardState.cs:500-504`)

Both safety heuristics unconditionally discard any threat from an enemy man moving backward, without
checking `ruleSet.MenCaptureBackward` — even though `GetCaptureDirections`
([MoveGenerator.cs:66-73](../Assets/Script/Gameplay/MoveGenerator.cs)) correctly allows backward man
captures whenever that flag is set.

**Failure scenario:** Russian/International/Brazilian/Canadian/Turkish/Pool Checkers (all
`menCaptureBackward:1`) — a man is reported "safe" even though an enemy man can legally capture it
backward, so `EvaluatePiece` awards a protected bonus instead of a vulnerability penalty, and
`ChooseSafestOrFirst` picks continuations that look safe but lose the piece on reply.

**Fix:** In `IsSafe`, only skip a backward-moving enemy's threat when
`!ruleSet.MenCaptureBackward && !enemy.IsCrownedKing`.

### M4 — Russian's rules text contradicts the (already-known-wrong) removal setting

**File:** [RussianRules.asset:17](../Assets/Script/Gameplay/RuleSets/RussianRules.asset)

The player-facing text states "Captured pieces are removed from the board immediately, which can
open up new paths mid-turn" — accurately describing the engine's *current* `deferCaptureRemoval:0`
behavior (already flagged as wrong in `CheckersRules.md`), but the opposite of the published Russian
Turkish-strike rule. **Must be fixed in the same change as the `deferCaptureRemoval` field itself —
fixing one without the other leaves the text and the code newly disagreeing in the other direction.**

### M5 — Italian's cantone is dark but not a playing square (sharpened root cause)

**Files:** [BoardGenerator.cs:77](../Assets/Script/Gameplay/BoardGenerator.cs) (square coloring) vs.
[BoardGenerator.cs:128](../Assets/Script/Gameplay/BoardGenerator.cs) (piece placement)

This confirms and sharpens the orientation defect already recorded in `CheckersRules.md`:
`darkSquareBottomRight:1` flips only the *rendered* square color; `GeneratePieces` still hardcodes
`(i + j) % 2 != 0` for which squares are playable, completely unsynchronized with the color flip. The
concrete result: all 12+12 Italian pieces land on the **light**-rendered squares, and the dark
bottom-right cantone sits empty and unreachable for the entire game — not merely "cosmetically off,"
but the *opposite* square set from what a dark-square variant should use.

**Fix:** Make `GeneratePieces`'s parity check consult `ruleSet.DarkSquareBottomRight` the same way
`GenerateBoard`'s coloring formula does, so playing squares always match the dark-colored squares.

### M6 — `IsSafe`'s board-edge shortcut is wrong for orthogonal movement

**File:** [MoveGenerator.cs:484-488](../Assets/Script/Gameplay/MoveGenerator.cs)

`IsSafe` returns "always safe" for any piece sitting on a board edge — sound for diagonal movement,
where every diagonal direction changes both coordinates, but not for Turkish's orthogonal scheme,
where a piece on an edge *column* is still vulnerable *vertically* and vice versa.

**Failure scenario:** Turkish 8×8, black man at (3,0) — left edge, not top/bottom row — white piece
at (4,0) directly below with (2,0) empty. A legal vertical capture exists, but `IsSafe` returns true
immediately because `col == 0`, feeding a false-safe verdict into both `BotPlayer.ChooseSafestOrFirst`
and, via the identical `BoardState.IsSafe` mirror, every leaf of the minimax search.

**Fix:** Gate the edge shortcut on `ruleSet.MovementScheme == MovementScheme.Diagonal`, or check
per-direction board-edge validity instead of a blanket row/col==0/max shortcut.

### M7 — `GameOver`/`Draw` RPCs have no already-ended guard

**File:** [GameManager.cs:485-489](../Assets/Script/Gameplay/GameManager.cs) (`GameOver`),
[:532-536](../Assets/Script/Gameplay/GameManager.cs) (`Draw`),
[:631-648](../Assets/Script/Gameplay/GameManager.cs) (`PrepareGameOverVisuals`)

None of these check `gameState` before running; `PrepareGameOverVisuals` sets
`gameState = Ending` as a side effect but never gates on it already being so.

**Failure scenario:** Combined with H5/H6's timing races — a move that both hits the no-progress
limit and races a timeout can trigger both a `GameOver("out of time")` and a `Draw("no progress")`
RPC in close succession; every client applies both, showing two stacked or contradictory result
screens.

**Fix:** Check and short-circuit on `gameState == Ending` at the top of `GameOver`, `Draw`, and
`ShowVictoryByForfeit`.

---

## Low

### L1 — No-progress draw can pre-empt a simultaneous blockade win

[GameManager.cs:298-302](../Assets/Script/Gameplay/GameManager.cs). `SwitchTurn` fires the
no-progress `Draw` RPC and returns before `ChangeTurn`/`StartTurn` ever runs — the only place a
blockade win (`CanPlay() == false`) is detected. A blockading move is necessarily non-capturing —
exactly the kind of move that increments the no-progress counter — so a move that simultaneously
hits the limit *and* blockades the opponent is scored a draw instead of the win the rules give.
*Fix:* check the opponent's legal-move count before firing the no-progress draw, or reorder so
blockade detection runs first.

### L2 — `BoardPosition` boxing in the capture search

[MoveGenerator.cs:556](../Assets/Script/Gameplay/MoveGenerator.cs) (and `:365`). `BoardPosition` has
no `Equals`/`IEquatable<T>`, so `List<BoardPosition>.Contains` boxes via `ObjectEqualityComparer` on
every square scanned inside `IsPassable`, which runs whenever `DeferCaptureRemoval` is active — and
the search re-runs multiple times per interaction (`CanMove`, `CheckMovablePieces`,
`SetPiecePosition`). GC pressure only, not a correctness bug. *Fix:* implement
`IEquatable<BoardPosition>` with field comparison.

### L3 — Safety heuristic is blind to flying-king range

[MoveGenerator.cs:478-539](../Assets/Script/Gameplay/MoveGenerator.cs) (mirrored at
`BoardState.cs:466-516`). `IsSafe` only inspects the four adjacent squares and never consults
`ruleSet.FlyingKings`, so a king several squares away on an open diagonal is invisible to the
heuristic feeding `EvaluatePiece`'s protected/vulnerable scoring and `ChooseSafestOrFirst`. Affects
all 7 flying-king rulesets. Scoped to AI positional judgment only — legality generation is
unaffected. *Fix:* when `FlyingKings` is set, scan outward along each direction for the first
occupied square rather than only the adjacent one.

### L4 — `capturedThisChain` leak on the non-acting multiplayer client

[Player.cs:412-416](../Assets/Script/Gameplay/Player.cs). The RPC'd `DestroyPieceAt` appends to
`capturedThisChain` on every receiving client, but only the mover's own local coroutine (never
reached by the non-acting replica) clears it. Pure dead-reference retention for the match's
duration — never read elsewhere, no gameplay effect. *Fix:* low priority; clear the list on
`ChangeTurn` receipt for the non-owning replica if desired.

### L5 — `WouldChainContinue` drops `ContinueAsKing` crowning (latent)

[Player.cs:174-180](../Assets/Script/Gameplay/Player.cs). Evaluates `CanPieceKill` *before*
crowning, while the authoritative check crowns first — would wrongly conclude a chain ended when a
newly-crowned king could actually continue. **Not currently live:** the only shipped ruleset using
`ContinueAsKing` (Russian) also ships `deferCaptureRemoval:0`, so the gate that would trigger this
path ([Player.cs:219](../Assets/Script/Gameplay/Player.cs)) never executes for Russian today.
**Must be fixed in tandem with any future Russian `deferCaptureRemoval` correction** — see
`CheckersRules.md`'s Russian caveats, which already flag that combination as untested.

### L6 — Capture search never models mid-chain promotion (latent)

[MoveGenerator.cs:326](../Assets/Script/Gameplay/MoveGenerator.cs). Freezes the mover's king-status
for the whole recursive search depth. Inert today only because Russian (the sole `ContinueAsKing`
ruleset) ships `mustCaptureMaximum:0`, so the sequence-length comparisons this would corrupt never
run. Currently corrupts only `ChooseSafestOrFirst`'s safety evaluation and the minimax search's tree
quality for Russian — never move legality, since live hop-by-hop execution re-derives correctly.
*Fix:* if Russian's `mustCaptureMaximum` is ever set to `1`, this must be fixed first.

### L7 — Italian's `PreferEarlierKingCapture` tiebreak double-applies mid-chain

[MoveGenerator.cs:182-190](../Assets/Script/Gameplay/MoveGenerator.cs). `GetLegalContinuations`
recomputes `FirstKingCaptureIndex` relative only to the *remaining* hops; once a prefix has already
captured a King, the true whole-chain index is already fixed, so re-applying this filter at each hop
illegally eliminates an equally-legal continuation. Only reachable under Italian (the sole ruleset
setting `preferEarlierKingCapture:1`), and only when two branches tie on remaining length/king-count
but differ in the order of a further King capture. *Fix:* compute `FirstKingCaptureIndex` once at
chain start relative to the whole sequence; stop re-filtering on it once a King has already been
captured in the played prefix.

### L8 — `Prefer*` tiebreak tiers are wrongly gated behind `MustCaptureMaximum` (latent)

[MoveGenerator.cs:158](../Assets/Script/Gameplay/MoveGenerator.cs) (mirrored at
`BoardState.cs:331-333`). The single guard `if (candidates.Count==0 || !ruleSet.MustCaptureMaximum)
return candidates;` skips all three `Prefer*` tiers whenever `MustCaptureMaximum` is false, even
though they're documented as independently controlled. No shipped ruleset currently combines
`mustCaptureMaximum:0` with any `Prefer*` flag, so this is purely latent. *Fix:* give each `Prefer*`
tier its own independent gate rather than sharing `MustCaptureMaximum`'s.

### L9 — AI search never models Turkish's single-man-vs-king rule

[BotMinimax.cs:142](../Assets/Script/Gameplay/BotMinimax.cs). `Search`'s only terminal check is
"zero legal moves"; `AIRules`/`BoardState` carry no `SingleManLosesToKing` field, so the bot's
lookahead never recognizes `GameManager`'s per-turn instant win/loss as a terminal node. Turkish-only;
the bot may pass up an immediate winning capture in favor of a materially-larger but slower line, or
fail to foresee its own loss several plies out. *Fix:* add `SingleManLosesToKing` to `AIRules` and
check it as an additional terminal condition in `Search`.

---

## Cosmetic

### CS1 — No ruleset's rules text discloses its own draw condition

Every `Assets/Script/Gameplay/RuleSets/*.asset`'s `longDescription` field ends its "Win:" section
with only the two win conditions and never mentions a draw, even though
`GameManager.SwitchTurn` ([GameManager.cs:298-301](../Assets/Script/Gameplay/GameManager.cs)) can and
does end any match in a draw via `NoProgressMoveLimit`. Systemic across all 9 assets — fix once in
whatever generates/authors this shared text.

### CS2 — Italian's "No-Removal" text oversells a provably inert flag

[ItalianRules.asset:17](../Assets/Script/Gameplay/RuleSets/ItalianRules.asset). With
`flyingKings:0`, the same board-geometry parity argument documented for American in
`CheckersRules.md` makes `DeferCaptureRemoval` provably unable to affect legality for Italian — and
for single captures, `Player.cs:219-228` destroys the piece immediately rather than at turn end
anyway. Both halves of "stays on the board — still blocking its square — until your turn ends" are
false for Italian specifically, even though the setting itself is harmless.

### CS3 — `midChainPromotionRule` is unreachable in Italian

[ItalianRules.asset:31](../Assets/Script/Gameplay/RuleSets/ItalianRules.asset). With
`menCaptureBackward:0`, a man standing on its own promotion row (a board edge) never has a legal
*forward* capture available, so `EndsTurnOnPromotion` and `DeferUntilChainEnds` are behaviorally
identical for Italian — only `ContinueAsKing` (unused here) would ever differ. No functional impact;
Italian ships the redundant-but-harmless value.

### CS4 — Stale comment misattributes a tiebreak to Brazilian instead of Spanish

[IRuleSet.cs:44](../Assets/Script/Gameplay/RuleSets/IRuleSet.cs),
[MoveGenerator.cs:154-155](../Assets/Script/Gameplay/MoveGenerator.cs). Both comments cite
"Brazilian/Italian" as examples of the most-Kings-captured tiebreak, but
`BrazilianRules.asset:29` sets `preferKingCaptures: 0` (correctly, per the IDF rulebook — see
`CheckersRules.md`'s Brazilian section). Spanish is the actual second real-world example. Risk: a
future maintainer trusting the comment could wrongly "fix" Brazilian's asset. *Fix:* correct the
comment to cite Spanish.

### CS5 — Stale comment on `MarkCaptured` describes behavior that no longer holds for multi-hop chains

[Piece.cs:142-145](../Assets/Script/Gameplay/Piece.cs). Claims a chain's final hop never reaches the
half-scaled `MarkCaptured` state because the early-destroy path handles it — true only for genuine
single-hop captures (the `capturedThisChain.Count == 1` gate). Every hop of a 2+-hop chain, including
its last, does go through `MarkCaptured` until the end-of-chain sweep. The comment (and a reference
elsewhere in `Player.cs`) also names a method, `Player.FinalizeCapturedChain`, that doesn't exist —
the sweep is inline at [Player.cs:282-288](../Assets/Script/Gameplay/Player.cs). *Fix:* correct the
comment; no behavioral change needed.

---

## Verified correct — do not re-investigate

- **Board setup:** piece counts, row bands, and dark-square parity were hand-verified for all 9
  rulesets (12/20/30/16 pieces per side as appropriate) against `BoardGenerator.GeneratePieces`; no
  overlap or miscount in any shipped configuration.
- **Mandatory capture enforcement:** capture is compulsory at piece-selection (`CheckMovablePieces`)
  and at every mid-chain hop (`ContinueAfterKill` never offers a quiet move) across all rulesets.
- **Tier cascade correctness:** the length → PreferKingMover → PreferKingCaptures →
  PreferEarlierKingCapture cascade (`ApplyMandatoryCaptureTiers`) matches the documented priority
  order and each stage correctly narrows rather than resets the candidate set — except for the one
  mid-chain PreferEarlierKingCapture edge case (L7).
- **`DeferCaptureRemoval` blocking logic** (`IsPassable`/`IsCaptured`) is correct at the generator
  level for every ruleset that uses it — C2's defect is specifically in *when* the real destroy
  happens relative to the continuation check, not in the search's own modeling of blocking.
- **Win-condition detection** (`CanMove`/`CanPlay`/`StartTurn`) correctly unifies "captured
  everything" and "no legal move" across all rulesets; `HasQuietMove`'s adjacent-only check is
  provably sufficient even for flying kings (a blocked adjacent square blocks the whole ray).
- **Undo/rematch state reset:** `movesWithoutProgress`, capture/king/chain counters,
  `isCrownedKing`, and per-player fields are correctly zeroed or freshly reconstructed on rematch; no
  cross-match state leak found.
- **Direction/promotion-row consistency:** `ForwardDirection`, `GetMoveDirections`/
  `GetCaptureDirections`, and `IsPromotionRow` are internally consistent across both player seats and
  both movement schemes for every ruleset.
- **AI/live parity for move *legality*** (not move *choice*): `MoveGenerator.cs` and `BoardState.cs`
  were diffed field-by-field for every ruleset flag (flying kings, backward capture, king immunity,
  tiebreak tiers, forbid-reversal, defer-removal blocking) — structurally identical, with no path to
  an actually-illegal bot move on the live board. (Move *choice quality* is a separate matter — see
  M1, L3, L9.)
- **`firstMoveColor` resolution:** correctly resolves whichever seat holds the configured starting
  color, in both online and offline setup, for every ruleset that sets the field. (The 2 rulesets
  that omit the field entirely — International and Russian — are a separate, already-known asset gap
  tracked in `CheckersRules.md`, not an engine bug.)
- **Master-migration and disconnect handling:** turn-timer deadlines are computed identically and
  independently per client off synced Photon time; `OnPlayerLeftRoom` always resolves to a forfeit
  for the survivor, with no exploitable rejoin path found.

## Coverage and limitations

- **Static review only** — no Unity Editor session, no live play, no execution of any traced
  scenario. Every failure scenario in this document is a hand-traced code path anchored to cited
  line numbers, not an observed repro.
- **Not read/inspected:** scene and prefab wiring (Button interactable states set in the Editor,
  `BotAISettingsSO` weight tuning), `GamePageManager`/`GamePage` UI code beyond what was needed to
  confirm `longDescription` rendering, audio, `MatchmakingConnectionManager`/`OnlineModeHandler`
  beyond identity-assignment tracing, and the Photon SDK's own source (RPC ordering/delivery
  guarantees were assumed from documented `RpcTarget.All` semantics, not verified against the
  package).
- **Not evaluated:** AI strength/tuning quality (`EvaluatePiece` weight calibration) except where it
  produces a parity or terminal-state modeling defect (M1, L3, L9) — a heuristic being "weak" in
  general was out of scope unless it diverged from what the bot's own search computed.
- **Published-rule baseline:** variant specifications were taken as given by `CheckersRules.md` and
  the audit brief; exact federation-rule wording was not independently re-sourced beyond what that
  document already asserts.
- **Multiplayer/networking:** reasoning about RPC race conditions (H5, H6, M7) is based on static
  analysis of call sites and documented Photon semantics, not a live multi-client session with real
  network jitter.
- All 9 ruleset assets were each read in full at least once during this audit, and every
  `ruleSet.`/`RuleSet.` consumer was searched project-wide, so no field's consumer was inferred from
  its name alone.

## Suggested fix order

Not a commitment to fix — a priority ordering if/when this list is worked through:

1. **C1, C2** — both corrupt actual match outcomes in ordinary play, not just AI/UI quality.
2. **H7** — the only finding here that's a security/integrity gap (cheating), not just a bug.
3. **H1, H3** — both leave the board in a genuinely wrong, unrecoverable state for the rest of the
   match (an illegal partial capture standing, or a piece stranded forever).
4. **H4** — same root cause as C1; fixing C1's underlying color-resolution helper likely fixes this
   for free.
5. **H2, H5, H6, M7** — real but narrower-window bugs; worth a pass but not blocking.
6. **M1–M6, L1–L9, CS1–CS5** — quality/cosmetic backlog; pick up alongside related work (e.g. fix M4
   and CS2 whenever Russian's/Italian's known asset issues in `CheckersRules.md` are next touched).
