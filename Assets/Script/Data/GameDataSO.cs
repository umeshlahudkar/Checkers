using UnityEngine;


[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable/GameData")]
public class GameDataSO : ScriptableObject
{
    public GameMode gameMode;
    public PlayerInfo ownPlayer;
    public PlayerInfo opponentPlayer;
}
