using UnityEngine;
using Photon.Pun;

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

    public void GenerateBoard()
    {
        blockHolderParent.localPosition = Vector3.zero;
        pieceHolderParent.localPosition = Vector3.zero;

        float screenWidth = canvasRect.rect.width;
        float totalWidth = screenWidth * 0.90f;

        blockSize = (totalWidth / 8);

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
                    block.SetBlock(i, j, whiteBlockSprite);
                }
                else
                {
                    block.SetBlock(i, j, blackBlockSprite);
                }

                GameplayController.Instance.board[i, j] = block;
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
    }

    public void GeneratePieces()
    {
        for(int i = 0; i < rows; i++)
        {
            for(int j = 0; j < colums; j++)
            {
                if((i + j) % 2 != 0)
                {
                    if (i < 3 && GameManager.Instance.GetPlayer(2).PhotonView.IsMine)
                    {
                        GameObject obj = PhotonNetwork.Instantiate(piecePrefab.name, GameplayController.Instance.board[i, j].transform.position, Quaternion.identity);
                        Piece piece = obj.GetComponent<Piece>();
                        PhotonView view = piece.PhotonView;

                        view.RPC(nameof(piece.SetPiece), RpcTarget.All, 2 ,i, j, (int)PieceType.White);
                        GameplayController.Instance.whitePieces.Add(piece);
                    }

                    if (i > 4 && GameManager.Instance.GetPlayer(1).PhotonView.IsMine)
                    {
                        GameObject obj = PhotonNetwork.Instantiate(piecePrefab.name, GameplayController.Instance.board[i, j].transform.position, Quaternion.identity);
                        Piece piece = obj.GetComponent<Piece>();
                        PhotonView view = piece.PhotonView;

                        view.RPC(nameof(piece.SetPiece), RpcTarget.All, 1, i, j, (int)PieceType.Black);

                        GameplayController.Instance.blackPieces.Add(piece);
                    }
                }
            }
        }
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
                        Piece piece = Instantiate(piecePrefab, GameplayController.Instance.board[i, j].transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetPiece(2, i, j, (int)player2_pieceType);
                        GameplayController.Instance.whitePieces.Add(piece);
                        
                    }

                    if (i > 4)
                    {
                        Piece piece = Instantiate(piecePrefab, GameplayController.Instance.board[i, j].transform.position, Quaternion.identity, pieceHolderParent);
                        piece.SetPiece(1, i, j, (int)player1_pieceType);
                        GameplayController.Instance.blackPieces.Add(piece);
                    }
                }
            }
        }
    }
}
