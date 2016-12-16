# Visual 7.0 Plugin

## Introduction
Allows you to utilize the Infor VISUAL COM objects to perform transactions within VISUAL 7.0 and below.  

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the Visual70.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the COM data sources you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the actual COM registration that is pointing to.  The entries defined in here are in JSON format separated by commas.  COM registrations should be made using the Infor-provided LsaDtaRegister.exe.

## Methods
### ExecuteCOMCall
This method execute a COM call using the Infor COM objects.

Required Arguments:
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* COMObject - The name of the COM objects as it is registered with Windows.  For example, 'VMFGShf.WorkOrder.1'
* Context - The context to use for the given COM object.  For example, 'RUN_LABOR'.
* Method - The method to invoke on the given COM call.  For example, 'Save'.
* Data - A JSON representation of the recordset to be passed into the COM object.

## Usage
As mentioned earlier, the first requirement for usage of this plugin is to use LsaDtaRegister.exe to register a database with the COM objects.  This program is typically in the same folder where you installed VISUAL to.  You should be able to test your connection and verify that the tests come back successfully before proceeding.  At that point you should be set to perform COM transactions:

*Creating a labor ticket*
```
    var rs = [ 
              	{
                  "EMPLOYEE_ID": "Your Employee ID",
                  "DEPARTMENT_ID": "Your Department ID",
                  "TRANSACTION_DATE": new Date(),
                  "BASE_ID": "WORKORDER BASE ID",
                  "LOT_ID": "WORKORDER LOT ID",
                  "SPLIT_ID": 0,
                  "SUB_ID": "WORKORDER SUB ID",
                  "SEQ_NO": "OPERATION SEQUENCE",
                  "RESOURCE_ID": "RESOURCE ID",
                  "GOOD_QTY": 0,
                  "DEVIATED_QTY": 0,
                  "RUN_COMPLETE_NOW": "N",
                  "DESCRIPTION": "WHATEVER DESCRIPTION YOU LIKE",
              	  "HOURS_WORKED": HOURS_WORKED
      			}
            ];
    
    // Now submit it to BRDB
    bezl.dataService.add(
      'SubmitLabor'
      ,'brdb'
      ,'Visual70'
      ,'ExecuteCOMCall'
      , { 
            "Connection": "Visual70"
            , "COMObject": "VMFGShf.WorkOrder.1"
            , "Context": "RUN_LABOR"
        	, "Method": "Save"
			, "Data": JSON.stringify(rs)
        }
        , 0);
```