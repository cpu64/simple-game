using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UmlToXmiExporter.Model;

namespace UmlToXmiExporter.Analysis;

public static class TypeHelper
{
    private static readonly HashSet<string> KnownPrimitives = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "int32", "uint", "uint32",
        "long", "int64", "ulong", "uint64",
        "short", "int16", "ushort", "uint16",
        "byte", "sbyte",
        "float", "single", "double", "decimal",
        "bool", "boolean",
        "string", "char",
        "void", "object"
    };

    private static readonly HashSet<string> CollectionGenericTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "List", "IList", "IReadOnlyList",
        "Collection", "ICollection", "IReadOnlyCollection",
        "IEnumerable", "HashSet", "ISet", "IReadOnlySet",
        "Queue", "Stack", "LinkedList", "ObservableCollection"
    };

    public static bool IsPrimitive(string typeName)
    {
        var clean = CleanTypeName(typeName);
        return KnownPrimitives.Contains(clean);
    }

    public static string NormalizePrimitiveName(string typeName)
    {
        var clean = CleanTypeName(typeName).ToLowerInvariant();
        return clean switch
        {
            "int32" or "uint32" or "int" or "uint" => "int",
            "int64" or "uint64" or "long" or "ulong" => "long",
            "int16" or "uint16" or "short" or "ushort" => "short",
            "byte" or "sbyte" => "byte",
            "single" or "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            "boolean" or "bool" => "bool",
            "string" => "string",
            "char" => "char",
            "void" => "void",
            "object" => "object",
            _ => clean
        };
    }

    public static string CleanTypeName(string rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType)) return "object";
        rawType = rawType.Trim();

        // Strip nullability suffix '?'
        if (rawType.EndsWith('?'))
        {
            rawType = rawType[..^1].Trim();
        }

        return rawType;
    }

    public static (string ElementType, bool IsCollection, int Lower, string Upper) AnalyzeMultiplicity(string rawType)
    {
        var clean = rawType.Trim();
        bool isNullable = clean.EndsWith('?');
        if (isNullable)
        {
            clean = clean[..^1].Trim();
        }

        // Array detection: T[], T[,], etc.
        if (clean.EndsWith(']'))
        {
            var bracketIdx = clean.IndexOf('[');
            if (bracketIdx > 0)
            {
                var elem = clean[..bracketIdx].Trim();
                return (elem, true, 0, "*");
            }
        }

        // Generic collection detection
        var match = Regex.Match(clean, @"^([\w\.]+)<(.+)>$");
        if (match.Success)
        {
            var genDef = match.Groups[1].Value;
            var innerArgs = match.Groups[2].Value;
            var simpleGenName = GetSimpleName(genDef);

            if (CollectionGenericTypes.Contains(simpleGenName))
            {
                var firstArg = GetFirstGenericArgument(innerArgs);
                return (firstArg, true, 0, "*");
            }

            if (simpleGenName.Equals("Dictionary", StringComparison.OrdinalIgnoreCase) ||
                simpleGenName.Equals("IDictionary", StringComparison.OrdinalIgnoreCase) ||
                simpleGenName.Equals("IReadOnlyDictionary", StringComparison.OrdinalIgnoreCase))
            {
                // For dictionary, extract the value type as main reference
                var parts = SplitGenericArguments(innerArgs);
                var valueType = parts.Count > 1 ? parts[1].Trim() : parts[0].Trim();
                return (valueType, true, 0, "*");
            }

            if (simpleGenName.Equals("Nullable", StringComparison.OrdinalIgnoreCase))
            {
                return (innerArgs.Trim(), false, 0, "1");
            }
        }

        int lower = isNullable ? 0 : 1;
        return (clean, false, lower, "1");
    }

    public static string GetSimpleName(string qualifiedName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName)) return string.Empty;
        var dotIdx = qualifiedName.LastIndexOf('.');
        return dotIdx >= 0 ? qualifiedName[(dotIdx + 1)..] : qualifiedName;
    }

    public static List<string> SplitGenericArguments(string argsString)
    {
        var results = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < argsString.Length; i++)
        {
            char c = argsString[i];
            if (c == '<') depth++;
            else if (c == '>') depth--;
            else if (c == ',' && depth == 0)
            {
                results.Add(argsString[start..i].Trim());
                start = i + 1;
            }
        }

        if (start < argsString.Length)
        {
            results.Add(argsString[start..].Trim());
        }

        return results;
    }

    private static string GetFirstGenericArgument(string argsString)
    {
        var list = SplitGenericArguments(argsString);
        return list.Count > 0 ? list[0] : argsString;
    }

    public static string SanitizeId(string input)
    {
        if (string.IsNullOrEmpty(input)) return "_anon";
        return Regex.Replace(input, @"[^\w\-]", "_");
    }
}
