using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins {
    class AmazonConnection_Developer {
        public string accessKey { get; set; }
        public string secretKey { get; set; }
        public string appName { get; set; }
        public string appVersion { get; set; }
    }

    class AmazonConnection_Seller{
        public string sellerId { get; set; }
        public string mwsAuthToken { get; set; }
        public string marketplaceId { get; set; }
    }
}
