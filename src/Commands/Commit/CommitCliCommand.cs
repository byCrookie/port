using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Commit;

internal class CommitCliCommand : AsyncCommand<CommitSettings>
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IContainerIdentifierAndTagEvaluator _containerIdentifierAndTagEvaluator;
    private readonly IIdentifierPrompt _identifierPrompt;

    public CommitCliCommand(ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainersQuery getRunningContainersQuery, IGetImageQuery getImageQuery,
        IContainerIdentifierAndTagEvaluator containerIdentifierAndTagEvaluator, IIdentifierPrompt identifierPrompt)
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
        _getImageQuery = getImageQuery;
        _containerIdentifierAndTagEvaluator = containerIdentifierAndTagEvaluator;
        _identifierPrompt = identifierPrompt;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommitSettings settings)
    {
        var tag = settings.Tag ?? $"{DateTime.Now:yyyyMMddhhmmss}";

        var container = await GetContainerAsync(settings);
        if (container == null)
        {
            throw new InvalidOperationException("No running container found");
        }

        await CommitContainerAsync(container, tag);

        return 0;
    }

    private async Task CommitContainerAsync(Container container, string tag)
    {
        var image = await _getImageQuery.QueryAsync(container.ImageName, container.ImageTag);
        if (image == null)
        {
            throw new InvalidOperationException(
                $"Image of running container {ImageNameHelper.JoinImageNameAndTag(container.ImageName, container.ImageTag)} not found");
        }

        while (image.Parent != null)
        {
            image = image.Parent;
        }

        var baseTag = image.Tag;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Creating image from running container {ContainerNameHelper.JoinContainerNameAndTag(container.Identifier, container.Tag)}",
                _ => _createImageFromContainerCommand.ExecuteAsync(container.Id, container.ImageName, baseTag, tag));
        AnsiConsole.WriteLine($"Created image with tag {tag}");
    }

    private async Task<Container?> GetContainerAsync(IContainerIdentifierSettings settings)
    {
        var containers = await _getRunningContainersQuery.QueryAsync();
        if (settings.ContainerIdentifier != null)
        {
            var (identifier, tag) = _containerIdentifierAndTagEvaluator.Evaluate(settings.ContainerIdentifier);
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
        else
        {
            var (identifier, tag) = _identifierPrompt.GetIdentifierOfContainerFromUser(containers, "commit");
            return containers.SingleOrDefault(c => c.Identifier == identifier && c.Tag == tag);
        }
    }
}