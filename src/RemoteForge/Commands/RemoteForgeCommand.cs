using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RemoteForge.Commands;

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
        foreach (RemoteForgeRegistration forge in RemoteForgeRegistration.Registrations.ToArray())
        {
            WriteVerbose($"Checking for forge '{forge.Name}' matches requested Name");

            if (MatchesName(forge.Name))
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
    public string Name { get; set; } = string.Empty;

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "Explicit"
    )]
    [Alias("Factory")]
    public ScriptBlock? ForgeFactory { get; set; }

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

    [Parameter]
    public SwitchParameter Force { get; set; }

    protected override void EndProcessing()
    {
        try
        {
            if (ParameterSetName == "Explicit")
            {
                Debug.Assert(ForgeFactory != null);
                RemoteForgeRegistration registration = RemoteForgeRegistration.Register(
                    Name,
                    (i) => CreateConnectionInfoFromFactory(i, ForgeFactory),
                    true,
                    description: Description,
                    force: Force);

                if (PassThru)
                {
                    WriteObject(registration);
                }
            }
            else
            {
                Debug.Assert(Assembly != null);
                RemoteForgeRegistration[] registrations = RemoteForgeRegistration.Register(
                    Assembly,
                    force: Force);
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

    private RunspaceConnectionInfo CreateConnectionInfoFromFactory(string info, ScriptBlock factory)
    {
        foreach (PSObject? item in factory.Invoke(info))
        {
            if (item?.BaseObject is RunspaceConnectionInfo connInfo)
            {
                return connInfo;
            }
            else if (item?.BaseObject is IRemoteForge forgeFactory)
            {
                return new RemoteForgeConnectionInfo(forgeFactory);
            }
        }

        throw new ArgumentException(
            $"Factory result for '{Name}:{info}' did not output a RunspaceConnectionInfo or IRemoteForge object");
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
    public string[] Name { get; set; } = Array.Empty<string>();

    protected override void ProcessRecord()
    {
        foreach (string forgeId in Name)
        {
            try
            {
                RemoteForgeRegistration.Unregister(forgeId);
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
    }
}
