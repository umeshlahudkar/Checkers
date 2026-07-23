using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameplayController : Service<GameplayController>
{
    public Block[,] board;
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

    private readonly List<Block> lastMoveHighlightedBlocks = new();
    private Tween pendingLastMoveTween;

    private readonly List<Block> hintHighlightedBlocks = new();

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
        ClearLastMoveHighlight();
        ClearHintHighlight();
    }

    // Tracks the opponent's currently-moving piece: the square it's leaving lights up first, then
    // once moveDuration has passed (matching the piece's own slide animation, see
    // Player.GetMoveDuration) the square it landed on lights up too - both stay highlighted
    // together, and every hop of a capture chain adds to this same set rather than replacing it,
    // so a multi-capture ends up with every square it touched highlighted, not just its last hop.
    // Deliberately never clears here - that only needs to happen once a *different* turn's move
    // starts, and since turns strictly alternate, our own next move already unconditionally clears
    // (see the IsLocalPlayer branch in Player.UpdateGrid) before the opponent's following turn can
    // call this again.
    public void ShowLastMoveInProgress(int fromRow, int fromCol, int toRow, int toCol, float moveDuration)
    {
        pendingLastMoveTween?.Kill();
        pendingLastMoveTween = null;

        Block fromBlock = board[fromRow, fromCol];
        if (!lastMoveHighlightedBlocks.Contains(fromBlock))
        {
            fromBlock.HighlightAsLastMove();
            lastMoveHighlightedBlocks.Add(fromBlock);
        }

        pendingLastMoveTween = DOVirtual.DelayedCall(moveDuration, () =>
        {
            pendingLastMoveTween = null;

            Block toBlock = board[toRow, toCol];
            if (!lastMoveHighlightedBlocks.Contains(toBlock))
            {
                toBlock.HighlightAsLastMove();
                lastMoveHighlightedBlocks.Add(toBlock);
            }
        });
    }

    public void ClearLastMoveHighlight()
    {
        pendingLastMoveTween?.Kill();
        pendingLastMoveTween = null;

        for (int i = 0; i < lastMoveHighlightedBlocks.Count; i++)
        {
            lastMoveHighlightedBlocks[i].ResetLastMoveHighlight();
        }
        lastMoveHighlightedBlocks.Clear();
    }

    // Suggested move from the Hint feature (Player.GetMoveDuration's turn-highlight sibling) - both
    // squares light up immediately and stay lit (no animation, no timed swap) until cleared.
    public void ShowHintHighlight(int fromRow, int fromCol, int toRow, int toCol)
    {
        ClearHintHighlight();

        Block fromBlock = board[fromRow, fromCol];
        fromBlock.ShowHint();
        hintHighlightedBlocks.Add(fromBlock);

        Block toBlock = board[toRow, toCol];
        toBlock.ShowHint();
        hintHighlightedBlocks.Add(toBlock);
    }

    public void ClearHintHighlight()
    {
        for (int i = 0; i < hintHighlightedBlocks.Count; i++)
        {
            hintHighlightedBlocks[i].ClearHint();
        }
        hintHighlightedBlocks.Clear();
    }
}
