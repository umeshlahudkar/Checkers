using UnityEngine;


[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable/GameData")]
public class GameDataSO : ScriptableObject
{
    public PlayerInfo ownPlayer;
    public PlayerInfo opponentPlayer;
}
