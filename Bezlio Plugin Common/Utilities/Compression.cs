using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace bezlio.Utilities
{
    public class Compression
    {
        public static string DecompressString(string base64)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            byte[] gzip = Convert.FromBase64String(base64);
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return Encoding.UTF8.GetString(memory.ToArray()).TrimEnd('\0');
                }
            }
        }

        public static string CompressString(string text)
        {
            // Create a byte array from the string
            var bytes = Encoding.UTF8.GetBytes(text.TrimEnd('\0'));

            // Create a memory stream of our bytes
            using (var msi = new MemoryStream(bytes)) {
                // Create our output memory stream
                using (var mso = new MemoryStream()) {
                    // Use GZipStream to compress
                    using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                        // Copy to the gzipstream which writes to the mso output stream
                        msi.CopyTo(gs);
                    }
                    // Convert the memory stream bytearray to base64 string
                    return Convert.ToBase64String(mso.ToArray());
                }
            }
        }
    }
}
