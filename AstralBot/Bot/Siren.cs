using FChat;
using FChat.Enums;
using System.Security.Cryptography;

namespace AstralBot.Bot
{
    public static class Siren
    {
        private static ChatConnection? Conn;

        public static void Initalize(ChatConnection? conn)
        {
            Conn = conn;
        }

        public static void Sing(MessageTypeEnum messageType, string message, string character, string? channel = null)
        {
            if (messageType == MessageTypeEnum.Private || channel == null)
                Conn?.SendPrivateMessage(character, message);
            else if (messageType == MessageTypeEnum.Channel)
                Conn?.SendChannelMessage(character, channel);
        }
    }

    public static class GuidStuff
    {
        public static string CreateId()
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Span<byte> bytes = stackalloc byte[9];
            RandomNumberGenerator.Fill(bytes);

            Span<char> result = stackalloc char[9];
            for (int i = 0; i < 9; i++)
                result[i] = chars[bytes[i] % chars.Length];

            return new string(result);
        }
    }
}
