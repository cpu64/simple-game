# UmlToXmiExporter

### Build project
```bash
dotnet build UmlToXmiExporter
```

### Run tests
```bash
dotnet test UmlToXmiExporter.Tests
```

### Export Eclipse UML2
```bash
dotnet run --project UmlToXmiExporter -- . -o model.uml -f eclipse
```

### Export OMG 2.5.1
```bash
dotnet run --project UmlToXmiExporter -- . -o model.xmi -f omg251
```

### Export OMG 2.5
```bash
dotnet run --project UmlToXmiExporter -- . -o model.xmi -f omg25
```
