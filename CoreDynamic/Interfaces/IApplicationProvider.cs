using CoreDynamic.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreDynamic.Interfaces
{
    public delegate void AuthorizationStatusChangedEventHandler(object sender, EventArgs e);
    public delegate void ConnectionStatusChangedEventHandler(object sender, EventArgs e);
    public delegate void AuthorizationFinishedEventHandler(object sender, EventArgs e);
    public interface IApplicationProvider
    {
        /// <summary>
        /// Whether or not developer mode is enabled.
        /// </summary>
        bool DeveloperMode { get; set; }

        /// <summary>
        /// Whether or not we are using CoreServer.
        /// </summary>
        bool UseCoreServer { get; set; }

        /// <summary>
        /// Whether we're successfully authenticated to TOWER.
        /// </summary>
        bool ConnectedToCoreServer { get; set; }

        /// <summary>
        /// Whether we're successfully authenticated to TOWER.
        /// </summary>
        bool ConnectedToTowerNetwork { get; set; }

        /// <summary>
        /// This event is fired when the authorization status
        /// of the directory service is changed.
        /// </summary>
        event AuthorizationStatusChangedEventHandler AuthorizationStatusChanged;

        /// <summary>
        /// This event is fired when the CoreServer connection status
        /// is changed.
        /// </summary>
        event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// <summary>
        /// This event is fired when the authorization process is finished.
        /// It is fired regardless of whether the authorization was successful or not.
        /// </summary>
        event AuthorizationFinishedEventHandler AuthorizationFinished;

        /// <summary>
        /// The application friendly name.
        /// </summary>
        string FriendlyName { get; set; }

        IAdfsServiceProvider AdfsProvider { get; set; }
        
        /// <summary>
        /// Checks whether CoreServer can be reached or not.
        /// </summary>
        /// <returns>Returns true if CoreServer is online, otherwise false.</returns>
        bool CheckCoreServerOnline();

        /// <summary>
        /// Attempts to authenticate and get a token from the ADFS server,
        /// if if can be reached.
        /// </summary>
        Task AuthenticateToNetwork();

        /// <summary>
        /// Deathenticates from the ADFS server.
        /// This removes the token from the in-memory cache.
        /// </summary>
        void DeauthenticateFromNetwork();

        /// <summary>
        /// Logs out from the ADFS server.
        /// This removes the token from both the in-memory cache, and the disk token store.
        /// The user must re-login after this method is called.
        /// </summary>
        void LogoutFromNetwork();

        /// <summary>
        /// Nuke the fucker
        /// </summary>
        /// <returns>Whether that shit's done</returns>
        Task LockoutApplication();

        /// <summary>
        /// Enrolls an application certificate by first
        /// retrieving an escrow key from the KES and then
        /// passing it to CoreServer.
        /// </summary>
        /// <returns>Task result.</returns>
        Task EnrollApplicationCert();

        /// <summary>
        /// Unenrolls the application certificate:
        /// revokes the certificate, destroys local certificates
        /// and instructs CoreServer to destroy its certificates.
        /// </summary>
        /// <returns>Task result.</returns>
        Task UnenrollApplicationCert();
    }
}
