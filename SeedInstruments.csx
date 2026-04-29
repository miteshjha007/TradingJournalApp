#!/usr/bin/env dotnet-script
// Run with: dotnet script SeedInstruments.csx
// Or just compile as a console app

#r "nuget: Npgsql, 8.0.0"

using Npgsql;

var connStr = "User ID=postgres;Password=Mitesh123#;Host=localhost;Port=5432;Database=TradingJournalDb;";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine("Connected to PostgreSQL!");

var sql = await File.ReadAllTextAsync("instrument_seed.sql");
await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("Seed completed successfully!");
