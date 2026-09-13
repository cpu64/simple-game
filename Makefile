.PHONY: all client server run-client run-server clean

all: client server

client:
	dotnet build Client/Client.csproj

server:
	dotnet build Server/Server.csproj

run-client:
	dotnet run --project Client/Client.csproj -- $(ARGS)

run-server:
	dotnet run --project Server/Server.csproj -- $(ARGS)

clean:
	dotnet clean Client/Client.csproj
	dotnet clean Server/Server.csproj
