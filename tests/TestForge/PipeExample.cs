using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using RemoteForge;

namespace TestForge;

public sealed class PipeInfo : IRemoteForge
{
    public static string ForgeName => "PipeTest";
    public static string ForgeDescription => "Test pipe transport";

    private readonly Dictionary<string, string>? _environment;

    private PipeInfo(Dictionary<string, string>? environment)
    {
        _environment = environment;
    }

    public string GetTransportString() => "PipeTest:";

    public static IRemoteForge Create(string info)
    {
        if (string.IsNullOrWhiteSpace(info))
        {
            return new PipeInfo(null);
        }
        else
        {
            Dictionary<string, string> envVars = new();
            foreach (string entry in info.Split(','))
            {
                string[] kvp = entry.Split('=', 2);
                envVars.Add(kvp[0], kvp[1]);
            }

            return new PipeInfo(envVars);
        }
    }

    public RemoteTransport CreateTransport()
    {
        if (_environment == null)
        {
            return new PwshTransport();
        }
        else
        {
            return new PwshTransport(_environment);
        }
    }
}

public sealed class PipeInfoWithOptions : IRemoteForge
{
    public static string ForgeName => "PipeTestWithOptions";

    public bool FailOnClose { get; }
    public bool FailOnCreate { get; }
    public bool FailOnRead { get; }
    public bool EndOnRead { get; }
    public bool FailOnWrite { get; }
    public bool LogMessages { get; }
    public bool WriteStderr { get; }
    public bool Hang { get; }

    private PipeInfoWithOptions(
        bool failOnClose,
        bool failOnCreate,
        bool failOnRead,
        bool endOnRead,
        bool failOnWrite,
        bool logMessages,
        bool writeStderr,
        bool hang
    )
    {
        FailOnClose = failOnClose;
        FailOnCreate = failOnCreate;
        FailOnRead = failOnRead;
        EndOnRead = endOnRead;
        FailOnWrite = failOnWrite;
        LogMessages = logMessages;
        WriteStderr = writeStderr;
        Hang = hang;
    }

    public static IRemoteForge Create(string info)
    {
        bool failOnClose, failOnCreate, failOnRead, endOnRead, failOnWrite, logMessages, writeStderr, hang;
        failOnClose = failOnCreate = failOnRead = endOnRead = failOnWrite = logMessages = writeStderr = hang = false;

        Uri pipeUri = new($"PipeTest://{info}");
        NameValueCollection infoQueries = HttpUtility.ParseQueryString(pipeUri.Query);
        for (int i = 0; i < infoQueries.Count; i++)
        {
            string? key = infoQueries.GetKey(i)?.ToLowerInvariant();
            string? value = infoQueries.Get(i);

            bool result;
            if (key == "logmessages" && bool.TryParse(value, out result))
            {
                logMessages = result;
            }
            else if (key == "failonclose" && bool.TryParse(value, out result))
            {
                failOnClose = result;
            }
            else if (key == "failoncreate" && bool.TryParse(value, out result))
            {
                failOnCreate = result;
            }
            else if (key == "failonread" && bool.TryParse(value, out result))
            {
                failOnRead = result;
            }
            else if (key == "endonread" && bool.TryParse(value, out result))
            {
                endOnRead = result;
            }
            else if (key == "failonwrite" && bool.TryParse(value, out result))
            {
                failOnWrite = result;
            }
            else if (key == "writestderr" && bool.TryParse(value, out result))
            {
                writeStderr = result;
            }
            else if (key == "hang" && bool.TryParse(value, out result))
            {
                hang = result;
            }
        }

        return new PipeInfoWithOptions(
            failOnClose,
            failOnCreate,
            failOnRead,
            endOnRead,
            failOnWrite,
            logMessages,
            writeStderr,
            hang);
    }

    public RemoteTransport CreateTransport()
    {
        if (WriteStderr)
        {
            return new PwshTransport("pwsh", new[] { "FakeArg" });
        }
        else
        {
            return new PwshTransport(
            FailOnClose,
            FailOnCreate,
            FailOnRead,
            EndOnRead,
            FailOnWrite,
            LogMessages,
            Hang);
        }
    }
}
