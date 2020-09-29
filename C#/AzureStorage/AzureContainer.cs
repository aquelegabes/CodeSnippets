#region
// Requires package:
// <PackageReference Include="Azure.Storage.Blobs" Version="12.6.0" />
#endregion

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Namespace
{
    /// <summary>
    /// Class responsible for handling container related operations.
    /// </summary>
    public sealed class AzureContainer
    {
        private BlobContainerClient containerClient;

        /// <summary>
        /// Constructor responsible for obtaining the container.
        /// </summary>
        /// <param name="containerClient">Container.</param>
        /// <exception cref="ArgumentNullException"><paramref name="containerClient"/> is null.</exception>
        public AzureContainer(BlobContainerClient containerClient)
        {
            this.containerClient = containerClient ?? throw new ArgumentNullException(paramName: nameof(containerClient), message: "Container client must not be null.");
        }

        /// <summary>
        /// Asynchronously uploads a file into this azure container.
        /// </summary>
        /// <param name="fileContents">File contents.</param>
        /// <param name="fileName">
        /// <para>File name to be uploaded.</para>
        /// <para>If it is a new file, the file name should be unique. <see cref="System.Guid"/> appended.</para>
        /// </param>
        /// <param name="overwrite">Optional: should the file be overwritten? Default: false</param>
        /// <returns>true if uploaded, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fileContents"/> is null or empty.</exception>
        public async Task<bool> UploadAsync(
            byte[] fileContents,
            string fileName,
            bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(paramName: nameof(fileName), message: "File name must not be null.");

            if (fileContents?.Length == 0)
                throw new ArgumentNullException(paramName: nameof(fileContents), message: "Cannot upload a file that has no content.");

            var blobClient = this.containerClient.GetBlobClient(fileName);
            using (var stream = new MemoryStream(fileContents))
            {
                var response = await blobClient.UploadAsync(stream, overwrite);
                return response.Value != null;
            }
        }

        /// <summary>
        /// Asynchronously downloads a file from this azure container.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <returns>An azure file.e</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        public async Task<AzureFile> DownloadAsync(
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(paramName: nameof(fileName), message: "File name must not be null.");

            var blobClient = this.containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync();
            if (response?.Value?.Content?.Length == 0)
                throw new FileNotFoundException($"File named '{fileName}' not found.");
            using (var ms = new MemoryStream())
            {
                response.Value.Content.CopyTo(ms);

                return new AzureFile
                {
                    Content = ms.ToArray(),
                    ContentType = response.Value.ContentType,
                    Size = ms.Length,
                    Name = fileName
                };
            }
        }

        /// <summary>
        /// Asynchronously deletes an existing file from a container.
        /// </summary>
        /// <remarks>
        /// Marks the specified blob or snapshot for deletion, if the blob exists. The blob is later deleted during garbage collection. Note that in order to delete a blob, you must delete all of its snapshots. You can delete both at the same time using <see cref="DeleteSnapshotsOption.IncludeSnapshots"/>.
        /// </remarks>
        /// <param name="fileName">File name.</param>
        /// <param name="snapshotsOption">Optional: snapshot deletion options.</param>
        /// <returns>true if deleted, otherwise false</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        public async Task<bool> DeleteAsync(
            string fileName, DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.IncludeSnapshots)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(paramName: nameof(fileName), message: "File name must not be null.");

            var blobClient = this.containerClient.GetBlobClient(fileName);
            return await blobClient.DeleteIfExistsAsync(snapshotsOption);
        }
    }
}
