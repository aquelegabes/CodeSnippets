namespace Namespace
{
    /// <summary>
    /// Class that represents an azure blob file.
    /// </summary>
    public sealed class AzureFile
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
    }
}
