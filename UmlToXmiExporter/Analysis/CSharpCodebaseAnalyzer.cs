using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UmlToXmiExporter.Model;

namespace UmlToXmiExporter.Analysis;

public class AnalyzerOptions
{
    public bool IncludeFields { get; set; } = true;
    public bool IncludePrivate { get; set; } = true;
    public List<string> ExcludePatterns { get; } = new()
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules", "UmlToXmiExporter"
    };
    public string ModelName { get; set; } = "UmlModel";
}

public class CSharpCodebaseAnalyzer
{
    private readonly AnalyzerOptions _options;

    public CSharpCodebaseAnalyzer(AnalyzerOptions? options = null)
    {
        _options = options ?? new AnalyzerOptions();
    }

    public List<string> DiscoverSourceFiles(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (File.Exists(fullPath))
        {
            if (fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { fullPath };
            }

            if (fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                fullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) ||
                fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
                return ScanDirectory(dir);
            }
        }

        if (Directory.Exists(fullPath))
        {
            return ScanDirectory(fullPath);
        }

        throw new DirectoryNotFoundException($"Target path not found: {path}");
    }

    private List<string> ScanDirectory(string dir)
    {
        var result = new List<string>();
        var dirInfo = new DirectoryInfo(dir);

        foreach (var subDir in dirInfo.GetDirectories())
        {
            if (ShouldExclude(subDir.Name))
            {
                continue;
            }

            result.AddRange(ScanDirectory(subDir.FullName));
        }

        foreach (var file in dirInfo.GetFiles("*.cs"))
        {
            if (!file.Name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
                !file.Name.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(file.FullName);
            }
        }

        return result;
    }

    private bool ShouldExclude(string name)
    {
        return _options.ExcludePatterns.Any(pattern =>
            pattern.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public UmlModel Analyze(string targetPath)
    {
        var files = DiscoverSourceFiles(targetPath);
        var syntaxTrees = new List<(string FilePath, SyntaxTree Tree)>();

        foreach (var file in files)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code, path: file);
            syntaxTrees.Add((file, tree));
        }

        var modelName = string.IsNullOrWhiteSpace(_options.ModelName)
            ? Path.GetFileName(Path.GetFullPath(targetPath))
            : _options.ModelName;

        var model = new UmlModel
        {
            Name = modelName,
            Id = $"model_{TypeHelper.SanitizeId(modelName)}"
        };

        // Phase 1: Discover all declared classifiers (Classes, Interfaces, Structs, Records, Enums)
        var discoveredClassifiers = new Dictionary<string, UmlClassifier>(StringComparer.Ordinal);
        var classifiersBySimpleName = new Dictionary<string, List<UmlClassifier>>(StringComparer.Ordinal);

        foreach (var (filePath, tree) in syntaxTrees)
        {
            var discoveryWalker = new TypeDiscoveryWalker(filePath, discoveredClassifiers, classifiersBySimpleName);
            discoveryWalker.Visit(tree.GetRoot());
        }

        // Add all discovered classifiers to model package hierarchy
        foreach (var classifier in discoveredClassifiers.Values)
        {
            model.AllClassifiersById[classifier.Id] = classifier;
            AddClassifierToModel(model, classifier);
        }

        // Phase 2: Extract members and relationships
        foreach (var (filePath, tree) in syntaxTrees)
        {
            var memberWalker = new MemberExtractionWalker(
                filePath,
                discoveredClassifiers,
                classifiersBySimpleName,
                _options);
            memberWalker.Visit(tree.GetRoot());
        }

        // Phase 3: Resolve types, multiplicity, associations, dependencies
        ResolveTypesAndRelationships(model, discoveredClassifiers, classifiersBySimpleName);

        return model;
    }

    private static void AddClassifierToModel(UmlModel model, UmlClassifier classifier)
    {
        if (string.IsNullOrWhiteSpace(classifier.Namespace))
        {
            model.GlobalClassifiers.Add(classifier);
            return;
        }

        var nsParts = classifier.Namespace.Split('.');
        var currentDict = model.RootPackages;
        UmlPackage? currentPkg = null;
        var accumulatedNs = "";

        foreach (var part in nsParts)
        {
            accumulatedNs = string.IsNullOrEmpty(accumulatedNs) ? part : $"{accumulatedNs}_{part}";
            if (!currentDict.TryGetValue(part, out currentPkg))
            {
                currentPkg = new UmlPackage
                {
                    Id = $"pkg_{TypeHelper.SanitizeId(accumulatedNs)}",
                    Name = part
                };
                currentDict[part] = currentPkg;
            }
            currentDict = currentPkg.SubPackages;
        }

        currentPkg?.Classifiers.Add(classifier);
    }

    private void ResolveTypesAndRelationships(
        UmlModel model,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName)
    {
        var existingAssociations = new HashSet<string>(StringComparer.Ordinal);
        var existingDependencies = new HashSet<string>(StringComparer.Ordinal);

        // Helper to resolve classifier ID for a given type name
        string ResolveTypeId(string rawType, UmlClassifier sourceClassifier)
        {
            var (elemType, isCollection, _, _) = TypeHelper.AnalyzeMultiplicity(rawType);
            var cleanType = TypeHelper.CleanTypeName(elemType);

            // 1. Primitive
            if (TypeHelper.IsPrimitive(cleanType))
            {
                var normName = TypeHelper.NormalizePrimitiveName(cleanType);
                if (!model.PrimitiveTypes.TryGetValue(normName, out var prim))
                {
                    prim = new UmlPrimitiveType
                    {
                        Id = $"prim_{normName}",
                        Name = normName
                    };
                    model.PrimitiveTypes[normName] = prim;
                    model.AllClassifiersById[prim.Id] = prim;
                }
                return prim.Id;
            }

            // 2. Discovered classifier (by full or simple name)
            var targetClassifier = FindDiscoveredClassifier(cleanType, sourceClassifier.Namespace, discoveredClassifiers, classifiersBySimpleName);
            if (targetClassifier != null)
            {
                return targetClassifier.Id;
            }

            // 3. Already registered external interface or class
            var simpleClean = TypeHelper.GetSimpleName(cleanType);
            if (model.ExternalInterfaces.TryGetValue(simpleClean, out var existingIface))
            {
                return existingIface.Id;
            }
            if (model.ExternalClasses.TryGetValue(simpleClean, out var existingCls))
            {
                return existingCls.Id;
            }

            // 4. External DataType
            var extId = $"ext_{TypeHelper.SanitizeId(simpleClean)}";
            if (!model.ExternalDataTypes.TryGetValue(simpleClean, out var extType))
            {
                extType = new UmlDataType
                {
                    Id = extId,
                    Name = simpleClean
                };
                model.ExternalDataTypes[simpleClean] = extType;
                model.AllClassifiersById[extId] = extType;
            }
            return extType.Id;
        }

        // Pass 1: Resolve all Generalizations and InterfaceRealizations
        // This ensures external base classes and implemented interfaces are registered
        // in ExternalClasses and ExternalInterfaces before member types are resolved.
        var initialClassifiers = model.AllClassifiersById.Values.ToList();
        foreach (var classifier in initialClassifiers)
        {
            if (classifier is UmlClass cls)
            {
                // Generalizations
                for (int i = 0; i < cls.Generalizations.Count; i++)
                {
                    var gen = cls.Generalizations[i];
                    var target = FindDiscoveredClassifier(gen.GeneralName, cls.Namespace, discoveredClassifiers, classifiersBySimpleName);
                    if (target != null)
                    {
                        cls.Generalizations[i] = new UmlGeneralization
                        {
                            Id = gen.Id,
                            SpecificId = gen.SpecificId,
                            GeneralId = target.Id,
                            GeneralName = target.Name
                        };
                    }
                    else
                    {
                        var simpleName = TypeHelper.GetSimpleName(gen.GeneralName);
                        var extId = $"cls_ext_{TypeHelper.SanitizeId(simpleName)}";
                        if (!model.ExternalClasses.TryGetValue(simpleName, out var extCls))
                        {
                            extCls = new UmlClass
                            {
                                Id = extId,
                                Name = simpleName
                            };
                            model.ExternalClasses[simpleName] = extCls;
                            model.AllClassifiersById[extId] = extCls;
                        }

                        cls.Generalizations[i] = new UmlGeneralization
                        {
                            Id = gen.Id,
                            SpecificId = gen.SpecificId,
                            GeneralId = extId,
                            GeneralName = gen.GeneralName
                        };
                    }
                }

                // Interface Realizations
                for (int i = 0; i < cls.InterfaceRealizations.Count; i++)
                {
                    var real = cls.InterfaceRealizations[i];
                    var target = FindDiscoveredClassifier(real.SupplierName, cls.Namespace, discoveredClassifiers, classifiersBySimpleName);
                    if (target != null)
                    {
                        cls.InterfaceRealizations[i] = new UmlInterfaceRealization
                        {
                            Id = real.Id,
                            ClientId = real.ClientId,
                            SupplierId = target.Id,
                            SupplierName = target.Name
                        };
                    }
                    else
                    {
                        var simpleName = TypeHelper.GetSimpleName(real.SupplierName);
                        var extId = $"iface_ext_{TypeHelper.SanitizeId(simpleName)}";
                        if (!model.ExternalInterfaces.TryGetValue(simpleName, out var extIface))
                        {
                            extIface = new UmlInterface
                            {
                                Id = extId,
                                Name = simpleName
                            };
                            model.ExternalInterfaces[simpleName] = extIface;
                            model.AllClassifiersById[extId] = extIface;
                        }

                        cls.InterfaceRealizations[i] = new UmlInterfaceRealization
                        {
                            Id = real.Id,
                            ClientId = real.ClientId,
                            SupplierId = extId,
                            SupplierName = real.SupplierName
                        };
                    }
                }
            }
            else if (classifier is UmlInterface iface)
            {
                // Interface Generalizations
                for (int i = 0; i < iface.Generalizations.Count; i++)
                {
                    var gen = iface.Generalizations[i];
                    var target = FindDiscoveredClassifier(gen.GeneralName, iface.Namespace, discoveredClassifiers, classifiersBySimpleName);
                    if (target != null)
                    {
                        iface.Generalizations[i] = new UmlGeneralization
                        {
                            Id = gen.Id,
                            SpecificId = gen.SpecificId,
                            GeneralId = target.Id,
                            GeneralName = target.Name
                        };
                    }
                    else
                    {
                        var simpleName = TypeHelper.GetSimpleName(gen.GeneralName);
                        var extId = $"iface_ext_{TypeHelper.SanitizeId(simpleName)}";
                        if (!model.ExternalInterfaces.TryGetValue(simpleName, out var extIface))
                        {
                            extIface = new UmlInterface
                            {
                                Id = extId,
                                Name = simpleName
                            };
                            model.ExternalInterfaces[simpleName] = extIface;
                            model.AllClassifiersById[extId] = extIface;
                        }

                        iface.Generalizations[i] = new UmlGeneralization
                        {
                            Id = gen.Id,
                            SpecificId = gen.SpecificId,
                            GeneralId = extId,
                            GeneralName = gen.GeneralName
                        };
                    }
                }
            }
        }

        // Pass 2: Process properties and operations
        foreach (var classifier in initialClassifiers)
        {
            if (classifier is UmlClass cls)
            {
                ProcessProperties(cls, cls.Properties, ResolveTypeId, model, discoveredClassifiers, classifiersBySimpleName, existingAssociations);
                ProcessOperations(cls, cls.Operations, ResolveTypeId, model, discoveredClassifiers, classifiersBySimpleName, existingDependencies);
            }
            else if (classifier is UmlInterface iface)
            {
                ProcessProperties(iface, iface.Properties, ResolveTypeId, model, discoveredClassifiers, classifiersBySimpleName, existingAssociations);
                ProcessOperations(iface, iface.Operations, ResolveTypeId, model, discoveredClassifiers, classifiersBySimpleName, existingDependencies);
            }
        }
    }

    private void ProcessProperties(
        UmlClassifier owner,
        List<UmlProperty> properties,
        Func<string, UmlClassifier, string> resolveTypeId,
        UmlModel model,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName,
        HashSet<string> existingAssociations)
    {
        foreach (var prop in properties)
        {
            prop.TypeId = resolveTypeId(prop.RawTypeName, owner);

            // Check if property points to a discovered class or interface
            var (elemType, isCollection, lower, upper) = TypeHelper.AnalyzeMultiplicity(prop.RawTypeName);
            var cleanElem = TypeHelper.CleanTypeName(elemType);
            var targetClassifier = FindDiscoveredClassifier(cleanElem, owner.Namespace, discoveredClassifiers, classifiersBySimpleName);

            if (targetClassifier != null && targetClassifier.Id != owner.Id)
            {
                var assocKey = $"{owner.Id}->{targetClassifier.Id}:{prop.Name}";
                if (existingAssociations.Add(assocKey))
                {
                    var assocId = $"assoc_{TypeHelper.SanitizeId(owner.Name)}_{TypeHelper.SanitizeId(prop.Name)}_{TypeHelper.SanitizeId(targetClassifier.Name)}";
                    prop.AssociationId = assocId;
                    model.Associations.Add(new UmlAssociation
                    {
                        Id = assocId,
                        Name = $"{owner.Name}_{prop.Name}",
                        SourceId = owner.Id,
                        TargetId = targetClassifier.Id,
                        PropertyId = prop.Id,
                        PropertyName = prop.Name,
                        IsCollection = isCollection,
                        LowerValue = lower,
                        UpperValue = upper
                    });
                }
            }
        }
    }

    private void ProcessOperations(
        UmlClassifier owner,
        List<UmlOperation> operations,
        Func<string, UmlClassifier, string> resolveTypeId,
        UmlModel model,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName,
        HashSet<string> existingDependencies)
    {
        foreach (var op in operations)
        {
            op.ReturnTypeId = resolveTypeId(op.ReturnTypeName, owner);

            // Dependency check for return type
            CheckDependency(owner, op.ReturnTypeName, model, discoveredClassifiers, classifiersBySimpleName, existingDependencies);

            foreach (var param in op.Parameters)
            {
                param.TypeId = resolveTypeId(param.TypeName, owner);
                CheckDependency(owner, param.TypeName, model, discoveredClassifiers, classifiersBySimpleName, existingDependencies);
            }
        }
    }

    private void CheckDependency(
        UmlClassifier source,
        string typeName,
        UmlModel model,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName,
        HashSet<string> existingDependencies)
    {
        var (elemType, _, _, _) = TypeHelper.AnalyzeMultiplicity(typeName);
        var cleanElem = TypeHelper.CleanTypeName(elemType);
        var target = FindDiscoveredClassifier(cleanElem, source.Namespace, discoveredClassifiers, classifiersBySimpleName);

        if (target != null && target.Id != source.Id)
        {
            // Do NOT create dependency if source already realizes target
            if (source is UmlClass cls && cls.InterfaceRealizations.Any(r => r.SupplierId == target.Id || r.SupplierName == target.Name))
            {
                return;
            }

            // Do NOT create dependency if source inherits from target
            if (source is UmlClass clsg && clsg.Generalizations.Any(g => g.GeneralId == target.Id || g.GeneralName == target.Name))
            {
                return;
            }

            if (source is UmlInterface ifc && ifc.Generalizations.Any(g => g.GeneralId == target.Id || g.GeneralName == target.Name))
            {
                return;
            }

            // Do NOT create dependency if an association already exists between source and target
            if (model.Associations.Any(a => (a.SourceId == source.Id && a.TargetId == target.Id) || (a.SourceId == target.Id && a.TargetId == source.Id)))
            {
                return;
            }

            // Do not create duplicate dependency
            var depKey = $"{source.Id}->{target.Id}";
            if (existingDependencies.Add(depKey))
            {
                model.Dependencies.Add(new UmlDependency
                {
                    Id = $"dep_{TypeHelper.SanitizeId(source.Name)}_{TypeHelper.SanitizeId(target.Name)}",
                    Name = string.Empty, // omit name so it doesn't display ugly "uses"
                    ClientId = source.Id,
                    SupplierId = target.Id
                });
            }
        }
    }

    private static UmlClassifier? FindDiscoveredClassifier(
        string typeName,
        string currentNamespace,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName)
    {
        var simpleName = TypeHelper.GetSimpleName(typeName);

        // 1. Direct qualified lookup
        if (discoveredClassifiers.TryGetValue(typeName, out var directMatch))
        {
            return directMatch;
        }

        // 2. Lookup within current namespace
        if (!string.IsNullOrEmpty(currentNamespace))
        {
            var currentNsQualified = $"{currentNamespace}.{simpleName}";
            if (discoveredClassifiers.TryGetValue(currentNsQualified, out var nsMatch))
            {
                return nsMatch;
            }
        }

        // 3. Simple name lookup
        if (classifiersBySimpleName.TryGetValue(simpleName, out var candidates) && candidates.Count > 0)
        {
            // Prefer one in current or child namespace
            if (!string.IsNullOrEmpty(currentNamespace))
            {
                var candidate = candidates.FirstOrDefault(c => c.Namespace.StartsWith(currentNamespace, StringComparison.Ordinal));
                if (candidate != null) return candidate;
            }

            return candidates[0];
        }

        return null;
    }
}
