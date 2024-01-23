using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
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
    private enum PipelineType
    {
        Output,
        Error,
        Information,
    }

    private readonly CancellationTokenSource _cancelToken = new();
    private readonly PSDataCollection<PSObject?> _inputPipe = new();
    private readonly BlockingCollection<(PipelineType, object?)> _outputPipe = new();
    private Task? _worker;

    private PropertyInfo? _preserveInvocationInfoOnceProperty = null;

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
        while (_outputPipe.TryTake(out var currentOutput, 0, _cancelToken.Token))
        {
            WriteResult(currentOutput.Item1, currentOutput.Item2);
        }
    }

    protected override void EndProcessing()
    {
        _inputPipe.Complete();

        foreach ((PipelineType pipelineType, object? data) in _outputPipe.GetConsumingEnumerable(_cancelToken.Token))
        {
            WriteResult(pipelineType, data);
        }
        _worker?.Wait(-1, _cancelToken.Token);
    }

    protected override void StopProcessing()
    {
        _cancelToken.Cancel();
    }

    private void WriteResult(PipelineType pipelineType, object? data)
    {
        switch (pipelineType)
        {
            case PipelineType.Output:
                WriteObject(data);
                break;

            case PipelineType.Error:
                ErrorRecord err = (ErrorRecord)data!;

                // We need to set this internal property to replicate how
                // Invoke-Command emits an ErrorRecord works.
                // FUTURE: Use UnsafeAccessor once .NET 8.0 is minimum
                _preserveInvocationInfoOnceProperty ??= typeof(ErrorRecord).GetProperty(
                    "PreserveInvocationInfoOnce",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new RuntimeException("Failed to find internal property ErrorRecord.PreserveInvocationInfoOnce");
                _preserveInvocationInfoOnceProperty.SetValue(err, true);

                WriteError(err);
                break;

            case PipelineType.Information:
                WriteInformation((InformationRecord)data!);
                break;
        }
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

            // Invoke-Command only forwards the output, error, and information
            // pipes. For now we ignore Verbose, Warning, Debug, and Progress.
            // This might be revisited in the future.
            using PSDataCollection<PSObject?> outputPipe = new();
            outputPipe.DataAdded += (s, e) =>
            {
                PSObject? obj = outputPipe[e.Index];
                if (obj != null)
                {
                    obj.Properties.Add(new PSNoteProperty("PSComputerName", info.ToString()));
                    obj.Properties.Add(new PSNoteProperty("RunspaceId", runspace.InstanceId));
                    obj.Properties.Add(new PSNoteProperty("PSShowComputerName", true));
                }
                _outputPipe.Add((PipelineType.Output, obj));
            };

            using PSDataCollection<ErrorRecord> errorPipe = new();
            errorPipe.DataAdded += (s, e) =>
            {
                ErrorRecord errorRecord = errorPipe[e.Index];
                OriginInfo oi = new(info.ToString(), runspace.InstanceId);
                RemotingErrorRecord remoteError = new(errorRecord, oi);

                _outputPipe.Add((PipelineType.Error, remoteError));
            };
            ps.Streams.Error = errorPipe;

            using PSDataCollection<InformationRecord> infoPipe = new();
            infoPipe.DataAdded += (s, e) =>
            {
                InformationRecord infoRecord = infoPipe[e.Index];
                // Ensures the host value isn't written twice
                if (infoRecord.Tags.Contains("PSHOST"))
                {
                    infoRecord.Tags.Add("FORWARDED");
                }

                _outputPipe.Add((PipelineType.Information, infoRecord));
            };
            ps.Streams.Information = infoPipe;

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

            // Using an explicit PSInvocationSettings ensures the invocation
            // info isn't added to the error record which provides a more
            // consistent experience with Invoke-Command for error records.
            PSInvocationSettings psis = new();
            try
            {
                await ps.InvokeAsync(_inputPipe, outputPipe, psis, null, null);
            }
            catch (RemoteException e)
            {
                _outputPipe.Add((PipelineType.Error, e.ErrorRecord));
                return;
            }
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
