using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        float startX = -((blockSize * colums) / 2  + (blockSize/2));
        float startY = ((blockSize * rows) / 2)  - (blockSize / 2);

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

                if((i+j) % 2 == 0)
                {
                    block.SetBlock(i, j, whiteBlockSprite, false);
                }
                else
                {
                    Piece piece = null;
                    if (i < 3)
                    {
                        piece = Instantiate(piecePrefab, block.transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetBlock(i, j, PieceType.White, whitePieceSprite);
                        GameplayController.instance.whitePieces.Add(piece);
                    }

                    if (i > 4)
                    {
                        piece = Instantiate(piecePrefab, block.transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetBlock(i, j, PieceType.Black, blackPieceSprite);
                        GameplayController.instance.blackPieces.Add(piece);
                    }

                    block.SetBlock(i, j, blackBlockSprite, piece == null ? false : true, piece);
                }

                GameplayController.instance.board[i, j] = block;

                currentX += blockSize;
            }

            currentX = startX;
            currentY -= blockSize;
        }

        blockHolderParent.localPosition += new Vector3(blockSize,0,0);
        pieceHolderParent.localPosition += new Vector3(blockSize, 0, 0);

        float borderX = (blockSize * colums) + (blockSize / 2);
        float borderY = (blockSize * rows) + (blockSize / 2);

        boardBorder.sizeDelta = new Vector2(borderX, borderY);

        GameplayController.instance.OnBoardReady();
    }
}
