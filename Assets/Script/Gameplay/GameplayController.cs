using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : Service<GameplayController>
{
    public Block[,] board;
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

    private IRuleSet ruleSet;

    private const float PieceAnimStagger = 0.04f;

    public void InitBoard(IRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
        board = new Block[ruleSet.Rows, ruleSet.Columns];
    }

    // Staggers the appear/disappear animation across every piece currently on the board (in
    // whatever order they were added, since there's no meaningful "correct" order to prefer), and
    // waits for the *last* piece's animation to finish before letting the caller continue - used
    // to hold off starting the first turn / showing the result screen until pieces have settled.
    public IEnumerator PlayPiecesAppearAnimation()
    {
        int count = AnimateAll((piece, delay) => piece.PlayAppearAnimation(delay));
        yield return new WaitForSeconds(StaggeredDuration(count, Piece.AppearDuration));
    }

    public IEnumerator PlayPiecesDisappearAnimation()
    {
        int count = AnimateAll((piece, delay) => piece.PlayDisappearAnimation(delay));
        yield return new WaitForSeconds(StaggeredDuration(count, Piece.DisappearDuration));
    }

    private int AnimateAll(System.Action<Piece, float> playAnimation)
    {
        int index = 0;
        for (int i = 0; i < whitePieces.Count; i++)
        {
            playAnimation(whitePieces[i], index * PieceAnimStagger);
            index++;
        }
        for (int i = 0; i < blackPieces.Count; i++)
        {
            playAnimation(blackPieces[i], index * PieceAnimStagger);
            index++;
        }
        return index;
    }

    private static float StaggeredDuration(int pieceCount, float perPieceDuration)
    {
        return (pieceCount > 0 ? (pieceCount - 1) * PieceAnimStagger : 0f) + perPieceDuration;
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
