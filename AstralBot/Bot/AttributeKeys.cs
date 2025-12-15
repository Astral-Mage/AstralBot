namespace AstralBot.Bot
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute 
    {
        public bool AutoIncrement { get; set; } = false;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ColumnAttribute(string? name = null) : Attribute
    {
        public string? Name { get; } = name;
        public bool Nullable { get; set; } = true;
        public bool Unique { get; set; } = false;
        public object? Default { get; set; } = null;
    }
}
