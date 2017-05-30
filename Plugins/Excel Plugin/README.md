# Excel Plugin

## Introduction
This plugin allows for the direct connection to Excel files without the steps involved in going through ODBC.  The ODBC layer provides for some additional flexibility (i.e. being able to write .SQL files to subselect and summarize data) but this plugin is great for users just getting started with Bezlio.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
No configuration is required to use this plugin.  Whenever it is installed, users will be able to access any Excel file (XLS or XLSX) within the private network.  Please note that when accessing files not on the BRDB server be sure to utilize UNC paths as opposed to mapped drive letter paths.

## Methods
### GetData
Returns all data from the given sheet.

Required Arguments:
* FileName - The full path to the Excel file.  Unless the file is stored on the same system running BRDB, be sure to use UNC paths here.
* SheetName - The sheet name within the given Excel workbook.
* FirstRowColumnNames - If the first row of your sheet contains column names, select 'Yes' here.  Otherwise select 'No'.

## Usage
This plugin can utilize connections made using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Get all of the data from 'Sheet1' in an Excel file stored at \\server\data\MyExcel.xlsx*
``` 
bezl.dataService.add(
  'MyExcelData'
  ,'brdb'
  ,'ExcelPlugin'
  ,'GetData'
  , 
    {"FileName": "\\server\data\MyExcel.xlsx"
    , "SheetName": "Sheet1"
    , "FirstRowColumnNames": "Yes"}
  , 0);
```