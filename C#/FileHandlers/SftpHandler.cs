using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Microsoft.AspNetCore.StaticFiles;

namespace Namespace
{
    /// <summary>
    /// Structured Sftp settings information.
    /// </summary>
    public class SftpSettings : FtpSettings
    {
        /// <summary>
        /// Sftp port number.
        /// </summary>
        /// <value>Default value is 22.</value>
        public override int Port { get; set; } = 22;
    }

    /// <summary>
    /// Class responsible for handling Sftp methods.
    /// </summary>
    public class SftpHandler : IFileHandler
    {
        private SftpClient sftpClient;
        private SftpSettings sftpSettings;

        /// <inheritdoc />
        [Obsolete("Method unused on Sftp.", true)]
        public Task<bool> PathExistsAsync(string relativePath)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<object> CanConnect(string relativePath)
        {
            var result = new Dictionary<string, object>();
            try
            {
                sftpClient.Connect();
                result["connectionExists"] = true;
                await Task.Run(() => sftpClient.ListDirectory(relativePath));
                result["folderExists"] = true;
                return result;
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                result["folderExists"] = false;
                return result;
            }
            catch (Exception)
            {
                return new Dictionary<string, object>
                {
                    ["connectionExists"] = false,
                    ["folderExists"] = false
                };
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task CreateDirectoryAsync(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentNullException(paramName: nameof(directoryName), message: "Directory name must not be null.");
            try
            {
                sftpClient.Connect();
                await Task.Run(() => sftpClient.CreateDirectory(directoryName));
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path must not be null.");
            try
            {
                sftpClient.Connect();
                await Task.Run(() => sftpClient.DeleteFile(filePath));
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(Stream fileResult, string fileLocation)
        {
            if (string.IsNullOrWhiteSpace(fileLocation))
                throw new ArgumentNullException(paramName: nameof(fileLocation), "File location must not be null.");

            try
            {
                sftpClient.Connect();
                await Task.Run(() =>
                {
                    var r = sftpClient.BeginDownloadFile(fileLocation, fileResult);
                    r.AsyncWaitHandle.WaitOne();
                });
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task<FileInfoResult> FileLookupAsync(
            string fileId = "", string fileName = "", string relativePath = "")
        {
            if (string.IsNullOrWhiteSpace(fileName)
                && string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException("File name and its path must not be null.");
            }

            try
            {
                sftpClient.Connect();
                object state = default;
                await Task.Run(() => state = sftpClient.ListDirectory(relativePath));

                var files = state as IEnumerable<SftpFile>;
                var file = files.FirstOrDefault(f => f.Name.Contains(fileName, StringComparison.OrdinalIgnoreCase));

                if (file == null)
                    throw new FileNotFoundException($"File named '{fileName}' was not found.");

                string contentType = "application/octet-stream";
                if (!string.IsNullOrWhiteSpace(fileName))
                    new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

                return new FileInfoResult
                {
                    Id = file.FullName,
                    Name = file.Name,
                    Size = file.Length,
                    CreatedTime = file.LastWriteTime,
                    MimeType = contentType
                };
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task<ListFileInfoResult> ListAsync(
            string relativeDirectory = "/", int pageSize = 100, object pageObject = null)
        {
            try
            {
                int page = (int?)pageObject ?? 1;

                sftpClient.Connect();
                object state = default;
                await Task.Run(() => state = sftpClient.ListDirectory(relativeDirectory));
                var files = state as IEnumerable<SftpFile>;
                ListFileInfoResult result = new ListFileInfoResult();
                if (files?.Any() == false)
                    return result;

                result.Files = files
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f =>
                    {
                        string contentType = "application/octet-stream";
                        if (!string.IsNullOrWhiteSpace(f.Name))
                            new FileExtensionContentTypeProvider().TryGetContentType(f.Name, out contentType);
                        return new FileInfoResult
                        {
                            Id = f.FullName,
                            Name = f.Name,
                            Size = f.Length,
                            CreatedTime = f.LastWriteTime,
                            MimeType = contentType
                        };
                    }).ToList();

                if (files.Count() > pageSize)
                    result.NextPageObject = page + 1;

                return result;
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public void LoadSettings<T>(T settings)
        {
            if (!(settings is SftpSettings sftpSettings)
                || !(settings is FtpSettings ftpSettings))
                throw new InvalidCastException("Settings is not of correctly type.");

            if (sftpSettings == null)
                sftpSettings = ftpSettings as SftpSettings;

            // TODO: needs further testing
            if (sftpSettings.Url.Contains(ConcreteFileHandlerFactory.Sftp))
                sftpSettings.Url = sftpSettings.Url.Replace(ConcreteFileHandlerFactory.Sftp, string.Empty);

            this.sftpSettings = sftpSettings;
            this.sftpClient = new SftpClient(
                host: sftpSettings.Url,
                port: sftpSettings.Port,
                username: sftpSettings.User,
                password: sftpSettings.Password);
        }

        /// <inheritdoc />
        public async Task MoveFileAsync(string filePath, string pathToMove)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(paramName: nameof(filePath), message: "File path must not be null.");
            if (string.IsNullOrWhiteSpace(pathToMove))
                throw new ArgumentNullException(paramName: nameof(pathToMove), message: "Path to move must not be null.");

            try
            {
                sftpClient.Connect();
                await Task.Run(() => sftpClient.RenameFile(filePath, pathToMove));
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }

        /// <inheritdoc />
        public async Task UploadFileAsync(
            Stream uploadingFile, string fileName, string relativeUploadPath)
        {
            if (uploadingFile?.Length == 0)
                throw new ArgumentNullException(paramName: nameof(uploadingFile), message: "File to upload must not be null.");
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(paramName: nameof(fileName), message: "File name must not be null.");
            if (string.IsNullOrWhiteSpace(relativeUploadPath))
                throw new ArgumentNullException(paramName: nameof(relativeUploadPath), message: "Relative uploading path must not be null.");
            try
            {
                sftpClient.Connect();
                string filePath = Path.Combine(relativeUploadPath, fileName);
                await Task.Run(() => sftpClient.UploadFile(uploadingFile, filePath));
            }
            finally
            {
                sftpClient.Disconnect();
            }
        }
    }
}
