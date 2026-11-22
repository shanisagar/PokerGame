namespace PokerDemo.Core
{
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    // Values match hand strength, so Two = 2 and Ace = 14.
    public enum Rank
    {
        Two = 2,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace
    }

    public readonly struct Card
    {
        public readonly Rank Rank;
        public readonly Suit Suit;

        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        // Short form like "As" or "Td", used when cards are sent over the network.
        public string Code => "23456789TJQKA"[(int)Rank - 2].ToString() + "cdhs"[(int)Suit];

        public override string ToString()
        {
            return Rank + " of " + Suit;
        }
    }
}
