
namespace Gameplay
{
    public class MoveInfo
    {
        public bool hasCrownSet;
        public Block previousBlock;
        public Block currentBlock;

        public PieceType deletedPieceType;
        public int deletedPiecePlayerId;
        public Block deletedPieceBlock;
        public bool wasDeletedPieceKrown;
    }
}
