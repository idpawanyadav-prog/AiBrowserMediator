using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.BLL;

public sealed class RetryExecutor : IRetryExecutor
{
    public async Task<(T Result, int Retries)> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccess,
        int maxAttempts,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await operation(cancellationToken);
                if (isSuccess(result))
                {
                    return (result, attempt - 1);
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delay.TotalMilliseconds * attempt), cancellationToken);
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        var final = await operation(cancellationToken);
        return (final, maxAttempts - 1);
    }
}
