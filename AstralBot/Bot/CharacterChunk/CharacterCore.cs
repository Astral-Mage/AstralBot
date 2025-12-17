using AstralBot.DataFrames;

namespace AstralBot.Bot.CharacterChunk
{
    public class CharacterCore
    {
        public IdInformation? IdInfo {  get; set; }
        public RpInformation? RpInfo { get; set; }
        public ClassInformation? ClassInfo { get; set; }
        public string GetName(bool rawname = false)
        {
            if (IdInfo == null) throw new Exception();
            if (rawname) return IdInfo.UserName;
            if (IdInfo.Nickname == null) return IdInfo.UserName;
            if (String.IsNullOrWhiteSpace(IdInfo.Nickname)) return IdInfo.UserName;
            return IdInfo.Nickname;
        }
    }
}
