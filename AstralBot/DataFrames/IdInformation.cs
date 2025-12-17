using AstralBot.Bot;

namespace AstralBot.DataFrames
{
    public class IdInformation
    {
        [PrimaryKey(AutoIncrement = true)]
        public int UserId { get; set; } = 0;


        [Column(Nullable = false, Unique = true)]
        public string UserName { get; set; } = string.Empty;


        [Column(Nullable = true)]
        public string? Nickname { get; set; } = null;
    }
}