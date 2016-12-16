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
            // Convert the base64 string into a byte array
            byte[] gzBuffer = Convert.FromBase64String(base64);
            using (MemoryStream ms = new MemoryStream()) {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 0, gzBuffer.Length);

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress)) {
                    // Read from the zip stream into our new buffer
                    zip.Read(buffer, 0, buffer.Length);
                }

                // Return the buffer encoded as a UTF8 string
                return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
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
