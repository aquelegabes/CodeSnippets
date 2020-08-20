using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Namespace
{
    /// <summary>
    /// Structured FTP settings information.
    /// </summary>
    public struct FTPSettings
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Checks if <see cref="Url"/> and <see cref="Password"/> are available.
        /// </summary>
        /// <returns>true if available, otherwise false</returns>
        public bool IsCredentialsAvailable => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
    }

    /// <summary>
    /// Class responsible for handling FTP methods.
    /// </summary>
    public class FTPHandler
    {
        private FTPSettings _settings;
        private readonly NetworkCredential _credentials;

        /// <summary>
        /// Constructor that manages configuring the handler using a <see cref="FTPSettings"/>.
        /// </summary>
        /// <param name="settings">Valid settings.</param>
        /// <exception cref="MissingFieldException"><see cref="FTPSettings.Url"/> is missing.</exception>
        public FTPHandler(FTPSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Url))
                throw new MissingFieldException(message: $"{nameof(settings.Url)} must not be missing.");

            _settings = settings;
            _credentials = new NetworkCredential(_settings.User, _settings.Password);
        }

        /// <summary>
        /// List all directories/files from a <see cref="FTPSettings.Url"/> in a <paramref name="relativeDirectory"/>.
        /// </summary>
        /// <remarks>
        /// <para><paramref name="relativeDirectory"/> is optional, if is not specified lists in the root <see cref="FTPSettings.Url"/>.</para>
        /// <para><paramref name="relativeDirectory"/> can also be specified in <see cref="FTPSettings.Url"/>.</para>
        /// </remarks>
        /// <param name="relativeDirectory">Optional: relative directory path.</param>
        /// <returns>A <see cref="System.Collections.IEnumerable"/> of string listing all directories and files.</returns>
        /// <exception cref="WebException">Fail when connecting to the ftp server.</exception>
        public async Task<IEnumerable<string>> ListDirectoriesAsync(string relativeDirectory = "")
        {
            var uri = new Uri(_settings.Url);
            if (!string.IsNullOrWhiteSpace(relativeDirectory))
                uri = new Uri(uri, relativeDirectory);

            var request = (FtpWebRequest)WebRequest.Create(uri);
            try
            {
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                if (_settings.IsCredentialsAvailable)
                    request.Credentials = _credentials;

                var response = (FtpWebResponse)await request.GetResponseAsync();

                using (var responseStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        var directories = new List<string>();
                        while (reader?.EndOfStream == false)
                        {
                            directories.Add(reader.ReadLine());
                        }
                        return directories;
                    }
                }
            }
            catch (Exception e)
            {
                throw new WebException("Fail when connecting to the ftp server.", e);
            }
            finally
            {
                request.Abort();
            }
        }

        /// <summary>
        /// Download a file from a <see cref="FTPSettings.Url"/> location.
        /// </summary>
        /// <remarks>
        /// <para><paramref name="fileLocation"/> use as a relative path location to <see cref="FTPSettings.Url"/>.</para>
        /// <para><paramref name="fileLocation"/> can also be specified in <see cref="FTPSettings.Url"/>.</para>
        /// </remarks>
        /// <param name="fileLocation">Optional: relative path file location.</param>
        /// <returns>The file as a <see cref="Stream"/>.</returns>
        /// <exception cref="WebException">Fail when connecting to the ftp server.</exception>
        public async Task<Stream> DownloadFileAsync(string fileLocation = "")
        {
            var uri = new Uri(_settings.Url);
            if (!string.IsNullOrWhiteSpace(fileLocation))
                uri = new Uri(uri, fileLocation);

            var request = (FtpWebRequest)WebRequest.Create(uri);
            try
            {
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                if (_settings.IsCredentialsAvailable)
                    request.Credentials = _credentials;

                var response = (FtpWebResponse)await request.GetResponseAsync();
                return response.GetResponseStream();
            }
            catch (Exception e)
            {
                throw new WebException("Fail when connecting to the ftp server.", e);
            }
        }

        /// <summary>
        /// Upload a file into the requested <see cref="FTPSettings.Url"/>
        /// </summary>
        /// <param name="file">A file as a <see cref="Stream"/>.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="relativeUploadPath">Upload location relative to <see cref="FTPSettings.Url"/>.</param>
        /// <returns>true if the file was uploaded, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> is null.</exception>
        /// <exception cref="WebException">Fail when communicating to the ftp server.</exception>
        public async Task<bool> UploadFileAsync(
            Stream file,
            string fileName,
            string relativeUploadPath)
        {
            if (file?.Length == 0)
                throw new ArgumentNullException(paramName: nameof(file), message: "File must not be null.");
            var uri = new Uri(_settings.Url);
            Uri uploadLocationUri = null;

            if (!string.IsNullOrWhiteSpace(relativeUploadPath))
            {
                uri = new Uri(uri, relativeUploadPath);
                uploadLocationUri = uri;
            }
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                uri = new Uri(uri, Path.Join(relativeUploadPath, fileName));
            }

            try
            {
                if (uploadLocationUri != null
                    && !await DirectoryExistsAsync(uploadLocationUri))
                {
                    await CreateDirectoryAsync(relativeUploadPath);
                }
            }
            catch { throw; }

            var uploadRequest = (FtpWebRequest)WebRequest.Create(uri);

            try
            {
                uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
                if (_settings.IsCredentialsAvailable)
                    uploadRequest.Credentials = _credentials;

                uploadRequest.ContentLength = file.Length;
                using (var requestStream = await uploadRequest.GetRequestStreamAsync())
                {
                    file.CopyTo(requestStream);
                }

                var response = await uploadRequest.GetResponseAsync() as FtpWebResponse;

                return true;
            }
            catch (Exception e)
            {
                throw new WebException("Fail when connecting to the ftp server.", e);
            }
            finally
            {
                uploadRequest.Abort();
            }
        }

        /// <summary>
        /// Creates a directory into the <see cref="FTPSettings.Url"/> using specified <paramref name="relativePath"/>.
        /// </summary>
        /// <param name="relativePath">Relative pah to <see cref="FTPSettings.Url"/></param>
        /// <returns>true if directory created, otherwise false</returns>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is null.</exception>
        /// <exception cref="WebException">Fail when connecting to the ftp server.</exception>
        public async Task<bool> CreateDirectoryAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentNullException(paramName: nameof(relativePath), message: "Caminho relativo não pode ser nulo.");

            var uri = new Uri(_settings.Url);

            var paths = relativePath.Split('/').ToList();
            paths.RemoveAll(string.IsNullOrWhiteSpace);

            try
            {
                string lastPath = string.Empty;
                for (int i = 0; i < paths.Count; i++)
                {
                    lastPath += paths[i] + "/";
                    var request = (FtpWebRequest)WebRequest.Create(new Uri(uri, lastPath));
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;

                    if (_settings.IsCredentialsAvailable)
                        request.Credentials = _credentials;

                    using (var response = await request.GetResponseAsync() as FtpWebResponse)
                    {
                        using (var responseStream = response.GetResponseStream()) { }
                    }

                    request.Abort();
                }
                return true;
            }
            catch (WebException e)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the specified <paramref name="url"/> exists.
        /// </summary>
        /// <param name="url">Url to the FTP.</param>
        /// <returns>true if exists, otherwise false</returns>
        /// <exception cref="TimeoutException">Connection timed out.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
        public async Task<bool> DirectoryExistsAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(paramName: nameof(url), message: $"{nameof(url)} não pode ser nula.");

            return await DirectoryExistsAsync(new Uri(url));
        }

        /// <summary>
        /// Check if the specified <paramref name="uri"/> exists.
        /// </summary>
        /// <param name="uri">Uri to the FTP.</param>
        /// <returns>true if exists, otherwise false</returns>
        /// <exception cref="TimeoutException">Connection timed out.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is null.</exception>
        public async Task<bool> DirectoryExistsAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(paramName: nameof(uri), message: $"{nameof(uri)} não pode ser nula.");

            var request = (FtpWebRequest)WebRequest.Create(uri);

            try
            {
                if (_settings.IsCredentialsAvailable)
                    request.Credentials = _credentials;
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                var response = await request.GetResponseAsync() as FtpWebResponse;

                return response != null;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch
            {
                return false;
            }
            finally
            {
                request.Abort();
            }
        }
    }
}
