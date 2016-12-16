# Microsoft SQL Server Plugin

## Introduction
Allows you to connect to a Microsoft SQL Server and run queries that both read and write data.  All of the administrator-permitted queries are stored as .SQL files on the file system, which restricts what users can see and do.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the SQLServer.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'sqlFileLocations' section (which starts with <setting name="sqlFileLocations" serializeAs="String"> and ends with </setting>) defines directories of .SQL files defining the queries you wish to permit your Bezlio users to run.  When they chose to interact with this plugin, they are going to simply call the location by locationName.  The entries defined in here are in JSON format separated by commas.
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the ODBC data sources you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the actual ODBC DSN that is pointing to.  The entries defined in here are in JSON format separated by commas.

## Methods
### ExecuteQuery
This method runs a query and returns back to the Bezl a table of data.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* QueryName - The name of the query to run (without the .sql extension) from within the SQL file location.
* Parameters - If you write your .SQL file with parameters (text enclosed in {}), this key / value pair listing will do a find and replace of those parameters before running the query.

### ExecuteNonQuery
This method runs a query that is intended to perform an update or insert into the target database.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* QueryName - The name of the query to run (without the .sql extension) from within the SQL file location.
* Parameters - If you write your .SQL file with parameters (text enclosed in {}), this key / value pair listing will do a find and replace of those parameters before running the query.

## Usage
After the plugin is configured, you are going to first need to create .SQL files that define the permitted queries for the given ODBC data source and store these in your defined sqlFileLocations.  A few examples:

*Selecting all rows from a table named Demo*
```
SELECT * FROM Demo
```

*Selecting specific rows from a table named Demo*
```
SELECT * FROM Demo WHERE MyField = 'XYZ';
```

*Defining an update statement for a table named Demo that allows specific fields to be updated with values that will be passed through from a Bezl*
```
UPDATE Demo SET MyOtherField = '{OtherFieldValue}', YetAnotherField = '{AnotherFieldValue}' WHERE MyField = '{MyFieldValue}'
```

Once these .SQL files have been created, you can now utilize them in a Bezl.  These queries can be utilized using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Loading the data from that Demo example query*
```
bezl.dataService.add(
  'BudgetData'
  ,'brdb'
  ,'SQLServer'
  ,'ExecuteQuery'
  , 
    {"Context": "Friendly Folder Name"
    , "Connection": "Friendly Connection Name"
    , "QueryName": "MyDemoQueryName"}
  , 0);
```

*Performing an update using the Demo update query*
```
bezl.dataService.add(
    'UpdateBudget'
    ,'brdb'
    ,'SQLServerODBC'
    ,'ExecuteNonQuery',
        { "Context": "Friendly Folder Name"
        , "Connection": "Friendly Connection Name"
        , "QueryName": "UpdateDemoQueryName"
        , "Parameters": 
          [
           { "Key": "OtherFieldValue", "Value": "123" },
           { "Key": "AnotherFieldValue", "Value": "xyz" },
           { "Key": "MyFieldValue", "Value": "abc" ) }
          ] }
    , 0);
```