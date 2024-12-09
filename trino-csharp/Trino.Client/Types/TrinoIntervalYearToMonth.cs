namespace Trino.Client.Types
{
    /// <summary>
    /// Represents a year to month interval
    /// </summary>
    public class TrinoIntervalYearToMonth
    {
        public int Year { get; private set; }
        public int Month { get; private set; }

        public TrinoIntervalYearToMonth(int year, int month)
        {
            Year = year;
            Month = month;
        }

        public override string ToString()
        {
            return $"{Year}-{Month}";
        }

        public override bool Equals(object obj)
        {
            if (obj is TrinoIntervalYearToMonth other)
            {
                return Year == other.Year && Month == other.Month;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Year.GetHashCode();
                hash = hash * 23 + Month.GetHashCode();
                return hash;
            }
        }
    }
}
