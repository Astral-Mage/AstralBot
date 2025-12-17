using AstralBot.Bot.CharacterChunk;
using FChat.Enums;

namespace AstralBot.DungeonSystem
{
    internal class Dungeon
    {
        internal int DungeonId { get; set; } = 0;
        internal string Name { get; set; } = string.Empty;
        internal string Description { get; set; } = string.Empty;
        internal int DungeonLevel { get; set; } = 1;

        List<CharacterCore> Players { get; set; } = [];

        DungeonLayout Layout { get; set; } = new();

        bool DungeonHasBeenSetup { get; set; } = false;

        internal int OwnerId { get; set; } = 0;

        internal Dungeon(CharacterCore character, DungeonLayout layout)
        {
            if (character.IdInfo == null) throw new Exception();
            OwnerId = character.IdInfo.UserId;

            Players.Add(character);
        }

        internal bool CheckCommand(string character, string command, string message, string channel)
        {
            bool reply = false;

            return reply;
        }

        internal void RunDungeon(FChat.ChatConnection conn, string character, string command, string message, string channel, MessageTypeEnum messageType)
        {

        }

        internal void AddPlayer(CharacterCore character)
        {

        }

        private Dungeon() { }
    }
}
