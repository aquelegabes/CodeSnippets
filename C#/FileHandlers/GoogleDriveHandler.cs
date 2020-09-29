#region
// Required package
// <PackageReference Include="Google.Apis.Drive.v3"/>
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.StaticFiles;

namespace Namespace
{
    public sealed class GoogleDriveHandler : IFileHandler, IDisposable
    {
        /// <summary>
        /// Mime type that a google drive folder have.
        /// </summary>
        public const string DirectoryMimeType = "application/vnd.google-apps.folder";
        /// <summary>
        /// Application name.
        /// </summary>
        private const string ApplicationName = "{ApplicationName}";
        private UserCredential Credentials { get; set; }
        private DriveService Service { get; set; }

        /// <inheritdoc />
        [Obsolete("This method is not used in GoogleDriveHandler.", true)]
        public Task<bool> CanConnect()
        {
            throw new NotImplementedException("Google drive handler does not require this method.");
        }

        /// <inheritdoc />
        [Obsolete("This method is not used in GoogleDriveHandler.", true)]
        public Task<bool> PathExistsAsync(string relativePath)
        {
            throw new NotImplementedException("Google drive handler does not require this method.");
        }

        /// <summary>
        /// <para>Retrieve <paramref name="fileId"/> file information.</para>
        /// <para>or</para>
        /// <para>Retrieve first matching <paramref name="partialFileName"/> file information.</para>
        /// </summary>
        /// <remarks>Case <paramref name="parentFolderId"/> is used, search will happen only inside specified folder.</remarks>
        /// <param name="fileId"></param>
        /// <param name="partialFileName">The partial file name</param>
        /// <param name="parentFolderId">Optional: parent folder id.</param>
        /// <returns>A dictionary containing "id", "fileName", "mimeType", "fileExtension".</returns>
        /// <exception cref="ArgumentException">Both <paramref name="fileId"/> and <paramref name="partialFileName"/> are null.</exception>
        /// <exception cref="FileNotFoundException">File is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Invalid token.</exception>
        public async Task<FileInfoResult> FileLookupAsync(
            string fileId = "",
            string partialFileName = "", string parentFolderId = "")
        {
            if (string.IsNullOrWhiteSpace(partialFileName)
                && string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File id or partial file name must be available.");
            }

            try
            {
                Google.Apis.Drive.v3.Data.File fileInfo = default;

                if (!string.IsNullOrWhiteSpace(fileId))
                {
                    var request = Service.Files.Get(fileId);
                    request.Fields = "files(*)";
                    fileInfo = await request.ExecuteAsync();
                    if (fileInfo == null)
                        throw new FileNotFoundException(message: "File not found.", fileName: fileId);
                }
                else if (!string.IsNullOrWhiteSpace(partialFileName))
                {
                    var request = Service.Files.List();
                    request.Q = $"name contains '{partialFileName}'";
                    if (!string.IsNullOrWhiteSpace(parentFolderId))
                        request.Q += $" and '{parentFolderId}' in parents";

                    request.Fields = "files(*)";
                    var result = await request.ExecuteAsync();
                    if (result?.Files?.Count == 0)
                        throw new FileNotFoundException(message: "File not found.", fileName: partialFileName);

                    fileInfo = result.Files[0];
                }

                return new FileInfoResult
                {
                    MimeType = fileInfo.MimeType,
                    Id = fileInfo.Id,
                    Name = fileInfo.Name,
                    CreatedTime = fileInfo.CreatedTime,
                    Size = fileInfo.Size ?? 0,
                };
            }
            catch (Google.GoogleApiException e)
            when (e.Message.Contains("file not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException(message: "File not found.", fileName: partialFileName);
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc/>
        /// <param name="directoryName">This is the directory name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="directoryName"/> is null or empty.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized token.</exception>
        public async Task CreateDirectoryAsync(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentNullException(paramName: nameof(directoryName), message: "Directory name must not be null.");

            try
            {
                var directory = new Google.Apis.Drive.v3.Data.File
                {
                    Name = directoryName,
                    MimeType = DirectoryMimeType
                };
                var request = Service.Files.Create(directory);
                await request.ExecuteAsync();
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc />
        /// <param name="fileResult">Stream to return the file.</param>
        /// <param name="fileId">This is the Google file id.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">File not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized token.</exception>
        public async Task DownloadFileAsync(Stream fileResult, string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentNullException(paramName: nameof(fileId), message: "File ID must not be null.");

            try
            {
                var request = Service.Files.Get(fileId);
                await request.DownloadAsync(fileResult);

                if (fileResult?.Length == 0)
                    throw new FileNotFoundException(message: "File not found.", fileName: fileId);
            }
            catch (Google.GoogleApiException e)
            when (e.Message.Contains("file not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException(message: "File not found.", fileName: fileId);
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc />
        /// <param name="fileId">The file id.</param>
        /// <param name="folderParentId">The folder parent Id to move to.</param>
        /// <exception cref="FileNotFoundException">File not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Invalid credentials.</exception>
        public async Task MoveFileAsync(string fileId, string folderParentId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentNullException(paramName: nameof(fileId), "File id must not be null.");

            if (string.IsNullOrWhiteSpace(folderParentId))
                throw new ArgumentNullException(paramName: nameof(folderParentId), "Folder parent id must not be null.");

            try
            {
                var fileReq = Service.Files.Get(fileId);
                // retrieve existing parents to remove
                fileReq.Fields = "parents";
                var fileResp = await fileReq.ExecuteAsync();
                string previousParents = string.Join(',', fileResp.Parents);

                var moveReq = Service.Files.Update(null, fileId);
                moveReq.AddParents = folderParentId;
                moveReq.RemoveParents = previousParents;
                moveReq.Fields = "id, parents";
                await moveReq.ExecuteAsync();
            }
            catch (Google.GoogleApiException e)
            when (e.Message.Contains("file not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException(message: "File or parent directory not found.", fileName: fileId);
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc />
        /// <param name="parentFolderId">This is the parent folder id.</param>
        /// <param name="pageSize">Items per page.</param>
        /// <param name="pageObject">Page token.</param>
        /// <exception cref="FileNotFoundException">Files not found.</exception>
        /// <exception cref="InvalidDataException"><paramref name="pageObject"/> is not a <see cref="string"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized token.</exception>
        public async Task<ListFileInfoResult> ListAsync(string parentFolderId = "", int pageSize = 100, object pageObject = default)
        {
            if (!(pageObject is string))
                throw new InvalidDataException($"'{nameof(pageObject)}' must be a string.");

            string nextPageToken = default;
            IList<Google.Apis.Drive.v3.Data.File> files = default;

            try
            {
                var request = Service.Files.List();
                request.Fields = "nextPageToken, files(id, name, mimeType, createdTime, size)";
                request.PageSize = pageSize;
                request.PageToken = (string)pageObject;

                if (!string.IsNullOrWhiteSpace(parentFolderId))
                    request.Q = $"'{parentFolderId}' in parents and ";
                // ignore files in trash
                request.Q = "trashed = false";

                var result = await request.ExecuteAsync();
                nextPageToken = result.NextPageToken;
                files = result.Files;

                if (files?.Count == 0)
                    throw new FileNotFoundException("Files not found.");

                var resultFiles = new ListFileInfoResult();

                files.ForEach(file =>
                {
                    var fileInfo = new FileInfoResult
                    {
                        MimeType = file.MimeType,
                        Id = file.Id,
                        Name = file.Name,
                        CreatedTime = file.CreatedTime,
                        Size = file.Size ?? 0,
                    };

                    resultFiles.Files.Add(fileInfo);
                });

                resultFiles.NextPageObject = nextPageToken;
                return resultFiles;
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para>Considerations regarding <paramref name="settings"/>:</para>
        /// <para>
        /// • <typeparamref name="T"/>Is expected to be a Dictionary{string, object}
        /// that contains ["secrets"](<see cref="ClientSecrets"/>) and a ["token"](<see cref="string"/>) which is the user refresh token.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidCastException">Settings is not a valid type.</exception>
        /// <exception cref="UnauthorizedAccessException">Invalid token.</exception>
        /// <exception cref="UnauthorizedAccessException">Could not refresh user credentials.</exception>
        public void LoadSettings<T>(T settings)
        {
            if (!(settings is Dictionary<string, object>))
                throw new InvalidCastException(message: "Settings is of invalid type.");

            var settingsd = settings as Dictionary<string, object>;

            IAuthorizationCodeFlow flow =
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = settingsd["secrets"] as ClientSecrets,
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore("Drive.Api.Auth.Store"),
            });

            var token = new TokenResponse
            {
                RefreshToken = (string)settingsd["token"]
            };

            Credentials = new UserCredential(flow, Guid.NewGuid().ToString(), token);

            try
            {
                Credentials.RefreshTokenAsync(CancellationToken.None).Wait();
                if (string.IsNullOrWhiteSpace(Credentials.Token.AccessToken))
                    throw new UnauthorizedAccessException("Unable to refresh access token.");

                Service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = Credentials,
                    ApplicationName = ApplicationName
                });
            }
            catch (AggregateException e)
            when (e.InnerException?.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new UnauthorizedAccessException("Invalid token.");
            }
            catch { throw; }
        }

        /// <inheritdoc/>
        /// <param name="file">File stream content.</param>
        /// <param name="fileName">File name.</param>
        /// <param name="parentFolderId">This is a Google folder Id.</param>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> to upload is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parentFolderId"/> folder id is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is missing extension.</exception>
        /// <exception cref="FileNotFoundException">File is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized token.</exception>
        public async Task UploadFileAsync(Stream file, string fileName, string parentFolderId)
        {
            if (file?.Length == 0)
                throw new ArgumentNullException(paramName: nameof(file), message: "File to upload must not be null.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(paramName: nameof(fileName), message: "File name must not be null.");

            var fileExt = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(fileExt))
                throw new ArgumentException(message: "File name is missing the file extension.", paramName: nameof(fileName));

            if (string.IsNullOrWhiteSpace(parentFolderId))
                throw new ArgumentNullException(paramName: nameof(parentFolderId), message: "Folder upload Id must not be null.");

            string contentType = "application/octet-stream";
            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

            var uploadingFile = new Google.Apis.Drive.v3.Data.File
            {
                Parents = new[] { parentFolderId },
                Name = fileName,
                MimeType = contentType
            };

            try
            {
                var request = Service.Files.Create(uploadingFile, file, uploadingFile.MimeType);
                await request.UploadAsync();
            }
            catch (Google.GoogleApiException e)
            when (e.Message.Contains("file not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException(message: "Parent directory not found.", fileName: parentFolderId);
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">File id is null.</exception>
        /// <exception cref="FileNotFoundException">File is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Invalid token.</exception>
        public async Task DeleteFileAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentNullException(paramName: nameof(fileId), message: "File id must not be null.");

            try
            {
                var request = Service.Files.Delete(fileId);
                await request.ExecuteAsync();
            }
            catch (Google.GoogleApiException e)
            when (e.Message.Contains("file not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException(message: "File not found.", fileName: fileId);
            }
            catch (TokenResponseException e)
            when (e.Message.Contains("unauthorized", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedAccessException("Unauthorized", e);
            }
            catch { throw; }
        }

        public void Dispose()
        {
            Service.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
