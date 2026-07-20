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

        public PieceType PieceType { get { return pieceType; } }
        public int Player_ID { get { return playerID; } }
        public int TurnMissCount { get { return turnMissCount; } }

        public PhotonView PhotonView { get { return thisPhotonView; } }

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

        public void OnHighlightedTargetBlockClick(Block block)
        {
            StartCoroutine(HandlePieceMovementAndPieceDelete(block));
        }

        private IEnumerator HandlePieceMovementAndPieceDelete(Block block)
        {
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
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

            yield return new WaitForSeconds(0.5f);

            bool justPromoted = !selectedPiece.IsCrownedKing && ServiceLocator.Get<GameManager>().RuleSet.IsPromotionRow(selectedPiece.Row_ID, selectedPiece.Player_ID);
            if (justPromoted)
            {
                thisPhotonView.RPC(nameof(CrownPieceAt), RpcTarget.All, selectedPiece.Row_ID, selectedPiece.Coloum_ID);
            }

            selectedPiece = block.Piece;

            if (hasDeleted && ServiceLocator.Get<MoveGenerator>().CanPieceKill(selectedPiece))
            {
                ContinueAfterKill(selectedPiece);
            }
            else
            {
                ServiceLocator.Get<GameManager>().SwitchTurn(hasDeleted || justPromoted);
                ResetNextToNextHighlightedBlock();
            }
        }

        protected abstract void ContinueAfterKill(Piece selectedPiece);

        public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove)
        {
            int sourceRow = -1;
            int sourceCol = -1;
            if (pieceToMove != null)
            {
                sourceRow = pieceToMove.Row_ID;
                sourceCol = pieceToMove.Coloum_ID;
            }
            thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, sourceRow, sourceCol);
        }

        [PunRPC]
        public void UpdateGrid(int targetRow, int targetCol, int sourceRow, int sourceCol)
        {
            Piece piece = null;
            bool hasPiece = sourceRow != -1;
            if (hasPiece)
            {
                piece = ServiceLocator.Get<GameplayController>().board[sourceRow, sourceCol].Piece;
                ServiceLocator.Get<GameplayController>().board[sourceRow, sourceCol].SetBlockPiece(false, null);

                MovePiece(piece, ServiceLocator.Get<GameplayController>().board[targetRow, targetCol]);
                ServiceLocator.Get<AudioManager>().PlayPieceMoveSound();
            }
            ServiceLocator.Get<GameplayController>().board[targetRow, targetCol].SetBlockPiece(hasPiece, piece);
        }

        // NOTE: these must be public. PUN resolves RPCs by reflecting the concrete component type
        // (HumanPlayer/BotPlayer) at runtime, and .NET's Type.GetMethods() does NOT surface a
        // *private* method declared on a base type when called on a derived type - so a private
        // [PunRPC] on this base Player class is invisible to the dispatcher and fails with
        // "RPC method not found". Public (or protected) inherited methods are returned normally.
        [PunRPC]
        public void DestroyPieceAt(int row, int col)
        {
            ServiceLocator.Get<GameplayController>().board[row, col].Piece.Destroy();
        }

        [PunRPC]
        public void CrownPieceAt(int row, int col)
        {
            ServiceLocator.Get<GameplayController>().board[row, col].Piece.SetCrownKing();
        }

        private bool AreAdjecent(Block b1, Block b2)
        {
            return (Mathf.Abs(b1.Row_ID - b2.Row_ID) == 1 && Mathf.Abs(b1.Coloum_ID - b2.Coloum_ID) == 1);
        }

        private void MovePiece(Piece pieceToMove, Block targetBlock)
        {
            Block pieceBlock = ServiceLocator.Get<GameplayController>().board[pieceToMove.Row_ID, pieceToMove.Coloum_ID];
            float duration = AreAdjecent(pieceBlock, targetBlock) ? 0.24f : 0.36f;

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
