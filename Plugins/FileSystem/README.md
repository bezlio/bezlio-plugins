# File System Plugin

## Introduction
Allows for directories within your network to be shared out over Bezlio, allowing users to get file listings and click to download files.  This plugin has a roadmap to also allow for the creation, rename, movement, and deletion of files within these shared directories (given the explicit permission provided by the server administrator).

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the FileSystem.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'fileSystemLocations' section (which starts with <setting name="fileSystemLocations" serializeAs="String"> and ends with </setting>) defines each of the file system locations you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the location by locationName and will never know or care where the files are actually stored.  The entries defined in here are in JSON format separated by commas.  

## Usage
Using Javascript, here are a few examples:

*Get a listing of XLS files in a directory*
``` 
bezl.dataService.add(
  'MyFiles'
  ,'brdb'
  ,'FileSystem'
  ,'GetFileList'
  , 
    {"Context": "Friendly Folder Name"
    , "FileName": "*.xls"}
  , 0);
```

*Download a file*
```
bezl.dataService.add(
    'FileDownload'
    ,'brdb'
    ,'FileSystem'
    ,'GetFile'
    , 
    { 
        'Context': 'Friendly Folder Name'
        , 'FileName': 'MyFile.xls'
    }
    ,0);
```