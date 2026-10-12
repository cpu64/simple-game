using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UmlToXmiExporter.Model;

namespace UmlToXmiExporter.Analysis;

public class TypeDiscoveryWalker : CSharpSyntaxWalker
{
    private readonly string _filePath;
    private readonly Dictionary<string, UmlClassifier> _discoveredClassifiers;
    private readonly Dictionary<string, List<UmlClassifier>> _classifiersBySimpleName;
    private readonly Stack<string> _namespaceStack = new();

    public TypeDiscoveryWalker(
        string filePath,
        Dictionary<string, UmlClassifier> discoveredClassifiers,
        Dictionary<string, List<UmlClassifier>> classifiersBySimpleName)
    {
        _filePath = filePath;
        _discoveredClassifiers = discoveredClassifiers;
        _classifiersBySimpleName = classifiersBySimpleName;
    }

    private string CurrentNamespace => string.Join(".", _namespaceStack.Reverse());

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
        RegisterClass(node, isRecord: false, isStruct: false);
        base.VisitClassDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        bool isStruct = node.Kind() == SyntaxKind.RecordStructDeclaration;
        RegisterClass(node, isRecord: true, isStruct: isStruct);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        RegisterClass(node, isRecord: false, isStruct: true);
        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var name = node.Identifier.Text;
        var ns = CurrentNamespace;
        var qualifiedName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

        if (!_discoveredClassifiers.ContainsKey(qualifiedName))
        {
            var iface = new UmlInterface
            {
                Id = $"iface_{TypeHelper.SanitizeId(qualifiedName)}",
                Name = name,
                Namespace = ns,
                Visibility = ExtractVisibility(node.Modifiers, isInterfaceMember: false),
                SourceFilePath = _filePath
            };

            _discoveredClassifiers[qualifiedName] = iface;
            AddBySimpleName(name, iface);
        }

        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var name = node.Identifier.Text;
        var ns = CurrentNamespace;
        var qualifiedName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

        if (!_discoveredClassifiers.ContainsKey(qualifiedName))
        {
            var enm = new UmlEnumeration
            {
                Id = $"enum_{TypeHelper.SanitizeId(qualifiedName)}",
                Name = name,
                Namespace = ns,
                Visibility = ExtractVisibility(node.Modifiers, isInterfaceMember: false),
                SourceFilePath = _filePath
            };

            _discoveredClassifiers[qualifiedName] = enm;
            AddBySimpleName(name, enm);
        }

        base.VisitEnumDeclaration(node);
    }

    private void RegisterClass(TypeDeclarationSyntax node, bool isRecord, bool isStruct)
    {
        var name = node.Identifier.Text;
        var ns = CurrentNamespace;
        var qualifiedName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

        if (!_discoveredClassifiers.ContainsKey(qualifiedName))
        {
            bool isAbstract = node.Modifiers.Any(SyntaxKind.AbstractKeyword);
            bool isStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);

            var cls = new UmlClass
            {
                Id = $"cls_{TypeHelper.SanitizeId(qualifiedName)}",
                Name = name,
                Namespace = ns,
                Visibility = ExtractVisibility(node.Modifiers, isInterfaceMember: false),
                IsAbstract = isAbstract,
                IsStatic = isStatic,
                IsRecord = isRecord,
                IsStruct = isStruct,
                SourceFilePath = _filePath
            };

            _discoveredClassifiers[qualifiedName] = cls;
            AddBySimpleName(name, cls);
        }
    }

    private void AddBySimpleName(string name, UmlClassifier classifier)
    {
        if (!_classifiersBySimpleName.TryGetValue(name, out var list))
        {
            list = new List<UmlClassifier>();
            _classifiersBySimpleName[name] = list;
        }
        list.Add(classifier);
    }

    public static UmlVisibility ExtractVisibility(SyntaxTokenList modifiers, bool isInterfaceMember)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return UmlVisibility.Public;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            return UmlVisibility.Protected;
        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return UmlVisibility.Package;
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return UmlVisibility.Private;

        // Default visibility
        return isInterfaceMember ? UmlVisibility.Public : UmlVisibility.Private;
    }
}
