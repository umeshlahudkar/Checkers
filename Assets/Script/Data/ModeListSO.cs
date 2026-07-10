using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModeList", menuName = "Scriptable/ModeList")]
public class ModeListSO : ScriptableObject
{
    public List<ModeInfo> modes;
}
