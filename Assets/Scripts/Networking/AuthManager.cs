using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Poker.Networking
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance;

        private string _serverURL = "http://localhost:3000";

        public string UserID { get; private set; }
        public string Username { get; private set; }
        public int Chips { get; private set; }
        public int Rank { get; private set; }
        public string Token { get; private set; }

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        public IEnumerator Register(string username, string password, Action<bool, string> callback)
        {
            var body = JsonConvert.SerializeObject(new { username, password });
            var request = new UnityWebRequest($"{_serverURL}/auth/register", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                callback(true, null);
            else
                callback(false, request.downloadHandler.text);
        }

        public IEnumerator Login(string username, string password, Action<bool, string> callback)
        {
            var body = JsonConvert.SerializeObject(new { username, password });
            var request = new UnityWebRequest($"{_serverURL}/auth/login", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                Token = response.token;
                UserID = response.userID;
                Username = response.username;
                Chips = response.chips;
                Rank = response.rank;
                SocketManager.Instance.Connect(Token);
                callback(true, null);
            }
            else
            {
                callback(false, request.downloadHandler.text);
            }
        }

        public void UpdateChips(int chips)
        {
            Chips = chips;
        }

        public void UpdateRank(int rank)
        {
            Rank = rank;
        }

        [Serializable]
        private class LoginResponse
        {
            public string token;
            public string userID;
            public string username;
            public int chips;
            public int rank;
        }
    }
}