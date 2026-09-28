using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UmlToXmiExporter.Analysis;
using UmlToXmiExporter.Model;
using UmlToXmiExporter.Serialization;

namespace UmlToXmiExporter;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            PrintUsage();
            return 0;
        }

        string inputPath = ".";
        string? outputPath = null;
        string? modelName = null;
        var dialect = XmiDialect.OmgUml251;
        bool includeFields = true;
        bool includePrivate = true;
        bool includeAssociations = true;
        bool includeDependencies = true;
        bool verbose = false;
        var customExcludes = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-i" or "--input" && i + 1 < args.Length)
            {
                inputPath = args[++i];
            }
            else if (arg is "-o" or "--output" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else if (arg is "-m" or "--model-name" && i + 1 < args.Length)
            {
                modelName = args[++i];
            }
            else if (arg is "-f" or "--format" && i + 1 < args.Length)
            {
                var formatStr = args[++i].ToLowerInvariant();
                dialect = formatStr switch
                {
                    "omg" or "omg251" or "uml251" => XmiDialect.OmgUml251,
                    "omg25" or "uml25" => XmiDialect.OmgUml25,
                    "eclipse" or "eclipse-uml2" or "uml2" => XmiDialect.EclipseUml2,
                    _ => throw new ArgumentException($"Unknown format: {formatStr}. Available: omg, omg25, eclipse")
                };
            }
            else if (arg == "--no-fields")
            {
                includeFields = false;
            }
            else if (arg == "--no-private")
            {
                includePrivate = false;
            }
            else if (arg == "--no-associations")
            {
                includeAssociations = false;
            }
            else if (arg == "--no-dependencies")
            {
                includeDependencies = false;
            }
            else if (arg is "-v" or "--verbose")
            {
                verbose = true;
            }
            else if (arg is "-e" or "--exclude" && i + 1 < args.Length)
            {
                var excludes = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                customExcludes.AddRange(excludes);
            }
            else if (!arg.StartsWith('-') && inputPath == ".")
            {
                inputPath = arg;
            }
        }

        try
        {
            var targetDir = Path.GetFullPath(inputPath);
            Console.WriteLine($"[UmlToXmiExporter] Scanning target: {targetDir}");

            var analyzerOptions = new AnalyzerOptions
            {
                IncludeFields = includeFields,
                IncludePrivate = includePrivate,
                ModelName = modelName ?? Path.GetFileName(targetDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            };

            foreach (var exc in customExcludes)
            {
                analyzerOptions.ExcludePatterns.Add(exc);
            }

            var analyzer = new CSharpCodebaseAnalyzer(analyzerOptions);
            var model = analyzer.Analyze(targetDir);

            int classCount = model.AllClassifiersById.Values.Count(c => c is UmlClass cls && !cls.IsRecord && !cls.IsStruct);
            int structCount = model.AllClassifiersById.Values.Count(c => c is UmlClass cls && cls.IsStruct);
            int recordCount = model.AllClassifiersById.Values.Count(c => c is UmlClass cls && cls.IsRecord);
            int interfaceCount = model.AllClassifiersById.Values.Count(c => c is UmlInterface);
            int enumCount = model.AllClassifiersById.Values.Count(c => c is UmlEnumeration);

            int totalProperties = model.AllClassifiersById.Values.Sum(c =>
                c is UmlClass cl ? cl.Properties.Count : (c is UmlInterface ifc ? ifc.Properties.Count : 0));
            int totalOperations = model.AllClassifiersById.Values.Sum(c =>
                c is UmlClass cl ? cl.Operations.Count : (c is UmlInterface ifc ? ifc.Operations.Count : 0));
            int totalGeneralizations = model.AllClassifiersById.Values.Sum(c =>
                c is UmlClass cl ? cl.Generalizations.Count : (c is UmlInterface ifc ? ifc.Generalizations.Count : 0));
            int totalRealizations = model.AllClassifiersById.Values.Sum(c =>
                c is UmlClass cl ? cl.InterfaceRealizations.Count : 0);

            Console.WriteLine($"[UmlToXmiExporter] Discovered Model Elements:");
            Console.WriteLine($"  - Classes:           {classCount}");
            if (structCount > 0) Console.WriteLine($"  - Structs:           {structCount}");
            if (recordCount > 0) Console.WriteLine($"  - Records:           {recordCount}");
            Console.WriteLine($"  - Interfaces:        {interfaceCount}");
            Console.WriteLine($"  - Enums:             {enumCount}");
            Console.WriteLine($"  - Properties/Fields: {totalProperties}");
            Console.WriteLine($"  - Operations:        {totalOperations}");
            Console.WriteLine($"  - Generalizations:   {totalGeneralizations}");
            Console.WriteLine($"  - Realizations:      {totalRealizations}");
            Console.WriteLine($"  - Associations:      {model.Associations.Count}");
            Console.WriteLine($"  - Dependencies:      {model.Dependencies.Count}");
            Console.WriteLine($"  - External Data Types: {model.ExternalDataTypes.Count}");
            Console.WriteLine($"  - External Interfaces: {model.ExternalInterfaces.Count}");
            Console.WriteLine($"  - External Classes:    {model.ExternalClasses.Count}");
            Console.WriteLine($"  - Primitive Types:   {model.PrimitiveTypes.Count}");

            if (verbose)
            {
                foreach (var c in model.AllClassifiersById.Values.Where(c => c is UmlClass or UmlInterface))
                {
                    Console.WriteLine($"    Classifier: {c.QualifiedName} ({c.GetType().Name})");
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                var ext = dialect == XmiDialect.EclipseUml2 ? "uml" : "xmi";
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{model.Name}.{ext}");
            }

            var serializerOptions = new SerializationOptions
            {
                Dialect = dialect,
                IncludeAssociations = includeAssociations,
                IncludeDependencies = includeDependencies,
                IncludeProperties = true,
                IncludeOperations = true
            };

            Console.WriteLine($"[UmlToXmiExporter] Serializing to XMI format ({dialect})...");
            var serializer = new XmiSerializer(serializerOptions);
            serializer.SerializeToFile(model, outputPath);

            // Self-verification: check well-formed XML
            var verifyDoc = XDocument.Load(outputPath);
            long fileSizeBytes = new FileInfo(outputPath).Length;

            Console.WriteLine($"[UmlToXmiExporter] Successfully generated: {outputPath} ({fileSizeBytes:N0} bytes)");
            Console.WriteLine($"[UmlToXmiExporter] Root Element: <{verifyDoc.Root?.Name.LocalName}> in namespace '{verifyDoc.Root?.Name.NamespaceName}'");

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[Error] {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.ToString());
            }
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"UmlToXmiExporter - Standalone C# CLI tool to export UML class diagrams to OMG UML XMI format for MSOSA / MagicDraw.

Usage:
  dotnet run --project UmlToXmiExporter -- [path] [options]
  or:
  UmlToXmiExporter [path] [options]

Arguments:
  [path]                    Target directory, solution (.sln, .slnx), or project (.csproj) to parse. Default: '.'

Options:
  -o, --output <file>       Output XMI file path (default: <ModelName>.xmi)
  -f, --format <format>     Target XMI format dialect:
                              'omg' or 'omg251'  - OMG UML 2.5.1 XMI (default, http://www.omg.org/spec/UML/20161101)
                              'omg25'            - OMG UML 2.5 XMI (http://www.omg.org/spec/UML/20131001)
                              'eclipse'          - Eclipse UML2 v5.x XMI (http://www.eclipse.org/uml2/5.0.0/UML)
  -m, --model-name <name>   Name for the root UML Model (default: target directory name)
  -e, --exclude <dirs>      Comma-separated list of additional directories to exclude
  --no-fields               Exclude fields (only include properties)
  --no-private              Exclude private members
  --no-associations         Do not generate uml:Association relationships
  --no-dependencies         Do not generate uml:Dependency relationships
  -v, --verbose             Enable detailed output
  -h, --help                Show this help message
");
    }
}
