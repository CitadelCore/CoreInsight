using CoreDRXLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreDRXLibrary.Providers
{
    /// <summary>
    /// Class for interaction between the GUI interface and the DRX document provider itself.
    /// </summary>
    public class FileEditorProvider
    {
        /// <summary>
        /// The DRX file instance.
        /// </summary>
        public IFileProvider MasterFile;

        /// <summary>
        /// Whether the document has loaded successfully.
        /// </summary>
        public bool IsReady = false;

        private IServiceProvider serviceProvider;

        private IDatabaseProvider databaseProvider;

        /// <summary>
        /// Initializes a new file editor instance.
        /// </summary>
        /// <param name="MasterFile">The master file to use.</param>
        /// <param name="MasterDatabase">The database provider to use.</param>
        public FileEditorProvider(IServiceProvider serviceProvider, IFileProvider masterFile)
        {
            this.serviceProvider = serviceProvider;
            MasterFile = masterFile;

            databaseProvider = this.serviceProvider.GetRequiredService<IDatabaseProvider>();
        }

        public FileEditorProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            MasterFile = new FileProvider(serviceProvider);

            databaseProvider = this.serviceProvider.GetRequiredService<IDatabaseProvider>();
        }

        /// <summary>
        /// Loads a DRX document.
        /// </summary>
        /// <returns>Whether the load was successful.</returns>
        public bool Load()
        {
            if (MasterFile.LoadDocument())
            {
                string fileHash = MasterFile.GetMetadata()["BodyHash"];
                string databaseHash = databaseProvider.GetDRXBodyHash(MasterFile.Id);

                if (fileHash != null && databaseHash != null)
                {
                    if (fileHash != databaseHash)
                    {
                        throw new Exception("Warning! The hash of this DRX does not match the hash stored in the database. This could be due to having an out of sync database, or opening someone else's files.");
                        //DialogResult dr = MessageBox.Show("The hash of this DRX does not match the hash stored in the database. This could be due to having an out of sync database, or opening someone else's files. Abort loading?", strings.DRXEditor, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        //if (dr == DialogResult.Yes) IsReady = false; return false;
                    }
                }
                else if (fileHash != null && databaseHash == null)
                {
                    databaseProvider.SetDRXBodyHash(MasterFile.Id, fileHash);
                    Task.Run(async () => { await databaseProvider.UpdateAndSaveDatabase(); });
                }

                IsReady = true;
                return true;
            }
            else
            {
                IsReady = false;
                return false;
            }
        }

        /// <summary>
        /// Initializes a new document.
        /// </summary>
        /// <returns>Whether initialization was successful.</returns>
        public bool Initialize()
        {
            if (MasterFile.InitializeDocument())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes an existing document.
        /// </summary>
        /// <returns>Whether deletion was successful.</returns>
        public async Task Delete(int id = -1)
        {
            await MasterFile.DeleteDocument();

            if (id != -1)
            {
                databaseProvider.DeleteDRXEntry(id);
            }
            else
            {
                databaseProvider.DeleteDRXEntry(MasterFile.Id);
            }

            await databaseProvider.UpdateAndSaveDatabase();
        }

        /// <summary>
        /// Creates a new document.
        /// </summary>
        /// <returns>Whether creation was successful.</returns>
        public bool Create()
        {
            if (MasterFile.UpdateDocument(true))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Updates an existing document with new changes.
        /// </summary>
        /// <returns>Whether the update was successful.</returns>
        public bool Update(bool Create = false)
        {
            if (MasterFile.UpdateDocument(Create))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Refreshes a document.
        /// </summary>
        /// <returns>Whether the refresh was successful.</returns>
        public bool Refresh()
        {
            if (MasterFile.LoadDocument())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the document to disk (with encryption if neccessary).
        /// </summary>
        /// <returns>Whether the save was successful.</returns>
        public async Task<bool> Save()
        {
            if (await MasterFile.SaveChanges() == true)
            {
                databaseProvider.SetDRXBodyHash(MasterFile.Id, MasterFile.BodyHash);

                // Add an entry to the database, if it dosen't exist.
                if (databaseProvider.GetDRXExists(MasterFile.Id) == false)
                {
                    databaseProvider.CreateDRXEntry(MasterFile.Id, MasterFile.CurrentFilePath, "DRX", MasterFile.FriendlyName);
                }

                await databaseProvider.UpdateAndSaveDatabase();

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
