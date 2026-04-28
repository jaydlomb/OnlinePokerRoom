using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;

namespace Poker.Networking
{
    public class SocketManager : MonoBehaviour
    {
        public static SocketManager Instance;
        private SocketIOUnity _socket;
        private string _serverURL = "https://onlinepokerroom.onrender.com";
        //private string _serverURL = "http://localhost:3000";
        private readonly Dictionary<string, List<Action<SocketIOResponse>>> _handlers = new();

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        public void Connect(string token)
        {
            var uri = new Uri(_serverURL);
            _socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Auth = new Dictionary<string, string> { { "token", token } },
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });
            _socket.JsonSerializer = new NewtonsoftJsonSerializer();
            _socket.OnConnected += (sender, e) => Debug.Log("Connected to server");
            _socket.OnDisconnected += (sender, e) => Debug.Log("Disconnected from server");
            _socket.OnError += (sender, e) => Debug.LogError($"Socket error: {e}");
            _socket.Connect();
        }

        public void Emit(string eventName, object data)
        {
            _socket?.Emit(eventName, data);
        }

        public void On(string eventName, Action<SocketIOResponse> callback)
        {
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers[eventName] = new List<Action<SocketIOResponse>>();
                // Register with the socket only once per event name
                _socket?.On(eventName, response =>
                {
                    foreach (var handler in _handlers[eventName])
                        handler(response);
                });
            }
            _handlers[eventName].Add(callback);
        }
        public void ClearHandlers()
        {
            _handlers.Clear();
        }

        private void OnApplicationQuit()
        {
            _socket?.Disconnect();
        }
    }
}