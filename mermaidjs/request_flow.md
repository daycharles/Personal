diff --git a//dev/null b/docs/request_flow.md
index 0000000000000000000000000000000000000000..588f6293a70711d4844a4e6ca746e7005f7d5c56 100644
--- a//dev/null
+++ b/docs/request_flow.md
@@ -0,0 +1,78 @@
+# Request Flow Overview

- +This document describes how a query travels from the UI to the database in the `PSIMSLeads` application.
- +## Flow Summary
  +1. **User Interface** – `Form1` loads settings and calls `FoxTalkClient.ConnectAsync` to open the socket connection.
  +2. **Query Construction** – `PSIMSLeadsQuery` builds XML requests such as `BuildCallPerson` or `BuildCallVehicle`.
  +3. **Network Transfer** – `FoxTalkClient` writes the query frames over TCP to the remote LEADS server.
  +4. **Response Handling** – incoming frames are parsed and processed by `FoxTalkClient` and `PSIMSLeadsQuery`.
  +5. **Persistence** – application data can be saved using `PSIMSLeadsDB` to the `CT_PSIMS` database.
- +## Relevant Code Snippets
- +`csharp
+// Connect to the server and start listening
+await _foxTalkClient.ConnectAsync(settings.SendIPAddress, settings.SendPort);
+await _foxTalkClient.StartCoreListeningThread().ConfigureAwait(true);
+` +_Form1.cs_【F:PSIMSLeads3/PSIMSLeads/Form1.cs†L30-L59】
- +```csharp
  +public async Task ConnectAsync(string serverAddress, int port)
  +{
- \_logger.LogResponse($"Connecting to server {serverAddress}:{port}...");
- await \_tcpClient.ConnectAsync(serverAddress, port);
- \_networkStream = \_tcpClient.GetStream();
- // Start read/write loops...
  +}
  +``` +_FoxTalkClient.cs_【F:PSIMSLeads3/PSIMSLeads/FoxTalkClient.cs†L104-L135】
- +`csharp
+_logger.LogResponse("Sending data message from {model.StateUsername} at workstation {model.WSID}");
+await GetCurrentClient().SendDataMessage(model);
+` +_PSIMSLeadsQuery.cs_ (inside `BuildCallPerson` and other builders)【F:PSIMSLeads3/PSIMSLeads/PSIMSLeadsQuery.cs†L86-L109】
- +```csharp
  +public async Task StoreAppData(int nFromService, string data)
  +{
- using (var db = new PSIMSContext(ConnectionString))
- {
-        var entity = new PSIMSStateData() { EntryData = data };
-        db.PSIMSStateDatas.Add(entity);
-        await db.SaveChangesAsync();
- }
  +}
  +``` +_PSIMSLeadsDB.cs_【F:PSIMSLeads3/PSIMSLeads/PSIMSLeadsDB.cs†L21-L48】
- +Connection strings for the database are defined in _App.config_:
- +`xml
+<add name="PSIMSContext" connectionString="Server=localhost;Database=CT_PSIMS;uid=CT_DBAdmin;password=22seavey;Trusted_Connection=True;" providerName="System.Data.SqlClient" />
+` +_App.config_【F:PSIMSLeads3/PSIMSLeads/App.config†L16-L18】
- +## Mermaid Diagram
- +```mermaid
  +sequenceDiagram
- participant UI as Form1
- participant Core as PSIMSCore
- participant Query as PSIMSLeadsQuery
- participant Client as FoxTalkClient
- participant Remote as LEADS Server
- participant DB as CT_PSIMS
-
- UI->>Client: ConnectAsync()
- UI->>Core: Send query
- Core->>Query: Build XML
- Query->>Client: SendDataMessage
- Client->>Remote: TCP frames
- Remote-->>Client: Response frames
- Client->>Query: Process response
- Query->>DB: StoreAppData()
  +```
-
