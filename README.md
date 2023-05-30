# AuthPermissions.CustomDatabaseExamples

This repo contains examples of using a _custom database_ provider when using the [AuthPermissions.AspNetCore](https://github.com/JonPSmith/AuthPermissions.AspNetCore) (shortened to _AuthP_). Go to the the AuthP's [documentation wiki](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki) and look for "custom database" for more information.

Before the 5.0.0 release of the AuthP library you only select the built-in SqlServer or PostgreSQL database providers. With the release of AuthP 5.0.0 you can use the main database providers that EF Core supports

| Supported database providers in V5.0.0   | Comment            |
| ---------------------------------------- | ------------------ |
| Microsoft.EntityFrameworkCore.SqlServer  | Built-in           |
| Npgsql.EntityFrameworkCore.PostgreSQL    | Built-in			|
| Microsoft.EntityFrameworkCore.Sqlite	   | See example in this repo |
| Microsoft.EntityFrameworkCore.Cosmos 	   | 					|
| Pomelo.EntityFrameworkCore.MySql 		   | [Pomelo Foundation Project](https://github.com/PomeloFoundation)					|
| MySql.EntityFrameworkCore				   | [MySQL project](https://dev.mysql.com/)					|
| Oracle.EntityFrameworkCore			   | [Oracle](https://www.oracle.com/database/technologies/appdev/dotnet.html)					|


For normal (non-sharding) applications you can use one type of database, but with [sharding / hybrid applications](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Multi-tenant-explained#2-defining-the-three-multi-tenant-database-arrangements) you can use multiple database providers, e.g. SqlServer for the admin part and CosmosDB for the tenant databases.

## Examples in this repo

At the moment there is one example which cover an normal (i.e. not sharding / hybrid) multi-tenant application using a custom database provider. I'm working on a example of using sharding multi-tenant application using a custom database provider, but at this time I have a bug and not enought time to work on it at this time.

### CustomDatabase1: normal multi-tenant application

The first example is for an normal (i.e. not sharding / hybrid) multi-tenant application. This example has three projects, all starting with `CustomDatabase1.`. This uses the Sqlite database provider, mainly because the ASP.NET Core Individual User Accounts Authentication supports Sqlite too. There are three projects:

| Projects                            | What they contain            |
| ----------------------------------- | ----------------------------- |
| CustomDatabase1.InvoiceCode         | The per-tenant code - an demo invoice app |
| CustomDatabase1.SqliteCustomParts	  | The code to use Sqlite to AuthP |
| CustomDatabase1.WebApp			  | The example WebApp to see how it works | 

_NOTE: Sqlite is different to other database providers as its connection string needs a FilePath to be added to the connection string. I solve this at the start of the ASP.NET Core's [Program](https://github.com/JonPSmith/AuthPermissions.CustomDatabaseExamples/blob/main/CustomDatabase1.WebApp/Program.cs)._

### CustomDatabase2: sharding multi-tenant application

Coming soon.

