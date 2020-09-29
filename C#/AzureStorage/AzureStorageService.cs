#region
// Requires package:
// <PackageReference Include="Azure.Storage.Blobs" Version="12.6.0" />
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Namespace
{
    /// <summary>
    /// Class responsible for handling storage related operations.
    /// </summary>
    public sealed class AzureStorageService
    {
        private readonly BlobServiceClient serviceClient;
        private readonly Dictionary<string, AzureContainer> containersClient =
            new Dictionary<string, AzureContainer>();

        /// <summary>
        /// Indexed property to manage container operations.
        /// </summary>
        /// <value>An AzureContainer with a specified <paramref name="containerName"/>.</value>
        /// <exception cref="ArgumentNullException"><paramref name="containerName"/> is null.</exception>
        public AzureContainer this[string containerName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(containerName))
                    throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

                return containersClient[containerName];
            }
            set
            {
                if (string.IsNullOrWhiteSpace(containerName))
                    throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

                containersClient[containerName] = value;
            }
        }

        public string StorageName { get; }

        /// <summary>
        /// Constructor responsible for obtaining an <see cref="AzureStorageOptions"/> from DI.
        /// </summary>
        /// <param name="options">Azure storage options.</param>
        public AzureStorageService(AzureStorageOptions options)
        {
            this.serviceClient = new BlobServiceClient(options.ConnectionString);
            this.StorageName = options.StorageName;

            // add existing containers into indexed property
            foreach (var containerItem in serviceClient.GetBlobContainers())
            {
                this[containerItem.Name] = new AzureContainer(serviceClient.GetBlobContainerClient(containerItem.Name));
            }
        }

        /// <summary>
        /// Try to get a container with specified name.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="container">Container to be outputted to.</param>
        /// <returns>true if container is obtained, otherwise false.</returns>
        public bool TryGetContainer(
            string containerName, out AzureContainer container)
        {
            try
            {
                container = this[containerName];
                return true;
            }
            catch
            {
                container = null;
                return false;
            }
        }

        /// <summary>
        /// Asynchronously creates a named container.
        /// </summary>
        /// <remarks>Returns a created unique name derived from <paramref name="containerName"/> and returns it.</remarks>
        /// <param name="containerName">Container name.</param>
        /// <returns>An unique generated container name.</returns>
        public async Task<AzureContainer> CreateContainerAsync(
            string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

            var uniqueName = $"{containerName}_{Guid.NewGuid():N}";
            this[uniqueName] = new AzureContainer(await serviceClient.CreateBlobContainerAsync(uniqueName));

            return this[uniqueName];
        }

        /// <summary>
        /// Create a named container.
        /// </summary>
        /// <remarks>Returns a created unique name derived from <paramref name="containerName"/> and returns it.</remarks>
        /// <param name="containerName">Container name.</param>
        /// <returns>An unique generated container name.</returns>
        public AzureContainer CreateContainer(
            string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

            var uniqueName = $"{containerName}_{Guid.NewGuid():N}";
            this[uniqueName] = new AzureContainer(serviceClient.CreateBlobContainer(containerName));

            return this[uniqueName];
        }

        /// <summary>
        /// Asynchronously deletes an existing container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <returns>true if deleted, otherwise false.</returns>
        public async Task<bool> DeleteContainerAsync(
            string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

            var response = await serviceClient.DeleteBlobContainerAsync(containerName);

            return response.Status == 200;
        }

        /// <summary>
        /// Delete an existing container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <returns>true if deleted, otherwise false.</returns>
        public bool DeleteContainer(
            string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException(paramName: nameof(containerName), message: "Container name must not be null.");

            var response = serviceClient.DeleteBlobContainer(containerName);
            return response.Status == 200;
        }

        /// <summary>
        /// List all available containers names.
        /// </summary>
        /// <returns>A collection of container names.</returns>
        public IEnumerable<string> ListContainersNames() =>
            this.containersClient.Select(c => c.Key);
    }
}
