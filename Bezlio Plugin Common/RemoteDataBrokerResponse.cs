using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Text;
using bezlio.Utilities;

namespace bezlio.rdb
{
    [DataContract]
    public class RemoteDataBrokerResponse
    {
        [JsonIgnore]
        private string data;
        private bool encrypt = false;

        public RemoteDataBrokerResponse()
        {
            this.Compress = true;
        }

        [JsonIgnore]
        public string Data
        {
            // TODO: What if someone sets data and then sets the compress/encrypt flag differently
            get
            {
                // If it's blank or null dont try doing the rest
                if (this.data == null || this.data == "") { return ""; }

                if (this.encrypt == true)
                {
                    // We encrypt the compressed value (otherwise the compression would be minimal)
                    var decrypt = Encoding.Default.GetString(AESThenHMAC.SimpleDecryptWithPassword(Convert.FromBase64String(this.data), this.EncryptKey));
                    if (this.Compress == true)
                    {
                        // Encrypted and compressed
                        return Compression.DecompressString(decrypt);
                    }
                    else
                    {
                        // Encrypted but not compressed
                        return decrypt;
                    }
                }
                else
                {
                    // It's not encrypted
                    if (this.Compress == true)
                    {
                        return Compression.DecompressString(this.data);
                    }
                    else
                    {
                        return this.data;
                    }
                }
            }
            set
            {
                if (value != "" && value != null)
                {
                    if (this.encrypt == true)
                    {
                        // Encrypted
                        if (this.Compress == true)
                        {
                            // Encrypted and compressed
                            // We compress then we encrypt
                            var compress = Compression.CompressString(value);
                            this.data = Convert.ToBase64String(AESThenHMAC.SimpleEncryptWithPassword(Encoding.Default.GetBytes(compress), this.EncryptKey));
                        }
                        else
                        {
                            // Encrypted only
                            this.data = Convert.ToBase64String(AESThenHMAC.SimpleEncryptWithPassword(Encoding.Default.GetBytes(value), this.EncryptKey));
                        }
                    }
                    else
                    {
                        // Not Encrypted
                        if (this.Compress == true)
                        {
                            // Compressed only
                            this.data = Compression.CompressString(value);
                        }
                        else
                        {
                            // Raw
                            this.data = value;
                        }
                    }
                }
                else
                {
                    this.data = "";
                }
            }
        }

        [JsonProperty(PropertyName = "rawData")]
        public string RawData
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
            }
        }

        [JsonProperty(PropertyName = "dataType")]
        public string DataType { get; set; }

        [JsonProperty(PropertyName = "compress")]
        public bool Compress { get; set; }

        [JsonProperty(PropertyName = "encrypt")]
        public bool Encrypt
        {
            get { return this.encrypt; }
            set
            {
                if (this.EncryptKey != "")
                {
                    // TODO: What if they set data then flip the flag
                    this.encrypt = value;
                }
            }
        }

        [JsonIgnore]
        public string EncryptKey { get; set; }

        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }

        [JsonProperty(PropertyName = "error")]
        public bool Error { get; set; }

        [JsonProperty(PropertyName = "errorText")]
        public string ErrorText { get; set; }
    }
}