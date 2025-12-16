using AstralBot.Bot;
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
    }
}
