using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RemoteForge;

public delegate RunspaceConnectionInfo RemoteForgeFactory(string info);

public sealed class RemoteForgeRegistration
{
    internal static List<RemoteForgeRegistration> Registrations { get; } = new();

    public string Id { get; }
    public string? Description { get; }
    public bool IsDefault { get; }
    internal RemoteForgeFactory CreateFactory { get; }

    internal RemoteForgeRegistration(
        string id,
        string? description,
        bool isDefault,
        RemoteForgeFactory createFactory)
    {
        Id = id;
        Description = description;
        IsDefault = isDefault;
        CreateFactory = createFactory;
    }

    public static RemoteForgeRegistration[] Register(Assembly assembly)
    {
        List<RemoteForgeRegistration> registrations = new();

        foreach (Type t in assembly.GetTypes())
        {
            if (t.GetInterface(nameof(IRemoteForge)) == null)
            {
                continue;
            }

            MethodInfo registerMethod = typeof(RemoteForgeRegistration).GetMethod(
                nameof(RegisterForgeType),
                BindingFlags.NonPublic | BindingFlags.Static)!;
            RemoteForgeRegistration registration = (RemoteForgeRegistration)registerMethod
                .MakeGenericMethod(new[] { t })
                .Invoke(null, null)!;

            registrations.Add(registration);
        }

        return registrations.ToArray();
    }

    private static RemoteForgeRegistration RegisterForgeType<T>() where T : IRemoteForge
        => Register(T.ForgeId, T.Create, T.ForgeDescription);

    public static RemoteForgeRegistration Register(
        string id,
        Func<string, IRemoteForge> factory,
        string? description = null,
        bool isDefault = false)
        => Register(
            id,
            (u) => new RemoteForgeConnectionInfo(factory(u)),
            description: description,
            isDefault: isDefault);

    public static RemoteForgeRegistration Register(
        string id,
        RemoteForgeFactory factory,
        string? description = null,
        bool isDefault = false
    )
    {
        if (TryGetForgeRegistration(id, out RemoteForgeRegistration? forge))
        {
            if (forge.CreateFactory.Method == factory.Method)
            {
                return forge;
            }
            else
            {
                throw new ArgumentException($"A forge with the id '{id}' has already been registered");
            }
        }

        RemoteForgeRegistration registration = new(id, description, isDefault, factory);
        Registrations.Add(registration);

        return registration;
    }

    public static void Unregister(string id)
    {
        if (TryGetForgeRegistration(id, out RemoteForgeRegistration? forge))
        {
            Registrations.Remove(forge);
        }
        else
        {
            throw new ArgumentException($"No forge has been registered with the id '{id}'");
        }
    }

    internal static RunspaceConnectionInfo CreateForgeConnectionInfo(string info)
    {
        string? scheme = null;
        int schemeSplit = info.IndexOf(':');
        if (schemeSplit == -1)
        {
            foreach (RemoteForgeRegistration registeredForge in Registrations)
            {
                if (registeredForge.IsDefault)
                {
                    scheme = registeredForge.Id;
                    break;
                }
            }
            Debug.Assert(scheme != null);
        }
        else
        {
            string[] infoSplit = info.Split(':', 2);
            scheme = infoSplit[0];
            info = infoSplit[1];
        }

        if (TryGetForgeRegistration(scheme, out RemoteForgeRegistration? forge))
        {
            return forge.CreateFactory(info);
        }

        throw new ArgumentException($"No valid forge registrations found with the id '{scheme}'");
    }

    private static bool TryGetForgeRegistration(
        string id,
        [NotNullWhen(true)] out RemoteForgeRegistration? registration)
    {
        string lowerId = id.ToLowerInvariant();
        foreach (RemoteForgeRegistration forge in Registrations)
        {
            if (forge.Id.ToLowerInvariant() == lowerId)
            {
                registration = forge;
                return true;
            }
        }

        registration = default;
        return false;
    }
}
