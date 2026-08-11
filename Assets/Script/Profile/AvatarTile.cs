using System;
using UnityEngine;
using UnityEngine.UI;

public class AvatarTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedBg;

    private int avtarIndex;
    private Action<int> onButtonClick;

    public void Setup(int index, Sprite avtarSprite, Sprite Bg, Action<int> onSelected)
    {
        avtarIndex = index;
        iconImage.sprite = avtarSprite;
        selectedBg.sprite = Bg;
        onButtonClick = onSelected;
    }

    public void OnButtonClick()
    {
        onButtonClick?.Invoke(avtarIndex);
    }

    public void SetSelected(Sprite bg)
    {
        selectedBg.sprite = bg;
    }
}
