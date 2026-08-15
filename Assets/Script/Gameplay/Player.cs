using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public abstract class Player : MonoBehaviour
    {
        [SerializeField] private PhotonView thisPhotonView;
        [SerializeField] private int playerID;

        private PieceType pieceType;
        [SerializeField] private int turnMissCount;

        protected readonly List<Block> highlightedBlocks = new();
        protected readonly List<Block> nextToNexthighlightedBlocks = new();
        protected readonly List<Piece> movablePieces = new();
        protected Piece selectedPiece;

        // The square jumped over by the most recent hop actually played this move. Only meaningful
        // while a capture chain is in progress (see IsChainInProgress below) - a subclass's
        // ContinueAfterKill uses it to derive that hop's direction for ForbidImmediateReversal
        // rulesets (Turkish), and OnHighlightedPieceClick's mid-chain guard re-reads it to redisplay
        // the same continuation without this method needing to run again.
        protected BoardPosition lastCapturedPosition;

        // Counts captures across an entire move (including every hop of a capture chain) - reported
        // via ReportChainLength for longestChainCount tracking. Not reset between individual hops.
        // Reset to 0 by SelectPieceForNewMove (a fresh piece picked up), by
        // HandlePieceMovementAndPieceDelete's end-of-chain branch (the chain reaching its natural
        // end), and by ResetChainState (a timed-out mid-chain sweep, via GameManager.ChangeTurn) -
        // all three points where a chain can genuinely be over. Every one of them must actually run
        // this reset: skipping any of them leaves IsChainInProgress permanently true from that point
        // on, since OnHighlightedPieceClick's own mid-chain guard (below) is what would otherwise
        // call SelectPieceForNewMove - the reset and the only path back to it are mutually gated.
        private int chainCaptureCount;

        // True once selectedPiece has already played at least one hop of a capture chain this move -
        // capturing is mandatory, so once a chain has started, only that same piece may act again
        // until it naturally ends; nothing else on the board is a legal alternative in the meantime,
        // even a different piece that also had its own capture available at the start of the turn.
        // Same reset lifetime as chainCaptureCount itself (see its own comment) - not just
        // SelectPieceForNewMove. Public (not just protected) so GamePage.RefreshHintUndoButtons can
        // gate the Hint/Undo buttons on it too - both would corrupt this same chain state exactly
        // like an abandoned chain does if invoked mid-chain (see HumanPlayer.ShowHint's own guard).
        public bool IsChainInProgress => chainCaptureCount > 0;

        // DeferCaptureRemoval rulesets only: pieces captured so far this same capture-chain move,
        // merely marked (Piece.MarkCaptured) rather than actually destroyed, so they keep blocking
        // their square until the chain truly ends. Same reset lifetime as chainCaptureCount - swept
        // and really destroyed by the inline sweep at the end of HandlePieceMovementAndPieceDelete,
        // right before the turn is handed over.
        private readonly List<Piece> capturedThisChain = new();

        public PieceType PieceType { get { return pieceType; } }
        public int Player_ID { get { return playerID; } }
        public int TurnMissCount { get { return turnMissCount; } }

        public PhotonView PhotonView { get { return thisPhotonView; } }

        // Whether it's still actually this player's turn - checked after an async search (bot move
        // search, hint search) resolves, in case a timeout or some other flow already moved the turn
        // on while the search was still running in the background.
        protected bool IsMyTurn => ServiceLocator.Get<GameManager>().CurrentTurn == playerID;

        // Bumped by ResetHighlightedBlocks - every time the board's highlight state gets cleared for
        // a fresh selection (a manual piece click, a new hint request, ...). An async flow that reads
        // this before starting and compares it again once it resolves can tell whether anything else
        // changed the selection in the meantime, and bail instead of stomping on it.
        private int selectionGeneration;
        protected int SelectionGeneration => selectionGeneration;

        // Whether this Player object represents the local viewing client, as opposed to the
        // opponent. Player numbering alone only tells the two apart in offline modes (VsBot/VsPlayer
        // always spawn player 1 as the local side, see GameManager.SetupLocalMatch) - in Multiplayer,
        // numbering flips with master-client role, so PhotonView.IsMine is the only reliable check
        // there (mirrors the same branch in GameManager.PlayGameOverSequence).
        public bool IsLocalPlayer
        {
            get
            {
                return ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer
                    ? thisPhotonView.IsMine
                    : playerID == 1;
            }
        }

        private void Start()
        {
            // Checked via PhotonNetwork.OfflineMode rather than GameManager.GameMode - the latter
            // is only set once GameManager's own init coroutine runs, which can happen later than
            // this Start() (e.g. for a networked instantiate of the *other* player, whose event can
            // arrive at any time). OfflineMode is already correct before this scene even loads, so
            // it doesn't race with GameManager's own startup.
            if(!PhotonNetwork.OfflineMode)
            {
                playerID = thisPhotonView.OwnerActorNr;
                pieceType = ResolveOnlinePieceType(thisPhotonView.Owner);
                ServiceLocator.Get<GameManager>().ListPlayer(this);
                SetTurnMissCount(0);
            }
        }

        // Each seat's color comes from the owning Photon player's own "pieceType" custom property
        // (set from their ProfilePage preference before joining the room - see
        // MatchmakingConnectionManager.Connect/CreateRoom/JoinRandomRoom, which also guarantees the
        // two seated players always requested opposite colors), not from OwnerActorNr - actor numbers
        // only reflect room join order and carry no color preference at all. Falls back to the old
        // actor-number rule only if the property somehow hasn't replicated yet.
        private PieceType ResolveOnlinePieceType(Photon.Realtime.Player owner)
        {
            if (owner.CustomProperties.TryGetValue(GameConstants.PhotonPlayerProperties.PieceType, out object pieceTypeValue))
            {
                return (PieceType)(int)pieceTypeValue;
            }

            return (playerID == 1) ? PieceType.Black : PieceType.White;
        }

        public void SetPlayer(int playerNumber, PieceType pieceType)
        {
            this.playerID = playerNumber;
            this.pieceType = pieceType;
            SetTurnMissCount(0);
        }

        public void SetTurnMissCount(int value)
        {
            turnMissCount = value;
            ServiceLocator.Get<GamePageManager>().GamePage.UpdateMissIndicators(playerID, turnMissCount);
        }

        public void ResetPlayer()
        {
            ResetHighlightedBlocks();
            ResetNextToNextHighlightedBlock();
        }

        public bool CanPlay()
        {
            if(ServiceLocator.Get<MoveGenerator>().CanMove(playerID))
            {
                PlayTurn();
                return true;
            }

            return false;
        }

        private void PlayTurn()
        {
            movablePieces.Clear();
            ServiceLocator.Get<MoveGenerator>().CheckMovablePieces(playerID, movablePieces);

            if (movablePieces.Count > 0)
            {
                OnTurnReady();
            }
        }

        protected abstract void OnTurnReady();

        public void UpdateTurnMissCount()
        {
            SetTurnMissCount(turnMissCount + 1);
        }

        protected void ResetHighlightedBlocks()
        {
            selectionGeneration++;

            for (int i = 0; i < highlightedBlocks.Count; i++)
            {
                highlightedBlocks[i].ResetBlock();
            }
            highlightedBlocks.Clear();
        }

        protected void ResetNextToNextHighlightedBlock()
        {
            for (int i = 0; i < nextToNexthighlightedBlocks.Count; i++)
            {
                nextToNexthighlightedBlocks[i].IsNextToNextHighlighted = false;
            }
            nextToNexthighlightedBlocks.Clear();
        }

        public virtual void OnHighlightedPieceClick(Piece clickedPiece)
        {
        }

        protected void SelectPieceForNewMove(Piece piece)
        {
            selectedPiece = piece;
            chainCaptureCount = 0;
            capturedThisChain.Clear();
        }

        // Called by GameManager when this player's turn is forcibly ended by a timeout while a
        // DeferCaptureRemoval capture chain is mid-way through - i.e. between hops, with no
        // HandlePieceMovementAndPieceDelete coroutine left running to ever reach its own
        // end-of-chain sweep. Runs that same real-destroy sweep here instead, then resets the same
        // chain-tracking state SelectPieceForNewMove normally resets: without this, the marked
        // pieces would stay on the board (still blocking their squares, still counted in the
        // remaining-piece totals) forever.
        //
        // Only ever called locally by GameManager.HandleTurnMissCount, which only runs on the master
        // client - every client keeps its own independent Player instances, so this sweep (and the
        // capturedThisChain/chainCaptureCount reset baked into it) only ever actually touches the
        // master's own local copy of this seat, never a remote client's. That's fine for the
        // DestroyPieceAt RPCs above (broadcast to RpcTarget.All, so every client's board ends up
        // correct regardless of who sent them) but not for chainCaptureCount, which is never RPC'd
        // anywhere - only ResetChainState below, called from GameManager.ChangeTurn (which DOES run
        // identically on every client), actually reaches the timed-out player's own device.
        public void FlushIncompleteChain()
        {
            for (int i = 0; i < capturedThisChain.Count; i++)
            {
                Piece piece = capturedThisChain[i];
                thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, piece.Row_ID, piece.Coloum_ID);
            }
            ResetChainState();
        }

        // Clears this player's capture-chain bookkeeping without destroying anything - safe to call
        // on every client identically, unlike FlushIncompleteChain above (which sends RPCs and must
        // only ever run once, from whichever single client decided the sweep). Called from
        // GameManager.ChangeTurn for the outgoing player on every client: a remote client that never
        // ran FlushIncompleteChain itself (e.g. the timed-out player's own device, when it isn't also
        // the master) still receives every DestroyPieceAt broadcast that sweep sent - so its board is
        // correct - but its own local capturedThisChain/chainCaptureCount fields are untouched by
        // those broadcasts and need this separate, RPC-free reset to actually reach zero. Also a
        // harmless no-op after a normal completed move, since HandlePieceMovementAndPieceDelete's own
        // end-of-chain sweep already cleared both fields before ChangeTurn ever runs.
        public void ResetChainState()
        {
            capturedThisChain.Clear();
            chainCaptureCount = 0;
        }

        public void OnHighlightedTargetBlockClick(Block block)
        {
            StartCoroutine(HandlePieceMovementAndPieceDelete(block));
        }

        private IEnumerator HandlePieceMovementAndPieceDelete(Block block)
        {
            // A legal move is being committed from this instant on - freeze the turn timer for the
            // whole commit animation (the settle waits below) so it can never independently declare
            // a timeout for a turn that has, in fact, already been decided in time. Resumed right
            // before ContinueAfterKill if a further hop is forced, or left paused if the turn is
            // ending here (StartTurn's own ResetTimer/StartTimer takes over for whoever moves next).
            // Broadcast rather than a direct local call - see GameManager.PauseTurnTimer for why the
            // master's own timer instance needs this too, not just the mover's.
            ServiceLocator.Get<GameManager>().PauseTurnTimer();

            ServiceLocator.Get<GameplayController>().ClearHintHighlight();
            ServiceLocator.Get<GamePageManager>().GamePage.SetHintUndoInteractable(false);
            ResetHighlightedBlocks();

            bool hasDeleted = false;

            if (block.IsNextToNextHighlighted)
            {
                // Read the captured piece's position from the block rather than deriving it
                // geometrically from the landing square - a flying king can capture from any
                // distance along the diagonal, so the two aren't a fixed offset apart. Kept as a
                // field, not a local, so a mid-chain re-click that re-displays this same
                // continuation (see OnHighlightedPieceClick's IsChainInProgress guard) can still
                // derive the same ForbidImmediateReversal direction without this method re-running.
                lastCapturedPosition = block.CapturedPosition;

                // DestroyPieceAt itself decides whether this is a real destroy or (for no-removal
                // rulesets) just a mark-as-captured - see there.
                thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, lastCapturedPosition.row_ID, lastCapturedPosition.col_ID);
                hasDeleted = true;
                chainCaptureCount++;
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece, hasDeleted);

            // A DeferCaptureRemoval capture always stays merely marked (Piece.MarkCaptured) - still
            // occupying its square, still blocking a flying king's path - through this whole settle
            // wait, uniformly whether this turns out to be a lone capture or one hop of a longer
            // chain. It used to be possible to destroy a lone capture for real immediately, right
            // here, as a purely cosmetic optimisation (skipping the mark-and-pulse animation for the
            // common single-capture case) - but that ran its own "is this the chain's last hop"
            // check against a board where the piece was still blocking, then genuinely removed it
            // before the authoritative canContinue check below ever ran. For a flying king, a square
            // that was closed a moment ago can become an open ray the instant that early destroy
            // fires, letting canContinue find (and force) a further capture the no-removal rule
            // should still have blocked. Always waiting for the authoritative check - and only ever
            // destroying via the end-of-chain sweep below - closes that gap; the cost is that a lone
            // capture no longer gets a single clean destroy animation, only the same mark-then-sweep
            // sequence a real multi-kill already used.
            yield return new WaitForSeconds(0.5f);

            selectedPiece = ServiceLocator.Get<GameplayController>().pieces[block.Row_ID, block.Coloum_ID];

            IRuleSet ruleSet = ServiceLocator.Get<GameManager>().RuleSet;
            bool reachedPromotionRow = !selectedPiece.IsCrownedKing && ruleSet.IsPromotionRow(selectedPiece.Row_ID, selectedPiece.Player_ID);

            // Russian-style: crown immediately, so the continuation check just below already sees
            // the piece as a king (e.g. picks up flying-capture range a mere man never had) and
            // must use those new powers this same turn.
            bool promotesImmediately = reachedPromotionRow && ruleSet.MidChainPromotionRule == MidChainPromotionRule.ContinueAsKing;
            if (promotesImmediately)
            {
                thisPhotonView.RPC(nameof(CrownPieceAt), RpcTarget.All, selectedPiece.Row_ID, selectedPiece.Coloum_ID);
            }

            // American/Italian-style: the turn ends the instant a piece reaches the promotion row,
            // even if the newly-crowned piece could otherwise keep capturing (e.g. backward) - so
            // no continuation is offered at all, regardless of what CanPieceKill would say.
            bool blocksContinuation = reachedPromotionRow && ruleSet.MidChainPromotionRule == MidChainPromotionRule.EndsTurnOnPromotion;

            // Otherwise (DeferUntilChainEnds, or not on the promotion row at all): checked with the
            // piece's CURRENT move set - not-yet-promoted unless promotesImmediately already
            // crowned it above - a man with a further legal capture from this square (some
            // rulesets let men capture backward) must keep playing that capture as a man even
            // though it's standing on the back row; it only actually crowns once the chain truly
            // has nowhere further to go from here.
            // Must respect the same ForbidImmediateReversal restriction ContinueAfterKill's own
            // GetLegalContinuations call below (see HumanPlayer/BotPlayer) enforces - CanPieceKill
            // finds captures in every direction with no such restriction, so on a
            // ForbidImmediateReversal ruleset (Turkish) it can say "yes, keep going" for a piece
            // whose only remaining capture is the reversal hop GetLegalContinuations then correctly
            // refuses, handing ContinueAfterKill an empty sequence list it isn't prepared for.
            (int dRow, int dCol) lastDirection = (
                System.Math.Sign(selectedPiece.Row_ID - lastCapturedPosition.row_ID),
                System.Math.Sign(selectedPiece.Coloum_ID - lastCapturedPosition.col_ID));
            bool canContinue = !blocksContinuation && hasDeleted
                && ServiceLocator.Get<MoveGenerator>().GetLegalContinuations(selectedPiece, lastDirection).Count > 0;

            if (canContinue)
            {
                // Control is handing back to the player (human) or immediately back into another
                // commit (bot, which will pause it again right at that coroutine's own top) - either
                // way this is the point the paused countdown should start running again, resuming
                // from wherever it was frozen rather than granting a fresh 15 seconds.
                ServiceLocator.Get<GameManager>().ResumeTurnTimer();
                ContinueAfterKill(selectedPiece, lastCapturedPosition);
            }
            else
            {
                bool justPromoted = promotesImmediately;
                if (!justPromoted && reachedPromotionRow)
                {
                    thisPhotonView.RPC(nameof(CrownPieceAt), RpcTarget.All, selectedPiece.Row_ID, selectedPiece.Coloum_ID);
                    justPromoted = true;
                }

                // The chain is over (this is reached exactly once per whole move, whether it took
                // one hop or many) - now's the single correct point to really destroy every piece
                // that was only marked-captured along the way, before the opponent's turn starts.
                // Re-running DestroyPieceAt for each one is what actually destroys it this time,
                // since IsCaptured is already true from when it was first marked.
                bool hadDeferredCaptures = capturedThisChain.Count > 0;
                for (int i = 0; i < capturedThisChain.Count; i++)
                {
                    Piece piece = capturedThisChain[i];
                    thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, piece.Row_ID, piece.Coloum_ID);
                }
                capturedThisChain.Clear();

                if (chainCaptureCount > 0)
                {
                    thisPhotonView.RPC(nameof(ReportChainLength), RpcTarget.All, chainCaptureCount);
                }

                // The move (the whole capture chain, if any) has now fully ended - reset so this
                // player's *next* turn isn't wrongly treated as still mid-chain. Without this,
                // IsChainInProgress stays true forever after this player's first-ever capturing
                // move, and OnHighlightedPieceClick's mid-chain guard would then refuse to call
                // SelectPieceForNewMove on every subsequent click for the rest of the match -
                // every click just re-displays the same stale, already-finished continuation.
                chainCaptureCount = 0;

                // Lets the deferred pieces' final disappear animation actually finish before the
                // turn visibly switches, instead of both landing in the same frame - only relevant
                // for DeferCaptureRemoval rulesets, since everyone else already destroyed their
                // captures back at hop time.
                if (hadDeferredCaptures)
                {
                    yield return new WaitForSeconds(Piece.DisappearDuration);
                }

                ServiceLocator.Get<GameManager>().SwitchTurn(hasDeleted || justPromoted);
                ResetNextToNextHighlightedBlock();
            }
        }

        // lastCapturedPosition is the square jumped over by the hop that was just actually played -
        // the caller derives that hop's direction from it (relative to selectedPiece's now-current,
        // post-hop position) so ForbidImmediateReversal rulesets (Turkish) can forbid the very next
        // hop from reversing straight back through it. Passed as a position rather than a direction
        // because a flying king's hop can cover more than one square, but the captured square and
        // the landing square are always exactly one direction apart regardless of that distance.
        protected abstract void ContinueAfterKill(Piece selectedPiece, BoardPosition lastCapturedPosition);

        public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove, bool isCapture)
        {
            int sourceRow = -1;
            int sourceCol = -1;
            if (pieceToMove != null)
            {
                sourceRow = pieceToMove.Row_ID;
                sourceCol = pieceToMove.Coloum_ID;
            }
            thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, sourceRow, sourceCol, isCapture);
        }

        [PunRPC]
        public void UpdateGrid(int targetRow, int targetCol, int sourceRow, int sourceCol, bool isCapture, PhotonMessageInfo info = default)
        {
            if (!ServiceLocator.Get<GameManager>().IsAuthorizedSender(info.Sender)) { return; }

            Piece piece = null;
            bool hasPiece = sourceRow != -1;
            if (hasPiece)
            {
                GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
                Block sourceBlock = gameplayController.board[sourceRow, sourceCol];
                Block targetBlock = gameplayController.board[targetRow, targetCol];

                piece = gameplayController.pieces[sourceRow, sourceCol];
                gameplayController.SetSquare(sourceRow, sourceCol, null);

                MovePiece(piece, targetBlock, isCapture);
                ServiceLocator.Get<AudioManager>().PlayPieceMoveSound();

                // Highlights only the opponent's move, and only for as long as the piece is actually
                // in flight: green on the square it's leaving while it slides, then swapped to the
                // square it lands on once the move finishes (see
                // GameplayController.ShowLastMoveInProgress). A move by our own side clears it instead
                // - it's no longer "the opponent's last move" once we've moved.
                //
                // VsPlayer (pass-and-play) has no single "local" side - both players share the same
                // device/view, so every move should show its own highlight rather than only ever
                // showing player 2's (IsLocalPlayer's playerID == 1 check would otherwise always
                // treat player 1 as "us" and clear instead of show for their moves).
                if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.VsPlayer)
                {
                    gameplayController.ClearLastMoveHighlight();
                    gameplayController.ShowLastMoveInProgress(sourceRow, sourceCol, targetRow, targetCol, GetMoveDuration(sourceBlock, targetBlock));
                }
                else if (IsLocalPlayer)
                {
                    gameplayController.ClearLastMoveHighlight();
                }
                else
                {
                    gameplayController.ShowLastMoveInProgress(sourceRow, sourceCol, targetRow, targetCol, GetMoveDuration(sourceBlock, targetBlock));
                }
            }
            ServiceLocator.Get<GameplayController>().SetSquare(targetRow, targetCol, piece);
        }

        // NOTE: these must be public. PUN resolves RPCs by reflecting the concrete component type
        // (HumanPlayer/BotPlayer) at runtime, and .NET's Type.GetMethods() does NOT surface a
        // *private* method declared on a base type when called on a derived type - so a private
        // [PunRPC] on this base Player class is invisible to the dispatcher and fails with
        // "RPC method not found". Public (or protected) inherited methods are returned normally.
        // Captured piece's sibling index at the moment it's destroyed, stashed for MovePiece's
        // sibling-order check right after (see there for why). Piece.Destroy() only clears the
        // board's reference to it and starts its fade-out - the transform itself (and its sibling
        // index) still exists until the fade completes, so reading it here is safe.
        private int lastCapturedPieceSiblingIndex = -1;

        // For DeferCaptureRemoval rulesets (International/Brazilian/Spanish/Canadian), a captured
        // piece must stay on the board - blocking its square, uncapturable again - until the whole
        // chain ends, rather than vanishing the instant it's jumped. The first hit on a given piece
        // this turn only marks it (Piece.MarkCaptured); the real destroy is deferred to a second
        // call, made from the end-of-chain sweep in HandlePieceMovementAndPieceDelete once
        // IsCaptured is already true. Every other ruleset always takes the real-destroy branch
        // immediately, exactly as before this fix.
        [PunRPC]
        public void DestroyPieceAt(int row, int col, PhotonMessageInfo info = default)
        {
            if (!ServiceLocator.Get<GameManager>().IsAuthorizedSender(info.Sender)) { return; }

            Piece capturedPiece = ServiceLocator.Get<GameplayController>().pieces[row, col];
            lastCapturedPieceSiblingIndex = capturedPiece.ThisTransform.GetSiblingIndex();

            // Computed before either branch mutates IsCaptured, so a DeferCaptureRemoval piece -
            // hit once here to mark it, then again from the end-of-chain sweep to actually destroy
            // it - only counts toward captureCount on that first call.
            bool isNewCapture = !capturedPiece.IsCaptured;

            if (ServiceLocator.Get<GameManager>().RuleSet.DeferCaptureRemoval && !capturedPiece.IsCaptured)
            {
                capturedPiece.MarkCaptured();
                capturedThisChain.Add(capturedPiece);
            }
            else
            {
                capturedPiece.Destroy();
            }

            if (isNewCapture)
            {
                ServiceLocator.Get<GameManager>().RegisterCapture(Player_ID);
            }
        }

        [PunRPC]
        public void CrownPieceAt(int row, int col, PhotonMessageInfo info = default)
        {
            if (!ServiceLocator.Get<GameManager>().IsAuthorizedSender(info.Sender)) { return; }

            ServiceLocator.Get<GameplayController>().pieces[row, col].SetCrownKing();
            ServiceLocator.Get<GameManager>().RegisterKingCrowned(Player_ID);
        }

        // Reports how many captures this whole turn's chain ended up with, so GameManager can track
        // each player's longest chain for the result screen. A PunRPC (like DestroyPieceAt/
        // CrownPieceAt above) rather than a direct GameManager call, since HandlePieceMovementAndPieceDelete
        // only runs on the client whose turn it is - every other client's GameManager needs this
        // relayed the same way it already gets board-state changes.
        [PunRPC]
        public void ReportChainLength(int chainLength, PhotonMessageInfo info = default)
        {
            if (!ServiceLocator.Get<GameManager>().IsAuthorizedSender(info.Sender)) { return; }
            ServiceLocator.Get<GameManager>().RegisterChainLength(Player_ID, chainLength);
        }

        // True for a single non-flying step in either scheme: a one-square diagonal move has both
        // deltas at 1, a one-square orthogonal move has one delta at 1 and the other at 0 - either
        // way the largest delta is exactly 1. Anything a flying king covers over a longer distance
        // has a larger delta on at least one axis.
        private bool AreAdjecent(Block b1, Block b2)
        {
            int rowDelta = Mathf.Abs(b1.Row_ID - b2.Row_ID);
            int colDelta = Mathf.Abs(b1.Coloum_ID - b2.Coloum_ID);
            return Mathf.Max(rowDelta, colDelta) == 1;
        }

        private float GetMoveDuration(Block fromBlock, Block toBlock)
        {
            return AreAdjecent(fromBlock, toBlock) ? 0.24f : 0.36f;
        }

        private void MovePiece(Piece pieceToMove, Block targetBlock, bool isCapture)
        {
            Block pieceBlock = ServiceLocator.Get<GameplayController>().board[pieceToMove.Row_ID, pieceToMove.Coloum_ID];
            float duration = GetMoveDuration(pieceBlock, targetBlock);

            // Kills any leftover shake (idle nudge / invalid-click feedback) so it can't fight over
            // anchoredPosition with the move that's about to start.
            pieceToMove.ThisTransform.DOKill();

            // Bring the moving piece above the piece it's jumping over, if it isn't already. Sibling
            // order is otherwise frozen at spawn time (all of player2/White's pieces are instantiated
            // before player1/Black's - see BoardGenerator.GeneratePieces), which is why a capture only
            // looked right when Black happened to be the one capturing. Only needed on a capture hop -
            // a plain move's path is never occupied by another piece (that would make it a capture,
            // not a plain move), so there's nothing to render above. Comparing against the specific
            // captured piece's index (rather than just moving to absolute-last) skips the reorder
            // whenever the mover already draws above it, even if some unrelated piece is currently last.
            if (isCapture && pieceToMove.ThisTransform.GetSiblingIndex() < lastCapturedPieceSiblingIndex)
            {
                pieceToMove.ThisTransform.SetAsLastSibling();
            }

            // Ease.OutBack overshoots slightly past the target before settling back into place - a
            // small bounce instead of a flat slide-and-stop.
            pieceToMove.ThisTransform.DOAnchorPos(targetBlock.ThisTransform.anchoredPosition, duration).SetEase(Ease.OutBack);
        }
    }
}

public struct BoardPosition
{
    public int row_ID;
    public int col_ID;

    public BoardPosition(int row, int col)
    {
        row_ID = row;
        col_ID = col;
    }
}
