using UnityEngine;
using UnityEngine.UI;

public class AvtarSelectionButton : MonoBehaviour
{
    [SerializeField] private int avtarIndex;
    [SerializeField] private Image buttonAvtarImg;
    [SerializeField] private Image avtarBgImg;

    private AvtarController avtarController;

    public void SetButton(int index, Sprite avtarSprite, Color bgColor, AvtarController controller)
    {
        avtarIndex = index;
        buttonAvtarImg.sprite = avtarSprite;
        SetAvtarBgColor(bgColor);
        avtarController = controller;
    }

    public void OnClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        avtarController.OnAvtarButtonClick(avtarIndex);
    }

    public void SetAvtarBgColor(Color color)
    {
        avtarBgImg.color = color;
    }
}
