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

    public void OnClick()
    {
        ProfileManager.Instance.SetAvtarIndex(avtarIndex);
    }

    public void SetAvtarBgColor(Color color)
    {
        avtarBgImg.color = color;
    }
}
