# Epicor 10 Plugin

## Introduction
Allows you to execute any of the Epicor 10 / 10.1 Business Objects, so anything you can do in the full client you can do with this plugin.  All of the Epicor BO files are accessed via .Net reflection, making this plugin patch-level independent and forward-compatible so long as Epicor does not change the naming conventions for DLLs.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the Epicor10.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the connections to Epicor you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the conenction by connectionName and will never know the app server URL, user name, or password.  The entries defined in here are in JSON format separated by commas.  
* The user name and password must be for a valid Epicor user account.
* The 'epicorClientPath' section (which starts with <setting name="epicorClientPath" serializeAs="String"> and ends with </setting>) provides the plugin with a path to where your Epicor client is installed.  This can be either a local path or a UNC path, but if it is the later be aware the Bezlio Remote Data Broker service will need to be run as a user that has permissions to that share.

## Usage
Within Bezlio, this plugin can be used to call any Epicor BO method.  Using Javascript, here are a few examples:

``` Execute a BAQ Named 'MyTestBAQ'
bezl.dataService.add(
  'MyBAQResults'
  ,'brdb'
  ,'Epicor10'
  ,'ExecuteBOMethod'
  , 
    {"Connection": "Friendly Connection Name"
    , "Company": "Your Epicor Company ID"
    , "BOName": "DynamicQuery"
    , "BOMethodName": "ExecuteByID"
    , "Parameters": [{ "Key": "queryID", "Value": "MyTestBAQ" }] }
  , 0);
```

``` Perform a GetByID using the PO Business Object
bezl.dataService.add(
    'PO'
    ,'brdb'
    ,'Epicor10'
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

``` Perform an UpdateExt using the PO Business Object (with the data from the previous GetByID example)
bezl.dataService.add(
  'Update'
  , 'brdb'
  , 'Epicor10'
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

Some of the things you may wish to do in Epicor may involve a number of consecutive BO calls that would be feasible but inconvienient to do within a Bezl.  To simplify these situations we have allocated a 'HelperMethods' subfolder with simplified methods.  For example, performing an adjustment to a job where labor hours or operation completed quantities can be a bit tedious and has been replaced with this helper:

```
      'SubmitLabor_' + i
      ,'brdb'
      ,'Epicor10'
      ,'JobAdjustment_LaborAdj'
      , { 
            "Connection": "Friendly Connection Name"
            , "Company": "Your Epicor Company ID"
            , "JobNum": "Your Job Number"
        	, "AssemblySeq": Your_Assembly_Sequence
        	, "OprSeq": Your_Operation_Sequence
        	, "EmployeeNum": "Your Employee ID"
        	, "LaborQty": Your_Labor_Qty
        	, "LaborHrs": Your_Labor_Hours
        	, "Complete": false
        	, "OpComplete": false
        }
        , 0);
  };
```