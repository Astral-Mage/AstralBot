using AstralBot.Bot;
using System.Text.Json.Serialization;

namespace AstralBot.DataFrames
{
    public class UserClassDetails
    {
        [JsonIgnore]
        public ClassFlyweights? Flyweights { get; set; } = null;

        [Column]
        public int Rank { get; set; } = 1;

        [Column]
        public int Experience { get; set; } = 0;

        public int GetXpNeededToRankUp()
        {
            if (Flyweights == null) return 0;
            if (Rank == Flyweights.MaxRank) return 99999999;

            int xpneeded = (int)Math.Round((decimal)(Flyweights.BaseLevelXp * Rank * (1 + Flyweights.Rarity)));
            return xpneeded;
        }
    }

    public class ClassInformation
    {
        [PrimaryKey]
        public int UserId { get; set; } = 0;

        [Column]
        public List<UserClassDetails> UserClasses { get; set; } = [];

        [Column]
        public List<int> UnlockedClasses { get; set; } = [];

        [Column]
        public int CurrentClassId { get; set; } = 0;

        public int CurrentRank { get; set; } = 1;

        [Column]
        public UserClassDetails CurrentClass { get; set; } = new();

        public ClassInformation(int userid, ClassFlyweights? startingClass)
        {
            if (startingClass == null) throw new Exception();
            UserId = userid;
            CurrentRank = 1;
            UserClasses = [];
            CurrentClass = new() { Flyweights = startingClass, Experience = 0, Rank = 1 };
            CurrentClassId = startingClass.ClassId;
            UnlockedClasses = [startingClass.ClassId];
        }
        public ClassInformation()
        { }

        public bool CheckForNewlyUnlockedClass()
        {
            bool reply = false;

            // add notification to user's notification list


            return reply;
        }

        // returns -1 if already max rank
        public int ApplyExperience(int basexp, int lenxp, bool shortpost, out bool rankedup)
        {
            int xpearned = 0;
            rankedup = false;

            if (CurrentClass != null && CurrentClass.Flyweights != null && CurrentRank == CurrentClass.Flyweights.MaxRank) return -1;
            if (CurrentClass == null || CurrentClass.Flyweights == null) return 0;

            CurrentClass.Experience += (int)Math.Round((decimal)(((shortpost ? basexp * .5 : basexp) + lenxp) * CurrentClass.Flyweights.XpGrowthRate), 0);
            if (CanRankUp() && CurrentClass.Experience >= CurrentClass.GetXpNeededToRankUp())
            {
                RankUp();
                rankedup = true;
            }

            return xpearned;
        }

        public void RankUp()
        {
            if (CurrentClass == null) return;
            CurrentClass.Experience = CurrentClass.GetXpNeededToRankUp();
            CurrentClass.Rank++;
        }

        public bool CanRankUp()
        {
            if (CurrentClass == null || CurrentClass.Flyweights == null) return false;
            return CurrentClass.Rank < CurrentClass.Flyweights.MaxRank;
        }
    }
}