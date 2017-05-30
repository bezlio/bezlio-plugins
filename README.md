# Bezlio Plugins
This repository holds each of the open source plugins for Bezlio.  Many of these plugins are included in the installer for Bezlio Remote Data Broker, but you will always find the latest versions here along with specialty plugins / branches that extend beyond what is in the installer.

## Contents

* [Crystal Reports](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/CrystalReports/)

Allows you to view any of your Crystal Reports within Bezlio as PDFs.  It supports prompting for any parameters you may have defined on a report and serves up content that has been freshly refreshed against your database.

* [Dummy](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Dummy/)

This plugin is intended to show you how to create a plugin.  It currently just takes the request data and mirrors it right back as a response.

* [Epicor 10](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Epicor10/)

Allows you to execute any of the Epicor 10 / 10.1 Business Objects, so anything you can do in the full client you can do with this plugin.  All of the Epicor BO files are accessed via .Net reflection, making this plugin patch-level independent and forward-compatible so long as Epicor does not change the naming conventions for DLLs.

* [Epicor 9.05](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Epicor905/)

Allows you to execute any of the Epicor 9.05 (and presumably all version 9) Business Objects, so anything you can do in the full client you can do with this plugin.  All of the Epicor BO files are accessed via .Net reflection, making this plugin patch-level independent and forward-compatible so long as Epicor does not change the naming conventions for DLLs.

* [Excel](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Excel%20Plugin/)

This plugin allows for the direct connection to Excel files without the steps involved in going through ODBC.  The ODBC layer provides for some additional flexibility (i.e. being able to write .SQL files to subselect and summarize data) but this plugin is great for users just getting started with Bezlio.  This plugin also supports the write of data to a new or existing workbook.

* [File System](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/FileSystem/)

Allows for directories within your network to be shared out over Bezlio, allowing users to get file listings and click to download files.  This plugin has a roadmap to also allow for the creation, rename, movement, and deletion of files within these shared directories (given the explicit permission provided by the server administrator).

* [Infor VISUAL 7.0](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Visual70/)

Allows you to utilize the Infor VISUAL COM objects to perform transactions within VISUAL 7.0 and below.  

* [Infor VISUAL 7.1/8](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Visual%208/)

Allows you to utilize the Infor VISUAL .Net objects to perform transactions within Visual 7.1 and above. 

* [Microsoft SQL Server](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/SQLServer/)

Allows you to connect to a Microsoft SQL Server and run queries that both read and write data.  All of the administrator-permitted queries are stored as .SQL files on the file system, which restricts what users can see and do.

* [ODBC](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/ODBC/)

Allows for you to connect to any ODBC data source defined on the BRDB server and permit the use of administrator-defined queries against those data sources.  Since most all databases have ODBC support, this plugin acts as a bit of a wildcard for most things folks will want to connect to.

* [RSS](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/RSS/)

Allows you to take any RSS feed and return it to a Bezl as data.

* [SMTP](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/SMTP%20Plugin)

Used to sent e-mails from within Bezlio.

* [Salesforce](https://github.com/bezlio/bezlio-plugins/tree/master/Plugins/Salesforce/)

Allows you to connect to an instance of Salesforce and interact with the data using simple query files.  As with the SQL Server and ODBC plugins, the administrator defines these permitted queries as .SQL files on the file system to to restrict exactly what users can do here.  This plugin also allows for the creations of objects in Salesforce.  This is particularly useful if you are using Bezlio to mash-up data and you might, for example, which to create an opportunity in Salesforce from data housed in another system or database.

## Compiling
These plugins should already be included with the installer for BRDB, so you typically should not need to compile from source.  However, should you need to do so you will need either Visual Studio 2015 (preferred) or Visual Studio Code.  It may be possible to use older versions of Visual Studio or Visual Studio Community.  This has not yet been tested by the Bezlio team so if you have success please let us know so we can update this documentation.
1. Start by downloading the source using the 'Clone or download' button and selecting 'Download ZIP'.  If you are familiar with Git you may also clone the repository which will allow you to submit updates if you are so inclined.
2. Unzip the file and load it into your development environment.  

### If you are using Visual Studio:
  * Souble-click 'Bezlio Plugins.sln' to load the solution.
  * Go to Build / Configuration Manager and ensure the 'Platform' listed for each project is set to 'x86'.  Close the dialog.
  * Press F6 to build the solution.
  * Locate the plugins in their respective Plugins\{PluginName}\bin\x86\Debug folders.

### If you are using Visual Studio Code, you will need to first install the C# plugin and also MSBuild.  Then:
  * Open the extracted folder in Visual Studio Code.
  * Press Ctl + Shift + B to build all projects.
  * Locate the plugins in their respective Plugins\{PluginName}\bin\x86\Debug folders.

## Installation
The installation steps for each plugin are all the same:
1. Take the compiled .DLL file (and .DLL.config file if present) and drop them into your BRDB Plugins directory.  By default this is at C:\Program Files (x86)\Bezlio Remote Data Broker\Plugins.  If you are updating an existing plugin you will need to stop the Bezlio Remote Data Broker service first.
2. Restart the Bezlio Remote Data Broker service in the Services control panel.
3. Configure the plugin by editing the .DLL.config file as described in each plugins documentation (linked above).