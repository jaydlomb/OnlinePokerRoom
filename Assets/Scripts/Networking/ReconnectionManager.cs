using UnityEngine;
using UnityEngine.SceneManagement;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;

namespace Poker.Networking
{
    public class ReconnectionManager : MonoBehaviour
    {
        public static ReconnectionManager Instance;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        private void Start()
        {
            SocketManager.Instance.On("disconnect", response =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (SceneManager.GetActiveScene().name == "Gameplay")
                    {
                        StartCoroutine(AttemptReconnect());
                    }
                });
            });

            SocketManager.Instance.On("game:rejoin_failed", response =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    SceneManager.LoadScene("Matchmaking");
                });
            });
        }

        private System.Collections.IEnumerator AttemptReconnect()
        {
            Debug.Log("Disconnected — attempting to reconnect...");
            yield return new WaitForSeconds(2f);

            SocketManager.Instance.Connect(AuthManager.Instance.Token);

            yield return new WaitForSeconds(2f);

            SocketManager.Instance.Emit("game:rejoin", new
            {
                lobbyID = GameSession.LobbyID
            });
        }
    }
}