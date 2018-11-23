using CoreDRXLibrary.Models;
using CoreDRXLibrary.Providers;
using CoreDynamic.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CoreDRXLibrary.Interfaces
{
    public enum DatabaseLocation
    {
        Filesystem,
        CoreServer,
        None
    }

    public delegate void DatabaseLoadedEventHandler(object sender, EventArgs e);
    public delegate Task FileLoadingStatusCallback(string loadingStatus);

    public interface IDatabaseProvider
    {
        /// <summary>
        /// Whether the database is valid.
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// The file location of the database.
        /// </summary>
        DatabaseLocation Location { get; set; }

        /// <summary>
        /// Gets whether the DRX at the specified ID
        /// actually exists in the database.
        /// This does not check the filesystem for the actual DRX file!
        /// </summary>
        /// <param name="id">The ID of the DRX.</param>
        /// <returns>Whether the DRX exists or not.</returns>
        bool GetDRXExists(int id);

        /// <summary>
        /// Retrieves the file path of a DRX. Will return null if the DRX does not exist.
        /// </summary>
        /// <param name="id">The ID of the DRX.</param>
        /// <returns>The exact file path to the DRX.</returns>
        string GetDRXFilePath(int id);

        /// <summary>
        /// Creates a new DRX entry in the database.
        /// </summary>
        /// <param name="id">The ID of the DRX.</param>
        /// <param name="filename">The file path of the DRX.</param>
        /// <param name="type">The type of the DRX.</param>
        /// <param name="title">The title of the DRX.</param>
        void CreateDRXEntry(int id, string filename, string type, string title);
        Task LoadDatabase(IServiceProvider serviceProvider);

        /// <summary>
        /// Deletes a DRX entry from the database.
        /// This should be pretty much NEVER USED unless the DRX is the last in the series. It will leave a blank space in the database.
        /// </summary>
        /// <param name="id">The ID of the DRX to delete.</param>
        void DeleteDRXEntry(int id);

        /// <summary>
        /// Retrieves the next available ID in the database to be filled with a DRX ID number,
        /// in the series specified. Throws an Exception if the series is full.
        /// </summary>
        /// <param name="Series"></param>
        /// <returns>The next available ID in the series.</returns>
        int GetNextID(int Series);

        /// <summary>
        /// Updates the database XML schema,
        /// performs a bit of formatting, and then calls SaveDatabase().
        /// </summary>
        /// <returns>A Task.</returns>
        Task UpdateAndSaveDatabase();

        /// <summary>
        /// Opens a DRX document and returns a FileEditorProvider after it's been retrieved.
        /// Attempts to retrieve from both CoreServer and the filesystem.
        /// </summary>
        /// <param name="id">The ID of the DRX to open.</param>
        /// <returns>A FileEditorProvider object wrapped in a Task.</returns>
        Task<FileEditorProvider> OpenDRXFromIDAsync(int id, FileLoadingStatusCallback callback = null);

        /// <summary>
        /// Returns a Dictionary of DRX documents in the database with the ID and title.
        /// </summary>
        /// <returns>A Dictionary with the DRX IDs and titles.</returns>
        Dictionary<int, string> GetDRXTitles();

        /// <summary>
        /// Gets the body hash of a specified DRX ID in the database.
        /// </summary>
        /// <param name="id">The DRX ID to get the hash of.</param>
        /// <returns>The DRX hash.</returns>
        string GetDRXBodyHash(int id);

        /// <summary>
        /// Sets the body hash of a specified DRX ID in the database.
        /// </summary>
        /// <param name="id">The DRX ID to set the hash of.</param>
        /// <param name="hash">The DRX hash.</param>
        void SetDRXBodyHash(int id, string hash);

        /// <summary>
        /// Retrieves all the series in the database.
        /// </summary>
        /// <returns>An IList with the integer series for all DRX series.</returns>
        IList<int> GetAllSeriesNumbers();

        IList<DocumentFlag> GetAllFlags();

        DocumentFlag GetFlagFromId(string id);

        void SaveFlag(DocumentFlag flag);

        /// <summary>
        /// This event is called when the database has finished loading,
        /// either from CoreServer or the filesystem.
        /// </summary>
        event DatabaseLoadedEventHandler DatabaseLoaded;
    }
}
