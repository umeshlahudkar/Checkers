using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board Data")]
    [SerializeField] private int rows;
    [SerializeField] private int colums;
    [SerializeField] private float blockSize;

    [Header("Board Block")]
    [SerializeField] private Block blockPrefab;
    [SerializeField] private Sprite whiteBlockSprite;
    [SerializeField] private Sprite blackBlockSprite;

    [Header("Piece")]
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private Sprite whitePieceSprite;
    [SerializeField] private Sprite blackPieceSprite;

    [Header("Piece Holder")]
    [SerializeField] private Transform pieceHolderParent;
    [SerializeField] private Transform blockHolderParent;

    [Header("Piece Holder")]
    [SerializeField] private RectTransform boardBorder;

    [Header("Board Canvas")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Board Canvas")]
    [SerializeField] private RectTransform layout;

    public void GenerateBoard()
    {
        Canvas.ForceUpdateCanvases(); // flushes pending canvas updates
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout); // rootLayoutRect = the RectTransform with your VerticalLayoutGroup

        blockHolderParent.localPosition = Vector3.zero;
        pieceHolderParent.localPosition = Vector3.zero;

        float screenWidth = canvasRect.rect.width;
        float totalWidth = screenWidth * 0.90f;

        blockSize = (totalWidth / 8);

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

                if ((i + j) % 2 == 0)
                {
                    block.SetBlock(i, j, whiteBlockSprite);
                }
                else
                {
                    block.SetBlock(i, j, blackBlockSprite);
                }

                ServiceLocator.Get<GameplayController>().board[i, j] = block;
                currentX += blockSize;
            }

            currentX = startX;
            currentY -= blockSize;
        }

        blockHolderParent.localPosition += new Vector3(blockSize, 0, 0);
        pieceHolderParent.localPosition += new Vector3(blockSize, 0, 0);

        float borderX = (blockSize * colums) + (blockSize / 2);
        float borderY = (blockSize * rows) + (blockSize / 2);

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
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < colums; j++)
            {
                if ((i + j) % 2 != 0)
                {
                    if (i < 3)
                    {
                        Piece piece = Instantiate(piecePrefab, ServiceLocator.Get<GameplayController>().board[i, j].transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetPiece(2, i, j, (int)player2_pieceType);
                        ServiceLocator.Get<GameplayController>().whitePieces.Add(piece);
                        
                    }

                    if (i > 4)
                    {
                        Piece piece = Instantiate(piecePrefab, ServiceLocator.Get<GameplayController>().board[i, j].transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetPiece(1, i, j, (int)player1_pieceType);
                        ServiceLocator.Get<GameplayController>().blackPieces.Add(piece);
                    }
                }
            }
        }
    }
}
