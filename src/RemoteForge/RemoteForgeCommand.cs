using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using RemoteForge.Shared;

[Cmdlet(
    VerbsCommon.Get,
    "RemoteForge"
)]
public sealed class GetRemoteForgeCommand : PSCmdlet
{
    [Parameter(
        Position = 0
    )]
    public string[] Name { get; set; } = Array.Empty<string>();

    protected override void EndProcessing()
    {
        foreach (KeyValuePair<string, Func<Uri, RunspaceConnectionInfo>> kvp in RemoteForgeRegistrations.Registrations)
        {
            if (Name.Length == 0 || Name.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                WriteObject(kvp.Key);
            }
        }
    }
}

[Cmdlet(
    VerbsLifecycle.Register,
    "RemoteForge",
    DefaultParameterSetName = "Explicit"
)]
public sealed class RegisterRemoteForgeCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "Explicit"
    )]
    [ValidateNotNullOrEmpty]
    public string Id { get; set; } = "";

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "Explicit"
    )]
    public Func<Uri, IRemoteForge>? ForgeFactory { get; set; }

    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "Assembly"
    )]
    public Assembly? Assembly { get; set; }

    protected override void EndProcessing()
    {
        try
        {
            if (ParameterSetName == "Explicit")
            {
                Debug.Assert(ForgeFactory != null);
                RemoteForgeRegistrations.Register(Id, ForgeFactory);
            }
            else
            {
                Debug.Assert(Assembly != null);
                RemoteForgeRegistrations.Register(Assembly);
            }
        }
        catch (ArgumentException e)
        {
            ErrorRecord err = new(
                e,
                "InvalidForgeRegistration",
                ErrorCategory.InvalidArgument,
                null);
            WriteError(err);
        }
    }
}

[Cmdlet(
    VerbsLifecycle.Unregister,
    "RemoteForge"
)]
public sealed class UnregisterRemoteForgeCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [ValidateNotNullOrEmpty]
    public string[] Id { get; set; } = Array.Empty<string>();

    protected override void ProcessRecord()
    {
        foreach (string forgeId in Id)
        {
            try
            {
                RemoteForgeRegistrations.Unregister(forgeId);
            }
            catch (ArgumentException e)
            {
                ErrorRecord err = new(
                    e,
                    "NoRegisteredForge",
                    ErrorCategory.InvalidArgument,
                    forgeId);
                WriteError(err);
            }
        }
        base.ProcessRecord();
    }
}

public static class RemoteForgeRegistrations
{
    internal static Dictionary<string, Func<Uri, RunspaceConnectionInfo>> Registrations { get; } = new();

    public static void Register(Assembly assembly)
    {
        foreach (Type t in assembly.GetTypes())
        {
            if (t.GetInterface(nameof(IRemoteForge)) == null)
            {
                continue;
            }

            MethodInfo registerMethod = typeof(RemoteForgeRegistrations).GetMethod(
                nameof(RegisterForgeType),
                BindingFlags.NonPublic | BindingFlags.Static)!;
            registerMethod.MakeGenericMethod(new[] { t }).Invoke(null, null);
        }
    }

    private static void RegisterForgeType<T>() where T: IRemoteForge
        => Register(T.ForgeId, T.Create);

    public static void Register(string id, Func<Uri, IRemoteForge> factory)
        => Register(id, (u) => new RemoteForgeConnectionInfo(factory(u)));

    public static void Register(string id, Func<Uri, RunspaceConnectionInfo> factory)
    {
        string lowerId = id.ToLowerInvariant();
        if (Registrations.ContainsKey(lowerId))
        {
            throw new ArgumentException($"A forge with the id '{id}' has already been registered");
        }

        Registrations[lowerId] = factory;
    }

    public static void Unregister(string id)
    {
        string lowerId = id.ToLowerInvariant();
        if (Registrations.ContainsKey(lowerId))
        {
            Registrations.Remove(lowerId);
        }
        else
        {
            throw new ArgumentException($"No forge has been registered with the id '{id}'");
        }
    }

    internal static RunspaceConnectionInfo CreateForgeConnectionInfo(Uri info)
    {
        string scheme = info.Scheme;
        if (Registrations.TryGetValue(scheme.ToLowerInvariant(), out var factory))
        {
            return factory(info);
        }

        throw new ArgumentException($"No valid forge registrations found with the id '{scheme}'");
    }
}
