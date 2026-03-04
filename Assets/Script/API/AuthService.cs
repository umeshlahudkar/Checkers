using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class AuthService
{
    private long tokenExpiry;
    private float elapcedTime;

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

                tokenExpiry = loginResponse.LoginExpireAt - (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
                elapcedTime = tokenExpiry;
                Debug.Log($"Guest login succesfull.... Access Token Expiry:{tokenExpiry}");
            },
            (error) =>
            {
                Debug.Log("Guest Login Failed " + error);
            });
    }

    private void RefreshToken()
    {
        RefreshTokenRequest tokenRefreshRequest = new RefreshTokenRequest();
        tokenRefreshRequest.RefreshToken = APIManager.Instance.loginResponse.RefreshToken;

        string json = JsonConvert.SerializeObject(tokenRefreshRequest);

        APIManager.Instance.Post("/api/auth/refresh-token", json,
           (result) =>
           {
               RefreshTokenResponse loginResponse = JsonConvert.DeserializeObject<RefreshTokenResponse>(result);
               //APIManager.Instance.SetToken(loginResponse);

               tokenExpiry = loginResponse.AccessTokenExpiry - (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
               elapcedTime = tokenExpiry;
               Debug.Log($"Refresh Token succesfull.... Access Token Expiry:{tokenExpiry}");
           },
           (error) =>
           {
               Debug.Log("Guest Login Failed " + error);
           });
    }

    public void Update()
    {
        if(elapcedTime > 0)
        {
            elapcedTime -= Time.deltaTime;
            if(elapcedTime <= 5f)
            {
                // Refresh token
                elapcedTime = 0f;
                RefreshToken();
            } 
        }
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
    public bool NewUser;
    public DateTime CreateAt;
    public DateTime LastLoginAt;
    public long LoginExpireAt;
    public List<String> Roles;
}

public class RefreshTokenRequest
{
    public string RefreshToken;
}

public class RefreshTokenResponse
{
    public string RefreshToken;
    public string AccessToken;
    public long AccessTokenExpiry;
}
