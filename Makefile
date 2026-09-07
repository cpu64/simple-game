MCS = mcs
MONO = mono

COMMON = Server.cs World.cs Player.cs InputState.cs InputCommand.cs GameConstants.cs SharedInputState.cs WorldSnapshot.cs

CLIENT_SOURCES = Client.cs GameWindow.cs $(COMMON)
SERVER_SOURCES = ServerMain.cs $(COMMON)

CLIENT_REFS = -r:System.Drawing -r:System.Windows.Forms

.PHONY: all client server run-client run-server clean

all: client server

client: Client.exe

Client.exe: $(CLIENT_SOURCES)
	$(MCS) $(CLIENT_REFS) -out:$@ $(CLIENT_SOURCES)

server: Server.exe

Server.exe: $(SERVER_SOURCES)
	$(MCS) -out:$@ $(SERVER_SOURCES)

run-client: client
	$(MONO) Client.exe

run-server: server
	$(MONO) Server.exe

clean:
	rm -f Client.exe Server.exe
