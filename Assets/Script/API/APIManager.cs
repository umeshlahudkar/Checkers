using System;
using System.Collections;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;

public class APIManager : Singleton<APIManager>
{
    private readonly string baseURL = "https://localhost:7182";
    private string jwtToken;

    public LoginResponse loginResponse;

    public long expiry;

    public AuthService authService = new AuthService();

    

    private void Update()
    {
        authService.Update();
    }

    public void SetToken(LoginResponse loginResponse)
    {
        this.loginResponse = loginResponse;
        expiry = loginResponse.LoginExpireAt - (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
    }

    public void Get(string endpointURL, Action<string> onSuccess, Action<string> onFailed)
    {
        StartCoroutine(Get_CO(endpointURL, onSuccess, onFailed));
    }

    private IEnumerator Get_CO(string endpointURL, Action<string> onSuccess, Action<string> onFailed)
    {
        string url = baseURL + endpointURL;

        using(UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            if(!string.IsNullOrEmpty(jwtToken))
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            }

            yield return webRequest.SendWebRequest();

            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(webRequest.error);
            }
        }
    }

    public void Post(string endpointURL, string jsonBody, Action<string> onSuccess, Action<string> onFailed)
    {
        StartCoroutine(Post_CO(endpointURL, jsonBody, onSuccess, onFailed));
    }

    private IEnumerator Post_CO(string endpointURL, string jsonBody, Action<string> onSuccess, Action<string> onFailed)
    {
        string url = baseURL + endpointURL;
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            }

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(webRequest.error);
            }
        }
    }

    public void Put(string endpointURL, string jsonBody, Action<string> onSuccess, Action<string> onFailed)
    {
        StartCoroutine(Put_CO(endpointURL, jsonBody, onSuccess, onFailed));
    }

    private IEnumerator Put_CO(string endpointURL, string jsonBody, Action<string> onSuccess, Action<string> onFailed)
    {
        string url = baseURL + endpointURL;
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            }

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(webRequest.error);
            }
        }
    }

    public void Delete(string endpointURL, Action<string> onSuccess, Action<string> onFailed)
    {
        StartCoroutine(Delete_CO(endpointURL, onSuccess, onFailed));
    }

    private IEnumerator Delete_CO(string endpointURL, Action<string> onSuccess, Action<string> onFailed)
    {
        string url = baseURL + endpointURL;
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "DELETE"))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(jwtToken))
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            }

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(webRequest.error);
            }
        }
    }
}

[System.Serializable]
public class ApiError
{
    public long StatusCode;
    public string Message;
    public string RawResponse;
}
