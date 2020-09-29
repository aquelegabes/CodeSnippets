using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Namespace
{
    /// <summary>
    /// Contract to implement on different file handlers.
    /// </summary>
    public interface IFileHandler
    {
        /// <summary>
        /// List directories/files.
        /// </summary>
        /// <returns>Listed files and directories.</returns>
        Task<ListFileInfoResult> ListAsync(string relativeDirectory = "", int pageSize = 100, object pageObject = default);

        /// <summary>
        /// Get information about a specified file. Use either fileId or fileName.
        /// </summary>
        /// <returns>Information about a file.</returns>
        Task<FileInfoResult> FileLookupAsync(string fileId = "", string fileName = "", string relativePath = "");

        /// <summary>
        /// Asynchronously download a file.
        /// </summary>
        /// <remarks>
        /// <para>Consideration regards <paramref name="fileResult"/>.</para>
        /// <para>• Caller is responsible for maintaining the <paramref name="fileResult"/> <see cref="Stream"/> open until the upload is completed.</para>
        /// <para>• Caller is responsible for closing the <paramref name="fileResult"/> <see cref="Stream"/>.</para>
        /// </remarks>
        /// <returns>A <see cref="Stream"/> continaing the file into the <paramref name="fileResult"/>.</returns>
        Task DownloadFileAsync(Stream fileResult, string fileLocation);

        /// <summary>
        /// Asynchronously move an existing file into a specified path.
        /// </summary>
        Task MoveFileAsync(string filePath, string pathToMove);

        /// <summary>
        /// Asynchronously upload a file.
        /// </summary>
        /// <remarks>
        /// <para>Consideration regards <paramref name="uploadingFile"/>.</para>
        /// <para>• Caller is responsible for maintaining the <paramref name="uploadingFile"/> <see cref="Stream"/> open until the upload is completed.</para>
        /// <para>• Caller is responsible for closing the <paramref name="uploadingFile"/> <see cref="Stream"/>.</para>
        /// </remarks>
        Task UploadFileAsync(Stream uploadingFile, string fileName, string relativeUploadPath);

        /// <summary>
        /// Create a directory.
        /// </summary>
        Task CreateDirectoryAsync(string relativePath);

        /// <summary>
        /// Asynchronously check if path exists.
        /// </summary>
        /// <returns>true if exists, otherwise false</returns>
        Task<bool> PathExistsAsync(string relativePath);

        /// <summary>
        /// <para>Load settings to the <see cref="IFileHandler"/>.</para>
        /// <para>Such as: credentials, url.</para>
        /// </summary>
        void LoadSettings<T>(T settings);

        /// <summary>
        /// Asynchronously delete a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>true if deleted, otherwise false.</returns>
        Task DeleteFileAsync(string fileName);

        /// <summary>
        /// Asynchronously check if a connection can be made;
        /// </summary>
        /// <returns>true if can connect, otherwise false</returns>
        Task<bool> CanConnect();
    }
}
