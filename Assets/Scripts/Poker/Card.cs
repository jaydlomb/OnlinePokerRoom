namespace Poker
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public class Card
    {
        public Suit Suit;
        public Rank Rank;

        public Card(Suit suit, Rank rank) { Suit = suit; Rank = rank; }

        public override string ToString() => $"{Rank} of {Suit}";
    }
} 