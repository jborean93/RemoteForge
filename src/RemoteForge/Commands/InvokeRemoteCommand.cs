using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;

namespace RemoteForge.Commands;

[Cmdlet(
    VerbsLifecycle.Invoke,
    "Remote",
    DefaultParameterSetName = "ScriptBlockArgList"
)]
[OutputType(typeof(object))]
public sealed class InvokeRemoteCommand : PSCmdlet, IDisposable
{
    private readonly CancellationTokenSource _cancelToken = new();
    private readonly PSDataCollection<PSObject?> _inputPipe = new();
    private readonly BlockingCollection<PSObject?> _outputPipe = new();
    private Task? _worker;

    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [Alias("ComputerName", "Cn")]
    public StringForgeConnectionInfoPSSession[] ConnectionInfo { get; set; } = Array.Empty<StringForgeConnectionInfoPSSession>();

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

    // Potentially look into using this to support free-form params for either
    // the ParamSplat or connection data
    // [Parameter(ValueFromRemainingArguments = true)]
    // public PSObject?[] UnboundArguments { get; set; } = Array.Empty<PSObject?>();

    protected override void BeginProcessing()
    {
        string commandToRun;
        if (ScriptBlock != null)
        {
            commandToRun = ScriptBlock.ToString();
        }
        else
        {
            string resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                FilePath,
                out ProviderInfo provider,
                out PSDriveInfo _);

            if (provider.ImplementingType != typeof(FileSystemProvider))
            {
                ErrorRecord err = new(
                    new ArgumentException($"The resolved path '{resolvedPath}' is not a FileSystem path but {provider.Name}"),
                    "FilePathNotFileSystem",
                    ErrorCategory.InvalidArgument,
                    FilePath);
                ThrowTerminatingError(err);
            }
            else if (!File.Exists(resolvedPath))
            {
                ErrorRecord err = new(
                    new FileNotFoundException($"Cannot find path '{resolvedPath}' because it does not exist", resolvedPath),
                    "FilePathNotFound",
                    ErrorCategory.ObjectNotFound,
                    FilePath);
                ThrowTerminatingError(err);
            }

            commandToRun = File.ReadAllText(resolvedPath);
        }

        // TODO: Check if `$using:...` is used in commandToRun
        OrderedDictionary? parameters = null;
        if (ParamSplat?.Count > 0)
        {
            parameters = new();
            foreach (DictionaryEntry kvp in ParamSplat)
            {
                PSObject? value = null;
                if (kvp.Value is PSObject psObject)
                {
                    value = psObject;
                }
                else if (kvp.Value != null)
                {
                    value = PSObject.AsPSObject(kvp.Value);
                }

                parameters.Add(kvp.Key, value);
            }
        }
        _worker = Task.Run(async () =>
        {
            try
            {
                await RunWorker(
                    commandToRun,
                    arguments: ArgumentList,
                    parameters: parameters);
            }
            finally
            {
                _outputPipe.CompleteAdding();
            }
        });
    }

    protected override void ProcessRecord()
    {
        foreach (PSObject? input in InputObject)
        {
            _inputPipe.Add(input);
        }

        // See if there is any output already ready to write.
        PSObject? currentOutput = null;
        while (_outputPipe.TryTake(out currentOutput, 0, _cancelToken.Token))
        {
            WriteObject(currentOutput);
        }
    }

    protected override void EndProcessing()
    {
        _inputPipe.Complete();

        foreach (PSObject? output in _outputPipe.GetConsumingEnumerable(_cancelToken.Token))
        {
            WriteObject(output);
        }
        _worker?.Wait(-1, _cancelToken.Token);
    }

    protected override void StopProcessing()
    {
        _cancelToken.Cancel();
    }

    private async Task RunWorker(
        string script,
        PSObject?[] arguments,
        IDictionary? parameters)
    {
        Queue<StringForgeConnectionInfoPSSession> connections = new(ConnectionInfo);
        List<Task> tasks = new(Math.Max(ThrottleLimit, ConnectionInfo.Length));
        do
        {
            if (connections.Count == 0 || tasks.Count >= tasks.Capacity)
            {
                Task doneTask = await Task.WhenAny(tasks);
                tasks.Remove(doneTask);
                await doneTask;
            }

            if (connections.TryDequeue(out var info))
            {
                Task t = Task.Run(async () => await RunScript(info, script, arguments, parameters));
                tasks.Add(t);
            }
        }
        while (tasks.Count > 0);
    }

    private async Task RunScript(
        StringForgeConnectionInfoPSSession info,
        string script,
        PSObject?[] arguments,
        IDictionary? parameters)
    {
        bool disposeRunspace = false;
        Runspace? runspace = info.PSSession?.Runspace;
        if (runspace == null)
        {
            disposeRunspace = true;
            runspace = await RunspaceHelper.CreateRunspaceAsync(
                info.ConnectionInfo,
                _cancelToken.Token,
                host: Host,
                typeTable: null,
                applicationArguments: null);
        }

        try
        {
            using PowerShell ps = PowerShell.Create(runspace);
            using PSDataCollection<PSObject?> outputPipe = new();
            outputPipe.DataAdded += (s, e) =>
            {
                PSObject? obj = outputPipe[e.Index];
                if (obj != null)
                {
                    obj.Properties.Add(new PSNoteProperty("PSComputerName", info.ToString()));
                    obj.Properties.Add(new PSNoteProperty("RunspaceId", runspace.Id));
                    obj.Properties.Add(new PSNoteProperty("PSShowComputerName", true));
                }
                _outputPipe.Add(obj);
            };
            // TODO: Figure out how to handle the other streams.

            ps.AddScript(script);
            if (arguments != null)
            {
                foreach (PSObject? obj in arguments)
                {
                    ps.AddArgument(obj);
                }
            }
            if (parameters != null)
            {
                ps.AddParameters(parameters);
            }
            await ps.InvokeAsync(_inputPipe, outputPipe);
        }
        finally
        {
            if (disposeRunspace)
            {
                runspace.Dispose();
            }
        }
    }

    public void Dispose()
    {
        _cancelToken?.Dispose();
        _inputPipe?.Dispose();
        _outputPipe?.Dispose();
        GC.SuppressFinalize(this);
    }
}
