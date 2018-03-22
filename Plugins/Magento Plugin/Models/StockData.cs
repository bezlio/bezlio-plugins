using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class StockData
    {
        /// <summary>
        /// 
        /// </summary>
        public StockData() { }

        /// <summary>
        /// Quantity of stock items for the current product
        /// </summary>
        /// <remarks>optional</remarks>
        public double? qty { get; set; }
        /// <summary>
        /// Quantity for stock items to become out of stock
        /// </summary>
        /// <remarks>optional</remarks>
        public double? min_qty { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Qty for Item's Status to Become Out of Stock option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_min_qty { get; set; }
        /// <summary>
        /// Choose whether the product can be sold using decimals (e.g., you can buy 2.5 product)
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? is_qty_decimal { get; set; }
        /// <summary>
        /// Defines whether the customer can place the order for products that are out of stock at the moment.
        /// </summary>
        /// <remarks>optional</remarks>
        public BackOrderStatus? backorders { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Backorders option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_backorders { get; set; }
        /// <summary>
        /// Minimum number of items in the shopping cart to be sold
        /// </summary>
        /// <remarks>optional</remarks>
        public double? min_sale_qty { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Minimum Qty Allowed in Shopping Cart option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_min_sale_qty { get; set; }
        /// <summary>
        /// Maximum number of items in the shopping cart to be sold
        /// </summary>
        /// <remarks>optional</remarks>
        public double? max_sale_qty { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Maximum Qty Allowed in Shopping Cart option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_max_sale_qty { get; set; }
        /// <summary>
        /// Defines whether the product is available for selling
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? is_in_stock { get; set; }
        /// <summary>
        /// The number of inventory items below which the customer will be notified
        /// </summary>
        /// <remarks>optional</remarks>
        public double? notify_stock_qty { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Notify for Quantity Below option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_notify_stock_qty { get; set; }
        /// <summary>
        /// Choose whether to view and specify the product quantity and availability and whether the product is in stock management
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? manage_stock { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Manage Stock option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_manage_stock { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Qty Increments option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool use_config_qty_increments { get; set; }
        /// <summary>
        /// The product quantity increment value
        /// </summary>
        /// <remarks>optional</remarks>
        public double qty_increments { get; set; }
        /// <summary>
        /// Choose whether the Config settings will be applied for the Enable Qty Increments option
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? use_config_enable_qty_inc { get; set; }
        /// <summary>
        /// Defines whether the customer can add products only in increments to the shopping cart
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? enable_qty_increments { get; set; }
        /// <summary>
        /// Defines whether the stock items can be divided into multiple boxes for shipping
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? is_decimal_divided { get; set; }

    }
}
