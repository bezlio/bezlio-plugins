# ODBC Plugin

## Introduction
Allows for you to connect to any ODBC data source defined on the BRDB server and permit the use of administrator-defined queries against those data sources.  Since most all databases have ODBC support, this plugin acts as a bit of a wildcard for most things folks will want to connect to.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the ODBC.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'sqlFileLocations' section (which starts with <setting name="sqlFileLocations" serializeAs="String"> and ends with </setting>) defines directories of .SQL files defining the queries you wish to permit your Bezlio users to run.  When they chose to interact with this plugin, they are going to simply call the location by locationName.  The entries defined in here are in JSON format separated by commas.
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the ODBC data sources you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the actual ODBC DSN that is pointing to.  The entries defined in here are in JSON format separated by commas.

## Methods
### ExecuteQuery
This method runs a query and returns back to the Bezl a table of data.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* DSN - The name of the connection as defined in the 'connections' section of the plugin config file.
* QueryName - The name of the query to run (without the .sql extension) from within the SQL file location.
* Parameters - If you write your .SQL file with parameters (text enclosed in {}), this key / value pair listing will do a find and replace of those parameters before running the query.

### ExecuteNonQuery
This method runs a query that is intended to perform an update or insert into the target database.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* DSN - The name of the connection as defined in the 'connections' section of the plugin config file.
* Connection - If you leave the DSN blank, you can fill in a connection string here for DSN-less connections.
* QueryName - The name of the query to run (without the .sql extension) from within the SQL file location.
* Parameters - If you write your .SQL file with parameters (text enclosed in {}), this key / value pair listing will do a find and replace of those parameters before running the query.

## Usage
After the plugin is configured, you are going to first need to create .SQL files that define the permitted queries for the given ODBC data source and store these in your defined sqlFileLocations.  A few examples:

*Selecting all rows from Sheet1 in an Excel spreadsheet*
```
SELECT * FROM [Sheet1$];
```

*Selecting specific rows from a Yearly Budget By Department tab in an Excel spreadsheet*
```
SELECT * FROM [Yearly Budget By Department$] WHERE [FiscalYear] = 2017;
```

*Defining an update statement for an Excel spreadsheet that allows specific fields to be updated with values that will be passed through from a Bezl*
```
UPDATE [Yearly Budget By Department$] SET [Budget] = '{Budget}', [SubmitterNotes] = '{SubmitterNotes}', [Editable] = '' WHERE [Department] = '{Department}' AND [FiscalYear] = {FiscalYear};
```

Once these .SQL files have been created, you can now utilize them in a Bezl.  These queries can be utilized using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Loading the data from that Yearly Budget By Department Excel example query*
```
bezl.dataService.add(
  'BudgetData'
  ,'brdb'
  ,'ODBC'
  ,'ExecuteQuery'
  , 
    {"Context": "Friendly Folder Name"
    , "DSN": "Friendly Connection Name"
    , "QueryName": "DepartmentBudgetsCurrentYear"}
  , 0);
```

*Performing an update using the Yearly Budget By Department Excel update query*
```
bezl.dataService.add(
    'UpdateBudget'
    ,'brdb'
    ,'ODBC'
    ,'ExecuteNonQuery',
        { "Context": "Friendly Folder Name"
        , "DSN": "Friendly Connection Name"
        , "QueryName": "DepartmentBudgetsUpdate"
        , "Parameters": 
          [
           { "Key": "Department", "Value": "Your Department ID" },
           { "Key": "FiscalYear", "Value": The_Fiscal_Year },
           { "Key": "Budget", "Value": The_Budget) }, 
           { "Key": "SubmitterNotes", "Value": "Any Notes You Want To Submit" } 
          ] }
    , 0);
```