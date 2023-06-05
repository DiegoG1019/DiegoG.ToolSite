using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Shared.Logging.Enrichers;
public class ExceptionDumper : ILogEventEnricher
{
    public const string ExceptionDumpProperty = "ExceptionDump";

    public string ExceptionDumpPath { get; }

    public ExceptionDumper(string dumpPath)
    {
        ExceptionDumpPath = dumpPath;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception is Exception e)
        {
            string excp = $"[{DateTimeOffset.UtcNow:dd_MM_yyyy hh_mm_ss tt}] {e.GetType().Name} -- {Guid.NewGuid()}";
            string file = Path.Combine(ExceptionDumpPath, excp);

            using (var stream = File.Open(file, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
                writer.Write(e.ToString());

            propertyFactory.CreateProperty(ExceptionDumpProperty, excp);
        }
    }
}
