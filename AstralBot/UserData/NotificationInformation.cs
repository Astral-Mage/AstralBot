using AstralBot.Bot;
using AstralBot.Enums;

namespace AstralBot.UserData
{
    internal class NotificationInformation
    {
        [PrimaryKey(AutoIncrement = true)]
        public int Id { get; set; }

        [Column(Unique = true)]
        public string Title { get; set; } = string.Empty;

        [Column]
        public string Description { get; set; } = string.Empty;

        [Column]
        public NotificationType NotificationType { get; set; } = NotificationType.None;
    }
}
