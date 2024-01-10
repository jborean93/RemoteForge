using System.Threading;

namespace RemoteForge.Shared;

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
    void CreateConnection(CancellationToken cancellationToken);

    /// <summary>
    /// Close the transport connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This is called when the Runspace is closed or if being Dispose()
    /// before it is closed.
    /// </remarks>
    void CloseConnection(CancellationToken cancellationToken);

    /// <summary>
    /// Sends the provided message to the transport target.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    void WriteMessage(string message, CancellationToken cancellationToken);

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
    string? WaitMessage(CancellationToken cancellationToken);
}
