namespace ThirteenIsh.Game.Swn;

internal class SwnStarshipCharacterSystem(ImmutableList<GamePropertyGroup> propertyGroups)
    : SwnCharacterSystem(SwnSystem.Starship, CharacterTypeCompatibility.Monster,
    null, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
    {
        // Default to some sensible starting stats depending on hull class.
        // These are based on the example starships in the SWN core rulebook pages 104-106
        // (using Strike Fighter, Patrol Boat, Fleet Cruiser and Battleship as the examples).
        var hullClassValue = GetProperty<GameProperty>(character, SwnSystem.HullClass).GetValue(character);

        var hitPoints = GetProperty<GameCounter>(character, SwnSystem.HitPoints);
        hitPoints.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "8",
                SwnSystem.Frigate => "25",
                SwnSystem.Cruiser => "60",
                SwnSystem.Capital => "120",
                _ => "8"
            }, character);

        var armorClass = GetProperty<GameCounter>(character, SwnSystem.ArmorClass);
        armorClass.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "16",
                SwnSystem.Frigate => "14",
                SwnSystem.Cruiser => "14",
                SwnSystem.Capital => "17",
                _ => "16"
            }, character);

        var armor = GetProperty<GameCounter>(character, SwnSystem.Armor);
        armor.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "5",
                SwnSystem.Frigate => "5",
                SwnSystem.Cruiser => "15",
                SwnSystem.Capital => "20",
                _ => "5"
            }, character);

        var speed = GetProperty<GameCounter>(character, SwnSystem.Speed);
        speed.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "5",
                SwnSystem.Frigate => "4",
                SwnSystem.Cruiser => "1",
                SwnSystem.Capital => "0",
                _ => "5"
            }, character);

        var skill = GetProperty<MonsterSkillCounter>(character, SwnSystem.Skill);
        skill.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "2",
                SwnSystem.Frigate => "2",
                SwnSystem.Cruiser => "2",
                SwnSystem.Capital => "3",
                _ => "2"
            }, character);

        var power = GetProperty<GameCounter>(character, SwnSystem.Power);
        power.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "5",
                SwnSystem.Frigate => "15",
                SwnSystem.Cruiser => "50",
                SwnSystem.Capital => "75",
                _ => "5"
            }, character);

        var mass = GetProperty<GameCounter>(character, SwnSystem.Mass);
        mass.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "2",
                SwnSystem.Frigate => "10",
                SwnSystem.Cruiser => "30",
                SwnSystem.Capital => "50",
                _ => "2"
            }, character);

        var crew = GetProperty<GameCounter>(character, SwnSystem.Crew);
        crew.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "1",
                SwnSystem.Frigate => "20",
                SwnSystem.Cruiser => "200",
                SwnSystem.Capital => "1000",
                _ => "1"
            }, character);

        var commandPoints = GetProperty<GameCounter>(character, SwnSystem.CommandPoints);
        commandPoints.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "4",
                SwnSystem.Frigate => "5",
                SwnSystem.Cruiser => "5",
                SwnSystem.Capital => "6",
                _ => "4"
            }, character);

        var weapons = GetProperty<GameCounter>(character, SwnSystem.Weapons);
        weapons.EditCharacterProperty(
            hullClassValue switch
            {
                SwnSystem.Fighter => "4",
                SwnSystem.Frigate => "4",
                SwnSystem.Cruiser => "5",
                SwnSystem.Capital => "6",
                _ => "4"
            }, character);
    }
}