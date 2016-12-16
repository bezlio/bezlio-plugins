# Salesforce Plugin

## Introduction
Allows you to connect to an instance of Salesforce and interact with the data using simple query files.  As with the SQL Server and ODBC plugins, the administrator defines these permitted queries as .SQL files on the file system to to restrict exactly what users can do here.  This plugin also allows for the creations of objects in Salesforce.  This is particularly useful if you are using Bezlio to mash-up data and you might, for example, which to create an opportunity in Salesforce from data housed in another system or database.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the Salesforce.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'sqlFileLocations' section (which starts with <setting name="sqlFileLocations" serializeAs="String"> and ends with </setting>) defines directories of .SQL files defining the queries you wish to permit your Bezlio users to run.  When they chose to interact with this plugin, they are going to simply call the location by locationName.  The entries defined in here are in JSON format separated by commas.
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the Salesforce data sources you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the actual Salesforce API details that is pointing to.  The entries defined in here are in JSON format separated by commas.

## Methods
### ExecuteQuery
This method runs a query and returns back to the Bezl a table of data.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* QueryName - The name of the query to run (without the .sql extension) from within the SQL file location.
* Parameters - If you write your .SQL file with parameters (text enclosed in {}), this key / value pair listing will do a find and replace of those parameters before running the query.

### CreateObject
Creates an object in Salesforce of the given type with the properties as specified in the parameters argument.

Required Arguments:
* Context - The name of the SQL file location as defined in the 'sqlFileLocations' section of the plugin config file.
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* ObjectType - The type of object within Salesforce this is to create (i.e. Account).
* Parameters - A key / value pair array for each of the properties to be associated to this new object being created.

## Usage
After the plugin is configured for queries, you are going to first need to create .SQL files that define the permitted queries for Salesforce.  For example:

*Return details from a Salesforce account having a UD field of CustID that matches a parameter being input from a Bezl*
```
SELECT 
    Id
    , Name
    , CustID__c
    , CustNum__c
    , BillingStreet
    , BillingCity
    , BillingState
    , BillingPostalCode
    , BillingCountry
    , ParentId
    , Type
FROM 
    Account 
WHERE 
    CustID__c = '{CustId}'
```

Then you can pull that data into your Bezl as follows:

```
bezl.dataService.add(
  'BudgetData'
  ,'brdb'
  ,'Salesforce'
  ,'ExecuteQuery'
  , 
    {
    "Context": "Friendly Folder Name"
    , "Connection": "Friendly Connection Name"
    , "QueryName": "Your Query Name"
    , "Parameters": [
        { "Key": "CustId", "Value": "Your Customer ID" }
    ]
    }
  , 0);
```

If using the CreateObject method no .SQL files are needed.  Here is an example creating an account in Salesforce:

```
var newCust = [];
newCust.push({"Key": "Name", "Value": "Customer Name"});
newCust.push({"Key": "Type", "Value": "Customer"});

bezl.dataService.add('sfCreate'
                    ,'brdb'
                    ,'Salesforce'
                    ,'CreateObject'
                    , 
                        { 
                            "Context": "Friendly Folder Name"
                            , "Connection": "Friendly Connection Name"
                            , "ObjectType": "Account"
                            , "Parameters": newCust 
                        }
                    ,0);
```