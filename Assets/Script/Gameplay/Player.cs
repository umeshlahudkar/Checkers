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

        // Counts captures across an entire move (including every hop of a capture chain) - reported
        // via ReportChainLength for longestChainCount tracking. Reset only when a fresh piece is
        // picked up (see SelectPieceForNewMove), not between individual hops.
        private int chainCaptureCount;

        // DeferCaptureRemoval rulesets only: pieces captured so far this same capture-chain move,
        // merely marked (Piece.MarkCaptured) rather than actually destroyed, so they keep blocking
        // their square until the chain truly ends. Same reset lifetime as chainCaptureCount - swept
        // and really destroyed in FinalizeCapturedChain, right before the turn is handed over.
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
                pieceType = (playerID == 1) ? PieceType.Black : PieceType.White;
                ServiceLocator.Get<GameManager>().ListPlayer(this);
                SetTurnMissCount(0);
            }
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

        public void OnHighlightedTargetBlockClick(Block block)
        {
            StartCoroutine(HandlePieceMovementAndPieceDelete(block));
        }

        // Mirrors the "does this piece have anywhere further to capture" half of the canContinue
        // check HandlePieceMovementAndPieceDelete runs after its settle wait - minus that check's
        // ContinueAsKing crowning side effect, which is safe to drop here because no
        // DeferCaptureRemoval ruleset (the only caller of this method) ever sets
        // MidChainPromotionRule to ContinueAsKing (only Russian does, and it doesn't defer capture
        // removal). Used to let a capture decide immediately whether it's this chain's last hop.
        private bool WouldChainContinue(Piece piece)
        {
            IRuleSet ruleSet = ServiceLocator.Get<GameManager>().RuleSet;
            bool reachedPromotionRow = !piece.IsCrownedKing && ruleSet.IsPromotionRow(piece.Row_ID, piece.Player_ID);
            bool blocksContinuation = reachedPromotionRow && ruleSet.MidChainPromotionRule == MidChainPromotionRule.EndsTurnOnPromotion;
            return !blocksContinuation && ServiceLocator.Get<MoveGenerator>().CanPieceKill(piece);
        }

        private IEnumerator HandlePieceMovementAndPieceDelete(Block block)
        {
            ServiceLocator.Get<GameplayController>().ClearHintHighlight();
            ServiceLocator.Get<GamePageManager>().GamePage.SetHintUndoInteractable(false);
            ResetHighlightedBlocks();

            bool hasDeleted = false;
            BoardPosition capturedPosition = default;

            if (block.IsNextToNextHighlighted)
            {
                // Read the captured piece's position from the block rather than deriving it
                // geometrically from the landing square - a flying king can capture from any
                // distance along the diagonal, so the two aren't a fixed offset apart.
                capturedPosition = block.CapturedPosition;

                // DestroyPieceAt itself decides whether this is a real destroy or (for no-removal
                // rulesets) just a mark-as-captured - see there.
                thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, capturedPosition.row_ID, capturedPosition.col_ID);
                hasDeleted = true;
                chainCaptureCount++;
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece, hasDeleted);

            // GameplayController.SetSquare (called from inside UpdateGrid) updates row/col/occupancy
            // synchronously regardless of how long the slide animation takes to visually finish, so
            // this doesn't need to wait for that - a DeferCaptureRemoval capture can find out RIGHT
            // NOW whether it's this chain's last hop, instead of only after the settle wait below.
            // Only actually acted on when capturedThisChain.Count == 1 (this hop is the chain's
            // ONLY capture so far, i.e. it's a plain single capture, not part of a multi-kill) - a
            // lone capture gets one clean destroy animation immediately instead of marking-and-
            // pulsing only to throw that away a moment later. A multi-kill still leaves every piece
            // marked-and-waiting so the whole chain's captures are swept and destroyed together,
            // simultaneously, once the chain truly ends - not the last one immediately and the rest
            // staggered in later.
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
            bool canContinue = !blocksContinuation && hasDeleted && ServiceLocator.Get<MoveGenerator>().CanPieceKill(selectedPiece);

            if (canContinue)
            {
                ContinueAfterKill(selectedPiece);
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

        protected abstract void ContinueAfterKill(Piece selectedPiece);

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
        public void UpdateGrid(int targetRow, int targetCol, int sourceRow, int sourceCol, bool isCapture)
        {
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
        public void DestroyPieceAt(int row, int col)
        {
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
        public void CrownPieceAt(int row, int col)
        {
            ServiceLocator.Get<GameplayController>().pieces[row, col].SetCrownKing();
            ServiceLocator.Get<GameManager>().RegisterKingCrowned(Player_ID);
        }

        // Reports how many captures this whole turn's chain ended up with, so GameManager can track
        // each player's longest chain for the result screen. A PunRPC (like DestroyPieceAt/
        // CrownPieceAt above) rather than a direct GameManager call, since HandlePieceMovementAndPieceDelete
        // only runs on the client whose turn it is - every other client's GameManager needs this
        // relayed the same way it already gets board-state changes.
        [PunRPC]
        public void ReportChainLength(int chainLength)
        {
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
