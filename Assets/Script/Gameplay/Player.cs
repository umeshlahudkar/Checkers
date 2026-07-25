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

        // Counts captures across an entire move (including every hop of a capture chain), so a
        // multi-capture can be celebrated with a "DOUBLE KILL!"-style callout. Reset only when a
        // fresh piece is picked up (see SelectPieceForNewMove), not between individual hops.
        private int chainCaptureCount;

        private static readonly Color KillStreakTextColor = new(1f, 0.3f, 0.3f);

        public PieceType PieceType { get { return pieceType; } }
        public int Player_ID { get { return playerID; } }
        public int TurnMissCount { get { return turnMissCount; } }

        public PhotonView PhotonView { get { return thisPhotonView; } }

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
        }

        public void OnHighlightedTargetBlockClick(Block block)
        {
            StartCoroutine(HandlePieceMovementAndPieceDelete(block));
        }

        private IEnumerator HandlePieceMovementAndPieceDelete(Block block)
        {
            ServiceLocator.Get<GameplayController>().ClearHintHighlight();
            ServiceLocator.Get<GamePageManager>().GamePage.SetHintUndoInteractable(false);
            ResetHighlightedBlocks();

            bool hasDeleted = false;

            if (block.IsNextToNextHighlighted)
            {
                // Read the captured piece's position from the block rather than deriving it
                // geometrically from the landing square - a flying king can capture from any
                // distance along the diagonal, so the two aren't a fixed offset apart.
                BoardPosition captured = block.CapturedPosition;

                thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, captured.row_ID, captured.col_ID);
                hasDeleted = true;
                chainCaptureCount++;
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece, hasDeleted);

            yield return new WaitForSeconds(0.5f);

            bool justPromoted = !selectedPiece.IsCrownedKing && ServiceLocator.Get<GameManager>().RuleSet.IsPromotionRow(selectedPiece.Row_ID, selectedPiece.Player_ID);
            if (justPromoted)
            {
                thisPhotonView.RPC(nameof(CrownPieceAt), RpcTarget.All, selectedPiece.Row_ID, selectedPiece.Coloum_ID);
            }

            selectedPiece = ServiceLocator.Get<GameplayController>().pieces[block.Row_ID, block.Coloum_ID];

            if (hasDeleted && ServiceLocator.Get<MoveGenerator>().CanPieceKill(selectedPiece))
            {
                ContinueAfterKill(selectedPiece);
            }
            else
            {
                if (chainCaptureCount >= 2)
                {
                    thisPhotonView.RPC(nameof(ShowGratificationText), RpcTarget.All, GetKillStreakText(chainCaptureCount));
                }

                ServiceLocator.Get<GameManager>().SwitchTurn(hasDeleted || justPromoted);
                ResetNextToNextHighlightedBlock();
            }
        }

        private static string GetKillStreakText(int captureCount)
        {
            return captureCount switch
            {
                2 => "DOUBLE KILL!",
                3 => "TRIPLE KILL!",
                _ => "MULTI KILL!"
            };
        }

        [PunRPC]
        public void ShowGratificationText(string text)
        {
            ServiceLocator.Get<GameManager>().ShowFloatingText(text, KillStreakTextColor);
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
                if (IsLocalPlayer)
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

        [PunRPC]
        public void DestroyPieceAt(int row, int col)
        {
            Piece capturedPiece = ServiceLocator.Get<GameplayController>().pieces[row, col];
            lastCapturedPieceSiblingIndex = capturedPiece.ThisTransform.GetSiblingIndex();
            capturedPiece.Destroy();
        }

        [PunRPC]
        public void CrownPieceAt(int row, int col)
        {
            ServiceLocator.Get<GameplayController>().pieces[row, col].SetCrownKing();
        }

        private bool AreAdjecent(Block b1, Block b2)
        {
            return (Mathf.Abs(b1.Row_ID - b2.Row_ID) == 1 && Mathf.Abs(b1.Coloum_ID - b2.Coloum_ID) == 1);
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
