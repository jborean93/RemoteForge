using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace RemoteForge.Commands;

internal class InvokeCommandJob : IDisposable
{
    public readonly Runspace _runspace;
    public readonly string _command;

    public InvokeCommandJob(
        StringForgeConnectionInfoPSSession connectionInfo,
        string command,
        PSObject?[]? argumentList = null,
        IDictionary? parameters = null)
    {
        _command = command;
        _runspace = RunspaceFactory.CreateRunspace();
    }

    public void Start()
    {

    }

    public void WriteInput(PSObject? inputObj)
    {

    }

    public void CompleteInput()
    {

    }

    public void Dispose()
    {
        _runspace?.Dispose();
        GC.SuppressFinalize(this);
    }
}

[Cmdlet(
    VerbsLifecycle.Invoke,
    "Remote",
    DefaultParameterSetName = "ScriptBlockArgList"
)]
[OutputType(typeof(object))]
public sealed class InvokeRemoteCommand : NewRemoteForgeSessionBase
{
    private List<PSObject?>? _inputCollection;

    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [Alias("Cn", "Connection")]
    public StringForgeConnectionInfoPSSession[] ComputerName { get; set; } = Array.Empty<StringForgeConnectionInfoPSSession>();

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ScriptBlockArgList"
    )]
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ScriptBlockParam"
    )]
    public ScriptBlock? ScriptBlock { get; set; }

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "FilePathArgList"
    )]
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "FilePathParam"
    )]
    [Alias("PSPath")]
    public string FilePath { get; set; } = string.Empty;

    [Parameter(
        Position = 2,
        ParameterSetName = "ScriptBlockArgList"
    )]
    [Parameter(
        Position = 2,
        ParameterSetName = "FilePathArgList"
    )]
    [Alias("Args")]
    public PSObject?[] ArgumentList { get; set; } = Array.Empty<PSObject>();

    [Parameter(
        Position = 2,
        ParameterSetName = "ScriptBlockParam"
    )]
    [Parameter(
        Position = 2,
        ParameterSetName = "FilePathParam"
    )]
    [Alias("Params")]
    public IDictionary? ParamSplat { get; set; }

    [Parameter(
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    public PSObject?[] InputObject { get; set; } = Array.Empty<PSObject>();

    [Parameter]
    public int ThrottleLimit { get; set; } = 32;

    // [Parameter(ValueFromRemainingArguments = true)]
    // public PSObject?[] UnboundArguments { get; set; } = Array.Empty<PSObject?>();

    protected override void BeginProcessing()
    {
        if (ComputerName.Length > ThrottleLimit)
        {
            _inputCollection = new();
        }

        base.BeginProcessing();
    }

    protected override void ProcessRecord()
    {
        foreach (PSObject? input in InputObject)
        {
            _inputCollection?.Add(input);
            // Add to currently running group
        }
    }

    protected override void EndProcessing()
    {
        string abc = "foo";

        WriteObject(abc);
        // Debug.Assert(ScriptBlock != null);

        // PSSession[] sessions = CreatePSSessions(ComputerName.Select(c => c.ConnectionInfo)).ToArray();
        // try
        // {
        //     List<(PowerShell, IAsyncResult)> tasks = new();
        //     foreach (PSSession s in sessions)
        //     {
        //         PowerShell ps = PowerShell.Create(sessions[0].Runspace);
        //         ps.AddScript(ScriptBlock.ToString());
        //         IAsyncResult invokeTask = ps.BeginInvoke();
        //         tasks.Add((ps, invokeTask));
        //     }

        //     WaitHandle.WaitAll(tasks.Select(t => t.Item2.AsyncWaitHandle).ToArray());
        //     foreach ((PowerShell ps, IAsyncResult invokeTask) in tasks)
        //     {
        //         PSDataCollection<PSObject> result = ps.EndInvoke(invokeTask);
        //         WriteObject(result, true);
        //         ps.Dispose();
        //     }

        // }
        // finally
        // {
        //     foreach (PSSession s in sessions)
        //     {
        //         s.Runspace.Close();
        //         s.Runspace.Dispose();
        //     }
        // }
    }
}
