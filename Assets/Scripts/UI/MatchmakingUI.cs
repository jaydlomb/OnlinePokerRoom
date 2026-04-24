using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Poker.Networking;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;

namespace Poker.UI
{
    public class MatchmakingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button findGameButton;
        [SerializeField] private Button leaveQueueButton;

        private bool _inQueue = false;

        private void Start()
        {
            findGameButton.onClick.AddListener(OnFindGame);
            leaveQueueButton.onClick.AddListener(OnLeaveQueue);
            leaveQueueButton.gameObject.SetActive(false);
            countdownText.text = "";
            statusText.text = "";

            // Listen for socket events
            SocketManager.Instance.On("queue:status", response =>
            {
                var data = response.GetValue<QueueStatus>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    statusText.text = $"Position: {data.position} | Players in Queue: {data.playersInQueue}");
            });

            SocketManager.Instance.On("queue:countdown", response =>
            {
                var data = response.GetValue<CountdownData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
                    _countdownCoroutine = StartCoroutine(CountdownTimer(data.seconds));
                });
            });

            SocketManager.Instance.On("queue:countdown_cancelled", response =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => countdownText.text = "");
            });

            SocketManager.Instance.On("lobby:created", response =>
            {
                var data = response.GetValue<LobbyCreated>();
                GameSession.LobbyID = data.lobbyID;
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay"));
            });
        }

        private void OnFindGame()
        {
            _inQueue = true;
            findGameButton.gameObject.SetActive(false);
            leaveQueueButton.gameObject.SetActive(true);
            statusText.text = "Searching for game...";

            SocketManager.Instance.Emit("queue:join", new
            {
                userID = AuthManager.Instance.UserID,
                username = AuthManager.Instance.Username,
                chips = AuthManager.Instance.Chips,
                rank = AuthManager.Instance.Rank
            });
        }

        private void OnLeaveQueue()
        {
            _inQueue = false;
            findGameButton.gameObject.SetActive(true);
            leaveQueueButton.gameObject.SetActive(false);
            statusText.text = "";
            countdownText.text = "";

            SocketManager.Instance.Emit("queue:leave", new { });
        }

        private Coroutine _countdownCoroutine;

        private IEnumerator CountdownTimer(int seconds)
        {
            float endTime = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < endTime)
            {
                int remaining = Mathf.CeilToInt(endTime - Time.realtimeSinceStartup);
                countdownText.text = $"Game starting in {remaining}s...";
                yield return null;
            }
            countdownText.text = "";
        }

        private class QueueStatus
        {
            public int position;
            public int playersInQueue;
        }

        private class CountdownData
        {
            public int seconds;
        }
    }
}

class LobbyCreated { public string lobbyID; }