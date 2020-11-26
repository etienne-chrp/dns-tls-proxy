using System;
using System.Linq;

namespace DnsTlsProxy
{
    public class DnsMessage
    {
        public byte[] Data { get; private set; }
        public byte[] Length => ConvertLittleEndian(BitConverter.GetBytes((ushort)Data.Length));

        public DnsMessage(byte[] data)
        {
            Data = data;
        }

        public static byte[] ConvertLittleEndian(byte[] input)
        {
            var output = input.ToArray();

            // Different computer architectures store data using different byte orders.
            // "Little-endian" means the most significant byte is on the right end of a word.
            // https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter.islittleendian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(output);

            return output;
        }
    }
}