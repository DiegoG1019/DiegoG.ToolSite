using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
            string excp = $"[{DateTimeOffset.UtcNow:dd_MM_yyyy hh_mm_ss tt}] {e.GetType().Name} -- {Guid.NewGuid()}.exception";
            string file = Path.Combine(ExceptionDumpPath, excp);

            using (var stream = File.Open(file, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                int tab = 0;
                WriteException(writer, e, ref tab);
            }

            propertyFactory.CreateProperty(ExceptionDumpProperty, excp);
        }
    }

    private static void WriteException(StreamWriter writer, Exception e, ref int tab)
    {
        writer.Write(e.ToString());
        if (e is AggregateException ae)
            WriteAggregateException(writer, ae, ref tab);

        Exception? ie = e.InnerException;
        while (ie is not null)
        {
            WriteInnerException(writer, ie, ref tab);
            ie = ie.InnerException;
        }
    }

    private static void WriteAggregateException(StreamWriter writer, AggregateException ae, ref int tab)
    {
        int horizontal = 0;
        ++tab;
        foreach (var e in ae.InnerExceptions)
        {
            writer.Write('\n');
            WriteTabs(writer, tab);
            writer.Write("-- aggregate: ");
            writer.Write(++horizontal);
            writer.Write("--->>>");
            WriteException(writer, e, ref tab);
        }
        --tab;
    }

    private static void WriteInnerException(StreamWriter writer, Exception ie, ref int tab)
    {
        tab++;
        writer.Write("\n\n");
        WriteTabs(writer, ++tab);
        writer.Write("- inner ");
        writer.Write(" -->> \n");
        WriteException(writer, ie, ref tab);
        --tab;
    }

    private static void WriteTabs(StreamWriter writer, int tab)
    {
        for (int i = 0; i < tab; i++)
            writer.Write('\t');
    }
}
