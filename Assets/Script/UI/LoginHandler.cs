using UnityEngine;

public class LoginHandler : MonoBehaviour
{
    public void OnGuestLoginClick()
    {
        Debug.Log("OnGuestLoginClick()");

        AuthService authService = new AuthService();
        authService.GuestLogin();
    }
}
