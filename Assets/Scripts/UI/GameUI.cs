using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;

namespace Poker.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text potText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text currentBetText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private TMP_InputField raiseAmountField;

        [SerializeField] private Transform holeCardContainer;
        [SerializeField] private Transform communityCardContainer;
        [SerializeField] private GameObject cardPrefab;

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
                {
                    ClearCards(holeCardContainer);
                    foreach (var card in data.cards)
                    {
                        SpawnCard(card, holeCardContainer);
                    }
                });
            });

            SocketManager.Instance.On("game:state", response =>
            {
                var data = response.GetValue<GameState>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    potText.text = $"Pot: {data.pot}";
                    phaseText.text = $"Phase: {data.phase}";
                    currentBetText.text = $"Current Bet: {data.currentBet}";

                    ClearCards(communityCardContainer);
                    foreach (var card in data.communityCards)
                    {
                        SpawnCard(card, communityCardContainer);
                    }
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
                        actionText.text = "Waiting for opponent...";
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
        }

        private void SpawnCard(CardData card, Transform container)
        {
            GameObject obj = Instantiate(cardPrefab, container);
            CardDisplay display = obj.GetComponent<CardDisplay>();
            if (display != null)
                display.SetCard(card.rank, card.suit);
        }

        private void ClearCards(Transform container)
        {
            foreach (Transform child in container)
                Destroy(child.gameObject);
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

        private class HandData { public List<CardData> cards; }
        private class CardData { public int rank; public string suit; }
        private class GameState { public string phase; public int pot; public int currentBet; public List<CardData> communityCards; }
        private class ActionRequest { public string userID; public List<string> actions; }
        private class ActionBroadcast { public string userID; public string action; public int amount; public int pot; }
        private class ShowdownData { public List<Winner> winners; }
        private class Winner { public string userID; public object hand; }
        private class PlayerLeft { public string userID; public string action; }
    }
}