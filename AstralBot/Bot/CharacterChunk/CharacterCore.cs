using AstralBot.UserData;

namespace AstralBot.Bot.CharacterChunk
{
    internal class CharacterCore
    {
        internal IdInformation? IdInfo {  get; set; }
        internal RpInformation? RpInfo { get; set; }
        internal ClassInformation? ClassInfo { get; set; }
        internal List<NotificationInformation>? Notifications { get; set; }

        public bool HasNewNotification()
        {
            return false;
        }
    }
}
