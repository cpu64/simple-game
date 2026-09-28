using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UmlToXmiExporter.Model;
using UmlToXmiExporter.Analysis;

namespace UmlToXmiExporter.Serialization;

public enum XmiDialect
{
    OmgUml25,   // http://www.omg.org/spec/UML/20131001
    OmgUml251,  // http://www.omg.org/spec/UML/20161101
    EclipseUml2 // http://www.eclipse.org/uml2/5.0.0/UML
}

public class SerializationOptions
{
    public XmiDialect Dialect { get; set; } = XmiDialect.OmgUml251;
    public bool IncludeOperations { get; set; } = true;
    public bool IncludeProperties { get; set; } = true;
    public bool IncludeAssociations { get; set; } = true;
    public bool IncludeDependencies { get; set; } = true;
}

public class XmiSerializer
{
    private readonly SerializationOptions _options;

    public XmiSerializer(SerializationOptions? options = null)
    {
        _options = options ?? new SerializationOptions();
    }

    public void SerializeToFile(UmlModel model, string outputPath)
    {
        var doc = BuildXmiDocument(model);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var xmlSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using var writer = XmlWriter.Create(outputPath, xmlSettings);
        doc.Save(writer);
    }

    public string SerializeToString(UmlModel model)
    {
        var doc = BuildXmiDocument(model);
        var sb = new StringBuilder();
        var xmlSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(sb, xmlSettings))
        {
            doc.Save(writer);
        }

        return sb.ToString();
    }

    public XDocument BuildXmiDocument(UmlModel model)
    {
        XNamespace xmiNs = "http://www.omg.org/spec/XMI/20131001";
        XNamespace umlNs = _options.Dialect switch
        {
            XmiDialect.OmgUml25 => "http://www.omg.org/spec/UML/20131001",
            XmiDialect.OmgUml251 => "http://www.omg.org/spec/UML/20161101",
            XmiDialect.EclipseUml2 => "http://www.eclipse.org/uml2/5.0.0/UML",
            _ => "http://www.omg.org/spec/UML/20161101"
        };

        var root = new XElement(xmiNs + "XMI",
            new XAttribute(XNamespace.Xmlns + "xmi", xmiNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "uml", umlNs.NamespaceName),
            new XAttribute(xmiNs + "version", "20131001")
        );

        var modelElem = new XElement(umlNs + "Model",
            new XAttribute(xmiNs + "type", "uml:Model"),
            new XAttribute(xmiNs + "id", model.Id),
            new XAttribute("name", model.Name)
        );
        root.Add(modelElem);

        // 1. Primitive Types
        if (model.PrimitiveTypes.Count > 0)
        {
            var primPkg = new XElement("packagedElement",
                new XAttribute(xmiNs + "type", "uml:Package"),
                new XAttribute(xmiNs + "id", "pkg_PrimitiveTypes"),
                new XAttribute("name", "PrimitiveTypes")
            );

            foreach (var prim in model.PrimitiveTypes.Values)
            {
                primPkg.Add(new XElement("packagedElement",
                    new XAttribute(xmiNs + "type", "uml:PrimitiveType"),
                    new XAttribute(xmiNs + "id", prim.Id),
                    new XAttribute("name", prim.Name)
                ));
            }
            modelElem.Add(primPkg);
        }

        // 2. External Types (Interfaces, Classes, Data Types)
        if (model.ExternalInterfaces.Count > 0 || model.ExternalClasses.Count > 0 || model.ExternalDataTypes.Count > 0)
        {
            var extPkg = new XElement("packagedElement",
                new XAttribute(xmiNs + "type", "uml:Package"),
                new XAttribute(xmiNs + "id", "pkg_ExternalTypes"),
                new XAttribute("name", "ExternalTypes")
            );

            foreach (var iface in model.ExternalInterfaces.Values)
            {
                extPkg.Add(new XElement("packagedElement",
                    new XAttribute(xmiNs + "type", "uml:Interface"),
                    new XAttribute(xmiNs + "id", iface.Id),
                    new XAttribute("name", iface.Name)
                ));
            }

            foreach (var cls in model.ExternalClasses.Values)
            {
                extPkg.Add(new XElement("packagedElement",
                    new XAttribute(xmiNs + "type", "uml:Class"),
                    new XAttribute(xmiNs + "id", cls.Id),
                    new XAttribute("name", cls.Name)
                ));
            }

            foreach (var ext in model.ExternalDataTypes.Values)
            {
                extPkg.Add(new XElement("packagedElement",
                    new XAttribute(xmiNs + "type", "uml:DataType"),
                    new XAttribute(xmiNs + "id", ext.Id),
                    new XAttribute("name", ext.Name)
                ));
            }
            modelElem.Add(extPkg);
        }

        // 3. Hierarchical Packages
        foreach (var pkg in model.RootPackages.Values)
        {
            modelElem.Add(SerializePackage(pkg, xmiNs, umlNs));
        }

        // 4. Global Classifiers (not in any namespace)
        foreach (var classifier in model.GlobalClassifiers)
        {
            modelElem.Add(SerializeClassifier(classifier, xmiNs, umlNs));
        }

        // 5. Associations
        if (_options.IncludeAssociations)
        {
            foreach (var assoc in model.Associations)
            {
                modelElem.Add(SerializeAssociation(assoc, xmiNs));
            }
        }

        // 6. Dependencies
        if (_options.IncludeDependencies)
        {
            foreach (var dep in model.Dependencies)
            {
                modelElem.Add(SerializeDependency(dep, xmiNs));
            }
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
    }

    private XElement SerializePackage(UmlPackage pkg, XNamespace xmiNs, XNamespace umlNs)
    {
        var pkgElem = new XElement("packagedElement",
            new XAttribute(xmiNs + "type", "uml:Package"),
            new XAttribute(xmiNs + "id", pkg.Id),
            new XAttribute("name", pkg.Name)
        );

        foreach (var subPkg in pkg.SubPackages.Values)
        {
            pkgElem.Add(SerializePackage(subPkg, xmiNs, umlNs));
        }

        foreach (var classifier in pkg.Classifiers)
        {
            pkgElem.Add(SerializeClassifier(classifier, xmiNs, umlNs));
        }

        return pkgElem;
    }

    private XElement SerializeClassifier(UmlClassifier classifier, XNamespace xmiNs, XNamespace umlNs)
    {
        if (classifier is UmlClass cls)
        {
            var clsElem = new XElement("packagedElement",
                new XAttribute(xmiNs + "type", "uml:Class"),
                new XAttribute(xmiNs + "id", cls.Id),
                new XAttribute("name", cls.Name),
                new XAttribute("visibility", FormatVisibility(cls.Visibility)),
                new XAttribute("isAbstract", cls.IsAbstract ? "true" : "false")
            );

            // clientDependency attribute on Class for Interface Realizations
            if (cls.InterfaceRealizations.Count > 0)
            {
                clsElem.Add(new XAttribute("clientDependency", string.Join(" ", cls.InterfaceRealizations.Select(r => r.Id))));
            }

            // Generalizations
            foreach (var gen in cls.Generalizations)
            {
                clsElem.Add(new XElement("generalization",
                    new XAttribute(xmiNs + "type", "uml:Generalization"),
                    new XAttribute(xmiNs + "id", gen.Id),
                    new XAttribute("general", gen.GeneralId)
                ));
            }

            // Interface Realizations
            foreach (var real in cls.InterfaceRealizations)
            {
                clsElem.Add(new XElement("interfaceRealization",
                    new XAttribute(xmiNs + "type", "uml:InterfaceRealization"),
                    new XAttribute(xmiNs + "id", real.Id),
                    new XAttribute("client", real.ClientId),
                    new XAttribute("supplier", real.SupplierId),
                    new XAttribute("contract", real.SupplierId)
                ));
            }

            // Properties
            if (_options.IncludeProperties)
            {
                foreach (var prop in cls.Properties)
                {
                    clsElem.Add(SerializeProperty(prop, xmiNs));
                }
            }

            // Operations
            if (_options.IncludeOperations)
            {
                foreach (var op in cls.Operations)
                {
                    clsElem.Add(SerializeOperation(op, xmiNs));
                }
            }

            return clsElem;
        }

        if (classifier is UmlInterface iface)
        {
            var ifaceElem = new XElement("packagedElement",
                new XAttribute(xmiNs + "type", "uml:Interface"),
                new XAttribute(xmiNs + "id", iface.Id),
                new XAttribute("name", iface.Name),
                new XAttribute("visibility", FormatVisibility(iface.Visibility)),
                new XAttribute("isAbstract", "true")
            );

            // Interface Generalizations
            foreach (var gen in iface.Generalizations)
            {
                ifaceElem.Add(new XElement("generalization",
                    new XAttribute(xmiNs + "type", "uml:Generalization"),
                    new XAttribute(xmiNs + "id", gen.Id),
                    new XAttribute("general", gen.GeneralId)
                ));
            }

            // Properties
            if (_options.IncludeProperties)
            {
                foreach (var prop in iface.Properties)
                {
                    ifaceElem.Add(SerializeProperty(prop, xmiNs));
                }
            }

            // Operations
            if (_options.IncludeOperations)
            {
                foreach (var op in iface.Operations)
                {
                    ifaceElem.Add(SerializeOperation(op, xmiNs));
                }
            }

            return ifaceElem;
        }

        if (classifier is UmlEnumeration enm)
        {
            var enumElem = new XElement("packagedElement",
                new XAttribute(xmiNs + "type", "uml:Enumeration"),
                new XAttribute(xmiNs + "id", enm.Id),
                new XAttribute("name", enm.Name),
                new XAttribute("visibility", FormatVisibility(enm.Visibility))
            );

            foreach (var lit in enm.Literals)
            {
                enumElem.Add(new XElement("ownedLiteral",
                    new XAttribute(xmiNs + "type", "uml:EnumerationLiteral"),
                    new XAttribute(xmiNs + "id", $"lit_{enm.Id}_{lit}"),
                    new XAttribute("name", lit)
                ));
            }

            return enumElem;
        }

        // Generic DataType
        return new XElement("packagedElement",
            new XAttribute(xmiNs + "type", "uml:DataType"),
            new XAttribute(xmiNs + "id", classifier.Id),
            new XAttribute("name", classifier.Name)
        );
    }

    private XElement SerializeProperty(UmlProperty prop, XNamespace xmiNs)
    {
        var propElem = new XElement("ownedAttribute",
            new XAttribute(xmiNs + "type", "uml:Property"),
            new XAttribute(xmiNs + "id", prop.Id),
            new XAttribute("name", prop.Name),
            new XAttribute("visibility", FormatVisibility(prop.Visibility)),
            new XAttribute("isStatic", prop.IsStatic ? "true" : "false"),
            new XAttribute("isReadOnly", prop.IsReadOnly ? "true" : "false")
        );

        if (!string.IsNullOrEmpty(prop.TypeId))
        {
            propElem.Add(new XAttribute("type", prop.TypeId));
        }

        if (!string.IsNullOrEmpty(prop.AssociationId))
        {
            propElem.Add(new XAttribute("association", prop.AssociationId));
        }

        // Multiplicity
        if (prop.LowerValue != 1 || prop.UpperValue != "1")
        {
            propElem.Add(new XElement("lowerValue",
                new XAttribute(xmiNs + "type", "uml:LiteralInteger"),
                new XAttribute(xmiNs + "id", $"{prop.Id}_lower"),
                new XAttribute("value", prop.LowerValue)
            ));

            propElem.Add(new XElement("upperValue",
                new XAttribute(xmiNs + "type", "uml:LiteralUnlimitedNatural"),
                new XAttribute(xmiNs + "id", $"{prop.Id}_upper"),
                new XAttribute("value", prop.UpperValue)
            ));
        }

        return propElem;
    }

    private XElement SerializeOperation(UmlOperation op, XNamespace xmiNs)
    {
        var opElem = new XElement("ownedOperation",
            new XAttribute(xmiNs + "type", "uml:Operation"),
            new XAttribute(xmiNs + "id", op.Id),
            new XAttribute("name", op.Name),
            new XAttribute("visibility", FormatVisibility(op.Visibility)),
            new XAttribute("isStatic", op.IsStatic ? "true" : "false"),
            new XAttribute("isAbstract", op.IsAbstract ? "true" : "false")
        );

        // Parameters
        foreach (var param in op.Parameters)
        {
            var paramElem = new XElement("ownedParameter",
                new XAttribute(xmiNs + "type", "uml:Parameter"),
                new XAttribute(xmiNs + "id", param.Id),
                new XAttribute("name", param.Name),
                new XAttribute("direction", param.Direction)
            );

            if (!string.IsNullOrEmpty(param.TypeId))
            {
                paramElem.Add(new XAttribute("type", param.TypeId));
            }

            if (!string.IsNullOrEmpty(param.DefaultValue))
            {
                paramElem.Add(new XElement("defaultValue",
                    new XAttribute(xmiNs + "type", "uml:LiteralString"),
                    new XAttribute(xmiNs + "id", $"{param.Id}_default"),
                    new XAttribute("value", param.DefaultValue)
                ));
            }

            opElem.Add(paramElem);
        }

        // Return Parameter (ONLY if not constructor and return type is NOT void)
        if (!op.IsConstructor &&
            !string.IsNullOrEmpty(op.ReturnTypeId) &&
            op.ReturnTypeId != "prim_void" &&
            !string.Equals(op.ReturnTypeName, "void", StringComparison.OrdinalIgnoreCase))
        {
            var retElem = new XElement("ownedParameter",
                new XAttribute(xmiNs + "type", "uml:Parameter"),
                new XAttribute(xmiNs + "id", $"{op.Id}_ret"),
                new XAttribute("direction", "return"),
                new XAttribute("type", op.ReturnTypeId)
            );

            var (_, isCollection, lower, upper) = TypeHelper.AnalyzeMultiplicity(op.ReturnTypeName);
            if (isCollection)
            {
                retElem.Add(new XElement("lowerValue",
                    new XAttribute(xmiNs + "type", "uml:LiteralInteger"),
                    new XAttribute(xmiNs + "id", $"{op.Id}_ret_lower"),
                    new XAttribute("value", lower)
                ));

                retElem.Add(new XElement("upperValue",
                    new XAttribute(xmiNs + "type", "uml:LiteralUnlimitedNatural"),
                    new XAttribute(xmiNs + "id", $"{op.Id}_ret_upper"),
                    new XAttribute("value", upper)
                ));
            }

            opElem.Add(retElem);
        }

        return opElem;
    }

    private XElement SerializeAssociation(UmlAssociation assoc, XNamespace xmiNs)
    {
        var endSourceId = $"{assoc.Id}_end_source";
        var targetEndRef = !string.IsNullOrEmpty(assoc.PropertyId) ? assoc.PropertyId : $"{assoc.Id}_end_target";

        var assocElem = new XElement("packagedElement",
            new XAttribute(xmiNs + "type", "uml:Association"),
            new XAttribute(xmiNs + "id", assoc.Id),
            new XAttribute("memberEnd", $"{targetEndRef} {endSourceId}")
        );

        if (string.IsNullOrEmpty(assoc.PropertyId))
        {
            var targetEnd = new XElement("ownedEnd",
                new XAttribute(xmiNs + "type", "uml:Property"),
                new XAttribute(xmiNs + "id", targetEndRef),
                new XAttribute("type", assoc.TargetId),
                new XAttribute("association", assoc.Id)
            );

            if (!string.IsNullOrEmpty(assoc.PropertyName))
            {
                targetEnd.Add(new XAttribute("name", assoc.PropertyName));
            }

            if (assoc.LowerValue != 1 || assoc.UpperValue != "1")
            {
                targetEnd.Add(new XElement("lowerValue",
                    new XAttribute(xmiNs + "type", "uml:LiteralInteger"),
                    new XAttribute(xmiNs + "id", $"{targetEndRef}_lower"),
                    new XAttribute("value", assoc.LowerValue)
                ));

                targetEnd.Add(new XElement("upperValue",
                    new XAttribute(xmiNs + "type", "uml:LiteralUnlimitedNatural"),
                    new XAttribute(xmiNs + "id", $"{targetEndRef}_upper"),
                    new XAttribute("value", assoc.UpperValue)
                ));
            }
            assocElem.Add(targetEnd);
        }

        // Source end (unnavigable end; name omitted to prevent cluttering diagram)
        var sourceEnd = new XElement("ownedEnd",
            new XAttribute(xmiNs + "type", "uml:Property"),
            new XAttribute(xmiNs + "id", endSourceId),
            new XAttribute("type", assoc.SourceId),
            new XAttribute("association", assoc.Id)
        );

        assocElem.Add(sourceEnd);

        return assocElem;
    }

    private XElement SerializeDependency(UmlDependency dep, XNamespace xmiNs)
    {
        var depElem = new XElement("packagedElement",
            new XAttribute(xmiNs + "type", "uml:Dependency"),
            new XAttribute(xmiNs + "id", dep.Id),
            new XAttribute("client", dep.ClientId),
            new XAttribute("supplier", dep.SupplierId)
        );

        if (!string.IsNullOrEmpty(dep.Name))
        {
            depElem.Add(new XAttribute("name", dep.Name));
        }

        return depElem;
    }

    private static string FormatVisibility(UmlVisibility visibility)
    {
        return visibility switch
        {
            UmlVisibility.Public => "public",
            UmlVisibility.Protected => "protected",
            UmlVisibility.Private => "private",
            UmlVisibility.Package => "package",
            _ => "public"
        };
    }
}
