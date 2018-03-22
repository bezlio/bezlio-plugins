using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class OrderItem
    {
        public OrderItem() {  }
        /// <summary>
        /// 
        /// </summary>
        public string sku { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double price { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_price { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_original_price { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double original_price { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double tax_percent { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double tax_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_tax_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double? base_discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_row_total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double row_total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_price_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double price_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_row_total_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double row_total_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int item_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? parent_item_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double qty_canceled { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double qty_invoiced { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double qty_ordered { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double qty_refunde { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double qty_shipped { get; set; }
    }
}
