using System;

namespace RemoteForge;

public interface IRemoteForge
{
    protected internal static virtual string ForgeId => throw new NotImplementedException();
    protected internal static virtual string? ForgeDescription => null;

    string GetTransportString() => this.GetType().Name;

    IRemoteForgeTransport CreateTransport();

    protected internal static virtual IRemoteForge Create(Uri info) => throw new NotImplementedException();
}
