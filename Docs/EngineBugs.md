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

**A correction after the audit:** two of its "confirmed" findings (originally labeled C1 and H4)
were retracted during implementation, after checking a detail the audit's own adversarial-verify
pass didn't chase down. See [Retracted findings](#retracted-findings) — worth reading before trusting
this document's "confirmed" label at face value elsewhere, since it shows the verify pass was not
airtight.

## Executive summary

Move and capture generation itself (`MoveGenerator.cs`, mirrored in `BoardState.cs` for the bot) is
fundamentally sound — mandatory-capture enforcement, tier cascades, board setup, and win-condition
detection were traced field-by-field across all 9 rulesets and held up far more often than not. The
real defects clustered in two places: **the capture-chain execution layer** in `Player.cs`
(board-state corruption and turn-lifecycle races), and **networking/RPC trust** (no sender or turn
authority validation anywhere). Both are now addressed — **every Critical and High finding in this
document is fixed or retracted**, leaving only Medium/Low/Cosmetic quality-and-documentation items
open. See [Status](#status) for the full rundown and for the honest caveats on what "fixed" does and
doesn't mean for a couple of these (particularly H5's residual network-latency window).

**C2** was the single most important finding — it could silently hand a player an illegal extra
capture in ordinary play, not just degrade AI quality or text — and is fixed. It was also the
audit's only surviving Critical: the other original Critical (C1, a claimed hardcoded color mapping)
and its companion High (H4) were both retracted during implementation, not fixed — see
[Retracted findings](#retracted-findings) below for why they were never real bugs.

The remaining six High findings (**H1, H2, H3, H5, H6, H7**) are now all fixed as well. Fixing them
surfaced one thing the original audit missed entirely: `Player.UpdateGrid` — the core RPC that
actually moves a piece on every client's board for *every* move, not just captures — had no sender
validation either, and wasn't on the audit's list of RPCs at all. It's covered now, alongside the six
RPCs the audit did find. See H7's section for the full list.

**All Medium findings are now resolved one way or another too.** Four are fixed (**M1, M3, M5, M6**);
one (**M4**) stays intentionally deferred, tied to the same untested Russian asset flag C2's
fix-order notes already flagged as needing play-testing rather than a blind change; and one
(**M2**) was put to an explicit choice rather than fixed by default, because it's a game-balance
question — how long every ruleset takes to reach a no-progress draw — dressed as a bug. The answer
was to leave it as the deliberate simplification `IRuleSet.cs` already documents it as. Implementing
M7 also produced a second correction of the same shape as H5's: the audit's own suggested fix
location for one of its three guards turned out to be structurally wrong once traced against its
actual caller, and the equivalent protection had to move up a level to where it would actually run.
See M7's section for what changed and why.

---

## Critical

### C2 — Early-destroy optimization lets a flying king play an illegal extra capture — Fixed

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

**Fix applied:** the early-destroy special case (the whole block quoted above, plus its only caller
`WouldChainContinue`) was deleted outright rather than patched. Every `DeferCaptureRemoval` capture
now stays merely marked through the entire settle wait — lone capture or chain hop alike — and is
only ever really destroyed by the pre-existing end-of-chain sweep, *after* the authoritative
`canContinue` check has already run. There is now exactly one code path that decides "is this
capture really over," instead of two that could disagree. Cosmetic cost: a lone capture no longer
gets a single clean destroy animation — it shows the same mark-then-sweep sequence a genuine
multi-kill already used. This also resolved CS5 (a stale comment describing the removed path) and a
dangling reference to a `Player.FinalizeCapturedChain` method that never existed.

---

## High

### H1 — Clicking a different piece mid-capture-chain silently abandons the mandatory continuation — Fixed

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

**Fix applied:** added `Player.IsChainInProgress` (`chainCaptureCount > 0`) and a guard at the top of
`HumanPlayer.OnHighlightedPieceClick`: while a chain is in progress, *any* piece click — including a
different piece, and including re-clicking `selectedPiece` itself — re-displays the same forced
continuation via `ContinueAfterKill` instead of falling through to `SelectPieceForNewMove`. The
same-piece re-click case turned out to matter too: `SelectPieceForNewMove` unconditionally clears
`capturedThisChain` without destroying what's in it, so without this guard a mid-chain re-click on
the correct piece would have leaked those pieces the same way abandoning the chain to a *different*
piece would have. One check covers both.

### H2 — Turkish's "no 180° reversal" rule isn't enforced across real hop boundaries — Fixed

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

**Fix applied:** `GetLegalContinuations` now takes a `(int dRow, int dCol)?` parameter and calls
`SearchCaptures` directly with it, instead of going through `FindCaptureSequences` (which always
starts fresh at `null`, correctly, for its other callers like `CanPieceKill`). `Player`'s abstract
`ContinueAfterKill` now takes the just-captured square's `BoardPosition` alongside the piece — both
`HumanPlayer` and `BotPlayer` derive the hop's unit direction from it (`Sign(current - captured)` on
each axis, which is always well-defined since a landing square can never coincide with the square it
jumped) and pass that through. Also closes the same gap for the bot: `BotPlayer` drives its own
capture chain through this exact code path, so it was equally exposed.

### H3 — A turn timeout mid-capture-chain permanently strands a captured piece — Fixed

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

**Fix applied:** added `Player.FlushIncompleteChain()` — runs the same real-destroy sweep the
end-of-chain path already uses over any leftover `capturedThisChain`, then resets it and
`chainCaptureCount` to zero. Called from the top of `GameManager.HandleTurnMissCount`, before either
branch (timeout-loss or turn-change) runs, so it applies regardless of which one fires; a no-op
whenever the timed-out player wasn't actually mid-chain (the common case). **This interacts with H1:**
once `IsChainInProgress` (added for H1) gates every piece click, a stale nonzero `chainCaptureCount`
left over from an abandoned chain would have permanently locked that player out of ever selecting a
new piece on their next turn — `FlushIncompleteChain` resetting it is what keeps that from happening.
The two fixes were implemented together specifically because of this interaction; landing H1 without
also landing H3 would have turned a piece-leak bug into a total-lockout bug.

### H5 — The turn timer keeps counting through a move's commit animation — Fixed (with a caveat)

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

**Fix applied, and why the simpler version of it wasn't enough:** `TimerController` gained
`PauseTimer`/`ResumeTimer`, freezing/resuming the countdown at its exact remaining value rather than
clearing it. The first version of this fix called them as plain local methods from
`HandlePieceMovementAndPieceDelete` — which turned out to only close the race when the mover and the
master client are the *same device*. Every client runs its own independent `TimerController`
instance off the shared `PhotonNetwork.Time` baseline, but it's specifically the **master's own copy**
that `HandleTurnMissCount`'s enforcement listens to (by design — see that method's existing comment
on why a backgroundable mover's device can't police its own clock). Pausing only the mover's local
instance does nothing to the master's separate, still-running one whenever they're different physical
devices — which is roughly half of real Multiplayer matches. (Offline VsPlayer pass-and-play is
unaffected either way, since mover and master are always the same single device there.)

The shipped fix broadcasts the pause/resume instead: `GameManager.PauseTurnTimer`/`ResumeTurnTimer`
send a `[PunRPC]` (`PauseTurnTimerRPC`/`ResumeTurnTimerRPC`, `RpcTarget.All`) that every client
applies to its own local `TimerController`, including whichever physical device is master. This
closes the race for both offline and true Multiplayer — with one honest caveat: it's a fire-and-forget
RPC, not a request/acknowledge round trip, so there's a residual window equal to actual network
latency (typically tens of milliseconds) between "mover commits a move" and "master's timer has
actually received the pause." The original bug's window was the *entire* commit animation
(500ms–1000ms+); this fix narrows it to roughly one network round-trip, which needed a genuine
architecture change (RPC broadcast, not a local call) to achieve — but doesn't claim to be a
mathematically zero window without a full request-acknowledge protocol, which would be a much larger
change for a residual race this narrow.

`TimerController.ResumeTimer` also had to guard against a subtler bug of its own: naively flipping
`isRunning` back to `true` unconditionally would have started the timer in VsBot (where
`enableTurnTimer:0` means it was never running to begin with). It now tracks
`wasPausedForCommit` — set only when `PauseTimer` actually froze a live countdown — so `ResumeTimer`
is a no-op unless there was a genuine pause to undo.

### H6 — Racing `ChangeTurn` RPCs can corrupt turn/miss-count/no-progress state — Fixed

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

**Fix applied:** added a `turnSequence` counter on `GameManager`, bumped every time `ChangeTurn`
actually applies. Both `SwitchTurn` and `HandleTurnMissCount` now capture the value they saw and send
it as an extra `expectedSequence` argument; `ChangeTurn` checks it first and returns immediately if it
no longer matches (meaning some other transition already applied first), rather than re-applying its
own now-stale view of `movesWithoutProgress`/miss counts on top. Reset to `0` in `ResetGameManager`
alongside the other match-lifetime counters, so it can't carry a stale value into a rematch.
Complements H5 rather than replacing it: H5 shrinks the race window that lets two `ChangeTurn`s fire
for the same transition in the first place; H6 makes sure that *if* it still happens (the residual
network-latency window H5's fix openly acknowledges), the second one can't corrupt state.

### H7 — No gameplay RPC validates sender identity or turn authority — Fixed

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

**One RPC the audit missed entirely:** re-checking every `[PunRPC]` in the project while implementing
this (rather than trusting the list above) turned up `Player.UpdateGrid` — the RPC that actually moves
a piece on every client's board for *every* move, capture or not. It wasn't in the audit's list, isn't
named anywhere in this finding, and had exactly the same missing-validation problem as the RPCs that
were found. Arguably the single most consequential one to have fixed, since it's the one that runs on
literally every move rather than only ones involving a capture or crowning.

**Fix applied:** added `GameManager.IsAuthorizedSender(Photon.Realtime.Player sender)`, and a
`if (!IsAuthorizedSender(info.Sender)) { return; }` guard at the top of **every** `[PunRPC]` method in
the project — all nine of them: `Player.DestroyPieceAt`, `CrownPieceAt`, `ReportChainLength`,
`UpdateGrid`, and `GameManager.ChangeTurn`, `GameOver`, `Draw`, `PauseTurnTimerRPC`,
`ResumeTurnTimerRPC` (the last two added by the H5 fix above, and just as exploitable as the rest —
a forged `PauseTurnTimerRPC` spam would let a cheating client freeze the opponent's clock
indefinitely). Confirmed by grepping `[PunRPC]` project-wide after the fact, which is how the
`UpdateGrid` gap surfaced in the first place.

`IsAuthorizedSender` treats exactly two sources as legitimate: the master client (the sole authority
for timeout/miss-driven actions, and for `Player.FlushIncompleteChain`'s H3 sweep, which the master
sends on a timed-out player's own `PhotonView` on that player's behalf), or the player whose turn it
currently is (the sole legitimate author of RPCs resulting from their own move). A `null` sender
(PUN's own convention for "this wasn't delivered as a real RPC") is treated as trusted, since that
only happens for a direct, non-RPC call — reachable only from code already running in this same
process, such as this file's own `[ContextMenu]` debug shortcuts (`ForceWin`/`ForceLose`/`ForceDraw`),
which call `GameOver`/`Draw` directly rather than through PUN and would otherwise have needed
rewiring. Every `[PunRPC]` method's trailing `PhotonMessageInfo info` parameter has a `= default` so
those direct calls keep compiling unchanged.

**What this does not do:** re-derive move legality. A cheating client can no longer forge an RPC as
someone else, but a client that *is* legitimately the current mover, calling `DestroyPieceAt`/
`UpdateGrid` with coordinates its own UI would never have produced, still gets it applied — closing
that fully would mean re-running `MoveGenerator` against the claimed move server-authoritatively (on
the master, or a real backend) before ever broadcasting the result, which is a materially bigger
architectural change than this fix and was judged out of proportion to what a single-issue fix should
take on. This is the one place in this document where "fixed" means "the specific vulnerability
described is closed," not "the whole class of RPC trust is now fully server-authoritative."

---

## Medium

### M1 — The bot discards its own searched capture sequence after the first hop — Fixed

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

**Fix applied:** `BotPlayer` now stores the search's chosen `CaptureSequence` (`pendingSequence`) and
a hop index into it (`pendingSequenceHopIndex`, starting at 1 since hop 0 is played immediately by
`MakeMove`) whenever it commits a capture; a quiet move clears it. A new
`ChoosePlannedOrSafestOrFirst` runs before every subsequent hop: if the plan still has a hop left,
it checks whether that hop's landing/captured squares match any of the currently-legal
continuations, and if so plays exactly that one — otherwise (plan exhausted, or no longer legal for
any reason) it clears the plan and falls back to the original `ChooseSafestOrFirst` heuristic, same
as before this fix. The validation step is defensive rather than strictly necessary given this
codebase's synchronous single-turn execution (nothing else can move mid-chain), but keeps a model/
reality divergence elsewhere in the AI (e.g. **L6**, mid-chain promotion not being modeled in the
search) from ever making the bot commit to a hop that isn't actually legal.

### M2 — No-progress draw counts plies, not move-pairs — Left as documented deliberate behavior

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

**Decision (asked explicitly, not assumed):** presented as a choice - fix the ply/move-pair
mismatch, leave it as the documented simplification, or add a narrower per-ruleset opt-out for the
3 variants with no move-limit draw at all. Chosen: leave it as-is. Rebalancing how long every one of
the 9 rulesets takes to reach a no-progress draw is a game-design tradeoff, not a pure correctness
bug - `IRuleSet.cs` already frames it as a deliberate simplification, so it wasn't treated as
something to fix by default the way the rest of this document's findings were. Recorded here so a
future pass doesn't have to re-derive the same reasoning, and so it's clear this was a considered
choice, not an oversight.

### M3 — `IsSafe` ignores `MenCaptureBackward` — Fixed

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

**Fix applied:** `IsSafe` now skips a backward-moving enemy's threat only when
`!ruleSet.MenCaptureBackward && !enemy.IsCrownedKing` — matching `GetCaptureDirections`'s own gate
exactly, so the safety heuristic and the actual capture-generation rule it's supposed to be modeling
finally agree. Fixed identically in both `MoveGenerator.IsSafe` and its `BoardState.IsSafe` mirror.

### M4 — Russian's rules text contradicts the (already-known-wrong) removal setting — Still deferred

**File:** [RussianRules.asset:17](../Assets/Script/Gameplay/RuleSets/RussianRules.asset)

The player-facing text states "Captured pieces are removed from the board immediately, which can
open up new paths mid-turn" — accurately describing the engine's *current* `deferCaptureRemoval:0`
behavior (already flagged as wrong in `CheckersRules.md`), but the opposite of the published Russian
Turkish-strike rule. **Must be fixed in the same change as the `deferCaptureRemoval` field itself —
fixing one without the other leaves the text and the code newly disagreeing in the other direction.**

**Status:** intentionally not touched, consistent with the earlier decision in `CheckersRules.md`
(and reaffirmed while fixing C2) to leave Russian's `deferCaptureRemoval` flag alone until it's
actually play-tested — it would be the first shipped pairing of `ContinueAsKing` with
`deferCaptureRemoval:1` anywhere in the project, an untested combination, not a safe one-line flip.
Since this text fix is explicitly scoped to land in the *same* change as that flag flip, it stays
deferred alongside it rather than being split off and fixed in isolation, which the finding itself
warns would just leave things wrong in the other direction.

### M5 — Italian's cantone is dark but not a playing square (sharpened root cause) — Fixed

**Files:** [BoardGenerator.cs:77](../Assets/Script/Gameplay/BoardGenerator.cs) (square coloring) vs.
[BoardGenerator.cs:128](../Assets/Script/Gameplay/BoardGenerator.cs) (piece placement)

This confirms and sharpens the orientation defect already recorded in `CheckersRules.md`:
`darkSquareBottomRight:1` flips only the *rendered* square color; `GeneratePieces` still hardcodes
`(i + j) % 2 != 0` for which squares are playable, completely unsynchronized with the color flip. The
concrete result: all 12+12 Italian pieces land on the **light**-rendered squares, and the dark
bottom-right cantone sits empty and unreachable for the entire game — not merely "cosmetically off,"
but the *opposite* square set from what a dark-square variant should use.

**Fix applied:** `GeneratePieces` now computes `isDarkSquare = ((i + j) % 2 == 0) == ruleSet.DarkSquareBottomRight`
— algebraically the negation of `GenerateBoard`'s own `isWhiteSquare` formula, so playing squares are
now provably always the dark-colored ones. Hand-verified for Italian: square (7,7) — the bottom-right
cantone — now evaluates `isDarkSquare = true`, matching its dark rendering, and since row 7 is one of
player 1's three piece rows, it correctly receives a piece. For all 8 other rulesets
(`DarkSquareBottomRight` is `false` everywhere else), the new formula algebraically reduces back to
exactly the original `(i + j) % 2 != 0` — verified unchanged, not just assumed so.

### M6 — `IsSafe`'s board-edge shortcut is wrong for orthogonal movement — Fixed

**File:** [MoveGenerator.cs:484-488](../Assets/Script/Gameplay/MoveGenerator.cs)

`IsSafe` returns "always safe" for any piece sitting on a board edge — sound for diagonal movement,
where every diagonal direction changes both coordinates, but not for Turkish's orthogonal scheme,
where a piece on an edge *column* is still vulnerable *vertically* and vice versa.

**Failure scenario:** Turkish 8×8, black man at (3,0) — left edge, not top/bottom row — white piece
at (4,0) directly below with (2,0) empty. A legal vertical capture exists, but `IsSafe` returns true
immediately because `col == 0`, feeding a false-safe verdict into both `BotPlayer.ChooseSafestOrFirst`
and, via the identical `BoardState.IsSafe` mirror, every leaf of the minimax search.

**Fix applied:** gated the shortcut on `ruleSet.MovementScheme == MovementScheme.Diagonal` in both
`MoveGenerator.IsSafe` and `BoardState.IsSafe`. For the 8 diagonal rulesets nothing changes (the
shortcut is provably correct there — any diagonal capture's landing square would fall off the
opposite edge). For Turkish specifically, an edge position now falls through to the existing
per-direction loop below, which already checks board-edge validity per direction and was already
correct on its own — the shortcut was purely a (wrong, for this one ruleset) optimization sitting in
front of already-correct logic.

### M7 — `GameOver`/`Draw` RPCs have no already-ended guard — Fixed

**File:** [GameManager.cs:485-489](../Assets/Script/Gameplay/GameManager.cs) (`GameOver`),
[:532-536](../Assets/Script/Gameplay/GameManager.cs) (`Draw`),
[:631-648](../Assets/Script/Gameplay/GameManager.cs) (`PrepareGameOverVisuals`)

None of these check `gameState` before running; `PrepareGameOverVisuals` sets
`gameState = Ending` as a side effect but never gates on it already being so.

**Failure scenario:** Combined with H5/H6's timing races — a move that both hits the no-progress
limit and races a timeout can trigger both a `GameOver("out of time")` and a `Draw("no progress")`
RPC in close succession; every client applies both, showing two stacked or contradictory result
screens.

**Fix applied, with a correction along the way:** `GameOver` and `Draw` both now short-circuit with
`if (gameState != GameState.Playing) { return; }` before starting their coroutines. `ShowVictoryByForfeit`
turned out to be the wrong place for the equivalent guard, despite that being exactly what the
finding's own suggested fix said to do: its caller,
`MatchSessionEventManager.PlayForfeitSequence`, already calls `PrepareGameOverVisuals()` — which sets
`gameState = Ending` as its first synchronous action — *immediately before* calling
`ShowVictoryByForfeit()`, every time, including on a completely legitimate first call. A guard inside
`ShowVictoryByForfeit` would see its own caller's transition having just happened and always reject,
so it would never actually run at all. The equivalent protection was moved one level up instead: added
to `MatchSessionEventManager.OnPlayerLeftRoom`'s existing `canOpenGameOverScreen` check (which
previously only checked whether a result *page* was already open), so a forfeit sequence now also
never starts if `GameManager.GameState` is no longer `Playing` — checked *before* `PrepareGameOverVisuals`
runs for the forfeit path, the same position in the sequence the other two guards occupy for theirs.

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

### L5 — `WouldChainContinue` drops `ContinueAsKing` crowning (latent) — Resolved (moot)

`Player.cs:174-180` originally. Evaluated `CanPieceKill` *before* crowning, while the authoritative
check crowns first — would have wrongly concluded a chain ended when a newly-crowned king could
actually continue. Flagged as **not currently live**: the only shipped ruleset using `ContinueAsKing`
(Russian) also ships `deferCaptureRemoval:0`, so the gate that would trigger this path
(`Player.cs:219`, the old early-destroy special case) never executed for Russian. The audit's own
recommendation was to fix it in tandem with any future Russian `deferCaptureRemoval` correction.

**Resolved as a side effect of C2, not by that recommended fix.** `WouldChainContinue` — this
finding's entire subject — was deleted outright when C2 removed the early-destroy special case that
was its only caller (see C2 above). There is no longer a method to have this bug in, for Russian or
any other ruleset, so nothing needs revisiting if/when Russian's `deferCaptureRemoval` is corrected.

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

### CS5 — Stale comment on `MarkCaptured` describes behavior that no longer holds for multi-hop chains — Fixed

[Piece.cs:142-145](../Assets/Script/Gameplay/Piece.cs). Claimed a chain's final hop never reaches the
half-scaled `MarkCaptured` state because the early-destroy path handles it — true only for genuine
single-hop captures (the `capturedThisChain.Count == 1` gate). Every hop of a 2+-hop chain, including
its last, went through `MarkCaptured` until the end-of-chain sweep. The comment (and a reference
elsewhere in `Player.cs`) also named a method, `Player.FinalizeCapturedChain`, that never existed —
the sweep was always inline.

**Fixed as a side effect of C2.** Removing the early-destroy path entirely (see C2 above) made the
comment's premise disappear rather than just its accuracy — every hop, including a lone capture, now
goes through `MarkCaptured` uniformly. Both this comment and the `FinalizeCapturedChain` references
in `Piece.cs` and `Player.cs` were rewritten to describe the current, simpler behavior.

---

## Retracted findings

Two of the audit's original "confirmed" findings — **C1** and **H4** — were retracted during
implementation. Recorded here rather than deleted, since the reason they're wrong is itself worth
preserving: it's exactly the kind of thing a future audit pass could re-discover and wrongly "fix"
again.

**Original claim (C1):** `GameManager.TryEndGameOnSingleManVsKing`/`GetSingleManVsKingWinner`
([GameManager.cs:454-483](../Assets/Script/Gameplay/GameManager.cs)) "hardcodes Black=player 1,
White=player 2" when computing Turkish's single-man-vs-king winner — claimed to invert the result in
offline modes, where `PvcModeHandler`/`PvpModeHandler` assign color to player 1 via
`Random.Range(1,3)` rather than always Black.

**Original claim (H4):** `GameManager.GetRemainingPieceCount`
([GameManager.cs:679-686](../Assets/Script/Gameplay/GameManager.cs)) has the identical hardcoded
mapping, swapping the pieces-left UI in the same offline scenario.

**Why both are wrong:** `GameplayController.whitePieces`/`blackPieces` are *not* partitioned by each
piece's actually-assigned color. Every site that populates or drains them —
`BoardGenerator.SpawnPiece` ([BoardGenerator.cs:165-181](../Assets/Script/Gameplay/BoardGenerator.cs)),
`Piece.Destroy` ([Piece.cs:96-114](../Assets/Script/Gameplay/Piece.cs)), and
`MoveGenerator.GetPiecesForPlayer` ([MoveGenerator.cs:75-79](../Assets/Script/Gameplay/MoveGenerator.cs))
— partitions purely on **player number** (`playerID==1` → `blackPieces`, `playerID==2` →
`whitePieces`), regardless of the `PieceType` those pieces were actually spawned with. `RestorePieceLayout`
([BoardGenerator.cs:214-228](../Assets/Script/Gameplay/BoardGenerator.cs)) preserves the same
convention on rematch. So despite their names, these two lists are really "player 1's pieces" and
"player 2's pieces" — and every consumer in the codebase, including the two flagged methods, already
treats them that way consistently.

Given that, the original code was already correct: `TryEndGameOnSingleManVsKing` pairs
`blackPieces` (≡ player 1's list) with player-number 1 and `whitePieces` (≡ player 2's list) with
player-number 2, which is exactly how those lists are always populated — independent of which color
either player is actually rendered as. The Turkish single-man-vs-king rule itself is also colorblind
(it only cares which *side* is reduced to one man, never which color that side displays), so there
was never a real mismatch for this rule to trip over. Same reasoning clears `GetRemainingPieceCount`.

**What actually happened:** a fix matching the audit's own recommendation — resolve each player's
piece list from `Player.PieceType` instead of a fixed player-number mapping — was implemented, and
then caught by re-checking `BoardGenerator`/`Piece.Destroy` before being trusted. That "fix" would
have decoupled the player-number label from the list's true (player-number-based) identity, and
introduced exactly the swapped-result bug the audit described — in the same offline scenario, in the
opposite direction. It was reverted before landing in this repository's history; both methods are
unchanged from their pre-audit state, now with a comment at each site explaining why the pairing is
correct, to head off a repeat of this exact false positive.

**Lesson for future audits of this codebase:** `whitePieces`/`blackPieces`'s names are misleading —
treat them as player-number buckets, not color buckets, and verify against `BoardGenerator.SpawnPiece`
before flagging anything that touches them as a color-mapping bug.

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
- **`GameplayController.whitePieces`/`blackPieces` are player-number buckets, not color buckets** —
  confirmed by checking every site that populates or drains them (`BoardGenerator.SpawnPiece`,
  `Piece.Destroy`, `MoveGenerator.GetPiecesForPlayer`, `RestorePieceLayout`), all of which key
  strictly on `playerID`, never on a piece's actual `PieceType`. See
  [Retracted findings](#retracted-findings) for why this makes `TryEndGameOnSingleManVsKing` and
  `GetRemainingPieceCount` already correct despite their misleading list names — don't re-flag either
  as a color-mapping bug without re-deriving this first.

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

Not a commitment to fix — a priority ordering for what's left. (C1 and H4 are omitted — see
[Retracted findings](#retracted-findings), nothing to fix there. C2, H1/H2/H3/H5/H6/H7, and
M1/M3/M5/M6/M7 are all fixed — see [Status](#status). M2 was resolved by explicit decision rather
than code; M4 stays intentionally deferred.)

1. **L1–L4, L6–L9, CS1–CS4** — the entire remaining backlog is Low/Cosmetic; none affect legality,
   security, or match-ending correctness. Pick up alongside related work (e.g. CS2 whenever Italian's
   known asset issue in `CheckersRules.md` is next touched).
2. **L5, CS5** — already done, no action needed; listed here only so they aren't mistaken for open
   items when skimming the Low/Cosmetic sections above.

## Status

- **C1, H4** — retracted, no fix needed; see [Retracted findings](#retracted-findings).
- **C2** — fixed. The early-destroy special case in `Player.HandlePieceMovementAndPieceDelete` was
  removed entirely; every `DeferCaptureRemoval` capture now stays merely marked through the whole
  settle wait and is only ever really destroyed by the existing end-of-chain sweep, uniformly for
  lone captures and multi-hop chains alike. The dead `WouldChainContinue` helper (the early-destroy
  path's only caller) was deleted along with it — which in turn resolved **L5** for free (its entire
  subject was that method; there's nothing left to have that bug). Also resolves **CS5** (the stale
  `MarkCaptured` comment describing the now-removed early-destroy path) and the dangling
  `FinalizeCapturedChain` references in both `Player.cs` and `Piece.cs` — that method never existed;
  the sweep was always inline. Cosmetic cost: a lone capture no longer gets a single clean destroy
  animation — it now shows the same mark-then-sweep sequence a genuine multi-kill already used.
- **H1** — fixed. See H1's section; a re-click on the correct piece needed the same guard as
  switching to a different one, for a reason that only became apparent while implementing it (see
  that section).
- **H2** — fixed. See H2's section; also closes the identical gap for the bot's own live capture
  execution, which shares the fixed code path.
- **H3** — fixed. See H3's section; implemented together with H1 because of a direct interaction
  between the two (a stale post-timeout state that H1's guard would otherwise have turned into a
  permanent input lockout).
- **H5** — fixed, with a residual network-latency window the section is explicit about rather than
  overclaiming as fully closed. See H5's section — the fix went through a real design correction
  mid-implementation (local pause → networked broadcast) once the master/mover distinction was
  worked through properly.
- **H6** — fixed. See H6's section; explicitly defense-in-depth for the residual window H5 leaves open,
  not a fix for a separate independent bug.
- **H7** — fixed for the specific vulnerability described (forged RPCs from an unauthorized sender),
  not for full move-relegitimization, which the section explains was judged too large a change for
  this fix. Also fixed one RPC (`Player.UpdateGrid`) the original audit never listed at all — found
  by grepping every `[PunRPC]` in the project during implementation rather than trusting the audit's
  enumeration. See H7's section for the full accounting and why that omission mattered more than the
  ones that were caught.
- **M1** — fixed. See M1's section; the bot now replays its own searched capture sequence hop-by-hop
  instead of re-deriving each hop independently, with a defensive fallback if the plan ever stops
  matching reality.
- **M2** — resolved by an explicit decision, not a code change: asked directly rather than assumed,
  given it's a game-balance question dressed as a bug. Left as the deliberate simplification
  `IRuleSet.cs` already documents. See M2's section.
- **M3, M6** — both fixed, in the same pass, since both live in the same `IsSafe` method (and its
  `BoardState.IsSafe` mirror) that this pass touched for two unrelated reasons. See their sections.
- **M4** — intentionally still deferred, for the same reason as Russian's `deferCaptureRemoval` flag
  itself (see C2's fix-order notes and `CheckersRules.md`'s Russian caveats) — this text fix is
  explicitly scoped to land in the same change as that flag, which hasn't been play-tested yet.
- **M5** — fixed. See M5's section; hand-verified against Italian's actual coordinates rather than
  just trusting the algebra, and confirmed the formula reduces to a no-op for the other 8 rulesets.
- **M7** — fixed, with the same kind of correction H5 needed: one of the three guards the finding
  asked for (`ShowVictoryByForfeit`'s) turned out to be structurally impossible where the finding
  said to put it, once traced against its actual caller. Moved to the correct location instead of
  leaving a guard that would silently never fire. See M7's section for the full trace.
- Everything Low/Cosmetic in this document is still open as of this writing.
