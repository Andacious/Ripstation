using Microsoft.UI.Dispatching;
using Ripstation.Services;

namespace Ripstation.Services;

public class WinUIDispatcher(DispatcherQueue dispatcherQueue) : IUiDispatcher
{
    public void Post(Action action) =>
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => action());
}
