namespace AbyssHunter.UI
{
    public static class MatchResult
    {
        public static bool HasEnded { get; private set; }

        public static bool TryEnd()
        {
            if (HasEnded)
            {
                return false;
            }

            HasEnded = true;
            return true;
        }

        public static void Reset()
        {
            HasEnded = false;
        }
    }
}
