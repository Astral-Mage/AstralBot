using AstralBot.Bot.CharacterChunk;
using AstralBot.Databasing;
using AstralBot.RoleplaySystem;
using FChat.Enums;

namespace AstralBot.Bot
{
    internal partial class Astral
    {
        internal void MessageReceivedHandler(string character, string channel, string message, MessageTypeEnum messagetype)
        {
            DefaultMessageHandler(character, channel, message, messagetype);
            RoleplayMessageHandler(character, channel, message, messagetype);
        }

        private void DefaultMessageHandler(string character, string channel, string message, MessageTypeEnum messagetype)
        {
            if (message.StartsWith(".gk"))
            {
                string target = character;
                if (message.Contains(' '))
                    target = message.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries).Last();
                if (Conn?.GetCharacterFromList(target) != null)
                {
                    Conn?.GetUserKinks(target, character, messagetype, channel);
                    ConsoleWriter.Write($"[Getting Kinks] {character}");
                }
            }
            else if (message.StartsWith(".jr"))
            {
                Conn?.JoinChannel(message.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries).Last());
            }
            else if (message.StartsWith(".lr"))
            {
                Conn?.LeaveChannel(message.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries).Last());
            }
            else if (message.StartsWith(".c"))
            {
                CharacterCore curCharacter = GetCharacterByName(character);
                if (curCharacter.IdInfo != null && curCharacter.ClassInfo != null && curCharacter.ClassInfo.CurrentClass.Flyweights != null)
                {
                    string rankstr = "";
                    for (int i = 0; i < curCharacter.ClassInfo.CurrentRank; i++) rankstr += "▮";
                    for (int i = 0; i < curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank - curCharacter.ClassInfo.CurrentRank; i++) rankstr += "▯";
                    string replystr = $"→ {curCharacter.IdInfo.Nickname ?? curCharacter.IdInfo.UserName} —— [b]⦅[/b]{curCharacter.ClassInfo.CurrentClass.Flyweights.Name}[b]⦆[/b] —— {rankstr}";
                    if (curCharacter.ClassInfo != null && curCharacter.ClassInfo.CurrentClass != null  && curCharacter.RpInfo  != null)
                    {
                        replystr += $"[sup] (EXP: {curCharacter.ClassInfo.CurrentClass.Experience}/{curCharacter.ClassInfo.CurrentClass.GetXpNeededToRankUp()}) — (AFK: {Math.Round((decimal)curCharacter.RpInfo.FleshKincaidScore, 2)}){((curCharacter.ClassInfo.CurrentRank == curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank) ? $" Max Rank!" : "")}[/sup]";
                    }
                    if (messagetype == MessageTypeEnum.Private) Conn?.SendPrivateMessage(character, replystr);
                    else if (messagetype == MessageTypeEnum.Channel) Conn?.SendChannelMessage(channel, replystr);
                }
            }
            else if (message.StartsWith(".sd") && character == "Astral")
            {
                ConsoleWriter.Write($"[Disconnecting] {character}");
                Conn?.Disconnect();
            }
        }

        private void RoleplayMessageHandler(string character, string channel, string message, MessageTypeEnum messagetype)
        {
            if (!message.StartsWith("/me")) return;

            CharacterCore curCharacter = GetCharacterByName(character);
            if (curCharacter != null && curCharacter.RpInfo != null)
            {
                var broken = BbCode.StripAndTrackColors(message);
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
                        tosend += $"[sup] — (EXP: {curCharacter.ClassInfo.CurrentClass.Experience}/{curCharacter.ClassInfo.CurrentClass.GetXpNeededToRankUp()}) — (AFK: {Math.Round((decimal)curCharacter.RpInfo.FleshKincaidScore, 2)}){(rankedup ? " — Rank Up!" : "")} {((curCharacter.ClassInfo.CurrentRank == curCharacter.ClassInfo.CurrentClass.Flyweights.MaxRank && rankedup) ? $"Max Rank!": "")}[/sup]";
                        SqliteSchema.Update(curCharacter.ClassInfo);
                        unlockedClass = curCharacter.ClassInfo.CheckForNewlyUnlockedClass();
                        if (unlockedClass)
                        {
                            tosend += $"[sup][color=red]Class Unlocked❗[/color][/sup]";
                        }
                    }

                    if (messagetype == MessageTypeEnum.Private || unlockedClass || rankedup ) Conn?.SendPrivateMessage(character, tosend);
                }
            }
        }

        internal void UserKinksReceivedHandler(string character, string requester, List<KeyValuePair<string, string>> information)
        {
            string toreply = string.Empty;
            toreply += $"[b][User Kinks Obtained ({information.Count})] {character}[/b]";
            foreach (var v in information)
                toreply += $"{Environment.NewLine}  -([b]{v.Key}[/b]) {v.Value}";

            Conn?.SendPrivateMessage(requester, toreply);
        }
    }
}