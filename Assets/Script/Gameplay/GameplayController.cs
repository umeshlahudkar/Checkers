using System.Collections.Generic;
using UnityEngine;

public class GameplayController : Service<GameplayController>
{
    public Block[,] board;
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

    private IRuleSet ruleSet;

    public void InitBoard(IRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
        board = new Block[ruleSet.Rows, ruleSet.Columns];
    }

    public void ResetGameplay()
    {
        for(int i = 0; i < ruleSet.Rows; i++)
        {
            for(int j = 0; j < ruleSet.Columns; j++)
            {
                if(board[i,j].Piece != null)
                {
                    Destroy(board[i, j].Piece.gameObject);
                }
                Destroy(board[i, j].gameObject);
            }
        }
        whitePieces.Clear();
        blackPieces.Clear();
    }
}
