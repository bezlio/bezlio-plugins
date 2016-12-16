# RSS Plugin

## Introduction
Allows you to take any RSS feed and return it to a Bezl as data.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
Not Applicable

## Methods
### GetFeed
Gets the data from a given RSS feed.

Required Arguments:
* Url - The URL of the RSS feed.

## Usage
With this plugin you can take a given RSS feed (here using the SlickDeals Front Page):

```
bezl.dataService.add(
    'MyFeed'
    ,'brdb'
    ,'RSS'
    ,'GetFeed',
        { "Url": "https://slickdeals.net/newsearch.php?mode=frontpage&searcharea=deals&searchin=first&rss=1" }
    , 0);
```

And then utilize that data within a Bezl.  For this particular feed example the text of the deal is stored in Title.Text and the URL is the Id.  The way this would have been determined is to simply output the data into a 'raw data view' in a Bezl:

```
<pre>{{bezl.data.MyFeed | json }}</pre>
```

Then when the important fields are noted you can make it look nice using HTML:

```
<ul>
  <li *ngFor="let deal of bezl.data.SlickDeals">
    <a href="{{deal.Id}}" target="_blank">{{deal.Title.Text}}</a>
  </li>
</ul>
```