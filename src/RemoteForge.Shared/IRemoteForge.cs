namespace RemoteForge.Shared;

public interface IRemoteForge
{
    IRemoteForgeTransport CreateTransport();
}
