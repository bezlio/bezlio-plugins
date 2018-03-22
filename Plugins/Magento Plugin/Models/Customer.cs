using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class Customers
    {
        public List<Customer> Items { get; set; }
        public Search_Criteria search_criteria { get; set; }
        public int total_count { get; set; }
    }

    public class Customer
    {
        /// <summary>
        /// 
        /// </summary>
        public Customer() { }

        /// <summary>
        /// Id of the customer
        /// </summary>
        public int entity_id { get; set; }
        /// <summary>
        /// The customer first name
        /// </summary>
        /// <remarks>required</remarks>
        public string firstname { get; set; }
        /// <summary>
        /// The customer last name
        /// </summary>
        /// <remarks>required</remarks>
        public string lastname { get; set; }
        /// <summary>
        /// The customer email address
        /// </summary>
        /// <remarks>required</remarks>
        public string email { get; set; }
        /// <summary>
        /// The customer password. The password must contain minimum 7 characters
        /// </summary>
        /// <remarks>required</remarks>
        public string password { get; set; }
        /// <summary>
        /// Website ID
        /// </summary>
        /// <remarks>required</remarks>
        public int website_id { get; set; }
        /// <summary>
        /// Customer group ID
        /// </summary>
        /// <remarks>required</remarks>
        public int group_id { get; set; }
        /// <summary>
        /// Defines whether the automatic group change for the customer will be disabled
        /// </summary>
        /// <remarks>optional</remarks>
        public bool? disable_auto_group_change { get; set; }
        /// <summary>
        /// Customer prefix
        /// </summary>
        /// <remarks>optional</remarks>
        public string prefix { get; set; }
        /// <summary>
        /// Customer middle name or initial
        /// </summary>
        /// <remarks>optional</remarks>
        public string middlename { get; set; }
        /// <summary>
        /// Customer suffix
        /// </summary>
        /// <remarks>optional</remarks>
        public string suffix { get; set; }
        /// <summary>
        /// Customer Tax or VAT number	
        /// </summary>
        /// <remarks>optional</remarks>
        public string taxvat { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Can't be set</remarks>
        public DateTime? last_logged_in { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Can't be set</remarks>
        public DateTime? created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Can't be set</remarks>
        public string created_in { get; set; }

        /// <summary>
        /// A dictionary of all specified attributes
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }
    }
}
