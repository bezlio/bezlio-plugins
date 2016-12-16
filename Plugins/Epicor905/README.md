# Epicor 9.05 Plugin

## Introduction
Allows you to execute any of the Epicor 9.05 (and presumably all version 9) Business Objects, so anything you can do in the full client you can do with this plugin.  All of the Epicor BO files are accessed via .Net reflection, making this plugin patch-level independent and forward-compatible so long as Epicor does not change the naming conventions for DLLs.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the Epicor905.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the connections to Epicor you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the app server URL, user name, or password.  The entries defined in here are in JSON format separated by commas.  
* The user name and password must be for a valid Epicor user account.
* The 'epicorClientPath' section (which starts with <setting name="epicorClientPath" serializeAs="String"> and ends with </setting>) provides the plugin with a path to where your Epicor client is installed.  This can be either a local path or a UNC path, but if it is the later be aware the Bezlio Remote Data Broker service will need to be run as a user that has permissions to that share.

## Methods
### ExecuteBOMethod
This method can execute any Epicor BO method.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* BOName - The base name of the Epicor business object (for example 'PO').
* BOMethodName - The name of the BO method to execute (for example 'UpdateExt').
* Parameters - A key / value pair list of parameters to pass into this BO method.

## Usage
Within Bezlio, this plugin can be used to call any Epicor BO method.  These connections can be made using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Execute a BAQ Named 'MyTestBAQ'*
``` 
bezl.dataService.add(
  'MyBAQResults'
  ,'brdb'
  ,'Epicor905'
  ,'ExecuteBOMethod'
  , 
    {"Connection": "Friendly Connection Name"
    , "Company": "Your Epicor Company ID"
    , "BOName": "DynamicQuery"
    , "BOMethodName": "ExecuteByID"
    , "Parameters": [{ "Key": "pcQueryID", "Value": "MyTestBAQ" }] }
  , 0);
```

*Perform a GetByID using the PO Business Object*
```
bezl.dataService.add(
    'PO'
    ,'brdb'
    ,'Epicor905'
    ,'ExecuteBOMethod'
    , 
    { 
        'Connection': 'Friendly Connection Name'
        , 'Company': 'Your Epicor Company ID'
        , 'BOName': 'PO'
        , 'BOMethodName': 'GetByID'
        , 'Parameters': [{ 'Key': 'poNum', 'Value': 'Your PO Number' }] 
    }
    ,0);
```

*Perform an UpdateExt using the PO Business Object (with the data from the previous GetByID example)*
``` 
bezl.dataService.add(
  'Update'
  , 'brdb'
  , 'Epicor905'
  , 'ExecuteBOMethod'
  ,
  {
    'Connection': 'Friendly Connection Name'
    , 'Company': 'Your Epicor Company ID'
    , 'BOName': 'PO'
    , 'BOMethodName': 'UpdateExt'
    , 'Parameters': [{ 'Key': 'ds', 'Value': JSON.stringify(bezl.data.PO) }]
  }
  , 0);
```