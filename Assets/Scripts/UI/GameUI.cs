using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;
using System.Linq;

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
                if (string.IsNullOrEmpty(raiseAmountField.text)) return;
                if (!int.TryParse(raiseAmountField.text, out int amount)) return;
                if (amount <= 0) return;
                SendAction("raise", amount);
            });

            SetButtonsActive(false);
            _lobbyID = GameSession.LobbyID;
            actionText.text = "";

            SocketManager.Instance.On("game:hand", response =>
            {
                var data = response.GetValue<HandData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ClearCards(holeCardContainer);
                    foreach (var card in data.cards)
                        SpawnCard(card, holeCardContainer);
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
                        SpawnCard(card, communityCardContainer);
                });
            });

            SocketManager.Instance.On("game:action_request", response =>
            {
                var data = response.GetValue<ActionRequest>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (data.userID == AuthManager.Instance.UserID)
                    {
                        foldButton.interactable = data.actions.Contains("fold");
                        checkButton.interactable = data.actions.Contains("check");
                        callButton.interactable = data.actions.Contains("call");
                        raiseButton.interactable = data.actions.Contains("raise");
                        actionText.text = "Your turn!";
                    }
                    else
                    {
                        SetButtonsActive(false);
                        actionText.text = "Waiting for opponent...";
                    }
                });
            });

            SocketManager.Instance.On("game:action_broadcast", response =>
            {
                var data = response.GetValue<ActionBroadcast>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    string username = FindObjectOfType<PlayerSlotsManager>().GetUsername(data.userID);
                    actionText.text = $"{username} did {data.action} | Pot: {data.pot}";
                    StartCoroutine(ClearActionText());
                });
            });

            SocketManager.Instance.On("game:showdown", response =>
            {
                var data = response.GetValue<ShowdownData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    SetButtonsActive(false);

                    if (data.winners != null && data.winners.Count > 0)
                    {
                        var w = data.winners[0];
                        string winnerName = FindObjectOfType<PlayerSlotsManager>().GetUsername(w.userID);

                        if (data.hands != null && data.hands.Count > 0)
                        {
                            // River showdown — reveal opponent hands
                            foreach (var playerHand in data.hands)
                            {
                                if (playerHand.userID == AuthManager.Instance.UserID) continue;
                                var slot = FindObjectOfType<PlayerSlotsManager>().GetSlotByUserID(playerHand.userID);
                                if (slot != null && slot.GetCardContainer() != null)
                                {
                                    foreach (var card in playerHand.cards)
                                        SpawnCard(card, slot.GetCardContainer());
                                }
                            }
                            actionText.text = $"{winnerName} wins with {w.handName}!";
                        }
                        else
                        {
                            actionText.text = $"{winnerName} wins!";
                        }
                    }

                    StartCoroutine(ClearCardsAfterDelay());
                });
            });

            SocketManager.Instance.On("game:player_left", response =>
            {
                var data = response.GetValue<PlayerLeft>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    string username = FindObjectOfType<PlayerSlotsManager>().GetUsername(data.userID);
                    actionText.text = $"{username} left the game";
                });
            });

            SocketManager.Instance.On("game:over", response =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    actionText.text = "Game Over!");
            });

            Debug.Log("Emitting game:ready for lobby: " + _lobbyID);
            SocketManager.Instance.Emit("game:ready", new
            {
                lobbyID = _lobbyID,
                userID = AuthManager.Instance.UserID
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

        private IEnumerator ClearActionText()
        {
            yield return new WaitForSeconds(3f);
            actionText.text = "";
        }

        private IEnumerator ClearCardsAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            ClearCards(holeCardContainer);
            ClearCards(communityCardContainer);
            // Also clear opponent slot card containers
            var slotsManager = FindObjectOfType<PlayerSlotsManager>();
            if (slotsManager != null)
            {
                foreach (var slot in slotsManager.GetAllSlots())
                {
                    if (slot.GetCardContainer() != null)
                        ClearCards(slot.GetCardContainer());
                }
            }
        }

        private class HandData { public List<CardData> cards; }
        private class CardData { public int rank; public string suit; }
        private class GameState { public string phase; public int pot; public int currentBet; public List<CardData> communityCards; }
        private class ActionRequest { public string userID; public List<string> actions; }
        private class ActionBroadcast { public string userID; public string action; public int amount; public int pot; }
        private class ShowdownData { public List<Winner> winners; public List<PlayerHand> hands; }
        private class PlayerHand { public string userID; public List<CardData> cards; }
        private class Winner { public string userID; public string handName; public object hand; }
        private class PlayerLeft { public string userID; public string action; }
    }
}