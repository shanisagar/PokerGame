namespace PokerDemo.Core
{
    // Counts play time and says when another long stretch has passed.
    // It is only used for a gentle break reminder, nothing is shown while it counts.
    public sealed class SessionTimer
    {
        private readonly float intervalSeconds;
        private float nextReminderAt;

        public float TotalSeconds { get; private set; }

        public SessionTimer(float intervalSeconds)
        {
            this.intervalSeconds = intervalSeconds;
            nextReminderAt = intervalSeconds;
        }

        // Returns true once each time another interval of play time has passed.
        public bool Advance(float deltaSeconds)
        {
            TotalSeconds += deltaSeconds;

            if (TotalSeconds >= nextReminderAt)
            {
                nextReminderAt += intervalSeconds;
                return true;
            }

            return false;
        }
    }
}
