using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge;

/// <summary>
/// Interface used for custom transport implementations.
/// </summary>
public interface IRemoteForgeTransport
{
    /// <summary>
    /// Create the connection for the transport.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This is called when the Runspace is set to open.
    /// </remarks>
    Task CreateConnection(CancellationToken cancellationToken);

    /// <summary>
    /// Close the transport connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This is called when the Runspace is closed or if being Dispose()
    /// before it is closed.
    /// </remarks>
    Task CloseConnection(CancellationToken cancellationToken);

    /// <summary>
    /// Sends the provided message to the transport target.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteMessage(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Wait for a message response from the target.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The message from the target or null/string.Empty if the transport is closed.
    /// </returns>
    /// <remarks>
    /// This should block until there is a message ready to return. Returning
    /// null or an empty string will signify the transport is closed.
    /// </remarks>
    Task<string?> WaitMessage(CancellationToken cancellationToken);
}
