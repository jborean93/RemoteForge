using System;

namespace RemoteForge.Shared;

public interface IRemoteForge
{
    protected internal static virtual string ForgeId => throw new NotImplementedException();
    protected internal static virtual string? ForgeDescription => null;

    IRemoteForgeTransport CreateTransport();

    protected internal static virtual IRemoteForge Create(Uri info) => throw new NotImplementedException();
}
