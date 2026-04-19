using System.Collections.Generic;
using UnityEngine;

namespace Poker
{
    [System.Serializable]
    public class Player
    {
        public string Name;
        public int Chips;

        public Player(string name, int chips = 1000) { Name = name; Chips = chips; }
    }

    public class PokerGameManager : MonoBehaviour
    {
        public List<Player> Players { get; private set; } = new();

        public void AddPlayer(string name) => Players.Add(new Player(name));
    }
}