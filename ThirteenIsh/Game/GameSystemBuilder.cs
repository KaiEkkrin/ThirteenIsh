namespace ThirteenIsh.Game;

internal sealed class GameSystemBuilder(string name)
{
    private readonly ImmutableList<GamePropertyGroup>.Builder _builder =
        ImmutableList.CreateBuilder<GamePropertyGroup>();

    public GameSystemBuilder AddPropertyGroup(GamePropertyGroupBuilder group)
    {
        _builder.Add(group.Build());
        return this;
    }

    public GameSystem Build(GameSystemLogicBase logic)
    {
        var propertyGroups = _builder.ToImmutable();

        // Do the validation step
        // Names and aliases must all be unique:
        HashSet<string> names = [];
        HashSet<string> aliases = [];
        foreach (var group in propertyGroups)
        {
            foreach (var property in group.Properties)
            {
                if (!names.Add(property.Name))
                    throw new InvalidOperationException($"{name}: Found two properties named {property.Name}");

                if (property.Alias is not null && !aliases.Add(property.Alias))
                    throw new InvalidOperationException($"{name}: Found two properties aliased {property.Alias}");
            }
        }

        return new GameSystem(name, logic, propertyGroups);
    }
}
