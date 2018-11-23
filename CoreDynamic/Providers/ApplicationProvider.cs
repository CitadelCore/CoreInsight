//using log4net;
//using log4net.Config;
using CoreDynamic.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
//using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using log4net;
using System.Net.Sockets;
using PCLStorage;
using System.Security.Cryptography;
using CoreDynamic.Utils;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;

namespace CoreDynamic.Providers
{
    

    /// <summary>
    /// Class which handles all negotiation between CoreService and the application using this framework.
    /// </summary>
    public class ApplicationProvider : IApplicationProvider
    {
        protected ILog Logger;

        public bool DeveloperMode { get => DeveloperModeInt; set { DeveloperModeInt = value; } }
        private bool DeveloperModeInt = false;

        public bool UseCoreServer { get => UseCoreServerInt; set { UseCoreServerInt = value; } }
        private bool UseCoreServerInt = false;

        private bool ConnectedToCoreServerInt = false;
        private bool ConnectedToTowerNetworkInt = false;

        public bool ConnectedToCoreServer
        {
            get { return ConnectedToCoreServerInt; }
            set { if (ConnectedToCoreServerInt != value) { ConnectedToCoreServerInt = value; ConnectionStatusChanged?.Invoke(this, EventArgs.Empty); } }
        }

        public bool ConnectedToTowerNetwork
        {
            get { return ConnectedToTowerNetworkInt; }
            set { if (ConnectedToTowerNetworkInt != value) { ConnectedToTowerNetworkInt = value; AuthorizationStatusChanged?.Invoke(this, EventArgs.Empty); } }
        }

        public event AuthorizationStatusChangedEventHandler AuthorizationStatusChanged;
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;
        public event AuthorizationFinishedEventHandler AuthorizationFinished;

        public string FriendlyName { get => FriendlyNameInt; set { FriendlyNameInt = value; } }
        private string FriendlyNameInt;

        public IAdfsServiceProvider AdfsProvider { get => AdfsProviderInt; set { AdfsProviderInt = value; } }
        private IAdfsServiceProvider AdfsProviderInt;

        /// <summary>
        /// Base application provider class.
        /// This is used for managing services that are required
        /// by applications using the CoreDynamic framework.
        /// </summary>
        /// <param name="serviceCollection">Service collection for DI.</param>
        /// <param name="applicationName">The short name of the application.</param>
        /// <param name="friendlyName">Friendly name of the application.</param>
        /// <param name="useCoreServer">Whether or not to use CoreServer.</param>
        public ApplicationProvider(IServiceCollection serviceCollection, string applicationName, string friendlyName, bool useCoreServer = false)
        {
            this.FriendlyName = friendlyName;
            this.UseCoreServer = useCoreServer;

            IServiceProvider Provider = serviceCollection.BuildServiceProvider();
            Logger = Provider.GetRequiredService<ILog>();

            Logger.Debug("Initializing application.");
#if !PUBLIC
            if (useCoreServer == true)
            {
                AdfsProvider = Provider.GetRequiredService<IAdfsServiceProvider>();
            }
#endif
        }

        public bool CheckCoreServerOnline()
        {
            Logger.Debug("Checking application connectivity to CoreServer.");

            try
            {
                CoreServerRequest csr = new CoreServerRequest("Tests", AdfsProvider);
                Task.Run(async () => { await csr.SendRequestAsync(); }).Wait();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Could NOT successfully connect to CoreServer! Error: " + e.Message, e);
                return false;
            }
        }

        public async Task AuthenticateToNetwork()
        {
            if (!ConnectedToTowerNetwork)
            {
                try
                {
                    try
                    {
                        Dns.GetHostEntry("coreserver.core");
                    }
                    catch (SocketException e)
                    {
                        Logger.Debug("No need to try authenticating to TOWER, we're not connected to the intranet.", e);
                        AuthorizationFinished?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    Logger.Debug("Attempting to get authorization token from TOWER ADFS.");
                    await AdfsProvider.GetAuthorizationToken();
                    Logger.Debug("Got authorization token successfully.");

                    ConnectedToTowerNetwork = true;

                    if (CheckCoreServerOnline()) { ConnectedToCoreServer = true; } else { ConnectedToCoreServer = false; }
                }
                catch (Exception e)
                {
                    ConnectedToCoreServer = false;

                    if (e.InnerException != null)
                    {
                        Logger.Error("Could NOT authenticate to TOWER: " + e.InnerException.Message, e.InnerException);

                        AuthorizationFinished?.Invoke(this, EventArgs.Empty);
                        throw new AdfsServiceException(e.InnerException.Message, e.InnerException);
                    }
                    else
                    {
                        Logger.Error("Could NOT authenticate to TOWER: " + e.Message, e);

                        AuthorizationFinished?.Invoke(this, EventArgs.Empty);
                        throw new AdfsServiceException(e.Message, e);
                    }
                }
            }

            AuthorizationFinished?.Invoke(this, EventArgs.Empty);
        }

        public void DeauthenticateFromNetwork()
        {
            Logger.Debug("Disconnecting from TOWER and revoking the current authorization token.");
            ConnectedToCoreServer = false;
            ConnectedToTowerNetwork = false;
            AdfsProvider.Disconnect();
        }

        public void LogoutFromNetwork()
        {
            Logger.Debug("Disconnecting from TOWER and removing login credentials from the local cache.");
            if (ConnectedToCoreServer)
            {
                ConnectedToCoreServer = false;
                ConnectedToTowerNetwork = false;
                AdfsProvider.Logout();

                return;
            }
        }

        public async Task LockoutApplication()
        {
            LogoutFromNetwork();

            IFolder applicationFolder = FileSystem.Current.LocalStorage;
            foreach (IFile file in await applicationFolder.GetFilesAsync())
            {
                try { await file.DeleteAsync(); } catch (Exception) { }
            }

            foreach (IFolder folder in await applicationFolder.GetFoldersAsync())
            {
                try { await folder.DeleteAsync(); } catch (Exception) { }
            }
        }

        public Task EnrollApplicationCert()
        {
            // Generate the certificates.
            string issuerDn = "E=security@towerdevs.xyz, C=GB, O=TOWER, OU=Security Dept, S=Scotland, L=Stirling, STREET=3 Airthrey Castle Yard University of Stirling";
            string certDn = "CN={0}, " + issuerDn;

            // Generate temporary cert
            string tempDn = String.Format(certDn, "Temporary Certificate");
            X509Certificate2 temporaryCert = CertificateUtilities.GenerateSignedX509Certificate(tempDn, issuerDn, DateTime.Now.AddMinutes(5), 2048, null, "Temporary Certificate", new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment), new ExtendedKeyUsage(KeyPurposeID.IdKPClientAuth));

            // Generate client cert
            string clientDn = String.Format(certDn, "CoreDynamic Client Enrollment Certificate");
            X509Certificate2 clientCert = CertificateUtilities.GenerateSignedX509Certificate(tempDn, issuerDn, DateTime.Now.AddYears(2), 4096, null, "CoreDynamic Client Enrollment Certificate", new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment), new ExtendedKeyUsage(KeyPurposeID.IdKPClientAuth));

            throw new NotImplementedException();
        }

        public Task UnenrollApplicationCert()
        {
            throw new NotImplementedException();
        }
    }
}
