using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RemoteForge;

public delegate RunspaceConnectionInfo RemoteForgeFactory(Uri info);

public sealed class RemoteForgeRegistration
{
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
}

[Cmdlet(
    VerbsCommon.Get,
    "RemoteForge"
)]
[OutputType(typeof(RemoteForgeRegistration))]
public sealed class GetRemoteForgeCommand : PSCmdlet
{
    private List<WildcardPattern> _matches = new();

    [Parameter(
        Position = 0
    )]
    public string[] Name { get; set; } = Array.Empty<string>();

    protected override void ProcessRecord()
    {
        foreach (string name in Name)
        {
            _matches.Add(new(name));
        }
    }

    protected override void EndProcessing()
    {
        foreach (RemoteForgeRegistration forge in RemoteForgeRegistrations.Registrations)
        {
            WriteVerbose($"Checking for forge '{forge.Id}' matches requested Name");

            if (MatchesName(forge.Id))
            {
                WriteObject(forge);
            }
        }
    }

    private bool MatchesName(string id)
    {
        if (_matches.Count == 0)
        {
            return true;
        }

        foreach (WildcardPattern pattern in _matches)
        {
            if (pattern.IsMatch(id))
            {
                return true;
            }
        }

        return false;
    }
}

[Cmdlet(
    VerbsLifecycle.Register,
    "RemoteForge",
    DefaultParameterSetName = "Explicit"
)]
[OutputType(typeof(RemoteForgeRegistration))]
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
        ParameterSetName = "Explicit"
    )]
    public string? Description { get; set; }

    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "Assembly"
    )]
    public Assembly? Assembly { get; set; }

    [Parameter]
    public SwitchParameter PassThru { get; set; }

    protected override void EndProcessing()
    {
        try
        {
            if (ParameterSetName == "Explicit")
            {
                Debug.Assert(ForgeFactory != null);
                RemoteForgeRegistration registration = RemoteForgeRegistrations.Register(
                    Id,
                    ForgeFactory,
                    description: Description);

                if (PassThru)
                {
                    WriteObject(registration);
                }
            }
            else
            {
                Debug.Assert(Assembly != null);
                RemoteForgeRegistration[] registrations = RemoteForgeRegistrations.Register(Assembly);
                if (PassThru)
                {
                    foreach (RemoteForgeRegistration registration in registrations)
                    {
                        WriteObject(registration);
                    }
                }
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
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
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
    internal static List<RemoteForgeRegistration> Registrations { get; } = new();

    public static RemoteForgeRegistration[] Register(Assembly assembly)
    {
        List<RemoteForgeRegistration> registrations = new();

        foreach (Type t in assembly.GetTypes())
        {
            if (t.GetInterface(nameof(IRemoteForge)) == null)
            {
                continue;
            }

            MethodInfo registerMethod = typeof(RemoteForgeRegistrations).GetMethod(
                nameof(RegisterForgeType),
                BindingFlags.NonPublic | BindingFlags.Static)!;
            RemoteForgeRegistration registration =  (RemoteForgeRegistration)registerMethod
                .MakeGenericMethod(new[] { t })
                .Invoke(null, null)!;

            registrations.Add(registration);
        }

        return registrations.ToArray();
    }

    private static RemoteForgeRegistration RegisterForgeType<T>() where T: IRemoteForge
        => Register(T.ForgeId, T.Create, T.ForgeDescription);

    public static RemoteForgeRegistration Register(
        string id,
        Func<Uri, IRemoteForge> factory,
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

    internal static RunspaceConnectionInfo CreateForgeConnectionInfo(Uri info)
    {
        string scheme = info.Scheme;
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
