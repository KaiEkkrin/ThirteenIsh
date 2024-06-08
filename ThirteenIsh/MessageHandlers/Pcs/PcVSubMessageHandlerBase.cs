using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Commands;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

internal abstract class PcVSubMessageHandlerBase<TMessage>(SqlDataService dataService, IRandomWrapper random)
    : MessageHandlerBase<TMessage> where TMessage : PcVSubMessageBase
{
    protected IRandomWrapper Random => random;

    protected sealed override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        TMessage message, CancellationToken cancellationToken = default)
    {
        var editOperation = CreateEditOperation(message);

        var result = message.Name != null
            ? await dataService.EditAdventurerAsync(message.GuildId, message.Name, editOperation, cancellationToken)
            : await dataService.EditAdventurerAsync(message.GuildId, message.UserId, editOperation, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            output =>
            {
                // If this wasn't a simple integer, show the working
                List<EmbedFieldBuilder> extraFields = [];
                if (message.DiceParseTree is not IntegerParseTree)
                {
                    extraFields.Add(new EmbedFieldBuilder()
                        .WithName("Roll")
                        .WithValue(output.Working));
                }

                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        ExtraFields = extraFields,
                        OnlyTheseProperties = [output.GameCounter.Name],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables,
                        Title = $"Set {output.GameCounter.Name} on {output.Adventurer.Name}"
                    });

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    protected abstract PcEditVariableOperation CreateEditOperation(TMessage message);
}
