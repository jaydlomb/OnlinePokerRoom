using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PimDeWitte.UnityMainThreadDispatcher;
using Poker.Networking;

namespace Poker.UI
{
    public class PlayerSlotsManager : MonoBehaviour
    {
        [SerializeField] private PlayerSlotUI[] playerSlots; 

        private readonly Dictionary<string, int> _userToSlot = new();

        private void Start()
        {
            foreach (var slot in playerSlots)
                slot.Clear();

            SocketManager.Instance.On("game:state", response =>
            {
                var data = response.GetValue<GameState>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log($"game:state received, players null: {data.players == null}, count: {data.players?.Count}");
                    if (_userToSlot.Count == 0 && data.players != null)
                        AssignSlots(data.players);

                    if (data.players != null)
                    {
                        foreach (var p in data.players)
                        {
                            if (!_userToSlot.TryGetValue(p.userID, out int idx)) continue;
                            playerSlots[idx].UpdateChips(p.chips);
                            Debug.Log($"Updating chips for {p.userID}: {p.chips}");
                            playerSlots[idx].SetFolded(p.folded);
                            playerSlots[idx].SetActiveTurn(p.userID == data.activePlayer);
                        }
                    }
                });
            });

            SocketManager.Instance.On("game:showdown", response =>
            {
                var data = response.GetValue<ShowdownData>();
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    foreach (var slot in playerSlots)
                        slot.SetActiveTurn(false);
                });
            });

            SocketManager.Instance.Emit("game:ready", new
            {
                lobbyID = GameSession.LobbyID,
                userID = AuthManager.Instance.UserID
            });
        }

        public string GetUsername(string userID)
        {
            if (_userToSlot.TryGetValue(userID, out int idx))
                return playerSlots[idx].GetUsername();
            return userID; // fallback to userID if not found
        }

        public PlayerSlotUI GetSlotByUserID(string userID)
        {
            if (_userToSlot.TryGetValue(userID, out int idx))
                return playerSlots[idx];
            return null;
        }

        public PlayerSlotUI[] GetAllSlots() => playerSlots;

        private void AssignSlots(List<PlayerInfo> players)
        {
            int localSlot = 0;
            int otherSlot = 1;

            foreach (var p in players)
            {
                bool isMe = p.userID == AuthManager.Instance.UserID;
                int slotIdx = isMe ? localSlot++ : otherSlot++;
                if (slotIdx >= playerSlots.Length) break;

                _userToSlot[p.userID] = slotIdx;
                playerSlots[slotIdx].Assign(p.userID, p.username, p.chips);
            }
        }

        private class GameState
        {
            public string phase;
            public int pot;
            public int currentBet;
            public string activePlayer;
            public List<PlayerInfo> players;
            public List<CardData> communityCards;
        }

        private class PlayerInfo
        {
            public string userID;
            public string username;
            public int chips;
            public bool folded;
        }

        private class CardData { public int rank; public string suit; }

        private class ShowdownData
        {
            public List<Winner> winners;
            public List<PlayerHand> hands;
        }

        private class Winner { public string userID; public object hand; }
        private class PlayerHand { public string userID; public List<CardData> cards; }
    }
}