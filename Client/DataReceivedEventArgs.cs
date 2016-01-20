namespace Client
{
    public class DataReceivedEventArgs
    {
        public DataReceivedEventArgs(byte[] buf)
        {
            this.Buffer = buf;
        }

        public byte[] Buffer { get; set; }
    }
}