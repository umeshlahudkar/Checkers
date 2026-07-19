using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
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
            if(ServiceLocator.Get<GameplayController>().CanMove(playerID))
            {
                PlayTurn();
                return true;
            }

            return false;
        }

        private void PlayTurn()
        {
            movablePieces.Clear();
            ServiceLocator.Get<GameplayController>().CheckMovablePieces(playerID, movablePieces);

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
                int row = block.Row_ID;
                int coloum = block.Coloum_ID;

                int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
                int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

                thisPhotonView.RPC(nameof(DestroyPieceAt), RpcTarget.All, targetRow, targetCol);
                hasDeleted = true;
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

            yield return new WaitForSeconds(0.5f);


            if (!selectedPiece.IsCrownedKing && ((selectedPiece.Player_ID == 2 && selectedPiece.Row_ID == 7) ||
                (selectedPiece.Player_ID == 1 && selectedPiece.Row_ID == 0)))
            {
                thisPhotonView.RPC(nameof(CrownPieceAt), RpcTarget.All, selectedPiece.Row_ID, selectedPiece.Coloum_ID);
            }

            selectedPiece = block.Piece;

            if (hasDeleted && ServiceLocator.Get<GameplayController>().CanPieceKill(selectedPiece))
            {
                ContinueAfterKill(selectedPiece);
            }
            else
            {
                ServiceLocator.Get<GameManager>().SwitchTurn();
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

                StartCoroutine(MovePiece(piece, ServiceLocator.Get<GameplayController>().board[targetRow, targetCol]));
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

        private IEnumerator MovePiece(Piece pieceToMove, Block targetBlock)
        {
            Block pieceBlock = ServiceLocator.Get<GameplayController>().board[pieceToMove.Row_ID, pieceToMove.Coloum_ID];
            float duration = AreAdjecent(pieceBlock, targetBlock) ? 0.2f : 0.32f;
            float elapcedTime = 0;

            Vector2 initialPos = pieceToMove.ThisTransform.anchoredPosition;
            Vector2 targetPos = targetBlock.ThisTransform.anchoredPosition;

            while (elapcedTime < duration)
            {
                elapcedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapcedTime / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                pieceToMove.ThisTransform.anchoredPosition = Vector2.Lerp(initialPos, targetPos, easedT);
                yield return null;
            }
            pieceToMove.ThisTransform.anchoredPosition = targetPos;
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
