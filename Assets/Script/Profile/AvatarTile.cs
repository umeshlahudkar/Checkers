using System;
using UnityEngine;
using UnityEngine.UI;

public class AvatarTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedBg;

    private int avtarIndex;
    private Action<int> onButtonClick;

    public void Setup(int index, Sprite avtarSprite, Action<int> onSelected)
    {
        avtarIndex = index;
        iconImage.sprite = avtarSprite;
        onButtonClick = onSelected;
    }

    public void OnButtonClick()
    {
        onButtonClick?.Invoke(avtarIndex);
    }

    public void SetSelected(bool isSelected)
    {
        selectedBg.SetActive(isSelected);
    }
}
