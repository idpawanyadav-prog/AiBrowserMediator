using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.DAL;

public sealed class FileWorkflowRepository(IWorkflowSerializer serializer) : IWorkflowRepository
{
    public async Task SaveAsync(WorkflowDocumentDto workflow, string path, CancellationToken cancellationToken = default)
        => await File.WriteAllTextAsync(path, serializer.Serialize(workflow), cancellationToken);

    public async Task<WorkflowDocumentDto> LoadAsync(string path, CancellationToken cancellationToken = default)
        => serializer.Deserialize(await File.ReadAllTextAsync(path, cancellationToken));
}
