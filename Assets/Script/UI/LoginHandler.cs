using UnityEngine;

public class LoginHandler : MonoBehaviour
{
    public void OnGuestLoginClick()
    {
        Debug.Log("OnGuestLoginClick()");
        APIManager.Instance.authService.GuestLogin();
    }
}
