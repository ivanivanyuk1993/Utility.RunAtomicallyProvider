namespace RunAtomicallyForMultipleResourceIdsProviderNS;

public interface IRunAtomicallyForMultipleResourceIdsProvider<TResourceId>
{
    public Task RunAtomicallyForMultipleResourceIdsAndGetAwaiter(
        Action action,
        IList<TResourceId> uniqueResourceIdList
    );

    public Task RunAtomicallyForMultipleResourceIdsAndGetAwaiter(
        Func<Task> asyncAction,
        IList<TResourceId> uniqueResourceIdList
    );
}