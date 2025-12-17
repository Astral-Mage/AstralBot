using FChat;
using FChat.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
