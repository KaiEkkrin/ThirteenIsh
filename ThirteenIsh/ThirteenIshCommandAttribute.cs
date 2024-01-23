namespace ThirteenIsh;

/// <summary>
/// Decorate implementations of IThirteenIshCommand with this so that
/// they are suitably registered as slash commands.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class ThirteenIshCommandAttribute : Attribute
{
    public ThirteenIshCommandAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }   

    public string Name { get; }
    public string Description { get; }
}
