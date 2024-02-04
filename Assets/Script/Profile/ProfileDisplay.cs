using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileDisplay : MonoBehaviour
{
    [SerializeField] private Image profileImg;
    [SerializeField] private TextMeshProUGUI usernameText;

    private void OnEnable()
    {
        ProfileManager.OnProfileChange += UpdateDisplay;
        //UpdateDisplay(ProfileManager.Instance.GetProfileAvtar(), ProfileManager.Instance.GetUserName());
    }

    private void UpdateDisplay(Sprite avtarSprite, string name)
    {
        if(avtarSprite == null)
        {
            profileImg.gameObject.SetActive(false);
        }
        else
        {
            profileImg.gameObject.SetActive(true);
            profileImg.sprite = avtarSprite;
        }
       
        usernameText.text = name;
    }

    private void OnDisable()
    {
        ProfileManager.OnProfileChange -= UpdateDisplay;
    }
}
