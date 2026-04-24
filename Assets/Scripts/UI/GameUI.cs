using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text potText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text currentBetText;
        [SerializeField] private TMP_Text communityCardsText;
        [SerializeField] private TMP_Text holeCardsText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private TMP_InputField raiseAmountField;

        private string _lobbyID;

        private void Start()
        {
            foldButton.onClick.AddListener(() => SendAction("fold", 0));
            checkButton.onClick.AddListener(() => SendAction("check", 0));
            callButton.onClick.AddListener(() => SendAction("call", 0));
            raiseButton.onClick.AddListener(() => {
                int amount = int.TryParse(raiseAmountField.text, out int val) ? val : 0;
                SendAction("raise", amount);
            });

            SetButtonsActive(false);
            _lobbyID = GameSession.LobbyID;

            SocketManager.Instance.On("game:hand", response =>
            {
                var data = response.GetValue<HandData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    holeCardsText.text = $"Your Hand: {string.Join(", ", data.cards)}");
            });

            SocketManager.Instance.On("game:state", response =>
            {
                var data = response.GetValue<GameState>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    potText.text = $"Pot: {data.pot}";
                    phaseText.text = $"Phase: {data.phase}";
                    currentBetText.text = $"Current Bet: {data.currentBet}";
                    communityCardsText.text = $"Community: {string.Join(", ", data.communityCards)}";
                });
            });

            SocketManager.Instance.On("game:action_request", response =>
            {
                var data = response.GetValue<ActionRequest>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (data.userID == AuthManager.Instance.UserID)
                    {
                        SetButtonsActive(true);
                        actionText.text = "Your turn!";
                    }
                    else
                    {
                        actionText.text = $"Waiting for opponent...";
                    }
                });
            });

            SocketManager.Instance.On("game:action_broadcast", response =>
            {
                var data = response.GetValue<ActionBroadcast>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    actionText.text = $"{data.userID} did {data.action} | Pot: {data.pot}";
                    SetButtonsActive(false);
                });
            });

            SocketManager.Instance.On("game:showdown", response =>
            {
                var data = response.GetValue<ShowdownData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    actionText.text = $"Winner: {data.winners[0].userID} with {data.winners[0].hand}");
            });

            SocketManager.Instance.On("game:player_left", response =>
            {
                var data = response.GetValue<PlayerLeft>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    actionText.text = $"{data.userID} left the game");
            });

            SocketManager.Instance.On("game:over", response =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    actionText.text = "Game Over!");
            });

            SocketManager.Instance.Emit("Lobby:ready", new { lobbyID = _lobbyID });
        }

        private void SendAction(string action, int amount)
        {
            SetButtonsActive(false);
            SocketManager.Instance.Emit("game:action", new
            {
                lobbyID = _lobbyID,
                userID = AuthManager.Instance.UserID,
                action,
                amount
            });
        }

        private void SetButtonsActive(bool active)
        {
            foldButton.interactable = active;
            checkButton.interactable = active;
            callButton.interactable = active;
            raiseButton.interactable = active;
        }

        private class LobbyData { public string lobbyID; public List<object> players; }
        private class HandData { public List<object> cards; }
        private class GameState { public string phase; public int pot; public int currentBet; public List<object> communityCards; }
        private class ActionRequest { public string userID; public List<string> actions; }
        private class ActionBroadcast { public string userID; public string action; public int amount; public int pot; }
        private class ShowdownData { public List<Winner> winners; }
        private class Winner { public string userID; public object hand; }
        private class PlayerLeft { public string userID; public string action; }
    }
}