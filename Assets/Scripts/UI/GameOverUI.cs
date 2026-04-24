using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;

namespace Poker.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultsText;
        [SerializeField] private Button requeueButton;
        [SerializeField] private Button leaveButton;

        private void Start()
        {
            gameOverPanel.SetActive(false);
            requeueButton.onClick.AddListener(OnRequeue);
            leaveButton.onClick.AddListener(OnLeave);

            SocketManager.Instance.On("game:over", response =>
            {
                var data = response.GetValue<GameOverData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    gameOverPanel.SetActive(true);

                    var me = data.players.Find(p => p.userID == AuthManager.Instance.UserID);
                    if (me != null)
                    {
                        AuthManager.Instance.UpdateChips(me.chips);
                        AuthManager.Instance.UpdateRank(me.rank);
                    }

                    string results = "Final Results:\n\n";
                    for (int i = 0; i < data.players.Count; i++)
                    {
                        var p = data.players[i];
                        results += $"#{i + 1} {p.username} — {p.chips} chips | Rank: {p.rank} ({(p.rankChange >= 0 ? "+" : "")}{p.rankChange})\n";
                    }
                    resultsText.text = results;
                });
            });
        }

        private void OnRequeue()
        {
            SceneManager.LoadScene("Matchmaking");
        }

        private void OnLeave()
        {
            Application.Quit();
        }

        private class GameOverData
        {
            public List<PlayerResult> players;
        }

        private class PlayerResult
        {
            public string userID;
            public string username;
            public int chips;
            public int rank;
            public int rankChange;
        }
    }
}