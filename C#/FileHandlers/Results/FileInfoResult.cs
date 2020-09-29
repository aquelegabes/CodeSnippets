using System;
using System.Collections.Generic;

namespace Namespace
{
    /// <summary>
    /// List result containing next page and a collection of <see cref="FileInfoResult"/>.
    /// </summary>
    public sealed class ListFileInfoResult
    {
        public object NextPageObject { get; set; }
        public ICollection<FileInfoResult> Files { get; set; } = new List<FileInfoResult>();
    }

    /// <summary>
    /// Basic file info.
    /// </summary>
    public sealed class FileInfoResult
    {
        public string Id { get; set; }
        public string MimeType { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? CreatedTime { get; set; }
        public long Size { get; set; }
        public object Additional { get; set; }
    }
}