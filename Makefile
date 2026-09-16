.PHONY: all client server test run-client run-server run-test clean

all: client server test

client:
	dotnet build Client/Client.csproj

server:
	dotnet build Server/Server.csproj

test:
	dotnet build Test/Test.csproj

run-client:
	dotnet run --project Client/Client.csproj -- $(ARGS)

run-server:
	dotnet run --project Server/Server.csproj -- $(ARGS)

run-test:
	dotnet run --project Test/Test.csproj -- $(ARGS)

clean:
	dotnet clean Client/Client.csproj
	dotnet clean Server/Server.csproj
	dotnet clean Test/Test.csproj
