using AstralBot.Bot;
using AstralBot.Bot.CharacterChunk;
using AstralBot.Enums;

namespace AstralBot.DataFrames
{
    public class ClassFlyweights
    {
        [PrimaryKey(AutoIncrement = true)]
        public int ClassId { get; set; } = 0;

        [Column(Unique = true)]
        public string Name { get; set; } = string.Empty;

        [Column]
        public string Description { get; set; } = string.Empty;

        [Column]
        public int Rarity { get; set; } = 0;

        [Column]
        public int MaxRank { get; set; } = 5;

        [Column]
        public ClassColors ClassColor { get; set; } = ClassColors.Gray;

        [Column]
        public int BaseLevelXp { get; set; } = 1000;

        [Column]
        public double XpGrowthRate { get; set; } = 1.0;

        [Column]
        public Dictionary<Stats, double> StatsGrowthRate { get; set; } = [];

        [Column]
        public Dictionary<ClassRequirementType, string> Requirements { get; set; } = [];

        public bool CheckIfUnlocked(CharacterCore character, out List<KeyValuePair<ClassRequirementType, string>> failedRequirements)
        {
            bool reply = false;
            failedRequirements = [];
            if (character.ClassInfo == null || character.IdInfo == null || character.ClassInfo.CurrentClass.Flyweights == null) return reply;
            if (character.ClassInfo.UnlockedClasses.Any(x => x.Equals(ClassId))) return reply;

            int satisfiedRequirements = 0;
            foreach (var requirement in Requirements)
            {
                switch (requirement.Key)
                {
                    case ClassRequirementType.ClassMaxxed:
                        {
                            UserClassDetails? fw2 = character.ClassInfo.UserClasses.FirstOrDefault(x => x.Flyweights?.ClassId.Equals(ClassId) ?? false);
                            if (fw2 == null && character.ClassInfo.CurrentClass.Flyweights.ClassId.Equals(ClassId)) fw2 = character.ClassInfo.CurrentClass;
                            if (fw2 == null) return false;
                            if (fw2.Rank.Equals(MaxRank))
                            {
                                satisfiedRequirements++;
                            }
                            else
                            {
                                failedRequirements.Add(requirement);
                            }
                        }
                        break;
                    case ClassRequirementType.ClassUnlocked:
                        {
                            if (character.ClassInfo.UnlockedClasses.Any(y => y.Equals(ClassId))) satisfiedRequirements++;
                            else failedRequirements.Add(requirement);
                        }
                        break;
                    default:
                        {
                            failedRequirements.Add(requirement);
                            ConsoleWriter.Write($"Unhandled requirement type for class: {Name} | {character.IdInfo.UserName}");
                        }
                        break;
                }
            }

            if (satisfiedRequirements == Requirements.Count) character.ClassInfo.UnlockedClasses.Add(ClassId);

            return reply;
        }
    }
}
