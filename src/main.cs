using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using ObjectLayoutInspector;

namespace Fahrenheit.Tools.Inspector;

public class FhInspectorWriter {
    private readonly TextWriterTraceListener _console;
    private readonly TextWriterTraceListener _file;

    public FhInspectorWriter(string log_file_path) {
        _console = new TextWriterTraceListener(Console.Out);
        _file    = new TextWriterTraceListener(File.Open(log_file_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));
    }

    public void Log(string msg) {
        _console.WriteLine(msg);
        _file   .WriteLine(msg);
        _file   .Flush();
    }
}

internal sealed class Program {

    private static Assembly _load_assembly_current_dir(string assembly_name) {
        string   fhcore_path = Path.Join(Directory.GetCurrentDirectory(), assembly_name);
        Assembly fhcore      = AssemblyLoadContext.Default.LoadFromAssemblyPath(fhcore_path);

        return fhcore;
    }

    private static void _inspect(string dest_path) {
        Stopwatch         perf   = Stopwatch.StartNew();
        FhInspectorWriter writer = new FhInspectorWriter(dest_path);
        Assembly          fhcore = _load_assembly_current_dir("fhcore.dll");

        foreach (Type type in fhcore.GetExportedTypes()) {
            // We ignore classes, delegates, and such.
            if (!type.IsValueType) {
                Console.WriteLine($"Skipped type {type.FullName}.");
                continue;
            }

            Console.WriteLine(type.FullName);
            TypeLayout layout = TypeLayout.GetLayout(type);

            writer.Log(layout.ToString(true));
        }

        Console.WriteLine($"Inspector emitted to {dest_path} in {perf.Elapsed}.");
    }

    private static void Main(string[] args) {
        Option<string> opt_dest_path  = new Option<string>("--dest") {
            Description = "Set the folder where the Inspector output file should be written.",
            Required    = true
        };

        RootCommand root_cmd = new RootCommand("Inspects the layout of all structures in the Fahrenheit core library and dumps them to disk.") {
            opt_dest_path,
        };

        ParseResult argparse_result = root_cmd.Parse(args);

        string dest_path      = argparse_result.GetValue(opt_dest_path) ?? "";
        string dest_file_path = Path.Join(dest_path, $"inspector-{Guid.NewGuid()}.txt");

        _inspect(dest_file_path);
    }
}
