# Visual 8 Plugin

## Introduction
Allows you to utilize the Infor VISUAL .Net objects to perform transactions within VISUAL 7.1 and above.  

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the Visual8.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines each of the .Net data sources you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the connection by connectionName and will never know the actual .Net registration that is pointing to.  The entries defined in here are in JSON format separated by commas.
* The 'visualClientPath' section (which starts with <setting name="visualClientPath" serializeAs="String"> and ends with </setting>) defines each of the folder location which includes the Infor Visual .Net client files.

## Methods
### ExecuteBOMethod
This method executes methods in the Infor Visual .Net API

Required Arguments:
* Connection - The name of the connection as defined in the 'connections' section of the plugin config file.
* BOName - The full name of the namespace for the target object. For instance customer orders are 'Lsa.Vmfg.Sales.CustomerOrder'
* Parameters - This is an array of parameters to pass for the methods you want to call. You can chain multiple methods together in a single call. The key defines the method and the value is a JSON object of the values to pass into the method call.

## Usage
You can load the Infor Visual .Net Objects into Visual Studio to see what methods are available. If you do not have Visual Studio available you can also use ILSpy to view available methods to call. Infor also provides samples with the .Net library.

The first method Load either creates a blank dataset if you pass no value to it, or you can pass a value to load an existing value. MergeDataSet updates the in transaction dataset with the values passed into the value parameter.

*Creating a customer order*
```
    // Now submit it to BRDB
    bezl.dataService.add('createOrder','brdb','Visual8','ExecuteBOMethod',
    { "Connection": "MyVE8Conn", "BOName": "Lsa.Vmfg.Sales.CustomerOrder",
    "Parameters": 
        [
        { "Key": "Load", "Value": JSON.stringify({customerID: ""}) },
        { "Key": "NewOrderRow", "Value": JSON.stringify({orderID: "<1>"}) },
        { "Key": "MergeDataSet", "Value": JSON.stringify({CUSTOMER_ORDER: [{CUSTOMER_ID: "MYCUSTID", SITE_ID: "MYSITE"}] }) },
        { "Key": "Save", "Value": JSON.stringify({}) }
        ] },0);

```