namespace Common
{
    public class UDPDataChunk
    {
        public long HashSumTotal { get; set; }

        public long HashSumChunk { get; set; }

        public int NumberOfChunk { get; set; }

        public int TotalChunks { get; set; }

        public byte[] Data { get; set; }

        public string Type { get; set; }
    }
}
