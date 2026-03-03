using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;


public class AuthService
{
    public void GuestLogin()
    {
        LoginRequest loginRequest = new LoginRequest();
        loginRequest.DeviceID = SystemInfo.deviceUniqueIdentifier;

        string json = JsonConvert.SerializeObject(loginRequest);

        APIManager.Instance.Post("/api/auth/guest-login", json,
            (result) =>
            {
                LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
                APIManager.Instance.SetToken(loginResponse);

                Debug.Log("Guest login succesfull " + loginResponse.ToString());
            },
            (error) =>
            {
                Debug.Log("Guest Login Failed " + error);
            });
    }

    
}

public class LoginRequest
{
    public string DeviceID;
}

[System.Serializable]
public class LoginResponse
{
    public Guid Id;
    public string DeviceID;
    public string Token;
    public string RefreshToken;
    public DateTime CreateAt;
    public DateTime LastLoginAt;
    public DateTime AccessExpireDate;
    public List<String> Roles;
}
