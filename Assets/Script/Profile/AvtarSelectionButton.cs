using UnityEngine;
using UnityEngine.UI;

public class AvtarSelectionButton : MonoBehaviour
{
    [SerializeField] private int avtarIndex;
    [SerializeField] private Image buttonAvtarImg;
    [SerializeField] private Image avtarBgImg;

    private void Start()
    {
        buttonAvtarImg.sprite = ProfileManager.Instance.GetSprite(avtarIndex);
    }

    private void OnEnable()
    {
        if(ProfileManager.Instance.GetSelectedAvtarIndex() == avtarIndex)
        {
            SetAvtarBgColor(ProfileManager.Instance.GetSelectedBgColor());
        }
        else
        {
            SetAvtarBgColor(ProfileManager.Instance.GetDefaultBgColor());
        }
    }

    public void OnClick()
    {
        ProfileManager.Instance.SetAvtarIndex(avtarIndex);
    }

    public void SetAvtarBgColor(Color color)
    {
        avtarBgImg.color = color;
    }
}
