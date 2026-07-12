using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardThemeList", menuName = "Scriptable/BoardThemeList")]
public class BoardThemeListSO : ScriptableObject
{
    public List<BoardThemeInfo> themes;
}
