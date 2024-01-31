using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RemoteForge;

public delegate RunspaceConnectionInfo RemoteForgeFactory(string info);

public sealed class RemoteForgeRegistration
{
    // We use a thread local storage value so the registrations are scoped to
    // a Runspace rather than be process wide.
    [ThreadStatic]
    private static List<RemoteForgeRegistration>? _registrations;

    internal static List<RemoteForgeRegistration> Registrations => _registrations ??= new();

    public string Name { get; }
    public string? Description { get; }
    internal RemoteForgeFactory CreateFactory { get; }

    internal RemoteForgeRegistration(
        string name,
        string? description,
        RemoteForgeFactory createFactory)
    {
        Name = name;
        Description = description;
        CreateFactory = createFactory;
    }

    public static RemoteForgeRegistration[] Register(Assembly assembly, bool force = false)
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
                .Invoke(null, new object[] { force })!;

            registrations.Add(registration);
        }

        return registrations.ToArray();
    }

    private static RemoteForgeRegistration RegisterForgeType<T>(bool force) where T : IRemoteForge
        => Register(
            T.ForgeName,
            (u) => new RemoteForgeConnectionInfo(T.Create(u)),
            T.ForgeDescription,
            force: force);

    public static RemoteForgeRegistration Register(
        string name,
        RemoteForgeFactory factory,
        string? description = null,
        bool force = false)
        => Register(name, factory, false, description: description, force: force);

    internal static RemoteForgeRegistration Register(
        string name,
        RemoteForgeFactory factory,
        bool doNotCheckExistingMethod,
        string? description = null,
        bool force = false)
    {
        if (TryGetForgeRegistration(name, out RemoteForgeRegistration? forge))
        {
            if (!doNotCheckExistingMethod && forge.CreateFactory.Method == factory.Method)
            {
                return forge;
            }
            else if (force)
            {
                Registrations.Remove(forge);
            }
            else
            {
                throw new ArgumentException($"A forge with the name '{name}' has already been registered");
            }
        }

        RemoteForgeRegistration registration = new(name, description, factory);
        Registrations.Add(registration);

        return registration;
    }

    public static void Unregister(string name)
    {
        if (TryGetForgeRegistration(name, out RemoteForgeRegistration? forge))
        {
            Registrations.Remove(forge);
        }
        else
        {
            throw new ArgumentException($"No forge has been registered with the name '{name}'");
        }
    }

    public static RunspaceConnectionInfo CreateForgeConnectionInfo(string info)
    {
        string? scheme = null;
        int schemeSplit = info.IndexOf(':');
        if (schemeSplit == -1)
        {
            scheme = "ssh";
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

        throw new ArgumentException($"No valid forge registrations found with the name '{scheme}'");
    }

    private static bool TryGetForgeRegistration(
        string name,
        [NotNullWhen(true)] out RemoteForgeRegistration? registration)
    {
        string lowerId = name.ToLowerInvariant();
        foreach (RemoteForgeRegistration forge in Registrations)
        {
            if (forge.Name.ToLowerInvariant() == lowerId)
            {
                registration = forge;
                return true;
            }
        }

        registration = default;
        return false;
    }
}
