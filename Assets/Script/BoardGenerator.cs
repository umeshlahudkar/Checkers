using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public int rows;
    public int colums;

    public float blockSize;

    public Block blockPrefab;

    public Sprite whiteBlockSprite;
    public Sprite blackBlockSprite;

    public Piece piecePrefab;

    public Sprite whitePieceSprite;
    public Sprite blackPieceSprite;

    public Transform pieceHolder;


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
                Block block = Instantiate(blockPrefab, transform);
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
                        piece = Instantiate(piecePrefab, block.transform.position, Quaternion.identity, pieceHolder);
                        piece.SetBlock(i, j, PieceType.White, whitePieceSprite);
                        GameplayController.instance.whitePieces.Add(piece);
                    }

                    if (i > 4)
                    {
                        piece = Instantiate(piecePrefab, block.transform.position, Quaternion.identity, pieceHolder);
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

        transform.localPosition += new Vector3(blockSize,0,0);
        pieceHolder.localPosition += new Vector3(blockSize, 0, 0);

        GameplayController.instance.OnBoardReady();
    }
}
