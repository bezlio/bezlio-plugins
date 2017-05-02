# SMTP Plugin

## Introduction
This plugin allows you to send an e-mail via your SMTP server through Bezlio.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins. 

## Configuration
In order to configure this plugin you will need to edit the SMTP.dll.config file in the plugins directory with the text editor of your choice.  Within this file, revise according to these guidelines:
* The 'fromAddresses' section (which starts with <setting name="fromAddresses" serializeAs="String"> and ends with </setting>) defines the various from addresses you may wish to utilize.  Each may have entirely different SMTP server information.  The entries defined in here are in JSON format separated by commas.

Properties:
* FromAddress - The from address as defined in the 'fromAddresses' section of the plugin config file.
* DisplayName - The display name that will be provided for the from address.
* SmtpServer - The SMTP server address.  Either within your network or outside.
* SmtpPort - The SMTP port number.  Typically 25 for non-SSL and 587 for SSL.
* SmtpUser - The user name for SMTP server authentication.  This is typically the same as the FromAddress.
* SmtpPassword - The password to use for SMTP server authentication.
* UseSSL - Indicates if the server requires SSL.

## Methods
### SendEmail
This method will send an e-mail to the specified recipients.

Required Arguments:
* From - The from address as defined in the 'fromAddresses' section of the plugin config file.
* To - The email address to send to.  If multiple recipients are desired, semi-colon separate.
* Cc - The email address to send to as CC.  If multiple recipients are desired, semi-colon separate.
* Bcc - The email address to send to as BCC.  If multiple recipients are desired, semi-colon separate.
* Subject - The e-mail subject.
* Body - The e-mail body, either in plain text or HTML.
* BodyIsHTML - Indicates whether the text provided for Body should be interpreted as HTML.  Values here can be 'Yes' or 'No'.

## Usage

```
bezl.dataService.add(
  'EmailTest'
  ,'brdb'
  ,'SMTP'
  ,'SendEmail'
  , { 
      "From"        : "yourUser@YourDomain.com",
       "To"         : "someRecipient@someDomain.com",
       "Subject"    : "This is a test e-mail",
       "Body"       : "Body can include <b>HTML</b>",
       "BodyIsHTML" : "Yes"
    }
  , 0);
```
