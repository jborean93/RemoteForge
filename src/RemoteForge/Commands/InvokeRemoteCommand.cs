using Microsoft.PowerShell.Commands;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Commands;

[Cmdlet(
    VerbsLifecycle.Invoke,
    "Remote",
    DefaultParameterSetName = "ScriptBlock"
)]
[OutputType(typeof(object))]
public sealed class InvokeRemoteCommand : PSCmdlet, IDisposable
{
    private class DefaultVariableValue
    {
        private static DefaultVariableValue? _instance;

        private DefaultVariableValue()
        {}

        public static DefaultVariableValue Value => _instance ??= new();
    }

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
    private MethodInfo? _errorRecordSetTargetObject;

    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [Alias("ComputerName", "Cn")]
    public StringForgeConnectionInfoPSSession[] ConnectionInfo { get; set; } = Array.Empty<StringForgeConnectionInfoPSSession>();

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ScriptBlock"
    )]
    public string? ScriptBlock { get; set; }

    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "FilePath"
    )]
    [Alias("PSPath")]
    public string FilePath { get; set; } = string.Empty;

    [Parameter(Position = 2)]
    [ArgumentsOrParametersTransformation]
    [Alias("Args", "Param", "Parameters")]
    public ArgumentsOrParameters? ArgumentList { get; set; }

    [Parameter(
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    public PSObject?[] InputObject { get; set; } = Array.Empty<PSObject>();

    [Parameter]
    public int ThrottleLimit { get; set; } = 32;

    protected override void BeginProcessing()
    {
        string commandToRun;
        if (ScriptBlock != null)
        {
            commandToRun = ScriptBlock;
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

        PSObject?[] arguments = Array.Empty<PSObject?>();
        OrderedDictionary? parameters = null;

        if (ArgumentList != null)
        {
            if (ArgumentList.Arguments != null)
            {
                arguments = ArgumentList.Arguments;
            }
            else if (ArgumentList.Parameters != null)
            {
                parameters = new();
                foreach (DictionaryEntry kvp in ArgumentList.Parameters)
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
        }

        Hashtable usingParameters = GetUsingParameters(commandToRun);
        if (usingParameters.Count > 0)
        {
            parameters ??= new();
            parameters["--%"] = usingParameters;
        }

        _worker = Task.Run(async () =>
        {
            try
            {
                await RunWorker(
                    commandToRun,
                    arguments: arguments,
                    parameters: parameters);
            }
            finally
            {
                _outputPipe.CompleteAdding();
            }
        });
    }

    private Hashtable GetUsingParameters(string script)
    {
        ScriptBlock? indexAccesor = null;

        Hashtable usingParams = new();

        // ParseInput will not fail on errors, to allow providing anything as
        // the script to run we don't care about that, FindAll will only work
        // if the script is valid PowerShell.
        ScriptBlockAst sbkAst = Parser.ParseInput(script, out var _1, out var _2);

        foreach (var usingStatement in sbkAst.FindAll((a) => a is UsingExpressionAst, true))
        {
            UsingExpressionAst usingAst = (UsingExpressionAst)usingStatement;
            VariableExpressionAst backingVariableAst = UsingExpressionAst.ExtractUsingVariable(usingAst);
            string varPath = backingVariableAst.VariablePath.UserPath;

            // The non-index $using variable ensures the key is lowercased.
            // The index variant is not in case the index itself is a string
            // as that could be case sensitive.
            string varText = usingAst.ToString();
            if (usingAst.SubExpression is VariableExpressionAst)
            {
                varText = varText.ToLowerInvariant();
            }
            string key = Convert.ToBase64String(Encoding.Unicode.GetBytes(varText));

            if (!usingParams.ContainsKey(key))
            {
                object? value = SessionState.PSVariable.GetValue(varPath, DefaultVariableValue.Value);
                if (value is DefaultVariableValue)
                {
                    string msg = $"The value of the using variable '{usingStatement}' cannot be retrieved because it has not been set in the local session.";
                    ErrorRecord err = new(
                        new ArgumentException(msg),
                        "UsingVariableIsUndefined",
                        ErrorCategory.InvalidArgument,
                        varPath);
                    ThrowTerminatingError(err);
                }

                if (usingAst.SubExpression is IndexExpressionAst usingIndex)
                {
                    // ConstantExpressionAst, StringConstantExpressionAst, and
                    // VariableExpressionAst with IsConstantVariable() all
                    // should return a value here. Those 3 scenarios are the
                    // only index scenarios that $using supports.
                    // SafeGetValue() fails in other scenarios which we warn
                    // about.
                    object index;
                    try
                    {
                        index = usingIndex.Index.SafeGetValue();
                    }
                    catch (InvalidOperationException e)
                    {
                        WriteWarning($"Failed to extract $using value: {e.Message}");
                        continue;
                    }

                    // PowerShell doesn't have a nice API to dynamically
                    // invoke the Item[] lookup so to avoid complex reflection
                    // code we just fallback to doing it in pwsh itself.
                    indexAccesor ??= System.Management.Automation.ScriptBlock.Create("$args[0][$args[1]]");
                    value = indexAccesor.Invoke(value, index).FirstOrDefault();
                }

                usingParams.Add(key, value);
            }
        }

        return usingParams;
    }

    protected override void ProcessRecord()
    {
        foreach (PSObject? input in InputObject)
        {
            _inputPipe.Add(input);
        }

        // See if there is any output already ready to write.
        while (_outputPipe.TryTake(out (PipelineType, object?) currentOutput, 0, _cancelToken.Token))
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
                WriteError((ErrorRecord)data!);
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

            if (connections.TryDequeue(out StringForgeConnectionInfoPSSession? info))
            {
                Task t = Task.Run(async () =>
                {
                    try
                    {
                        await RunScript(info, script, arguments, parameters);
                    }
                    catch (Exception e)
                    {
                        string msg = $"Failed to run script on '{info}': {e.Message}";
                        ErrorRecord errorRecord = new(
                            e,
                            "ExecuteException",
                            ErrorCategory.NotSpecified,
                            info.ToString())
                        {
                            ErrorDetails = new(msg),
                        };
                        _outputPipe.Add((PipelineType.Error, errorRecord));
                    }
                });
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

                // Unfortunately this isn't exposed publicly but it's very nice
                // to be able to link back a record with a remote connection.
                _errorRecordSetTargetObject ??= typeof(ErrorRecord).GetMethod(
                    "SetTargetObject",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _errorRecordSetTargetObject?.Invoke(errorRecord, new[] { info.ToString() });

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
            // The InvocationInfo is also useless from the remote error record
            // so returning it won't make any difference.
            PSInvocationSettings psis = new();
            await ps.InvokeAsync(_inputPipe, outputPipe, psis, null, null);
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

public sealed class ArgumentsOrParametersTransformation : ArgumentTransformationAttribute
{
    public override object Transform(
        EngineIntrinsics engineIntrinsics,
        object inputData)
    {
        if (inputData is IList inputArray)
        {
            return new ArgumentsOrParameters(inputArray);
        }
        else if (inputData is IDictionary inputDict)
        {
            return new ArgumentsOrParameters(inputDict);
        }
        else
        {
            return new ArgumentsOrParameters(new[] { inputData });
        }
    }
}

public sealed class ArgumentsOrParameters
{
    internal PSObject?[]? Arguments { get; }
    internal IDictionary? Parameters { get; }

    public ArgumentsOrParameters(IList arguments)
    {
        Arguments = new PSObject?[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
        {
            object? obj = arguments[i];
            if (obj is PSObject psObj)
            {
                Arguments[i] = psObj;
            }
            else
            {
                Arguments[i] = obj == null ? null : PSObject.AsPSObject(obj);
            }
        }
    }

    public ArgumentsOrParameters(IDictionary parameters)
    {
        Parameters = parameters;
    }
}
