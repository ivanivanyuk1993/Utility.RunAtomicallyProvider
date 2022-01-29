using RunAtomicallyAsyncProviderNS;

namespace RunAtomicallyAsyncProviderWithFinalActionNS;

public class RunAtomicallyAsyncProviderWithFinalAction : RunAtomicallyAsyncProvider
{
    private readonly Action<RunAtomicallyAsyncProviderWithFinalAction> _finalAction;

    public bool IsEmptyVolatile => Thread.VolatileRead(address: ref AsyncActionCount) == 0;

    public RunAtomicallyAsyncProviderWithFinalAction(Action<RunAtomicallyAsyncProviderWithFinalAction> finalAction)
    {
        _finalAction = finalAction;
    }

    protected override async Task StartRunningAsyncActionLoop(Func<Task> firstAsyncAction)
    {
        await base.StartRunningAsyncActionLoop(firstAsyncAction: firstAsyncAction);
        _finalAction(obj: this);
    }
}