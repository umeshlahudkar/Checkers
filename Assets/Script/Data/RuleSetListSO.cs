using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuleSetList", menuName = "Scriptable/RuleSetList")]
public class RuleSetListSO : ScriptableObject
{
    public List<RuleSetSO> ruleSets;
}
