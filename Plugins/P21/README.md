# P21 Plugin

## Introduction
This plugin supports P21 API calls in conjunction with the MindHarbor API (licensed seperately).  The plugin may also support the native P21 API or any REST service that requires token authentication.

## Installation
See generic plugin instructions at https://github.com/bezlio/bezlio-plugins.

## Configuration
This plugin does not require a configuration file.

## Methods
### Execute
This method executes any of the installed MindHarbor API endpoints.  To determine the available endpoints go to http://YOUR_SERVER_NAME:8000/swagger

Required Arguments:
* AuthTokenUrl - The path to where authorization tokens are provided.  Typically http://YOUR_SERVER_NAME:8000/token
* AuthClientId - The client ID to receive the authorization token. Typically 'bezlio_client'. 
* AuthClientSecret - The password provided by MindHarbor for use with the API.
* Method - The REST method for the endpoint being called.  Possible values are DELETE, GET, HEAD, MERGE, OPTIONS, PATCH, POST, PUT 
* DataUrl - The endpoint URL for the call you wish to make. For example, http://YOUR_SERVER_NAME:8000/api/v1/OrderExport/UpdateP21Quote
* DataBody - The JSON input required for this endpoint to operate.

## Usage

*Get Customer Specific Pricing For A List Of Parts / Quantities*
```
bezl.dataService.add(
    'GetPricing'
    ,'brdb'
    ,'P21'
    ,'Execute',
        { AuthTokenUrl: 'http://YOUR_SERVER_NAME:8000/token'
        , AuthClientId: 'bezlio_client'
        , Method: 'POST'
        , DataUrl: 'http://YOUR_SERVER_NAME:8000/api/v2/CustomerPricings/GetCustomerPricings'
        , DataBody: JSON.stringify({
          CustomerId: 12345
          , CompanyId: 'ABC'
          , LocationId: 10
          , ShipToId: 1111
          , Items: [
            {
              InvMastUid: 11111
              , Uom: 'EA'
              , Qty: 500
            },
            {
              InvMastUid: 11112
              , Uom: 'EA'
              , Qty: 6000
            }
          ]
        })
    , 0);
```

*Get Customer / Product Specific Taxes*
```
bezl.dataService.add(
    'GetPricing'
    ,'brdb'
    ,'P21'
    ,'Execute',
        { AuthTokenUrl: 'http://YOUR_SERVER_NAME:8000/token'
        , AuthClientId: 'bezlio_client'
        , Method: 'POST'
        , DataUrl: 'http://YOUR_SERVER_NAME:8000/api/v1/products/ProductsTaxJurisdictionsGroupByShipTo'
        , DataBody: JSON.stringify({
          CustomerId: 12345
          , CompanyId: 'ABC'
          , ShipToId: 1111
          , Items: [
            {
              InvMastUid: 11111
              , LocationId: 10
            },
            {
              InvMastUid: 11112
              , LocationId: 10
            }
          ]
        })
    , 0);
```

*Save A New Quote*
```
bezl.dataService.add(
    'GetPricing'
    ,'brdb'
    ,'P21'
    ,'Execute',
        { AuthTokenUrl: 'http://YOUR_SERVER_NAME:8000/token'
        , AuthClientId: 'bezlio_client'
        , Method: 'POST'
        , DataUrl: 'http://YOUR_SERVER_NAME:8000/api/v1/OrderExport/CreateP21Quote'
        , DataBody: JSON.stringify({
          OeHdr: {
            AddressId: 12345
            , ContactId: 1010101
            , LocationId: 10
            , ProjectedOrder: 'N'
            , WebReferenceNo: 'my-unique-id'
            , CompanyId: 'ABC'
            , CustomerId: 123
            , PoNo: 'AE123'
          }
          , Lines: [
            {
              CompanyNo: 'ABC'
              , ItemId: '11111'
              , QtyOrdered: 10
              , ShipLocId: 10
              , SourceLocId: 10
              , UnitPrice: 500
              , UnitOfMeasure: 'EA'
              , ManualPriceOverride: 'Y'
              , ExtendedDescription: 'Extended Description Text'
            }
          ]
          ]
        })
    , 0);
```