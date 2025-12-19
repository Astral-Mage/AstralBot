using AstralBot.Bot.CharacterChunk;
using AstralBot.Databasing;
using AstralBot.DungeonSystem;
using AstralBot.Enums;
using AstralBot.RoleplaySystem;
using FChat.Enums;

namespace AstralBot.Bot
{
    internal partial class Astral
    {
        internal void MessageReceivedHandler(string character, string channel, string message, MessageTypeEnum messagetype)
        {
            // split the message into command and message
            string command = message.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries).First();
            string parsedmessage = message.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries).Last();
            if (string.IsNullOrWhiteSpace(parsedmessage) || parsedmessage.Equals(command))
                parsedmessage = string.Empty;

            if (DungeonMessageHandler(command, character, channel, parsedmessage, messagetype)) return;
            if (DefaultMessageHandler(command, character, channel, parsedmessage, messagetype)) return;
            if (RoleplayMessageHandler(command, character, channel, parsedmessage, messagetype)) return;
        }

        private bool DefaultMessageHandler(string command, string character, string channel, string message, MessageTypeEnum messagetype)
        {
            bool commandHandled = true;
            if (command.Equals(BotCommands.GetKinks))
            {
                string target = string.IsNullOrWhiteSpace(message) ? character : message;
                if (Conn?.GetCharacterFromList(target) != null)
                {
                    Conn?.GetUserKinks(target, character, messagetype, channel);
                    ConsoleWriter.Write($"[Getting Kinks] {character}");
                }
            }
            else if (command.Equals(BotCommands.JoinChannel))
            {
                if (!string.IsNullOrWhiteSpace(message))
                    Conn?.JoinChannel(message);
            }
            else if (command.Equals(BotCommands.LeaveChannel))
            {
                if (string.IsNullOrEmpty(message))
                    Conn?.LeaveChannel(channel);
                else
                    Conn?.LeaveChannel(message);
            }
            else if (command.Equals(BotCommands.GetCard))
            {
                CharacterCore curCharacter = GetCharacterByName(character);
                if (curCharacter.IdInfo != null && curCharacter.ClassInfo != null && curCharacter.ClassInfo.CurrentClass.Flyweights != null)
                {
                    string rankstr = "";
                    for (int i = 0; i < curCharacter.ClassInfo.CurrentRank; i++) rankstr += "▮";
                    for (int i = 0; i < curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank - curCharacter.ClassInfo.CurrentRank; i++) rankstr += "▯";
                    string replystr = $"→ {curCharacter.IdInfo.Nickname ?? curCharacter.IdInfo.UserName} —— [b]⦅[/b]{curCharacter.ClassInfo.CurrentClass.Flyweights.Name}[b]⦆[/b] —— {rankstr}";
                    if (curCharacter.ClassInfo != null && curCharacter.ClassInfo.CurrentClass != null && curCharacter.RpInfo != null)
                    {
                        replystr += $"[sup] (EXP: {curCharacter.ClassInfo.CurrentClass.Experience}/{curCharacter.ClassInfo.CurrentClass.GetXpNeededToRankUp()}) — (AFK: {Math.Round((decimal)curCharacter.RpInfo.FleshKincaidScore, 2)}){((curCharacter.ClassInfo.CurrentRank == curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank) ? $" Max Rank!" : "")}[/sup]";
                    }

                    Siren.Sing(messagetype, replystr, character, channel);
                }
            }
            else if (command.Equals(BotCommands.Shutdown) && character == "Astral")
            {
                ConsoleWriter.Write($"[Disconnecting] {character}");
                Conn?.Disconnect();
            }
            else
                commandHandled = false;

            return commandHandled;
        }

        private bool RoleplayMessageHandler(string command, string character, string channel, string message, MessageTypeEnum messagetype)
        {
            if (!command.Equals("/me")) return false;
            if (messagetype == MessageTypeEnum.Private) return false;

            CharacterCore curCharacter = GetCharacterByName(character);
            if (curCharacter != null && curCharacter.RpInfo != null)
            {
                var broken = BBCodeParser.StripAndTrackColors(message);
                PostBreakdown? parsed = WritingEvaluator.EvaluatePost(broken.Text, character);
                if (parsed != null)
                {
                    curCharacter.RpInfo.TotalPosts++;
                    if (broken.ColorOpenCount > 0) curCharacter.RpInfo.PostsWithColor++;
                    curCharacter.RpInfo.TotalParagraphs += parsed.Paragraphs;
                    curCharacter.RpInfo.TotalWords += parsed.Words;
                    curCharacter.RpInfo.FleshKincaidScore = (curCharacter.RpInfo.FleshKincaidScore * (curCharacter.RpInfo.TotalPosts - 1) + parsed.FleshKincaid) / (curCharacter.RpInfo.TotalPosts);
                    SqliteSchema.Update(curCharacter.RpInfo);
                    bool shortpost = parsed.Chars < 300;
                    string tosend = $"[sup]{(shortpost ? "🔻" : "")}Post Details | ([b]Rating:{parsed.FleshKincaid}[/b]) ([b]Base Experience:{parsed.BaseExperience}[/b]) ([b]Len Experience:{parsed.LengthExperience}[/b])[/sup]";
                    bool unlockedClass = false;
                    bool rankedup = false;
                    if (curCharacter.ClassInfo != null && curCharacter.ClassInfo.CurrentClass.Flyweights != null)
                    {
                        curCharacter.ClassInfo.ApplyExperience(parsed.BaseExperience, parsed.LengthExperience, shortpost, out rankedup);
                        tosend += $"[sup] — (EXP: {curCharacter.ClassInfo.CurrentClass.Experience}/{curCharacter.ClassInfo.CurrentClass.GetXpNeededToRankUp()}) — (AFK: {Math.Round((decimal)curCharacter.RpInfo.FleshKincaidScore, 2)}){(rankedup ? " — Rank Up!" : "")} {((curCharacter.ClassInfo.CurrentRank == curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank && rankedup) ? $"Max Rank!" : "")}[/sup]";
                        SqliteSchema.Update(curCharacter.ClassInfo);
                        unlockedClass = CheckForNewlyUnlockedClass(curCharacter, out _);
                        if (unlockedClass)
                        {
                            tosend += $"[sup][color=red]Class Unlocked❗[/color][/sup]";
                        }
                    }

                    if (unlockedClass || rankedup) Siren.Sing(MessageTypeEnum.Private, tosend, character, channel);
                }
            }
            return true;
        }
        private bool DungeonMessageHandler(string command, string character, string channel, string message, MessageTypeEnum messagetype)
        {
            bool commandHandled = false;

            if (command.Equals(BotCommands.CreateDungeon))
            {
                Dungeon? dung = FindDungeonByCharacterName(character);
                if (dung == null)
                {
                    CharacterCore curCharacter = GetCharacterByName(character);
                    dung = new Dungeon(curCharacter, new DungeonLayout());
                    ActiveDungeonRuns.Add(dung);
                    Siren.Sing(messagetype, $"Dungeon has been created, {curCharacter.GetName()}", character);
                }
                else
                {
                    Siren.Sing(messagetype, $"You already have an active dungeon in progress or setup phase.", character);
                }
                commandHandled = true;
            }
            else if (ActiveDungeonRuns.Count != 0 && Conn != null)
            {
                CharacterCore curCharacter = GetCharacterByName(character);
                List<Dungeon> foundDungeons = [];
                ActiveDungeonRuns.ForEach((run) =>
                {
                    if (run.CheckCommand(messagetype, curCharacter, command, message, channel))
                    {
                        commandHandled = true;
                        return;
                    }
                    return;
                });
            }
            return commandHandled;
        }

        private Dungeon? FindDungeonByCharacterName(string character)
        {
            Dungeon? reply = null;

            ActiveDungeonRuns.ForEach(x =>
            {
                if (x.Players.Any(y => y.GetName(true).Equals(character)))
                {
                    reply = x;
                    return;
                }
            });

            return reply;
        }

        private bool CheckForNewlyUnlockedClass(CharacterCore character, out List<string> unlockedClasses)
        {
            bool reply = false;
            unlockedClasses = [];
            if (Flyweights == null || character.ClassInfo == null || character.IdInfo == null) return reply;
            foreach (var classToCheck in Flyweights)
            {
                if (classToCheck.CheckIfUnlocked(character, out List<KeyValuePair<ClassRequirementType, string>> _))
                    unlockedClasses.Add(classToCheck.Name);
            }

            return reply;
        }
    }
}