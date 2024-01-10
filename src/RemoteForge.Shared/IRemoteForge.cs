using System;

namespace RemoteForge.Shared;

public interface IRemoteForge
{
    IRemoteForgeTransport CreateTransport();
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RemoteForgeAttribute : Attribute
{
    public string Id { get; }
    public string FactoryMethod { get; }

    public RemoteForgeAttribute(string id, string factoryMethod)
    {
        Id = id;
        FactoryMethod = factoryMethod;
    }
}
