using AstralBot.Bot;
using AstralBot.Bot.CharacterChunk;
using FChat;
using FChat.Enums;

namespace AstralBot.DungeonSystem
{
    internal class Dungeon
    {
        internal string DungeonId { get; set; } = string.Empty;
        internal string Name { get; set; } = string.Empty;
        internal string Description { get; set; } = string.Empty;
        internal int DungeonLevel { get; set; } = 1;

        internal List<CharacterCore> Players { get; set; } = [];

        internal DungeonLayout Layout { get; set; } = new();

        internal bool DungeonHasBeenSetup { get; set; } = false;

        internal int OwnerId { get; set; } = 0;

        private readonly object _locker = new();

        internal Dungeon(CharacterCore character, DungeonLayout layout)
        {
            if (character.IdInfo == null) throw new Exception();
            OwnerId = character.IdInfo.UserId;
            Layout = layout;
            Players.Add(character);
            DungeonId = GuidStuff.CreateId();
        }

        internal bool CheckCommand(MessageTypeEnum messagetype, CharacterCore character, string command, string message, string channel)
        {
            bool reply = false;
            lock (_locker)
            {
                HandleDungeonCommands(messagetype, character, command, message, channel);
            }
            return reply;
        }

        private bool HandleDungeonCommands(MessageTypeEnum messagetype, CharacterCore character, string command, string message, string channel)
        {
            if (character.IdInfo == null) return false;
            bool reply = true;

            if (command.Equals(BotCommands.JoinDungeon))
            {
                if (OwnerId == character.IdInfo.UserId)
                {
                    Siren.Sing(MessageTypeEnum.Private, $"Why are you trying to join your own run, {character}?", character.IdInfo.UserName);
                    return true;
                }

                var owner = Players.First(x => x?.IdInfo?.UserId == OwnerId);
                if (Players.Any(x => x.GetName(true).Equals(character.IdInfo.UserName)))
                {
                    Siren.Sing(MessageTypeEnum.Private, $"You've already joined {owner.GetName()}'s run, {character}?", character.IdInfo.UserName);
                    return true;
                }
                

            }
            else reply = false;

            return reply;
        }

        internal void RunDungeon(string character, string command, string message, string channel, MessageTypeEnum messageType)
        {

        }

        internal void AddPlayer(CharacterCore character)
        {

        }

        private Dungeon() { }
    }
}
