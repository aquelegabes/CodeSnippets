namespace Namespace
{
    /// <summary>
    /// Class responsible for maping azure storage options.
    /// </summary>
    public sealed class AzureStorageOptions
    {
        public string StorageName { get; set; }
        public string Key { get; set; }
        public string ConnectionString { get; set; }
    }
}
