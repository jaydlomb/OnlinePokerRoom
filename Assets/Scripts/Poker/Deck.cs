using System.Collections.Generic;
using UnityEngine;

namespace Poker
{
    public class Deck
    {
        private List<Card> _cards = new();

        public Deck() => Reset();

        public void Reset()
        {
            _cards.Clear();
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
                foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
                    _cards.Add(new Card(s, r));
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Deal()
        {
            Card top = _cards[0];
            _cards.RemoveAt(0);
            return top;
        }
    }
}