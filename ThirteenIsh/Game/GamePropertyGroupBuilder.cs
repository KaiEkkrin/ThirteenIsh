namespace ThirteenIsh.Game;

internal class GamePropertyGroupBuilder(string name)
{
    private readonly ImmutableList<GamePropertyBase>.Builder _builder = ImmutableList.CreateBuilder<GamePropertyBase>();

    public GamePropertyGroupBuilder AddProperty(GamePropertyBase property)
    {
        _builder.Add(property);
        return this;
    }

    public GamePropertyGroupBuilder AddProperties(params GamePropertyBase[] properties)
    {
        _builder.AddRange(properties);
        return this;
    }

    public GamePropertyGroup Build()
    {
        return new GamePropertyGroup(name, _builder.ToImmutable());
    }

    public GamePropertyGroupBuilder OrderByName()
    {
        _builder.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return this;
    }
}
