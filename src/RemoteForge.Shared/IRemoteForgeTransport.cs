using System.Threading;

namespace RemoteForge.Shared;

public interface IRemoteForgeTransport
{
    void CreateConnection(CancellationToken cancellationToken);

    void CloseConnection(CancellationToken cancellationToken);

    void WriteMessage(string message, CancellationToken cancellationToken);

    string WaitMessage(CancellationToken cancellationToken);
}
