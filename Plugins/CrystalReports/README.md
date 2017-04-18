# Crystal Reports Plugin

## Introduction
The Crystal Reports plugin allows you to view any of your Crystal Reports within Bezlio as PDFs.  It supports prompting for any parameters you may have defined on a report and serves up content that has been freshly refreshed against your database.  If you wish to implement some sort of 'report instances', look into using Logicity (www.logicitysuite.com) to export your reports with a Save action on a schedule and use the FileSystem plugin to provide users with access.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.  In supplement to this, the Crystal Reports plugin also requires the Crystal Reports runtime to be installed.  The easiest way to accomplish this and have the tool described throughout this documentation is to simply install the free or Pro version of Logicity (www.logicitysuite.com).  If you prefer to install the Crystal Reports runtime directly, it can be installed via this link: https://wiki.scn.sap.com/wiki/display/BOBJ/Crystal+Reports%2C+Developer+for+Visual+Studio+Downloads.  Be sure to download from the MSI 32-bit links.

## Configuration
In order to configure this plugin you will need to edit the CrystalReports.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'rptFileLocations' section (which starts with <setting name="rptFileLocations" serializeAs="String"> and ends with </setting>) defines directories of Crystal Reports files you wish to make available to your users.  The Crystal Reports Viewer app expects an entry to be defined here with a locationName of 'Default' and a locationPath pointing to where your RPT files are stored.  On the locationPath, ensure each backslash is defined as a double backslash (so C:\MyReports\ becomes c:\\MyReports\\) and that you always have a double backslash at the end of the path.  Also, a UNC path is acceptable in this field but if it is on a different server you will need to switch the account 'Bezlio Remote Data Broker' runs as to a service account with appropriate permissions.  The entries defined in here are in JSON format separated by commas.
* The 'connections' section (which starts with <setting name="connections" serializeAs="String"> and ends with </setting>) defines the credentials each of your reports need in order to run.  The best way to know what to put here is to use Logicity (www.logicitysuite.com) to launch the report and observe what it prompts you for (if anything) when you go to run the report.  If the report runs without filling in any info you can just skip this section.  All reports use this collection of connections and will apply all of them needed by a given report.  The entries defined in here are in JSON format separated by commas.

## Methods
### GetReportList
This method returns a listing of all reports in a given FolderName (locationName in config file) with their full details including parameters and connections.  This method is what is used by the Crystal Reports Viewer app to return the listing of reports presented along the left hand side of the Bezl.

Required Arguments:
* FolderName - The name of the RPT file location as defined in the 'rptFileLocations' section of the plugin config file.

### ReturnAsPDF
This method runs a given Crystal Report and returns it as a PDF stream to be rendered in the web browser.

Required Arguments:
* FolderName - The name of the RPT file location as defined in the 'rptFileLocations' section of the plugin config file.
* ReportName - The name of the RPT file to be run.
* Parameters - The parameters to be applied to your report.  Ths is a key / value pair structure where the value portion can be a simple value (for discreate non-range parameters) or a range defined as { StartValue: '', EndValue: '' }.  If multiple values are to be specified, the value can be enclosed with square brackets and values comma seperated.

## Usage
After the plugin is configured, most users will simply install and run the Crystal Reports Viewer app from within Bezlio apps and be all set.  However, if you wish to create a custom Bezl that interacts with Crystal Reports you can do so as follows:

Once the RPT files are in place and the connections are defined, you can now utilize them in a Bezl.  These methods can be utilized using either the wizard-based data connections tool in Bezlio or with Javascript code (which would give you the most flexibility).  We will document a few examples here in Javascript:

*Loading the report listing from a folder we gave the name Demo in our config file*
```
bezl.dataService.add(
  'ReportListing'
  ,'brdb'
  ,'CrystalReports'
  ,'GetReportList'
  , 
    { "FolderName": "Demo" }
  , 0);
```

*Executing a report named MyReport.rpt (no parameters needed) in the Demo folder and returning it as a PDF*
```
bezl.dataService.add(
    'Report'
    ,'brdb'
    ,'CrystalReports'
    ,'ReturnAsPDF',
        { "FolderName": "Demo"
        , "ReportName": "MyReport.rpt"
        , "Parameters": [ ] }
    , 0);
```

When you get back the response, it is going to be sent over as a byte array so you will need to use onDataChange to turn it into an object you can work with.  If you have an iframe in your HTML like this:

```
<iframe id="viewer" src="" type="application/pdf" height=800 style="overflow: auto; width: 100%;">
</iframe>
```

You can dynamically populate that with the response that comes back by adding something to your onDataChange like this:

```
        if (bezl.data.Report) { 
            var sliceSize = 1024;
            var byteCharacters = atob(bezl.data.Report);
            var bytesLength = byteCharacters.length;
            var slicesCount = Math.ceil(bytesLength / sliceSize);
            var byteArrays = new Array(slicesCount);
            for (var sliceIndex = 0; sliceIndex < slicesCount; ++sliceIndex) {
            var begin = sliceIndex * sliceSize;
            var end = Math.min(begin + sliceSize, bytesLength);
            var bytes = new Array(end - begin);
            for (var offset = begin, i = 0 ; offset < end; ++i, ++offset) {
                bytes[i] = byteCharacters[offset].charCodeAt(0);
            }
            byteArrays[sliceIndex] = new Uint8Array(bytes);
            }

            var file = new Blob(byteArrays, {type: 'application/pdf'});     
            var fileURL = URL.createObjectURL(file);
            var viewer = $(bezl.container.nativeElement).find('#viewer')[0];
            viewer.src = fileURL;  
            bezl.vars.reportLoading = false;
        }
```
