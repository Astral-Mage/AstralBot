using AstralBot.RoleplaySystem;
using FChat;
using FChat.EventArguments;

namespace AstralBot
{
    public partial class Program
    {
        private static void UserProfileInfoReceivedHandler(object sender, UserInfoEventArgs e)
        {
            ConsoleWriter.Write($"[User Profile Info Obtained ({e.Information.Count})] {e.Character}");
        }

        private static void UserLoggedHandler(object sender, UserLoggedEventArgs e)
        {
            ConsoleWriter.Write($"[{(e.UserStatus == LoginStatus.Offline ? "Logout" : "Login")}] {e.Identity}");
        }

        private static void UserLeftChannelHandler(object sender, ChannelEntryEventArgs e)
        {
            ConsoleWriter.Write($"[Left ({e.Channel})] {e.Identity}");
        }

        private static void ChannelsReceivedHandler(object sender, ChannelListEventArgs e)
        {
            ConsoleWriter.Write($"[Channels Obtained] {e.Channels.Count}");
        }

        private static void UserJoinedChannelHandler(object sender, ChannelEntryEventArgs e)
        {
            ConsoleWriter.Write($"[Joined ({e.Channel})] {e.Identity}");
        }

        private static void TypingChangedHandler(object sender, TypingEventArgs e)
        {
            ConsoleWriter.Write($"[Type Status ({e.Character})] {e.Status}");
        }

        private static void SystemMessageReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            ConsoleWriter.Write($"[System ({e.Channel})] {e.Message}");
        }

        private static void ServerStatsReceivedHandler(object sender, ServerStatsEventArgs e)
        {
            ConsoleWriter.Write($"[Server Stats] Start Time: {e.StartTime} | Current Time: {e.CurrentTime} | Connected Users: {e.CurrentUsers} | Logins: {e.UsersAcceptedSinceStart}");
        }

        private static void FriendListReceivedHandler(object sender, FriendListEventArgs e)
        {
            ConsoleWriter.Write($"[Friends Obtained ({e.Friends.Count})] {string.Join(", ", e.Friends)}");
        }

        private static void CharacterListReceivedHandler(object sender, CharacterListEventArgs e)
        {
            ConsoleWriter.Write($"[Character List Obtained ({e.Characters.Count})]");
        }
        private static void ErrorReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            ConsoleWriter.Write($"[Error ({e.ErrorNumber})] {e.Message}");
        }

        private static void ChannelDataUpdatedHandler(object sender, ChannelDataEventArgs e)
        {
            ConsoleWriter.Write($"[Channel Data ({e.Channel})] Mode: {e.Mode} | Usercount: {e.Users.Count}");
        }

        private static void BroadcastReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            ConsoleWriter.Write($"[Broadcast] {e.Message}");
        }

        private static void AdReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            ConsoleWriter.Write($"[{e.Channel} ({e.Character})] (Ad): {e.Message}");
        }

        private static void UserKinksReceivedHandler(object sender, UserInfoEventArgs e)
        {
            _AstralBot?.UserKinksReceivedHandler(e.Character, e.Channel, e.Operator, e.Information, e.MessageType);
            ConsoleWriter.Write($"[Kinks Obtained ({e.Character})] {e.Message}");
        }

        private static void PrivateMessageReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            _AstralBot?.MessageReceivedHandler(e.Character, e.Channel, e.Message, e.MessageType);
            ConsoleWriter.Write($"[Private Message ({e.Character})] {e.Message}");
        }

        private static void ChannelMessageReceivedHandler(object sender, ServerChatMessageEventArgs e)
        {
            _AstralBot?.MessageReceivedHandler(e.Character, e.Channel, e.Message, e.MessageType);
            ConsoleWriter.Write($"[Channel Message ({e.Character})] {e.Channel}: {e.Message}");
        }
    }
}