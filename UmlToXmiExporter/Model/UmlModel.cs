using System.Collections.Generic;

namespace UmlToXmiExporter.Model;

public enum UmlVisibility
{
    Public,
    Protected,
    Private,
    Package
}

public abstract class UmlClassifier
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public UmlVisibility Visibility { get; init; } = UmlVisibility.Public;
    public bool IsAbstract { get; init; }
    public bool IsStatic { get; init; }
    public string? SourceFilePath { get; init; }

    public string QualifiedName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
}

public class UmlClass : UmlClassifier
{
    public List<UmlProperty> Properties { get; } = new();
    public List<UmlOperation> Operations { get; } = new();
    public List<UmlGeneralization> Generalizations { get; } = new();
    public List<UmlInterfaceRealization> InterfaceRealizations { get; } = new();
    public bool IsRecord { get; init; }
    public bool IsStruct { get; init; }
}

public class UmlInterface : UmlClassifier
{
    public List<UmlProperty> Properties { get; } = new();
    public List<UmlOperation> Operations { get; } = new();
    public List<UmlGeneralization> Generalizations { get; } = new();
}

public class UmlEnumeration : UmlClassifier
{
    public List<string> Literals { get; } = new();
}

public class UmlDataType : UmlClassifier
{
}

public class UmlPrimitiveType : UmlClassifier
{
}

public class UmlProperty
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public UmlVisibility Visibility { get; init; } = UmlVisibility.Public;
    public required string RawTypeName { get; init; }
    public string? ElementTypeName { get; init; }
    public string? TypeId { get; set; }
    public string? AssociationId { get; set; }
    public bool IsStatic { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsCollection { get; init; }
    public bool IsNullable { get; init; }
    public int LowerValue { get; init; } = 1;
    public string UpperValue { get; init; } = "1";
    public bool IsField { get; init; }
}

public class UmlOperation
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public UmlVisibility Visibility { get; init; } = UmlVisibility.Public;
    public bool IsStatic { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsConstructor { get; init; }
    public required string ReturnTypeName { get; init; }
    public string? ReturnTypeId { get; set; }
    public List<UmlParameter> Parameters { get; } = new();
}

public class UmlParameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? TypeId { get; set; }
    public string Direction { get; init; } = "in"; // "in", "out", "inout", "return"
    public string? DefaultValue { get; init; }
}

public class UmlGeneralization
{
    public required string Id { get; init; }
    public required string SpecificId { get; init; }
    public required string GeneralId { get; init; }
    public required string GeneralName { get; init; }
}

public class UmlInterfaceRealization
{
    public required string Id { get; init; }
    public required string ClientId { get; init; }
    public required string SupplierId { get; init; }
    public required string SupplierName { get; init; }
}

public class UmlAssociation
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public string? PropertyId { get; set; }
    public string? PropertyName { get; init; }
    public bool IsCollection { get; init; }
    public int LowerValue { get; init; } = 0;
    public string UpperValue { get; init; } = "1";
}

public class UmlDependency
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ClientId { get; init; }
    public required string SupplierId { get; init; }
}

public class UmlPackage
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, UmlPackage> SubPackages { get; } = new();
    public List<UmlClassifier> Classifiers { get; } = new();
}

public class UmlModel
{
    public required string Name { get; init; }
    public required string Id { get; init; }
    public Dictionary<string, UmlPackage> RootPackages { get; } = new();
    public List<UmlClassifier> GlobalClassifiers { get; } = new();
    public List<UmlAssociation> Associations { get; } = new();
    public List<UmlDependency> Dependencies { get; } = new();
    public Dictionary<string, UmlClassifier> AllClassifiersById { get; } = new();
    public Dictionary<string, UmlPrimitiveType> PrimitiveTypes { get; } = new();
    public Dictionary<string, UmlDataType> ExternalDataTypes { get; } = new();
    public Dictionary<string, UmlInterface> ExternalInterfaces { get; } = new();
    public Dictionary<string, UmlClass> ExternalClasses { get; } = new();
}
