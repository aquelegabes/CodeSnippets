using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Namespace
{
    /// <summary>
    /// Abstract class to generate a <see cref="IFileHandler"/> accordingly.
    /// </summary>
    public abstract class FileHandlerFactory
    {
        /// <summary>
        /// Get a concrete <see cref="IFileHandler"/>.
        /// </summary>
        /// <param name="handler">Type of handler.</param>
        /// <returns>An implementation of <see cref="IFileHandler"/></returns>
        public abstract IFileHandler GetHandler(Type handler);
        /// <summary>
        /// Get a concrete <see cref="IFileHandler"/>.
        /// </summary>
        /// <typeparam name="THandler">Type of handler.</typeparam>
        /// <returns>An implementation of <see cref="IFileHandler"/></returns>
        public abstract IFileHandler GetHandler<THandler>();
        /// <summary>
        /// Get a concrete <see cref="IFileHandler"/>.
        /// </summary>
        /// <paramref name="enumType">Type of handler.</paramref>
        /// <returns>An implementation of <see cref="IFileHandler"/></returns>
        public abstract IFileHandler GetHandler(Enumeradores.FileHandlerType enumType);
    }

    public class ConcreteFileHandlerFactory : FileHandlerFactory
    {
        public const string Ftp = "ftp://";
        public const string Ftps = "ftps://";
        public const string Sftp = "sftp://";

        private readonly Dictionary<Type, IFileHandler> Handlers = new Dictionary<Type, IFileHandler>
        {
            { typeof(FtpHandler), new FtpHandler() },
            { typeof(GoogleDriveHandler), new GoogleDriveHandler() },
            { typeof(SftpHandler), new SftpHandler()}
            // TODO: needs checking
            // { typeof(OneDriveHandler), new OneDriveHandler() }
        };

        private readonly Dictionary<Enumeradores.FileHandlerType, IFileHandler> EnumHandlers = new Dictionary<Enumeradores.FileHandlerType, IFileHandler>
        {
            [Enumeradores.FileHandlerType.FTP] = new FtpHandler(),
            [Enumeradores.FileHandlerType.GoogleDrive] = new GoogleDriveHandler(),
            [Enumeradores.FileHandlerType.SFTP] = new SftpHandler(),
            // TODO: needs checking
            // [Enumeradores.FileHandlerType.OneDrive] = new OneDriveHandler(),
        };

        /// <summary>
        /// Load settings based on a <see cref="Models.FileHandlerInfo"/>.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="sp">Service provider.</param>
        /// <returns>An object with its settings.</returns>
        public static object SettingsLoader(
            Models.FileHandlerInfo info, IServiceProvider sp)
        {
            object settings;
            switch (info.Type)
            {
                case Enum.Enumeradores.FileHandlerType.GoogleDrive:
                    settings = new GoogleSettings
                    {
                        ConnectionToken = info.ConnectionToken,
                        Secrets = sp.GetService<Google.Apis.Auth.OAuth2.ClientSecrets>()
                    };
                    break;
                case Enum.Enumeradores.FileHandlerType.SFTP:
                case Enum.Enumeradores.FileHandlerType.FTP:
                    settings = new FtpSettings
                    {
                        Url = info.Host,
                        User = info.User,
                        Password = info.Password,
                        Port = info.Port
                    };
                    break;

                // TODO: waiting for one drive file handler.
                case Enum.Enumeradores.FileHandlerType.OneDrive:
                default:
                    throw new InvalidDataException("Handler type not available.");
            }
            return settings;
        }

        /// <inheritdoc />
        public override IFileHandler GetHandler(Enumeradores.FileHandlerType enumType)
        {
            try
            {
                return EnumHandlers[enumType];
            }
            catch
            {
                throw new ArgumentException(message: "Invalid handler type.", paramName: nameof(enumType));
            }
        }

        /// <inheritdoc />
        public override IFileHandler GetHandler(Type handler)
        {
            try
            {
                return Handlers[handler];
            }
            catch
            {
                throw new ArgumentException(message: "Invalid handler type.", paramName: nameof(handler));
            }
        }

        /// <inheritdoc />
        public override IFileHandler GetHandler<THandler>()
        {
            return GetHandler(typeof(THandler));
        }
    }
}
