using UnityEngine;
using UnityEngine.UI;

public class AvtarSelectionButton : MonoBehaviour
{
    [SerializeField] private int avtarIndex;
    [SerializeField] private Image buttonAvtarImg;
    [SerializeField] private Image avtarBgImg;

    private AvtarController avtarController;

    private void Start()
    {
        //buttonAvtarImg.sprite = ProfileManager.Instance.GetAvtarSprite(avtarIndex);
    }

    public void SetButton(int index, Sprite avtarSprite, Color bgColor, AvtarController controller)
    {
        avtarIndex = index;
        buttonAvtarImg.sprite = avtarSprite;
        SetAvtarBgColor(bgColor);
        avtarController = controller;
    }

    //private void OnEnable()
    //{
    //    if(ProfileManager.Instance.GetSelectedAvtarIndex() == avtarIndex)
    //    {
    //        SetAvtarBgColor(ProfileManager.Instance.GetSelectedBgColor());
    //    }
    //    else
    //    {
    //        SetAvtarBgColor(ProfileManager.Instance.GetDefaultBgColor());
    //    }
    //}

    public void OnClick()
    {
        avtarController.OnAvtarButtonClick(avtarIndex);
    }

    public void SetAvtarBgColor(Color color)
    {
        avtarBgImg.color = color;
    }
}
