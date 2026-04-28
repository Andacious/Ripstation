namespace Ripstation.Services;

/// <summary>
/// Abstracts posting work onto the UI thread.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>Schedules <paramref name="action"/> to run on the UI thread.</summary>
    void Post(Action action);
}
