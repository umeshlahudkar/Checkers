using UnityEngine;

public class AvtarSelectionScreen : MonoBehaviour
{
    [SerializeField] private GameObject faderScreen;

    [SerializeField] private Color defaultColor;
    [SerializeField] private Color selectedColor;

    [SerializeField] private AvtarSelectionButton[] avtarSelectionButtons;

    private int selectAvtarIndex = -1;

    private void OnEnable()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        int savedAvtarIndex = ProfileManager.Instance.GetProfileAvtarIndex();
        for (int i = 1; i <= avtarSelectionButtons.Length; i++)
        {
            if (savedAvtarIndex == i)
            {
                selectAvtarIndex = i;
                avtarSelectionButtons[i-1].SetButton(i, ProfileManager.Instance.GetAvtar(i), selectedColor, this);
            }
            else
            {
                avtarSelectionButtons[i-1].SetButton(i, ProfileManager.Instance.GetAvtar(i), defaultColor, this);
            }
        }
    }

    public void HighlightSelectedAvtar(int index)
    {
        if (selectAvtarIndex != -1)
        {
            avtarSelectionButtons[selectAvtarIndex - 1].SetAvtarBgColor(defaultColor);
        }

        selectAvtarIndex = index;

        avtarSelectionButtons[selectAvtarIndex - 1].SetAvtarBgColor(selectedColor);
    }

    public void OnSaveButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ProfileManager.Instance.SetAvtar(selectAvtarIndex);
        faderScreen.SetActive(false);
        gameObject.Deactivate();
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.Deactivate();
    }
}
