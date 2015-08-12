<h1>Script Migrations</h1>
Manage your database changes reliably similar to Ruby on Rails' Active Record Migrations, instead using scripts written in the target database's language.

Written in .NET and (currently only) supporting Microsoft Sql Server

This concept has two major concepts: Runners and Scripts

Scripts are the database changes you would like to apply, and runners are the interface between the application and the database.

<h2>Building</h2>
Browse to the `.\Builds` folder in PowerShell, execute the following command:

	.\build-default.ps1

<h2>Scripts</h2>
Scripts are to be named with the format `YYYYMMDDHHMMSS_ScriptName_(up|down).sql`.  The 'UP' migration will perform actions to bring the database forward in time, while 'DOWN' does the inverse.

A sample migration to create a table can be as follows:

**20150120150259_Create-MyTable_up.sql**

    CREATE TABLE [MyTable]
    (
        [ID] INT NOT NULL IDENTITY(1,1),
        [FirstName] NVARCHAR(128) NOT NULL,
        [LastName] NVARCHAR(128) NOT NULL,
        PRIMARY KEY([ID])
    )
    
    INSERT INTO [MyTable]([FirstName], [LastName]) VALUES('Matthew', 'Lymer')
    
**20150120150259_Create-MyTable_down.sql**

    DROP TABLE [MyTable]
    
<h2>Runners</h2>

Currently there is only one runner (SqlServerMigrator), but the application was designed to make implementing new runners trivial.  See the SqlServerMigrator project for an example on how to implement one for a different dbms.

The runner is the interface between the application and the database server of choice.  

Be sure to copy the binaries from the output directory of SqlServerMigrator to the MigratorConsole folder.

<h2>Usage</h2>

There are two main use-cases, migrating a database forward in time (performing an 'UP'), and doing the reverse (performing a 'DOWN')

<h3>Performing an UP</h3>

To move a database forward in time, you must tell the `MigratorConsole.exe` you wish to do so by providing the `/up` flag in addition to providing a `/runner`, a database `/connectionstring`, and the location to the `/scripts`.

    MigratorConsole.exe /up /runner="SqlServerMigrator, SqlServerMigrator.RunnerFactory" /connectionstring="server=localhost;database=mydb;trusted_connection=true" /scripts="C:\dbscripts"
    
The output will show which migrations were run, and how long they took

    Starting migration 20150120150259, 'Create-MyTable'...                    Done! (0.014 s)
    
The database will now have the approperiate change, re-running the same command will no longer execute this UP script.

<h3>Performing a DOWN</h3>

To move a database backward in time, it is very similar to performing an UP, except you must tell `MigratorConsole.exe` how backward in time you want it to go.  The `/version` flag is used to specify how far back to go, if providing a migration number (eg 20150120150259), it will run all DOWN scripts that are a **higher** version than what's provided.  Providing a zero will remove all migrations.  If you provide a migration version that does not exist you will get an error.

    MigratorConsole.exe /down /version=0 /runner="SqlServerMigrator, SqlServerMigrator.RunnerFactory" /connectionstring="server=localhost;database=mydb;trusted_connection=true" /scripts="C:\dbscripts"
