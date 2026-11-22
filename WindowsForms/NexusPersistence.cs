using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace NexusWinForms;

internal sealed record SavedGame(Guid Id, string Name, string Explorer, string Reality, int Energy, int MissionProgress, DateTime UpdatedAt);

internal sealed class NexusPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly string connectionString;

    internal NexusPersistence()
    {
        connectionString = Environment.GetEnvironmentVariable("NEXUS_SQL_CONNECTION_STRING")
            ?? "Server=localhost\\SQLEXPRESS;Database=NEXUS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
    }

    internal async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync(cancellationToken);
            await using var create = new SqlCommand("IF DB_ID(N'NEXUS') IS NULL CREATE DATABASE [NEXUS];", master);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        string schemaPath = Path.Combine(AppContext.BaseDirectory, "database", "002-schema.sql");
        string schema = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(schema, connection) { CommandTimeout = 60 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<Guid> SaveAsync(string saveName, GameSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int explorerId = await EnsureExplorerAsync(connection, transaction, snapshot, cancellationToken);
            int realityId = await EnsureRealityAsync(connection, transaction, snapshot.Reality, cancellationToken);
            int missionId = await EnsureMissionAsync(connection, transaction, snapshot.MissionName, cancellationToken);
            Guid saveId = await FindOrCreateSaveAsync(connection, transaction, saveName, explorerId, realityId, missionId, cancellationToken);
            string json = JsonSerializer.Serialize(snapshot, JsonOptions);

            await using (var update = new SqlCommand(@"
UPDATE dbo.Expedition SET RealityId=@RealityId, MissionId=@MissionId, Status=@Status,
 Energy=@Energy, ConnectionValue=@Connection, Danger=@Danger, PositionRow=@Row,
 PositionColumn=@Column, SnapshotJson=@SnapshotJson, UpdatedAt=SYSUTCDATETIME()
WHERE ExpeditionId=@Id;", connection, transaction))
            {
                update.Parameters.AddWithValue("@RealityId", realityId);
                update.Parameters.AddWithValue("@MissionId", missionId);
                update.Parameters.AddWithValue("@Status", snapshot.MissionComplete ? "Completed" : snapshot.IsConnected ? "Active" : "Paused");
                update.Parameters.AddWithValue("@Energy", snapshot.Energy);
                update.Parameters.AddWithValue("@Connection", snapshot.Connection);
                update.Parameters.AddWithValue("@Danger", snapshot.Danger);
                update.Parameters.AddWithValue("@Row", snapshot.PositionRow);
                update.Parameters.AddWithValue("@Column", snapshot.PositionColumn);
                update.Parameters.AddWithValue("@SnapshotJson", json);
                update.Parameters.AddWithValue("@Id", saveId);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            int expeditionMissionId = await EnsureExpeditionMissionAsync(connection, transaction, saveId, missionId, snapshot, cancellationToken);

            await using (var deleteChildren = new SqlCommand("DELETE FROM dbo.InventoryItem WHERE ExpeditionId=@Id; DELETE FROM dbo.GameEvent WHERE ExpeditionId=@Id; DELETE FROM dbo.ObjectiveProgress WHERE ExpeditionMissionId=@MissionInstanceId;", connection, transaction))
            {
                deleteChildren.Parameters.AddWithValue("@Id", saveId);
                deleteChildren.Parameters.AddWithValue("@MissionInstanceId", expeditionMissionId);
                await deleteChildren.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var item in snapshot.Inventory ?? new())
            {
                await using var command = new SqlCommand(@"
DECLARE @itemId int;
SELECT @itemId=ItemId FROM dbo.ItemDefinition WHERE Name=@Name AND TypeName=@Type;
IF @itemId IS NULL BEGIN INSERT dbo.ItemDefinition(Name,TypeName) VALUES(@Name,@Type); SET @itemId=CONVERT(int,SCOPE_IDENTITY()); END;
INSERT dbo.InventoryItem(ExpeditionId,ItemId,Quantity) VALUES(@ExpeditionId,@itemId,@Quantity);", connection, transaction);
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Type", item.Type);
                command.Parameters.AddWithValue("@ExpeditionId", saveId);
                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            bool[] objectives = [snapshot.AnomalyFound, snapshot.CoreFound, snapshot.EntityDefeated, snapshot.ExtractionComplete];
            for (int i = 0; i < objectives.Length; i++)
            {
                await using var progress = new SqlCommand(@"INSERT dbo.ObjectiveProgress(ExpeditionMissionId,ObjectiveId,Completed,CompletedAt)
SELECT @ExpeditionMissionId,ObjectiveId,@Completed,CASE WHEN @Completed=1 THEN SYSUTCDATETIME() ELSE NULL END
FROM dbo.MissionObjective WHERE MissionId=@MissionId AND ObjectiveOrder=@Order;", connection, transaction);
                progress.Parameters.AddWithValue("@ExpeditionMissionId", expeditionMissionId);
                progress.Parameters.AddWithValue("@MissionId", missionId);
                progress.Parameters.AddWithValue("@Order", i + 1);
                progress.Parameters.AddWithValue("@Completed", objectives[i]);
                await progress.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var gameEvent in snapshot.Events ?? new())
            {
                await using var eventCommand = new SqlCommand("INSERT dbo.GameEvent(ExpeditionId,EventType,Description,OccurredAt) VALUES(@ExpeditionId,@Type,@Description,@OccurredAt);", connection, transaction);
                eventCommand.Parameters.AddWithValue("@ExpeditionId", saveId);
                eventCommand.Parameters.AddWithValue("@Type", gameEvent.Type);
                eventCommand.Parameters.AddWithValue("@Description", gameEvent.Description);
                eventCommand.Parameters.AddWithValue("@OccurredAt", gameEvent.OccurredAt);
                await eventCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return saveId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    internal async Task<List<SavedGame>> ListAsync(string explorerName = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(@"
SELECT e.ExpeditionId,e.SaveName,p.Name,r.Code,e.Energy,
 (SELECT COUNT(*) FROM dbo.ObjectiveProgress op JOIN dbo.ExpeditionMission em ON em.ExpeditionMissionId=op.ExpeditionMissionId WHERE em.ExpeditionId=e.ExpeditionId AND op.Completed=1),e.UpdatedAt
FROM dbo.Expedition e JOIN dbo.ExplorerProfile p ON p.ExplorerId=e.ExplorerId JOIN dbo.Reality r ON r.RealityId=e.RealityId
WHERE (@Explorer IS NULL OR p.Name=@Explorer) ORDER BY e.UpdatedAt DESC;", connection);
        command.Parameters.AddWithValue("@Explorer", (object)explorerName ?? DBNull.Value);
        var games = new List<SavedGame>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            games.Add(new SavedGame(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetDateTime(6)));
        return games;
    }

    internal async Task<GameSnapshot> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT SnapshotJson FROM dbo.Expedition WHERE ExpeditionId=@Id;", connection);
        command.Parameters.AddWithValue("@Id", id);
        string json = (string)await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("No se encontró la partida seleccionada.");
        return JsonSerializer.Deserialize<GameSnapshot>(json, JsonOptions) ?? throw new InvalidDataException("La partida guardada no tiene un formato válido.");
    }

    internal async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("DELETE dbo.Expedition WHERE ExpeditionId=@Id;", connection);
        command.Parameters.AddWithValue("@Id", id);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new InvalidOperationException("La partida ya no existe.");
    }

    private static async Task<int> EnsureExplorerAsync(SqlConnection c, SqlTransaction t, GameSnapshot s, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"
IF NOT EXISTS(SELECT 1 FROM dbo.ExplorerProfile WHERE Name=@Name)
 INSERT dbo.ExplorerProfile(Name,Gender,Age,Fitness,CharacterId) VALUES(@Name,@Gender,@Age,@Fitness,@CharacterId);
SELECT ExplorerId FROM dbo.ExplorerProfile WHERE Name=@Name;", c, t);
        cmd.Parameters.AddWithValue("@Name", s.ExplorerName); cmd.Parameters.AddWithValue("@Gender", s.Gender);
        cmd.Parameters.AddWithValue("@Age", s.Age); cmd.Parameters.AddWithValue("@Fitness", s.Fitness); cmd.Parameters.AddWithValue("@CharacterId", s.CharacterId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task<int> EnsureRealityAsync(SqlConnection c, SqlTransaction t, string code, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.Reality WHERE Code=@Code) INSERT dbo.Reality(Code,Name) VALUES(@Code,@Code); SELECT RealityId FROM dbo.Reality WHERE Code=@Code;", c, t);
        cmd.Parameters.AddWithValue("@Code", code); return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task<int> EnsureMissionAsync(SqlConnection c, SqlTransaction t, string name, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.MissionDefinition WHERE Name=@Name) INSERT dbo.MissionDefinition(Name) VALUES(@Name); SELECT MissionId FROM dbo.MissionDefinition WHERE Name=@Name;", c, t);
        cmd.Parameters.AddWithValue("@Name", name); int missionId = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        string[] descriptions = ["Localizar la anomalía", "Recuperar el núcleo", "Superar la entidad", "Completar la extracción"];
        for (int order = 1; order <= descriptions.Length; order++)
        {
            await using var objective = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM dbo.MissionObjective WHERE MissionId=@MissionId AND ObjectiveOrder=@Order) INSERT dbo.MissionObjective(MissionId,ObjectiveOrder,Description) VALUES(@MissionId,@Order,@Description);", c, t);
            objective.Parameters.AddWithValue("@MissionId", missionId); objective.Parameters.AddWithValue("@Order", order); objective.Parameters.AddWithValue("@Description", descriptions[order - 1]);
            await objective.ExecuteNonQueryAsync(ct);
        }
        return missionId;
    }

    private static async Task<int> EnsureExpeditionMissionAsync(SqlConnection c, SqlTransaction t, Guid expeditionId, int missionId, GameSnapshot snapshot, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"
IF NOT EXISTS(SELECT 1 FROM dbo.ExpeditionMission WHERE ExpeditionId=@ExpeditionId AND MissionId=@MissionId)
 INSERT dbo.ExpeditionMission(ExpeditionId,MissionId,Status,Experience) VALUES(@ExpeditionId,@MissionId,'Active',0);
UPDATE dbo.ExpeditionMission SET Status=@Status,Experience=@Experience WHERE ExpeditionId=@ExpeditionId AND MissionId=@MissionId;
SELECT ExpeditionMissionId FROM dbo.ExpeditionMission WHERE ExpeditionId=@ExpeditionId AND MissionId=@MissionId;", c, t);
        cmd.Parameters.AddWithValue("@ExpeditionId", expeditionId); cmd.Parameters.AddWithValue("@MissionId", missionId);
        cmd.Parameters.AddWithValue("@Status", snapshot.MissionComplete ? "Completed" : "Active"); cmd.Parameters.AddWithValue("@Experience", snapshot.Experience);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task<Guid> FindOrCreateSaveAsync(SqlConnection c, SqlTransaction t, string name, int explorerId, int realityId, int missionId, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"
DECLARE @id uniqueidentifier;
SELECT @id=ExpeditionId FROM dbo.Expedition WHERE ExplorerId=@ExplorerId AND SaveName=@Name;
IF @id IS NULL BEGIN SET @id=NEWID(); INSERT dbo.Expedition(ExpeditionId,ExplorerId,SaveName,RealityId,MissionId,Status,Energy,ConnectionValue,Danger,PositionRow,PositionColumn,SnapshotJson) VALUES(@id,@ExplorerId,@Name,@RealityId,@MissionId,'Active',0,100,0,-1,-1,'{}'); END;
SELECT @id;", c, t);
        cmd.Parameters.AddWithValue("@ExplorerId", explorerId); cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@RealityId", realityId); cmd.Parameters.AddWithValue("@MissionId", missionId);
        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }
}
