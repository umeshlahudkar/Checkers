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
            if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
            {
                playerID = thisPhotonView.OwnerActorNr;
                pieceType = (playerID == 1) ? PieceType.Black : PieceType.White;
                ServiceLocator.Get<GameManager>().ListPlayer(this);
                turnMissCount = 0;
            }
        }

        public void SetPlayer(int playerNumber, PieceType pieceType)
        {
            this.playerID = playerNumber;
            this.pieceType = pieceType;
            turnMissCount = 0;
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
            turnMissCount++;
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
            turnMissCount = 0;
            ResetHighlightedBlocks();

            bool hasDeleted = false;

            if (block.IsNextToNextHighlighted)
            {
                int row = block.Row_ID;
                int coloum = block.Coloum_ID;

                int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
                int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

                Piece piece = ServiceLocator.Get<GameplayController>().board[targetRow, targetCol].Piece;
                if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
                {
                    piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
                }
                else
                {
                    piece.Destroy();
                }
                hasDeleted = true;
                block.IsNextToNextHighlighted = false;
            }

            UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

            yield return new WaitForSeconds(0.5f);


            if (!selectedPiece.IsCrownedKing && ((selectedPiece.Player_ID == 2 && selectedPiece.Row_ID == 7) ||
                (selectedPiece.Player_ID == 1 && selectedPiece.Row_ID == 0)))
            {
                if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
                {
                    selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
                }
                else
                {
                    selectedPiece.SetCrownKing();
                }
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
            if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
            {
                int viewId = -1;
                if (pieceToMove != null)
                {
                    viewId = pieceToMove.PhotonView.ViewID;
                }
                thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, viewId);
            }
            else
            {
                ServiceLocator.Get<GameplayController>().board[pieceToMove.Row_ID, pieceToMove.Coloum_ID].SetBlockPiece(false, null);

                StartCoroutine(MovePiece(pieceToMove, ServiceLocator.Get<GameplayController>().board[targetRow, targetCol]));
                ServiceLocator.Get<AudioManager>().PlayPieceMoveSound();
                ServiceLocator.Get<GameplayController>().board[targetRow, targetCol].SetBlockPiece(true, pieceToMove);
            }

        }

        [PunRPC]
        public void UpdateGrid(int targetRow, int targetCol, int viewId)
        {
            Piece piece = null;
            if (viewId != -1)
            {
                piece = PhotonView.Find(viewId).GetComponent<Piece>();
                ServiceLocator.Get<GameplayController>().board[piece.Row_ID, piece.Coloum_ID].SetBlockPiece(false, null);

                StartCoroutine(MovePiece(piece, ServiceLocator.Get<GameplayController>().board[targetRow, targetCol]));
                ServiceLocator.Get<AudioManager>().PlayPieceMoveSound();
            }
            ServiceLocator.Get<GameplayController>().board[targetRow, targetCol].SetBlockPiece((viewId != -1), piece);
        }

        private bool AreAdjecent(Block b1, Block b2)
        {
            return (Mathf.Abs(b1.Row_ID - b2.Row_ID) == 1 && Mathf.Abs(b1.Coloum_ID - b2.Coloum_ID) == 1);
        }

        private IEnumerator MovePiece(Piece pieceToMove, Block targetBlock)
        {
            Block pieceBlock = ServiceLocator.Get<GameplayController>().board[pieceToMove.Row_ID, pieceToMove.Coloum_ID];
            float time = AreAdjecent(pieceBlock, targetBlock) ? 0.15f : 0.25f;
            float elapcedTime = 0;

            Vector3 initialPos = pieceToMove.transform.position;
            Vector3 targetPos = targetBlock.transform.position;

            while (elapcedTime < time)
            {
                elapcedTime += Time.deltaTime;
                Vector3 pos = Vector3.Lerp(initialPos, targetPos, elapcedTime / time);
                pieceToMove.transform.position = pos;
                yield return null;
            }
            pieceToMove.transform.position = targetPos;
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
