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

### ExecuteBAQ
This method allows you to easily run an Epicor BAQ.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* BaqId - The name of the Epicor BAQ.
* Parameters - A key / value pair list of parameters for this BAQ.

### JobAdjustment_LaborAdj
Perform a labor adjustment on a job.  This is a helper method (one of many to come) that simplifies what could have been done using ExecuteBOMethod, but would have taken several steps.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* JobNum - Job Number.
* AssemblySeq - Assembly sequence.
* OprSeq - Operation sequence.
* EmployeeNum - Employee ID
* LaborQty - Labor quantity to adjust into job.
* LaborHrs - Labor hours to adjust into the job.
* Complete - Job complete flag.
* OpComplete - Operation complete flag.

### Labor_ClockIn
This helper method allows you to clock in an array of employees via MES.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* EmployeeNum - An array of employee numbers to clock out.
* Shift - The shift number to clock the array of employees onto.
* Plant - An optional argument exists on this helper method to specify the plant.  If omitted the default plant is used.

### Labor_ClockOut
This helper method allows you to clock an array of LaborHedSeq onto a job within MES.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* LaborHedSeq - An array of LaborHedSeq to clock onto the specified job.
* JobNum - The job number to clock the array of LaborHedSeq onto.
* JobAsm - The job assembly to clock the array of LaborHedSeq onto.
* JobOp - The job operation to clock the array of LaborHedSeq onto.
* Setup - A boolean to specify whether the job to clock onto should be for setup.
* Plant - An optional argument exists on this helper method to specify the plant.  If omitted the default plant is used.

### Labor_EndActivities
This helper method allows you to end activity for an array of LaborDataSet (obtained via GetByID on Labor object).

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* LaborDataSet - A LaborDataSet to end activities on.
* Plant - An optional argument exists on this helper method to specify the plant.  If omitted the default plant is used.

### Labor_StartActivity
This helper method allows you to clock out an array of employees via MES.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* EmployeeNum - An array of employee numbers to clock in.
* Shift - The shift number to clock the array of employees onto.
* Plant - An optional argument exists on this helper method to specify the plant.  If omitted the default plant is used.

### Materials_IssueReturnToJob
This helper method allows you to perform multiple material issues in the same transaction while also dynamically adding materials to jobs.

Required Arguments:
* Connection - The name of the Epicor connection as defined in the 'connections' section of the plugin config file.
* Company - Epicor company ID.
* AddMaterials - true / false to determine if you want materials to be dynamically added to job.
* Transactions - An array of objects using the following structure to represent the desired transactions:

```
{
      JobNum			: 	m.job,
      AssemblySeq		:	m.asm,
      MtlSeq			:	m.mtlSeq,
      TranType			:	'STK-MTL',
      PartNum			:	m.partNum,
      TranQty			:	i.qty,
      UOM				:	i.uom,
      FromWarehouseCode	:	i.whse,
      FromBinNum		:	i.bin,
      ToWarehouseCode	:	i.whse,
      ToBinNum			:	i.bin
}
```

* Plant - An optional argument exists on this helper method to specify the plant.  If omitted the default plant is used.

## Usage
Within Bezlio, this plugin can be used to call any Epicor BO method.  These connections can be made using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Execute a BAQ Named 'MyTestBAQ'*
``` 
bezl.dataService.add(
  'MyBAQResults'
  ,'brdb'
  ,'Epicor905'
  ,'ExecuteBAQ'
  , 
    {"Connection": "Friendly Connection Name"
    , "Company": "Your Epicor Company ID"
    , "BaqId": "MyTestBAQ"
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

## Troubleshooting

*Could not load file or assembly Epicor.Mfg.Core.Session.dll*
If you receive an error message similar to the following in BRDB admin:

```
An exception occurred executing method.  Could not load file or assembly 'File:///G:\Epicor905\Epicor.Mfg.Core.Session.dll' or one of its dependencies.  Operation is not supported.  (Exception from HRESULT: 0x80131515)
``` 

This is due to a file permissions issue.  The BRDB service is by default set to run as 'Local System' which may lack the permissions to get to the noted location.  In order to resolve this error try the following fixes:

1. Open the file properties for C:\Program Files (x86)\Bezlio Remote Data Broker\Bezlio Remote Data Broker.exe and check 'Run this program as an administrator' on the 'Compatibility' tab if the noted path is on a local drive to where BRDB is installed.  Restart the service and try again.

2. If the noted path is on a network drive, change the user under which 'Bezlio Remote Data Broker' runs to an Active Directory user having permissions to the share location.