using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RemoteForge;

/// <summary>
/// Base transport class. See ProcessTransport
/// for a transport based on process stdio.
/// </summary>
public abstract class RemoteTransport : IDisposable
{
    /// <summary>
    /// Write the input provided to the transport target.
    /// </summary>
    /// <param name="input">
    /// The PSRemoting payload to write to the target.
    /// </param>
    /// <param name="cancellationToken">
    /// Task cancellation token.
    /// </param>
    protected abstract Task WriteInput(string input, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the output from the transport.
    /// </summary>
    /// <param name="cancellationToken">
    /// Task cancellation token.
    /// </param>
    /// <returns>The line returned from the transport.</returns>
    protected abstract Task<string?> ReadOutput(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the error from the transport.
    /// </summary>
    /// <param name="cancellationToken">
    /// Task cancellation token.
    /// </param>
    /// <returns>The error message from the transport or null for no error.</returns>
    /// <remarks>
    /// Not all transports have a separate error stream, the default behaviour
    /// is to return null which is treated as no error.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    protected virtual Task<string?> ReadError(CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    /// <summary>
    /// Opens the transport connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// Task cancellation token.
    /// </param>
    /// <remarks>
    [ExcludeFromCodeCoverage]
    protected virtual Task Open(CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Closes the transport connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// Task cancellation token.
    /// </param>
    [ExcludeFromCodeCoverage]
    protected virtual Task Close(CancellationToken cancellationToken)
        => Task.CompletedTask;

    internal async Task Run(
        ChannelReader<string> reader,
        ChannelWriter<string> writer,
        CancellationToken cancellationToken)
    {
        Task errorTask;
        Task outputTask;
        bool raisedError = false;

        await Open(cancellationToken);
        try
        {
            errorTask = Task.Run(async () =>
            {
                string? result;
                try
                {
                    result = await ReadError(cancellationToken);
                }
                catch (Exception e)
                {
                    writer.TryComplete(e);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result))
                {
                    raisedError = true;
                    writer.TryComplete(new Exception(result));
                }
            }, cancellationToken);

            outputTask = Task.Run(async () =>
            {
                while (true)
                {
                    string? result;
                    try
                    {
                        result = await ReadOutput(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        writer.TryComplete(e);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        // Wait 200 milliseconds to see if the error task wrote
                        // an error and favour that instead of the generic one
                        // here.
                        try
                        {
                            await errorTask.WaitAsync(new TimeSpan(200 * TimeSpan.TicksPerMillisecond),
                                cancellationToken);
                        }
                        catch (TimeoutException)
                        { }

                        if (!(errorTask.IsCompleted && raisedError))
                        {
                            writer.TryComplete(new Exception(
                                "Transport has returned no data before it has been closed"));
                        }

                        break;
                    }

                    await writer.WriteAsync(result, cancellationToken);
                }
            }, cancellationToken);

            while (true)
            {
                string msg;
                try
                {
                    msg = await reader.ReadAsync(cancellationToken);
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                await WriteInput(msg, cancellationToken);
            }
        }
        catch
        {
            raisedError = true;
            throw;
        }
        finally
        {
            await Close(cancellationToken);
        }

        await Task.WhenAll(outputTask, errorTask);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    { }
}
