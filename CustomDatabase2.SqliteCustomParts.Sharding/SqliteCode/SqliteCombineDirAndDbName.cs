// // Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// // Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.Data.Sqlite;

namespace CustomDatabase2.CustomParts.Sharding.SqliteCode;

/// <summary>
/// The Sqlite database is different from many other database providers that the database name
/// contains a filePath. When using AuthP sharding you need a way to separate the directory from
/// the database name. This service provides a simple way to combine the dynamic directory part
/// and the known database name and extension.
/// </summary>
public class SqliteCombineDirAndDbName : ISqliteCombineDirAndDbName
{
    public const string SqliteDirectory = "{AppDir}";

    private readonly string _directoryForDbs;
    private readonly string _dbExtension;

    /// <summary>
    /// You must register this with the directory where the Sqlite databases should be found 
    /// </summary>
    /// <param name="directoryForDbs">Provide the path to the directory where the Sqlite dbs should be found.</param>
    /// <param name="dbExtension">OPTIONAL: if provided, then the database filename will be set to this value.
    /// Otherwise it will be set to "sqlite".</param>
    public SqliteCombineDirAndDbName(string directoryForDbs, string dbExtension = "sqlite")
    {
        _directoryForDbs = directoryForDbs ?? throw new ArgumentNullException(nameof(directoryForDbs));
        if (_directoryForDbs.EndsWith('\\'))
            _directoryForDbs = _directoryForDbs.Substring(0, _directoryForDbs.Length - 1);
        _dbExtension = dbExtension;
    }

    /// <summary>
    /// This builds the Sqlite connection string with the option to add the directory
    /// where the database is stored. If you add the "{AppDir}" string before your database
    /// name, then the "{AppDir}" part will be replaced by the <see cref="_directoryForDbs"/>
    /// which is defined when this service is registered 
    /// </summary>
    /// <param name="baseConnectionString">The Sqlite connection string from the appsettings.json file</param>
    /// <param name="databaseName">Sets the database extension if no extension on database in the connections string.
    /// NOTE: if not set then it uses ".sqlite" as the database extension.</param>
    /// <returns>A Sqlite connection string with the data source set to the directory and database filename.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public string AddDirectoryToConnection(string? baseConnectionString,
        string? databaseName = null)
    {
        if (baseConnectionString == null)
            throw new InvalidOperationException("Connection string not found.");

        var builder = new SqliteConnectionStringBuilder(baseConnectionString);
        var localDataSource = builder.DataSource;

        if (databaseName != null)
        {
            //We need to create the whole DataSource, including adding the directory
            databaseName = Path.GetExtension(databaseName).Length == 0
                ? databaseName + $".{_dbExtension}"
                : databaseName;
            builder.DataSource = localDataSource.Contains(SqliteDirectory)
                ? $"{_directoryForDbs}\\{databaseName}"
                : databaseName;
        }
        else
            //Simply replace the "{AppDir}" part to the directory. Assumes the connection string has \\ after the {AppDir}
            builder.DataSource = localDataSource.Replace(SqliteDirectory, _directoryForDbs);

        return builder.ConnectionString;
    }
}