using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class Orders
    {
        public Orders() { }
        public List<Order> order { get; set; }
    }

    public class Order
    {
        public Order() {}

        /// <summary>
        /// Id of the order
        /// </summary>
        public int entity_id  {  get; set; }
        /// <summary>
        /// Id of the customer
        /// </summary>
        public int? customer_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double? base_discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_shipping_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_shipping_tax_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_subtotal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_tax_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double base_grand_total { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? base_total_paid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double? base_total_refunded { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double tax_amount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double grand_total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        
        public double subtotal { get; set; }

        /// <summary>
        /// 
        /// </summary>

        public double shipping_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double shipping_tax_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double store_to_order_rate { get; set; }

        /// <summary>
        /// 
        /// </summary>

        public double? total_paid { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double? total_refunded { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double? discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double base_shipping_discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double base_subtotal_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double base_total_due { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double total_due { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string base_currency_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string discount_description { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double shipping_discount_amount { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double subtotal_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public double shipping_incl_tax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string payment_method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string gift_message_from { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string gift_message_to { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string gift_message_body { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string tax_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string tax_rate { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public List<OrderAddress> addresses { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<OrderItem> order_items { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<OrderComment> order_comments { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string remote_ip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string store_currency_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string store_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string increment_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string coupon_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string shipping_description { get; set; }
    }
}
