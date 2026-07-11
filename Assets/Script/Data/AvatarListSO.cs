using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarList", menuName = "Scriptable/AvatarList")]
public class AvatarListSO : ScriptableObject
{
    public List<Sprite> avatars;
}
