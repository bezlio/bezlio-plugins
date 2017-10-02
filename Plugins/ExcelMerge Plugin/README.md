# Excel Plugin

## Introduction
This plugin allows for the direct connection to Excel files without the steps involved in going through ODBC.  The ODBC layer provides for some additional flexibility (i.e. being able to write .SQL files to subselect and summarize data) but this plugin is great for users just getting started with Bezlio.  This plugin also supports the write of data to a new or existing workbook.

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

### WriteFile
Writes a dataset to an Excel sheet on a new or existing workbook.

Required Arguments:
* FileName - The full path to the Excel file.  Unless the file is stored on the same system running BRDB, be sure to use UNC paths here.
* SheetName - The sheet name within the given Excel workbook.
* FirstRowColumnNames - If the first row of your sheet contains column names, select 'Yes' here.  Otherwise select 'No'.
* SheetData - JSON string representing a data table to be written to an Excel sheet.

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
    { 
      FileName: "\\\\server\\data\\MyExcel.xlsx"
      , SheetName: "Sheet1"
      , FirstRowColumnNames: "Yes" 
    }
  , 0);
```

*Write the data from MyExcelData to a new workbook on a sheet named Sheet2*
``` 
bezl.dataService.add(
  'WriteTest'
  ,'brdb'
  ,'ExcelPlugin'
  ,'WriteFile'
  , 
    { 
      FileName: "\\\\server\\data\\MyExcelTwo.xlsx"
      , SheetName: "Sheet2"
      , FirstRowColumnNames: "Yes" 
      , SheetData: JSON.stringify(bezl.data.MyExcelData)
    }
  , 0);
```