using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace CoreDRXLibrary.Controllers
{
    class BackupController
    {
        public static void BackupDatabase()
        {
            string DocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            //ZipFile.CreateFromDirectory(DatabaseProvider.Provider.UserAppdata + @"\DRX", FileDialog.FileName, CompressionLevel.Optimal, false);
        }

        public static void RestoreDatabase()
        {
            string DocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
