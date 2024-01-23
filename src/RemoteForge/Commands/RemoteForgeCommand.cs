using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
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
        foreach (RemoteForgeRegistration forge in RemoteForgeRegistration.Registrations)
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
    public Func<string, IRemoteForge>? ForgeFactory { get; set; }

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
                RemoteForgeRegistration registration = RemoteForgeRegistration.Register(
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
                RemoteForgeRegistration[] registrations = RemoteForgeRegistration.Register(Assembly);
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
        base.ProcessRecord();
    }
}
