namespace Ripstation.Services;

/// <summary>
/// Abstracts posting work onto the UI thread.
/// Production implementation uses WPF Dispatcher; tests use a synchronous stub.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>Schedules <paramref name="action"/> to run on the UI thread.</summary>
    void Post(Action action);
}
