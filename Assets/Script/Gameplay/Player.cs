using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        protected int playerID;
        protected PieceType pieceType;
        protected int turnMissCount;

        protected readonly List<Block> highlightedBlocks = new();
        protected readonly List<Block> nextToNexthighlightedBlocks = new();
        protected readonly List<Piece> movablePieces = new();
        protected Piece selectedPiece;

        public PieceType PieceType { get { return pieceType; } }
        public int Player_ID { get { return playerID; } }
        public int TurnMissCount { get { return turnMissCount; } }


        protected MoveInfo moveInfo;

      
        public virtual void SetPlayer(int playerNumber, PieceType pieceType)
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
            if(GameplayController.Instance.CanMove(playerID))
            {
                return true;
            }
            return false;
        }

        public virtual void PlayTurn() { }

        public virtual void ShowMoveHint() { }

        public virtual void OnHighlightedPieceClick(Piece clickedPiece) { }

        public virtual void OnHighlightedTargetBlockClick(Block targetBlock) { }

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
      
        protected bool AreAdjecent(Block b1, Block b2)
        {
            return (Mathf.Abs(b1.Row_ID - b2.Row_ID) == 1 && Mathf.Abs(b1.Coloum_ID - b2.Coloum_ID) == 1);
        }

        protected IEnumerator MovePiece(Piece pieceToMove, Block targetBlock)
        {
            Block pieceBlock = GameplayController.Instance.board[pieceToMove.Row_ID, pieceToMove.Coloum_ID];
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

