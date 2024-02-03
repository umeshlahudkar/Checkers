using UnityEngine;
using UnityEngine.UI;

public class AvtarSelectionButton : MonoBehaviour
{
    [SerializeField] private int avtarIndex;
    [SerializeField] private Image buttonAvtarImg;
    [SerializeField] private Image avtarBgImg;

    private AvtarSelectionScreen avtarController;

    public void SetButton(int index, Sprite avtarSprite, Color bgColor, AvtarSelectionScreen controller)
    {
        avtarIndex = index;
        buttonAvtarImg.sprite = avtarSprite;
        SetAvtarBgColor(bgColor);
        avtarController = controller;
    }

    public void OnClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        avtarController.HighlightSelectedAvtar(avtarIndex);
    }

    public void SetAvtarBgColor(Color color)
    {
        avtarBgImg.color = color;
    }
}
