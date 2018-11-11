using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common
{
    public class UDPdgramDataParser
    {
        public UDPdgramDataParser()
        {
        }

        public void WriteFile(List<UDPDataChunk> chunks, string filename)
        {
            var orderedCollection = chunks.OrderBy(x => x.NumberOfChunk).ToList();
            var bytes = orderedCollection.SelectMany(x => x.Data).ToArray();

            File.WriteAllBytes(filename, bytes);

        }

        public byte[] ReadFile(string filename)
        {
            return File.ReadAllBytes(filename);
        }

        public List<UDPDataChunk> GetDataChunks(byte[] dataBytes, string type )
        {
            var maxSizeOfChunk = 45000;

            var numberOfChunks = (int)Math.Ceiling(dataBytes.Length / (double)maxSizeOfChunk);

            var list = new List<UDPDataChunk>();

            var totalhashsum = GetHashSum(dataBytes);

            for (var i = 0; i < numberOfChunks; i++)
            {
                var data = dataBytes.Skip(i * maxSizeOfChunk).Take(maxSizeOfChunk).ToArray();
                list.Add(new UDPDataChunk
                {
                    Data = data,
                    HashSumChunk = Int64.Parse($"{i}{GetHashSum(data)}"),
                    NumberOfChunk = i,
                    TotalChunks = numberOfChunks,
                    HashSumTotal = totalhashsum,
                    Type = type
                });
            }

            return list;
        }

        public long GetHashSum(byte[] bytes)
        {
            return bytes.Select(x => (long)x).Aggregate((acc, x) => acc + x);
        }
    }
}
