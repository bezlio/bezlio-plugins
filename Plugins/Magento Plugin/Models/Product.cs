using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class Products
    {
        public Products() { }
        public List<Product> product { get; set; }
    }
    public class Product
    {
        /// <summary>
        /// 
        /// </summary>
        public Product() {
            group_price = new List<GroupPrice>();
            tier_price = new List<TierPrice>();
            stock_data = new StockData();
            Attributes = new Dictionary<string, string>();
        }

        /// <summary>
        /// Id of the product
        /// </summary>
        public int entity_id { get; set; }

        /// <summary>
        /// Product type. Can have the "simple" value
        /// </summary>
        /// <remarks>required</remarks>
        public string type_id { get; set; }
        /// <summary>
        /// Attribute set for the product
        /// </summary>
        /// <remarks>required</remarks>
        public int attribute_set_id { get; set; }
        /// <summary>
        /// Product SKU	
        /// </summary>
        /// <remarks>required</remarks>
        public string sku { get; set; }
        /// <summary>
        /// Product name
        /// </summary>
        /// <remarks>required</remarks>
        public string name { get; set; }
        /// <summary>
        /// Product meta title
        /// </summary>
        /// <remarks>optional</remarks>
        public string meta_title { get; set; }
        /// <summary>
        /// Product meta description
        /// </summary>
        /// <remarks>optional</remarks>
        public string meta_description { get; set; }
        /// <summary>
        /// A friendly URL path for the product
        /// </summary>
        /// <remarks>optional</remarks>
        public string url_key { get; set; }
        /// <summary>
        /// Custom design applied for the product page
        /// </summary>
        /// <remarks>optional</remarks>
        public string custom_design { get; set; }
        /// <summary>
        /// Page template that can be applied to the product page
        /// </summary>
        /// <remarks>optional</remarks>
        public string page_layout { get; set; }
        /// <summary>
        /// Defines how the custom options for the product will be displayed. Can have the following values: Block after Info Column or Product Info Column
        /// </summary>
        /// <remarks>optional</remarks>
        public string options_container { get; set; }
        /// <summary>
        /// Product country of manufacture. This is the 2 letter ISO code of the country.
        /// </summary>
        /// <remarks>optional</remarks>
        public string country_of_manufacture { get; set; }
        /// <summary>
        /// The Apply MAP option. Defines whether the price in the catalog in the frontend is substituted with a Click for price link
        /// </summary>
        /// <remarks>optional</remarks>
        public ManufacturerPriceEnablement? msrp_enabled { get; set; }
        /// <summary>
        /// Defines how the price will be displayed in the frontend. Can have the following values: In Cart, Before Order Confirmation, and On Gesture
        /// </summary>
        /// <remarks>optional</remarks>
        public PriceTypeDisplay? msrp_display_actual_price_type { get; set; }
        /// <summary>
        /// Defines whether the gift message is available for the product
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? gift_message_available { get; set; }
        /// <summary>
        /// Product price
        /// </summary>
        /// <remarks>required</remarks>
        public double price { get; set; }
        /// <summary>
        /// Product special price
        /// </summary>
        /// <remarks>optional</remarks>
        public double? special_price { get; set; }
        /// <summary>
        /// Product weight
        /// </summary>
        /// <remarks>required</remarks>
        public double weight { get; set; }
        /// <summary>
        /// The Manufacturer's Suggested Retail Price option. The price that a manufacturer suggests to sell the product at
        /// </summary>
        /// <remarks>optional</remarks>
        public double? msrp { get; set; }
        /// <summary>
        /// Product status. Can have the following values: 1- Enabled, 2 - Disabled.
        /// </summary>
        /// <remarks>required</remarks>
        public ProductStatus status { get; set; }
        /// <summary>
        /// Product visibility. Can have the following values: 1 - Not Visible Individually, 2 - Catalog, 3 - Search, 4 - Catalog, Search.
        /// </summary>
        /// <remarks>required</remarks>
        public ProductVisibility visibility { get; set; }
        /// <summary>
        /// Defines whether the product can be purchased with the help of the Google Checkout payment service. Can have the following values: Yes and No
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? enable_googlecheckout { get; set; }
        /// <summary>
        /// Product tax class. Can have the following values: 0 - None, 2 - taxable Goods, 4 - Shipping, etc., depending on created tax classes.
        /// </summary>
        /// <remarks>required</remarks>
        public int? tax_class_id { get; set; }
        /// <summary>
        /// Product description.
        /// </summary>
        /// <remarks>required</remarks>
        public string description { get; set; }
        /// <summary>
        /// Product short description.
        /// </summary>
        /// <remarks>optional</remarks>
        public string short_description { get; set; }
        /// <summary>
        /// Product meta keywords
        /// </summary>
        /// <remarks>optional</remarks>
        public string meta_keyword { get; set; }
        /// <summary>
        /// An XML block to alter the page layout
        /// </summary>
        /// <remarks>optional</remarks>
        public string custom_layout_update { get; set; }
        /// <summary>
        /// Date starting from which the special price will be applied to the product
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? special_from_date { get; set; }
        /// <summary>
        /// Date till which the special price will be applied to the product
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? special_to_date { get; set; }
        /// <summary>
        /// Date starting from which the product is promoted as a new product
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? news_from_date { get; set; }
        /// <summary>
        /// Date till which the product is promoted as a new product
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? news_to_date { get; set; }
        /// <summary>
        /// Date starting from which the custom design will be applied to the product page
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? custom_design_from { get; set; }
        /// <summary>
        /// Date till which the custom design will be applied to the product page
        /// </summary>
        /// <remarks>optional</remarks>
        public DateTime? custom_design_to { get; set; }
        /// <summary>
        /// Product group price
        /// </summary>
        /// <remarks>optional</remarks>
        public List<GroupPrice> group_price { get; set; }
        /// <summary>
        /// Product tier price
        /// </summary>
        /// <remarks>optional</remarks>
        public List<TierPrice> tier_price { get; set; }
        /// <summary>
        /// Product inventory data
        /// </summary>
        /// <remarks>optional</remarks>
        public StockData stock_data { get; set; }
        /// <summary>
        /// A dictionary of all specified attributes
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }

    }
}
