using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board Data")]
    [SerializeField] private float blockSize;

    private IRuleSet ruleSet;

    [Header("Board Block")]
    [SerializeField] private Block blockPrefab;
    [SerializeField] private Sprite whiteBlockSprite;
    [SerializeField] private Sprite blackBlockSprite;

    [Header("Piece")]
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private Sprite whitePieceSprite;
    [SerializeField] private Sprite blackPieceSprite;
    [SerializeField] private Sprite crownedWhitePieceSprite;
    [SerializeField] private Sprite crownedBlackPieceSprite;

    [Header("Piece Holder")]
    [SerializeField] private Transform pieceHolderParent;
    [SerializeField] private Transform blockHolderParent;

    [Header("Piece Holder")]
    [SerializeField] private RectTransform boardBorder;
    [SerializeField] private float offset;


    [Header("Board Canvas")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Board Canvas")]
    [SerializeField] private RectTransform layout;

    public Sprite GetPieceSprite(PieceType pieceType)
    {
        return pieceType == PieceType.White ? whitePieceSprite : blackPieceSprite;
    }

    public void GenerateBoard(IRuleSet ruleSet)
    {
        Piece.white_piece = whitePieceSprite;
        Piece.black_piece = blackPieceSprite;
        Piece.crowned_white_piece = crownedWhitePieceSprite;
        Piece.crowned_black_piece = crownedBlackPieceSprite;

        this.ruleSet = ruleSet;
        int rows = ruleSet.Rows;
        int colums = ruleSet.Columns;

        Canvas.ForceUpdateCanvases(); // flushes pending canvas updates
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout); // rootLayoutRect = the RectTransform with your VerticalLayoutGroup

        blockHolderParent.localPosition = Vector3.zero;
        pieceHolderParent.localPosition = Vector3.zero;

        float screenWidth = canvasRect.rect.width;
        float totalWidth = screenWidth * 0.85f;

        blockSize = (totalWidth / colums);

        float startX = -((blockSize * colums) / 2 + (blockSize / 2));
        float startY = ((blockSize * rows) / 2) - (blockSize / 2);

        float currentX = startX;
        float currentY = startY;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < colums; j++)
            {
                Block block = Instantiate(blockPrefab, blockHolderParent);
                block.gameObject.name = "Block " + i + " " + j;
                block.ThisTransform.localPosition = new Vector3(currentX, currentY, 0);
                block.ThisTransform.sizeDelta = new Vector2(blockSize, blockSize);

                // Flipped wholesale (every square, not just the corner) for rulesets whose corner
                // convention differs (Italian: dark bottom-right) - flipping every square is what
                // keeps the alternating pattern intact while landing the opposite color in that
                // corner. Every other modeled ruleset leaves this false, so (i+j)%2==0 alone still
                // decides it, exactly as before.
                bool isWhiteSquare = ((i + j) % 2 == 0) != ruleSet.DarkSquareBottomRight;
                block.SetBlock(i, j, isWhiteSquare ? whiteBlockSprite : blackBlockSprite);

                ServiceLocator.Get<GameplayController>().board[i, j] = block;
                currentX += blockSize;
            }

            currentX = startX;
            currentY -= blockSize;
        }

        blockHolderParent.localPosition += new Vector3(blockSize, 0, 0);
        pieceHolderParent.localPosition += new Vector3(blockSize, 0, 0);

        //float borderX = (blockSize * colums) + (blockSize / 2);
        //float borderY = (blockSize * rows) + (blockSize / 2);

        //boardBorder.sizeDelta = new Vector2(borderX, borderY);

        ApplyBorder(rows);
    }

    [ContextMenu("Setup Border")]
    public void ApplyBorder(int rows)
    {
        float gridSize = (blockSize * rows);
        float borderX = gridSize + (gridSize * offset);
        float borderY = gridSize + (gridSize * offset);

        boardBorder.sizeDelta = new Vector2(borderX, borderY);
    }

    // In multiplayer, the board's blocks are laid out with a fixed row convention (rows 0-2 =
    // white, rows 5-7 = black) regardless of which side the local player is on, so the local
    // player's own pieces need to render at the bottom by flipping the board 180 degrees.
    // blockHolderParent's own pivot isn't at the grid's visual center (its +blockSize position
    // offset, set above, exists specifically to compensate for that), so rotating it directly
    // would mirror that offset and shift the whole board off-center. Rotating their shared
    // parent instead - whose origin already coincides with the grid's true center, by
    // construction of that same offset - avoids the issue entirely, and since neither holder
    // rotates relative to the other, they stay in sync for the move-animation code that lerps
    // raw anchoredPosition between a piece and a block.
    public void SetBoardOrientation(bool isFlipped)
    {
        Quaternion rotation = isFlipped ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
        blockHolderParent.parent.localRotation = rotation;
    }

    public void GeneratePieces(PieceType player1_pieceType, PieceType player2_pieceType)
    {
        int rows = ruleSet.Rows;
        int colums = ruleSet.Columns;
        int pieceRowsPerSide = ruleSet.PieceRowsPerSide;

        // Most variants fill starting from each player's own back row inward. Turkish dama instead
        // leaves that very back row empty and starts filling one row further in, so the whole block
        // is offset by one row from each edge.
        int backRowOffset = ruleSet.LeaveBackRowEmpty ? 1 : 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < colums; j++)
            {
                // Mirrors GenerateBoard's own coloring formula (see there) so playing squares
                // always land on the dark-colored ones: isWhiteSquare there is
                // ((i+j)%2==0) != DarkSquareBottomRight, so dark is the negation of that, which
                // simplifies to ((i+j)%2==0) == DarkSquareBottomRight. For every ruleset except
                // Italian (the only one with the flag set), DarkSquareBottomRight is false, so
                // this reduces back to the original (i+j)%2!=0 exactly - only Italian's parity
                // actually changes. Before this fix, GeneratePieces never consulted the flag at
                // all, so Italian's pieces landed on the light-rendered squares instead.
                bool isDarkSquare = ((i + j) % 2 == 0) == ruleSet.DarkSquareBottomRight;
                if (ruleSet.PiecesOnAllSquares || isDarkSquare)
                {
                    if (i >= backRowOffset && i < backRowOffset + pieceRowsPerSide)
                    {
                        SpawnPiece(2, i, j, player2_pieceType);
                    }

                    if (i < rows - backRowOffset && i >= rows - backRowOffset - pieceRowsPerSide)
                    {
                        SpawnPiece(1, i, j, player1_pieceType);
                    }
                }
            }
        }
    }

    private Piece SpawnPiece(int playerID, int row, int col, PieceType pieceType)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();

        Piece piece = Instantiate(piecePrefab, gameplayController.board[row, col].transform.position, Quaternion.identity, pieceHolderParent);
        piece.SetPiece(playerID, row, col, (int)pieceType);

        if (playerID == 2)
        {
            gameplayController.whitePieces.Add(piece);
        }
        else
        {
            gameplayController.blackPieces.Add(piece);
        }

        return piece;
    }

    // Destroys every current piece immediately (no capture animation - this is a rewind, not a
    // kill) and clears the board's occupancy, so RestorePieceLayout can rebuild from a clean slate.
    private void ClearAllPieces()
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();

        for (int i = 0; i < gameplayController.whitePieces.Count; i++)
        {
            Destroy(gameplayController.whitePieces[i].gameObject);
        }
        for (int i = 0; i < gameplayController.blackPieces.Count; i++)
        {
            Destroy(gameplayController.blackPieces[i].gameObject);
        }
        gameplayController.whitePieces.Clear();
        gameplayController.blackPieces.Clear();

        for (int i = 0; i < ruleSet.Rows; i++)
        {
            for (int j = 0; j < ruleSet.Columns; j++)
            {
                gameplayController.SetSquare(i, j, null);
            }
        }
    }

    // Used by Undo to rebuild the entire piece layout from a snapshot taken at the start of an
    // earlier turn - see GameManager's history stack. Rebuilding from scratch (rather than trying to
    // reverse individual moves/captures/promotions in place) sidesteps a captured piece's GameObject
    // being truly gone after its disappear animation, and isCrownedKing having no un-set path.
    public void RestorePieceLayout(PieceSnapshot[,] layout)
    {
        ClearAllPieces();

        int rows = layout.GetLength(0);
        int cols = layout.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                PieceSnapshot cell = layout[i, j];
                if (!cell.present) { continue; }

                Piece piece = SpawnPiece(cell.playerID, i, j, cell.pieceType);
                piece.SetKingState(cell.isCrownedKing);
            }
        }
    }
}
