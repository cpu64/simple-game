using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UmlToXmiExporter.Model;

namespace UmlToXmiExporter.Analysis;

public class MemberExtractionWalker : CSharpSyntaxWalker
{
    private readonly string _filePath;
    private readonly Dictionary<string, UmlClassifier> _discoveredClassifiers;
    private readonly Dictionary<string, List<UmlClassifier>> _classifiersBySimpleName;
    private readonly AnalyzerOptions _options;
    private readonly Stack<string> _namespaceStack = new();
    private readonly Stack<UmlClassifier> _classifierStack = new();

    public MemberExtractionWalker(
        string filePath,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName,
        AnalyzerOptions options)
    {
        _filePath = filePath;
        _discoveredClassifiers = discoveredClassifiers;
        _classifiersBySimpleName = classifiersBySimpleName;
        _options = options;
    }

    private string CurrentNamespace => string.Join(".", _namespaceStack.Reverse());
    private UmlClassifier? CurrentClassifier => _classifierStack.Count > 0 ? _classifierStack.Peek() : null;

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        _namespaceStack.Push(node.Name.ToString());
        base.VisitNamespaceDeclaration(node);
        _namespaceStack.Pop();
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        _namespaceStack.Push(node.Name.ToString());
        base.VisitFileScopedNamespaceDeclaration(node);
        _namespaceStack.Pop();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        EnterType(node.Identifier.Text, node.BaseList, node);
        base.VisitClassDeclaration(node);
        ExitType();
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        EnterType(node.Identifier.Text, node.BaseList, node);

        // Process positional parameters as properties
        if (CurrentClassifier is UmlClass cls && node.ParameterList != null)
        {
            foreach (var param in node.ParameterList.Parameters)
            {
                var pName = param.Identifier.Text;
                var rawType = param.Type?.ToString() ?? "object";
                var (elemType, isCollection, lower, upper) = TypeHelper.AnalyzeMultiplicity(rawType);

                if (!cls.Properties.Any(p => p.Name == pName))
                {
                    cls.Properties.Add(new UmlProperty
                    {
                        Id = $"prop_{TypeHelper.SanitizeId(cls.Name)}_{TypeHelper.SanitizeId(pName)}",
                        Name = pName,
                        RawTypeName = rawType,
                        ElementTypeName = elemType,
                        Visibility = UmlVisibility.Public,
                        IsReadOnly = true,
                        IsCollection = isCollection,
                        LowerValue = lower,
                        UpperValue = upper
                    });
                }
            }
        }

        base.VisitRecordDeclaration(node);
        ExitType();
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        EnterType(node.Identifier.Text, node.BaseList, node);
        base.VisitStructDeclaration(node);
        ExitType();
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        EnterType(node.Identifier.Text, node.BaseList, node);
        base.VisitInterfaceDeclaration(node);
        ExitType();
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        EnterType(node.Identifier.Text, null, node);
        if (CurrentClassifier is UmlEnumeration enm)
        {
            foreach (var member in node.Members)
            {
                var litName = member.Identifier.Text;
                if (!enm.Literals.Contains(litName))
                {
                    enm.Literals.Add(litName);
                }
            }
        }
        base.VisitEnumDeclaration(node);
        ExitType();
    }

    private void EnterType(string name, BaseListSyntax? baseList, TypeDeclarationSyntax node)
    {
        var ns = CurrentNamespace;
        var qualifiedName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

        if (!_discoveredClassifiers.TryGetValue(qualifiedName, out var classifier))
        {
            return;
        }

        _classifierStack.Push(classifier);

        // Extract base types and interface realizations
        if (baseList != null)
        {
            foreach (var baseType in baseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();
                var cleanBaseName = TypeHelper.CleanTypeName(baseTypeName);
                var simpleBase = TypeHelper.GetSimpleName(cleanBaseName);

                bool isInterface = IsLikelyInterface(simpleBase);

                if (classifier is UmlClass cls)
                {
                    if (isInterface)
                    {
                        var realId = $"real_{TypeHelper.SanitizeId(cls.Name)}_{TypeHelper.SanitizeId(simpleBase)}";
                        if (!cls.InterfaceRealizations.Any(r => r.SupplierName == simpleBase))
                        {
                            cls.InterfaceRealizations.Add(new UmlInterfaceRealization
                            {
                                Id = realId,
                                ClientId = cls.Id,
                                SupplierId = string.Empty, // resolved later
                                SupplierName = simpleBase
                            });
                        }
                    }
                    else
                    {
                        var genId = $"gen_{TypeHelper.SanitizeId(cls.Name)}_{TypeHelper.SanitizeId(simpleBase)}";
                        if (!cls.Generalizations.Any(g => g.GeneralName == simpleBase))
                        {
                            cls.Generalizations.Add(new UmlGeneralization
                            {
                                Id = genId,
                                SpecificId = cls.Id,
                                GeneralId = string.Empty, // resolved later
                                GeneralName = simpleBase
                            });
                        }
                    }
                }
                else if (classifier is UmlInterface iface)
                {
                    // In UML, interface-to-interface is generalization
                    var genId = $"gen_{TypeHelper.SanitizeId(iface.Name)}_{TypeHelper.SanitizeId(simpleBase)}";
                    if (!iface.Generalizations.Any(g => g.GeneralName == simpleBase))
                    {
                        iface.Generalizations.Add(new UmlGeneralization
                        {
                            Id = genId,
                            SpecificId = iface.Id,
                            GeneralId = string.Empty, // resolved later
                            GeneralName = simpleBase
                        });
                    }
                }
            }
        }
    }

    private void EnterType(string name, BaseListSyntax? baseList, EnumDeclarationSyntax node)
    {
        var ns = CurrentNamespace;
        var qualifiedName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        if (_discoveredClassifiers.TryGetValue(qualifiedName, out var classifier))
        {
            _classifierStack.Push(classifier);
        }
    }

    private void ExitType()
    {
        if (_classifierStack.Count > 0)
        {
            _classifierStack.Pop();
        }
    }

    private bool IsLikelyInterface(string simpleName)
    {
        // 1. Check if discovered as an interface
        if (_classifiersBySimpleName.TryGetValue(simpleName, out var list))
        {
            if (list.Any(c => c is UmlInterface))
            {
                return true;
            }
            if (list.Any(c => c is UmlClass))
            {
                return false;
            }
        }

        // 2. Standard C# convention: starts with 'I' and second letter is uppercase
        return simpleName.Length > 1 && simpleName[0] == 'I' && char.IsUpper(simpleName[1]);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var current = CurrentClassifier;
        if (current == null) return;

        bool isInterface = current is UmlInterface;
        var visibility = TypeDiscoveryWalker.ExtractVisibility(node.Modifiers, isInterface);
        if (!_options.IncludePrivate && visibility == UmlVisibility.Private)
        {
            return;
        }

        var propName = node.Identifier.Text;
        var rawType = node.Type.ToString();
        var (elemType, isCollection, lower, upper) = TypeHelper.AnalyzeMultiplicity(rawType);
        bool isStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);
        bool isReadOnly = node.AccessorList == null ||
            !node.AccessorList.Accessors.Any(a =>
                a.Kind() == SyntaxKind.SetAccessorDeclaration ||
                a.Kind() == SyntaxKind.InitAccessorDeclaration);

        var prop = new UmlProperty
        {
            Id = $"prop_{TypeHelper.SanitizeId(current.Name)}_{TypeHelper.SanitizeId(propName)}",
            Name = propName,
            Visibility = visibility,
            RawTypeName = rawType,
            ElementTypeName = elemType,
            IsStatic = isStatic,
            IsReadOnly = isReadOnly,
            IsCollection = isCollection,
            LowerValue = lower,
            UpperValue = upper,
            IsField = false
        };

        if (current is UmlClass cls)
        {
            if (!cls.Properties.Any(p => p.Name == propName))
            {
                cls.Properties.Add(prop);
            }
        }
        else if (current is UmlInterface iface)
        {
            if (!iface.Properties.Any(p => p.Name == propName))
            {
                iface.Properties.Add(prop);
            }
        }

        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (!_options.IncludeFields) return;

        var current = CurrentClassifier;
        if (current is not UmlClass cls) return;

        var visibility = TypeDiscoveryWalker.ExtractVisibility(node.Modifiers, isInterfaceMember: false);
        if (!_options.IncludePrivate && visibility == UmlVisibility.Private)
        {
            return;
        }

        bool isStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);
        bool isReadOnly = node.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) || node.Modifiers.Any(SyntaxKind.ConstKeyword);
        var rawType = node.Declaration.Type.ToString();
        var (elemType, isCollection, lower, upper) = TypeHelper.AnalyzeMultiplicity(rawType);

        foreach (var variable in node.Declaration.Variables)
        {
            var fieldName = variable.Identifier.Text;
            var prop = new UmlProperty
            {
                Id = $"field_{TypeHelper.SanitizeId(cls.Name)}_{TypeHelper.SanitizeId(fieldName)}",
                Name = fieldName,
                Visibility = visibility,
                RawTypeName = rawType,
                ElementTypeName = elemType,
                IsStatic = isStatic,
                IsReadOnly = isReadOnly,
                IsCollection = isCollection,
                LowerValue = lower,
                UpperValue = upper,
                IsField = true
            };

            if (!cls.Properties.Any(p => p.Name == fieldName))
            {
                cls.Properties.Add(prop);
            }
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var current = CurrentClassifier;
        if (current == null) return;

        bool isInterface = current is UmlInterface;
        var visibility = TypeDiscoveryWalker.ExtractVisibility(node.Modifiers, isInterface);
        if (!_options.IncludePrivate && visibility == UmlVisibility.Private)
        {
            return;
        }

        var methodName = node.Identifier.Text;
        var returnType = node.ReturnType.ToString();
        bool isStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);
        bool isAbstract = node.Modifiers.Any(SyntaxKind.AbstractKeyword);

        var existingCount = (current is UmlClass c ? c.Operations : (current is UmlInterface i ? i.Operations : null))?
            .Count(op => op.Name == methodName) ?? 0;

        var opId = $"op_{TypeHelper.SanitizeId(current.Name)}_{TypeHelper.SanitizeId(methodName)}_{(existingCount > 0 ? (existingCount + 1).ToString() : "1")}";

        var op = new UmlOperation
        {
            Id = opId,
            Name = methodName,
            Visibility = visibility,
            IsStatic = isStatic,
            IsAbstract = isAbstract,
            ReturnTypeName = returnType
        };

        foreach (var param in node.ParameterList.Parameters)
        {
            var pName = param.Identifier.Text;
            var pType = param.Type?.ToString() ?? "object";
            string direction = "in";
            if (param.Modifiers.Any(SyntaxKind.OutKeyword)) direction = "out";
            else if (param.Modifiers.Any(SyntaxKind.RefKeyword)) direction = "inout";

            op.Parameters.Add(new UmlParameter
            {
                Id = $"param_{TypeHelper.SanitizeId(op.Id)}_{TypeHelper.SanitizeId(pName)}",
                Name = pName,
                TypeName = pType,
                Direction = direction,
                DefaultValue = param.Default?.Value.ToString()
            });
        }

        if (current is UmlClass cl)
        {
            cl.Operations.Add(op);
        }
        else if (current is UmlInterface ifc)
        {
            ifc.Operations.Add(op);
        }

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var current = CurrentClassifier;
        if (current is not UmlClass cls) return;

        var visibility = TypeDiscoveryWalker.ExtractVisibility(node.Modifiers, isInterfaceMember: false);
        if (!_options.IncludePrivate && visibility == UmlVisibility.Private)
        {
            return;
        }

        var ctorName = node.Identifier.Text;
        var existingCount = cls.Operations.Count(op => op.IsConstructor);

        var opId = $"ctor_{TypeHelper.SanitizeId(cls.Name)}_{(existingCount > 0 ? (existingCount + 1).ToString() : "1")}";

        var op = new UmlOperation
        {
            Id = opId,
            Name = ctorName,
            Visibility = visibility,
            IsStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsConstructor = true,
            ReturnTypeName = "void"
        };

        foreach (var param in node.ParameterList.Parameters)
        {
            var pName = param.Identifier.Text;
            var pType = param.Type?.ToString() ?? "object";
            string direction = "in";
            if (param.Modifiers.Any(SyntaxKind.OutKeyword)) direction = "out";
            else if (param.Modifiers.Any(SyntaxKind.RefKeyword)) direction = "inout";

            op.Parameters.Add(new UmlParameter
            {
                Id = $"param_{TypeHelper.SanitizeId(op.Id)}_{TypeHelper.SanitizeId(pName)}",
                Name = pName,
                TypeName = pType,
                Direction = direction,
                DefaultValue = param.Default?.Value.ToString()
            });
        }

        cls.Operations.Add(op);
        base.VisitConstructorDeclaration(node);
    }
}
