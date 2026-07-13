namespace Enterprise.Agent.Shared.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// Awaits <paramref name="task"/> but throws <see cref="TimeoutException"/> if it does not
    /// complete within <paramref name="timeout"/>. Honours the supplied cancellation token.
    /// </summary>
    public static async Task<T> WithTimeout<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);
        if (completed == task)
        {
            timeoutCts.Cancel();
            return await task.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException($"Operation did not complete within {timeout.TotalSeconds:0.##}s.");
    }
}
