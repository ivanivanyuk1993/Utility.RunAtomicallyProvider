using RunAtomicallyProviderNS;

namespace RunAtomicallyForMultipleResourceIdsFromStaticListProviderNS;

internal class ResourceQueue
{
    internal readonly RunAtomicallyProvider RunAtomicallyProvider = new();
    internal readonly Queue<Func<Task>> RunOrWaitAsyncActionQueue = new();
}