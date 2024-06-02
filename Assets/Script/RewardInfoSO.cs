using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardData", menuName = "Scriptable/RewardData")]
public class RewardInfoSO : ScriptableObject
{
    public RewardInfo[] rewards;
}
