using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserNameInputScreen : MonoBehaviour
{
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Button closeButton;

    private void OnEnable()
    {
        closeButton.interactable = ProfileManager.Instance.HasUserNameSet;
    }

    public void OnSaveButtonClick()
    {
        string username = usernameInputField.text;
        if (!string.IsNullOrEmpty(username))
        {
            ProfileManager.Instance.SetUserName(username);
            AudioManager.Instance.PlayButtonClickSound();
            gameObject.SetActive(false);
            faderScreen.SetActive(false);
        }
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.SetActive(false);
    }
}
