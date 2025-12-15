using AstralBot.Bot;

namespace AstralBot.UserData
{
    public class RpInformation
    {
        [PrimaryKey(AutoIncrement = false)]
        public int UserId { get; set; } = 0;

        [Column]
        public int TotalWords { get; set; } = 0;

        [Column]
        public int TotalParagraphs { get; set; } = 0;

        [Column]
        public int TotalPosts { get; set; } = 0;

        [Column]
        public int PostsWithColor { get; set; } = 0;

        [Column]
        public double FleshKincaidScore { get; set; } = 0;

        public RpInformation(int userid)
        {
            UserId = userid;
            TotalParagraphs = 0;
            TotalPosts = 0;
            TotalWords = 0;
            PostsWithColor = 0;
            FleshKincaidScore = 0;
        }

        public RpInformation()
        { }
    }
}