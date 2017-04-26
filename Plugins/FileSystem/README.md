# File System Plugin

## Introduction
Allows for directories within your network to be shared out over Bezlio, allowing users to get file listings and click to download files.  This plugin has a roadmap to also allow for the creation, rename, movement, and deletion of files within these shared directories (given the explicit permission provided by the server administrator).

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
In order to configure this plugin you will need to edit the FileSystem.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'fileSystemLocations' section (which starts with <setting name="fileSystemLocations" serializeAs="String"> and ends with </setting>) defines each of the file system locations you wish to expose to Bezlio users.  When they chose to interact with this plugin, they are going to simply call the location by locationName and will never know or care where the files are actually stored.  The entries defined in here are in JSON format separated by commas.  

## Methods
### GetFileList
Gets a file listing of the specified file system location.

Required Arguments:
* Context - The name of the file system location as defined in the 'fileSystemLocations' section of the plugin config file.
* FileName - A search pattern to limit the files returned (for example '*.xls').

Return Values (for each file):
    Attributes 
    CreationTime 
    CreationTimeUtc 
    Directory 
    DirectoryName 
    Exists 
    Extension 
    FullName 
    IsReadOnly 
    LastAccessTime 
    LastAccessTimeUtc
    LastWriteTime
    LastWriteTimeUtc 
    Length 
    Name 
    BaseName 

### GetFile
Retrieve a file from a given specified file system location.  The file is converted into a byte array so that it can be transmitted over the Bezlio communication protocol.

Required Arguments:
* Context - The name of the file system location as defined in the 'fileSystemLocations' section of the plugin config file.
* FileName - Name of the file to retrieve.

### WriteFile
Writes a file provided in the Bytes argument (expected to be in a Base64 format).

Required Arguments:
* Context - The name of the file system location as defined in the 'fileSystemLocations' section of the plugin config file.
* FileName - Name of the file to retrieve.
* Bytes - Bytes of file to write.

## Usage
This plugin can utilize connections made using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

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

*Write a file*
This example would be added to ngOnInit and adds a signature pad to a Bezl.  The bezl.dataService line is where we are invoking this plugin.

```
require(['https://cdnjs.cloudflare.com/ajax/libs/signature_pad/1.5.3/signature_pad.min.js'], function(functions) {
  var signaturePad = new SignaturePad(document.getElementById('signature-pad'), {
    backgroundColor: 'rgba(255, 255, 255, 0)',
    penColor: 'rgb(0, 0, 0)'
  });
  
  var saveButton = document.getElementById('save');
  var cancelButton = document.getElementById('clear');

  saveButton.addEventListener('click', function (event) {
    var dataURI = signaturePad.toDataURL('image/tiff');
    bezl.dataService.add('WriteFile','brdb','FileSystem','WriteFile',
		{ "Context": "MyFiles", "FileName": "blah.tiff", "Bytes": dataURI.replace(/^data:image\/(png|jpg);base64,/, "") },0);
  });

  cancelButton.addEventListener('click', function (event) {
    signaturePad.clear();
  });

});
```