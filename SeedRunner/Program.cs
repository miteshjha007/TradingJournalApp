using Npgsql;
var connStr = "User ID=postgres;Password=Mitesh123#;Host=localhost;Port=5432;Database=TradingJournalDb;";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine("Connected!");
var sql = File.ReadAllText(@"d:\Mitesh\StockMarket\TradingJournalApp\instrument_seed.sql");
await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("Seed completed!");
