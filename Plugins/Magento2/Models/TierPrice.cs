using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class TierPrice
    {
        /// <summary>
        /// 
        /// </summary>
        public TierPrice() { }

        /// <summary>
        /// Website ID
        /// </summary>
        /// <remarks>optional</remarks>
        public int? website_id { get; set; }
        /// <summary>
        /// Customer group
        /// </summary>
        /// <remarks>optional</remarks>
        public int? cust_group { get; set; }
        /// <summary>
        /// Tier price
        /// </summary>
        /// <remarks>optional</remarks>
        public double? price { get; set; }
        /// <summary>
        /// Price quantity
        /// </summary>
        /// <remarks>optional</remarks>
        public double? price_qty { get; set; }
    }
}
