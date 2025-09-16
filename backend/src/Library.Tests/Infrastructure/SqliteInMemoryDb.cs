using System;
using Library.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests.Infrastructure;

public static class SqliteInMemoryDb
{
    public static (LibraryDbContext Db, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var db = new LibraryDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }

    public static void Dispose(LibraryDbContext db, SqliteConnection connection)
    {
        db.Dispose();
        connection.Dispose();
    }
}


