using AstralBot.Bot.CharacterChunk;
using AstralBot.Databasing;
using AstralBot.DataFrames;
using AstralBot.DungeonSystem;
using AstralBot.Enums;
using FChat;

namespace AstralBot.Bot
{
    internal partial class Astral
    {
        readonly string StartingClass = "Survivor";
        readonly string DatabaseName = "AstralBot.db";

        ChatConnection? Conn { get; }
        List<CharacterCore>? Characters { get; set; } = [];
        List<ClassFlyweights>? Flyweights { get; set; } = [];
        List<Dungeon> ActiveDungeonRuns { get; set; } = [];

        internal Astral(ChatConnection? _Conn) 
        { 
            Conn = _Conn;
            Siren.Initalize(Conn);

            SqliteSchema.SetDatabaseToLoad(DatabaseName);
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Data", DatabaseName)))
            {
                StartupDBCreation();
            }

            // IdInformation
            var idinfo = SqliteSchema.GetAll<IdInformation>();
            idinfo.ForEach(idi => Characters.Add(new CharacterCore() { IdInfo = idi }));

            // RpInformation
            var rpinfo = SqliteSchema.GetAll<RpInformation>();

            // ClassFlyweights
            var cfly = SqliteSchema.GetAll<ClassFlyweights>() ?? [];
            cfly.ForEach(fly => Flyweights.Add(fly));

            // Finish
            foreach (var chara in Characters)
            {
                // RpInformation
                if (chara.IdInfo != null) chara.RpInfo = rpinfo.FirstOrDefault(x => x.UserId.Equals(chara.IdInfo.UserId));
                if (chara.RpInfo == null && chara.IdInfo != null)
                {
                    chara.RpInfo = new(chara.IdInfo.UserId);
                    SqliteSchema.Update(chara.RpInfo);
                }

                // ClassInformation
                ClassInformation? cinfo = null;
                if (chara.IdInfo != null) cinfo = SqliteSchema.GetById<ClassInformation>(chara.IdInfo.UserId);
                if (chara.IdInfo != null && cinfo == null) cinfo = new ClassInformation(chara.IdInfo.UserId, GetFlyweightByName(StartingClass));
                chara.ClassInfo = cinfo;
                SqliteSchema.Update(cinfo);
            }
        }

        internal ClassFlyweights GetFlyweightByName(string name)
        {
            ClassFlyweights? reply = Flyweights?.FirstOrDefault(x => x.Name != null && x.Name.Equals(name));
            return reply ?? throw new Exception();
        }

        internal CharacterCore GetCharacterByName(string name)
        {
            CharacterCore? reply = Characters?.FirstOrDefault(ch => ch.IdInfo != null && ch.IdInfo.UserName.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (reply == null && Flyweights != null)
            {
                reply = new CharacterCore() { IdInfo = new() { UserName = name } };
                SqliteSchema.Insert(reply.IdInfo);
                reply.RpInfo = new(reply.IdInfo.UserId);
                SqliteSchema.Insert(reply.RpInfo);
                reply.ClassInfo = new(reply.IdInfo.UserId, Flyweights.First(x => x.Name != null && x.Name.Equals(StartingClass)) ?? null);
                SqliteSchema.Insert(reply.ClassInfo);
                Characters?.Add(reply);
            }

            if (reply == null || reply.ClassInfo == null || reply.ClassInfo.CurrentClass == null || Flyweights == null) throw new Exception();
            if (reply.ClassInfo.CurrentClass.Flyweights == null)
            {
                reply.ClassInfo.CurrentClass.Flyweights = Flyweights.First(x => reply.ClassInfo.CurrentClassId == x.ClassId);
            }
            return reply ?? throw new Exception();
        }

        private void StartupDBCreation()
        {
            // create tables
            SqliteSchema.CreateTable<IdInformation>();
            SqliteSchema.CreateTable<RpInformation>();
            SqliteSchema.CreateTable<ClassFlyweights>();
            SqliteSchema.CreateTable<ClassInformation>();

            // ClassFlyweights
            var cfly = SqliteSchema.GetAll<ClassFlyweights>();

            // Insert Base Class if Needed
            if (cfly.Count == 0)
            {
                cfly = [];

                ClassFlyweights newclass = new()
                {
                    Name = StartingClass,
                    Rarity = 0,
                    MaxRank = 3,
                    XpGrowthRate = 1.0,
                    BaseLevelXp = 1000,
                    Description = "After getting thrust through space and time, you managed to come out unscathed. It's a miricle in and of itself you didn't die. Now you struggle to survive in an unknown land.",
                    Requirements = [],
                    ClassColor = ClassColors.Gray,
                    StatsGrowthRate = new Dictionary<Stats, double>() { { Stats.Attack, 1 }, { Stats.Hit, 1 }, { Stats.Intelligence, 1 }, { Stats.Durability, 1 }, { Stats.Luck, 1 }, { Stats.Life, 1000 } },
                };
                cfly.Add(newclass);
                SqliteSchema.Insert(newclass);

                ClassFlyweights newclasss = new()
                {
                    Name = "Adventurer",
                    Rarity = 1,
                    MaxRank = 5,
                    XpGrowthRate = 1.0,
                    BaseLevelXp = 1200,
                    Description = "You've survived for some time. Now familiar with the world around you, you begin to hone a wide range of skills as you venture out to make a name for yourself.",
                    Requirements = new Dictionary<ClassRequirementType, string>() { { ClassRequirementType.ClassMaxxed, StartingClass } },
                    ClassColor = ClassColors.White,
                    StatsGrowthRate = new Dictionary<Stats, double>() { { Stats.Attack, 1.2 }, { Stats.Hit, 1.1 }, { Stats.Intelligence, 1 }, { Stats.Durability, 1.1 }, { Stats.Luck, 1.1 }, { Stats.Life, 1200 } },
                };
                cfly.Add(newclasss);
                SqliteSchema.Insert(newclasss);
            }
        }
    }
}
