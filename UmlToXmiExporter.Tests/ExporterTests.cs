using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UmlToXmiExporter.Analysis;
using UmlToXmiExporter.Model;
using UmlToXmiExporter.Serialization;
using Xunit;

namespace UmlToXmiExporter.Tests;

public class ExporterTests
{
    [Fact]
    public void TestAnalyzeCodeSnippet_ExtractsTypesAndRelationships()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "uml_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var code = @"
namespace Game.Core
{
    public interface IEntity
    {
        int Id { get; }
        void Update(float delta);
    }

    public abstract class BaseEntity : IEntity
    {
        public int Id { get; set; }
        public abstract void Update(float delta);
    }

    public class Monster : BaseEntity
    {
        public int Health { get; set; }
        public override void Update(float delta) { }
    }

    public interface ISpawner
    {
        void Trigger();
    }

    public class World
    {
        public System.Collections.Generic.List<Monster> Monsters { get; set; } = new();
        public void Spawn(Monster monster) { }
        public void Run(ISpawner spawner) { }
    }
}
";
            File.WriteAllText(Path.Combine(tempDir, "Test.cs"), code);

            var analyzer = new CSharpCodebaseAnalyzer(new AnalyzerOptions { ModelName = "TestModel" });
            var model = analyzer.Analyze(tempDir);

            // Assertions
            Assert.Equal("TestModel", model.Name);
            Assert.Contains(model.AllClassifiersById.Values, c => c.Name == "IEntity" && c is UmlInterface);
            Assert.Contains(model.AllClassifiersById.Values, c => c.Name == "BaseEntity" && c is UmlClass);
            Assert.Contains(model.AllClassifiersById.Values, c => c.Name == "Monster" && c is UmlClass);
            Assert.Contains(model.AllClassifiersById.Values, c => c.Name == "World" && c is UmlClass);

            var baseEntity = model.AllClassifiersById.Values.OfType<UmlClass>().First(c => c.Name == "BaseEntity");
            Assert.Contains(baseEntity.InterfaceRealizations, r => r.SupplierName == "IEntity");

            var monster = model.AllClassifiersById.Values.OfType<UmlClass>().First(c => c.Name == "Monster");
            Assert.Contains(monster.Generalizations, g => g.GeneralName == "BaseEntity");

            var spawner = model.AllClassifiersById.Values.OfType<UmlInterface>().First(c => c.Name == "ISpawner");

            // Association from World to Monster (List<Monster>)
            Assert.Contains(model.Associations, a => a.Name.Contains("World") && a.TargetId == monster.Id && a.IsCollection);

            // Redundant dependency from World to Monster is suppressed because an association exists
            Assert.DoesNotContain(model.Dependencies, d => d.ClientId.Contains("World") && d.SupplierId == monster.Id);

            // Dependency from World to ISpawner (Run method parameter without association)
            Assert.Contains(model.Dependencies, d => d.ClientId.Contains("World") && d.SupplierId == spawner.Id);

            // Serialization test
            var serializer = new XmiSerializer(new SerializationOptions { Dialect = XmiDialect.OmgUml251 });
            var xml = serializer.SerializeToString(model);

            Assert.Contains("xmlns:uml=\"http://www.omg.org/spec/UML/20161101\"", xml);
            Assert.Contains("xmlns:xmi=\"http://www.omg.org/spec/XMI/20131001\"", xml);
            Assert.Contains("xmi:type=\"uml:Class\"", xml);
            Assert.Contains("xmi:type=\"uml:Interface\"", xml);
            Assert.Contains("xmi:type=\"uml:Generalization\"", xml);
            Assert.Contains("xmi:type=\"uml:InterfaceRealization\"", xml);
            Assert.Contains("xmi:type=\"uml:Association\"", xml);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [InlineData(XmiDialect.OmgUml25, "http://www.omg.org/spec/UML/20131001")]
    [InlineData(XmiDialect.OmgUml251, "http://www.omg.org/spec/UML/20161101")]
    [InlineData(XmiDialect.EclipseUml2, "http://www.eclipse.org/uml2/5.0.0/UML")]
    public void TestDialectNamespaces(XmiDialect dialect, string expectedUmlNs)
    {
        var model = new UmlModel { Name = "DialectTest", Id = "model_1" };
        var cls = new UmlClass { Id = "cls_1", Name = "Foo" };
        model.GlobalClassifiers.Add(cls);
        model.AllClassifiersById[cls.Id] = cls;

        var serializer = new XmiSerializer(new SerializationOptions { Dialect = dialect });
        var xml = serializer.SerializeToString(model);

        Assert.Contains($"xmlns:uml=\"{expectedUmlNs}\"", xml);
        Assert.Contains("xmlns:xmi=\"http://www.omg.org/spec/XMI/20131001\"", xml);
    }

    [Fact]
    public void TestReferentialIntegrity_OnSimpleGameCodebase()
    {
        // Analyze the actual simple-game directory
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var analyzer = new CSharpCodebaseAnalyzer(new AnalyzerOptions
        {
            ModelName = "simple-game",
            ExcludePatterns = { "bin", "obj", ".git", "UmlToXmiExporter", "UmlToXmiExporter.Tests" }
        });
        var model = analyzer.Analyze(rootDir);

        var serializer = new XmiSerializer(new SerializationOptions { Dialect = XmiDialect.OmgUml251 });
        var xmiString = serializer.SerializeToString(model);

        var doc = XDocument.Parse(xmiString);
        XNamespace xmiNs = "http://www.omg.org/spec/XMI/20131001";

        // Collect all declared xmi:id
        var allIds = new HashSet<string>();
        foreach (var elem in doc.Descendants())
        {
            var idAttr = elem.Attribute(xmiNs + "id");
            if (idAttr != null)
            {
                Assert.True(allIds.Add(idAttr.Value), $"Duplicate xmi:id found: {idAttr.Value}");
            }
        }

        // Verify referential integrity of references
        foreach (var elem in doc.Descendants())
        {
            // general in Generalization
            var general = elem.Attribute("general");
            if (general != null)
            {
                Assert.True(allIds.Contains(general.Value), $"Broken general reference: {general.Value} in {elem}");
            }

            // client, supplier, contract in InterfaceRealization
            var client = elem.Attribute("client");
            if (client != null)
            {
                Assert.True(allIds.Contains(client.Value), $"Broken client reference: {client.Value} in {elem}");
            }

            var supplier = elem.Attribute("supplier");
            if (supplier != null)
            {
                Assert.True(allIds.Contains(supplier.Value), $"Broken supplier reference: {supplier.Value} in {elem}");
            }

            var contract = elem.Attribute("contract");
            if (contract != null)
            {
                Assert.True(allIds.Contains(contract.Value), $"Broken contract reference: {contract.Value} in {elem}");
            }

            // type in Property and Parameter
            var type = elem.Attribute("type");
            if (type != null)
            {
                Assert.True(allIds.Contains(type.Value), $"Broken type reference: {type.Value} in {elem}");
            }

            // memberEnd in Association
            var memberEnd = elem.Attribute("memberEnd");
            if (memberEnd != null)
            {
                var ends = memberEnd.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var endId in ends)
                {
                    Assert.True(allIds.Contains(endId), $"Broken memberEnd reference: {endId} in {elem}");
                }
            }

            // association in Property (ownedEnd)
            var assoc = elem.Attribute("association");
            if (assoc != null)
            {
                Assert.True(allIds.Contains(assoc.Value), $"Broken association reference: {assoc.Value} in {elem}");
            }
        }

        // Verify key entities from simple-game exist
        Assert.Contains("cls_Player", allIds);
        Assert.Contains("cls_Slime", allIds);
        Assert.Contains("cls_Entity", allIds);
        Assert.Contains("iface_ICollidable", allIds);
        Assert.Contains("iface_IBinarySerializable", allIds);
    }
}
