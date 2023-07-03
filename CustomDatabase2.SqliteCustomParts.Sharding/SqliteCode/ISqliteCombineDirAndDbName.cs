// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace CustomDatabase2.CustomParts.Sharding.SqliteCode;

public interface ISqliteCombineDirAndDbName
{
    /// <summary>
    /// This builds the Sqlite connection string with the option to add the directory
    /// where the database is stored. If you add the "{AppDir}" string before your database
    /// name, then the "{AppDir}" part will be replaced by the <see cref="SqliteCombineDirAndDbName._directoryForDbs"/>
    /// which is defined when this service is registered 
    /// </summary>
    /// <param name="baseConnectionString">The Sqlite connection string from the appsettings.json file</param>
    /// <param name="databaseName">Sets the database extension if no extension on database in the connections string.
    /// NOTE: if not set then it uses ".sqlite" as the database extension.</param>
    /// <returns>A Sqlite connection string with the data source set to the directory and database filename.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    string AddDirectoryToConnection(string? baseConnectionString,
        string? databaseName = null);
}