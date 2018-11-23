using CoreDRXLibrary;
using CoreDRXLibrary.Controllers;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Models;
using CoreDRXLibrary.Providers;
using CoreDynamic.Interfaces;
using CoreDynamic.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoreDRXEditor.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FileSelectionView : ContentPage
	{
        private IServiceProvider serviceProvider;
        private IDatabaseProvider databaseProvider;
        private IApplicationProvider applicationProvider;

        private ObservableCollection<DocumentTemplate> fileProviderGroups = new ObservableCollection<DocumentTemplate>();

		public FileSelectionView(IServiceProvider serviceProvider)
		{
			InitializeComponent();
            documentList.ItemsSource = fileProviderGroups;

            this.serviceProvider = serviceProvider;

            databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
            applicationProvider = serviceProvider.GetRequiredService<IApplicationProvider>();
            

            Task.Run(async () => {
                await RefreshView();
                await HandleLaunchHooks();
            }); 
		}

        private async Task HandleLaunchHooks() {

            try
            {

#if __WINDOWS__
                if (UWP.App.launchUri != null && UWP.App.launchUri.Scheme == "drx")
                {
                    Uri launchUri = UWP.App.launchUri;
                    string serverName = launchUri.Authority;
                    string drxPath = launchUri.PathAndQuery;

                    int series = Convert.ToInt32(drxPath.Split("/")[0]);
                    int singular = Convert.ToInt32(drxPath.Split("/")[1]);

                    int fullId = series + singular;

                    foreach (DocumentTemplate template in fileProviderGroups) {
                        if (template.Id == fullId) {
                            documentList.SelectedItem = template;
                            return;
                        }
                    }

                    await DisplayAlert("No such document", "Couldn't find DRX " + fullId.ToString() + " in the database.", "Continue");
                    return;
                }
#endif

            }
            catch (Exception e) {
                await DisplayAlert("Error parsing launch hooks", "Couldn't parse the launch hooks: " + e.Message, "Continue");
            }
        }

        public async Task RefreshView()
        {
            IDictionary<int, string> fileList = databaseProvider.GetDRXTitles();

            if (databaseProvider.Location == DatabaseLocation.CoreServer && applicationProvider.ConnectedToCoreServer)
            {
                await LoadFromCoreServer(fileList);
            }
            else
            {
                await LoadFromFileSystem(fileList);
            }
        }

        private void RefreshList(IDictionary<int, IDictionary<string, dynamic>> fileData)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                fileProviderGroups.Clear();

                foreach (KeyValuePair<int, IDictionary<string, dynamic>> document in fileData)
                {
                    Classification sec = Classification.ParseFromId(document.Value["SecurityLevel"]);
                    fileProviderGroups.Add(new DocumentTemplate() { Title = document.Value["FriendlyName"], Id = document.Key, Description = "ID: " + document.Key.ToString() + ". Security level: " + sec.BuildDescription() + "." });
                }
            });

            /**
            IList<int> AllSeries = databaseProvider.GetAllSeriesNumbers();

            foreach (int series in AllSeries)
            {
                DocumentTemplateGroup templateGroup = new DocumentTemplateGroup(String.Format("Series {0}", series.ToString() + "00"), series.ToString());

                foreach (KeyValuePair<int, IDictionary<string, dynamic>> document in fileData.Where((obj) => { return Convert.ToInt32(obj.Key.ToString().ToCharArray()[0].ToString()) == series; })) {
                    templateGroup.Add(new DocumentTemplate() { Title = document.Value["FriendlyName"], Id = document.Key, Description = "ID: " + document.Key.ToString() + ". Security level: " + DRXBaseClass.SecurityFlagToDescriptive(document.Value["SecurityLevel"]) + "." });
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    fileProviderGroups.Add(templateGroup);
                });
            }*/
        }
        
        private async Task LoadFromCoreServer(IDictionary<int, string> fileList)
        {
            IDictionary<int, IDictionary<string, dynamic>> fileData = new Dictionary<int, IDictionary<string, dynamic>>();

            try
            {
                RemoteDRXController controller = new RemoteDRXController(serviceProvider);
                fileData = await controller.GetMetadata(fileList.Keys.ToList());
            }
            catch (Exception)
            {
                await LoadFromFileSystem(fileList);
                return;
            }

            RefreshList(fileData);
        }

        private async Task LoadFromFileSystem(IDictionary<int, string> fileList)
        {
            IDictionary<int, IDictionary<string, dynamic>> fileData = new Dictionary<int, IDictionary<string, dynamic>>();

            foreach (KeyValuePair<int, string> File in fileList)
            {
                try
                {
                    RemoteDRXController controller = new RemoteDRXController(serviceProvider);
                    IFileProvider documentFile = await controller.GetFile(File.Key);
                    Dictionary<string, dynamic> res = documentFile.GetMetadata();

                    fileData.Add(File.Key, res);

                    documentFile.Dispose();
                } catch (Exception) { }
            }

            RefreshList(fileData);
        }

        private async void DocumentSelectionChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            // Deselect the item so it dosen't have to be reselected
            // if the user chooses to go back to the list after they quit the editor
            ((ListView)sender).SelectedItem = null;

            DocumentTemplate item = (DocumentTemplate) e.SelectedItem;

            PleaseWaitPage pleaseWait = new PleaseWaitPage();
            await Navigation.PushModalAsync(pleaseWait);

            pleaseWait.SetStatus("Downloading the document.");

            await Task.Run(async () => {
                FileEditorProvider provider = await databaseProvider.OpenDRXFromIDAsync(item.Id, (message) =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopModalAsync();
                        await DisplayAlert("Document load error", message, "Continue");
                    });

                    return Task.CompletedTask;
                });

                if (provider == null)
                    return;

                Device.BeginInvokeOnMainThread(() => { pleaseWait.SetStatus("Loading the document."); });
                provider.Load();

                Device.BeginInvokeOnMainThread(async () => {
                    if (provider.MasterFile.EncryptionEnabled)
                    {
                        DecryptionView decryptionView = new DecryptionView(serviceProvider, provider);
                        decryptionView.OnDecrypted += DecryptionView_OnDecrypted;

                        if (Navigation.ModalStack.Count > 0)
                            await Navigation.PopModalAsync();

                        await Navigation.PushModalAsync(decryptionView);

                        return;
                    }

                    await Navigation.PopModalAsync();
                    await Navigation.PushAsync(new EditorView(serviceProvider, this, provider));
                });
            });
        }

        private async Task DecryptionView_OnDecrypted(FileEditorProvider fileEditorProvider)
        {
            await Navigation.PushAsync(new EditorView(serviceProvider, this, fileEditorProvider));
        }

        private async Task DeleteButton_Clicked(object sender, EventArgs e)
        {
            if (await DisplayAlert("Delete document?", "Are you sure you want to delete this document? Backups can be retained in CoreServer for up to 30 days.", "Continue", "Abort") == false)
                return;

            int id = ((DocumentTemplate)((MenuItem)sender).CommandParameter).Id;

            if (id != (databaseProvider.GetNextID(ParserUtilities.IdToSeries(id)) - 1))
            {
                await DisplayAlert("Cannot delete DRX", "This DRX cannot be deleted because it is not the last in the series.", "Continue");
                return;
            }

            try
            {
                PleaseWaitPage pleaseWait = new PleaseWaitPage();
                pleaseWait.SetStatus("Deleting the document.");
                await Navigation.PushModalAsync(pleaseWait);

                FileEditorProvider editor = await databaseProvider.OpenDRXFromIDAsync(id);
                editor.Load();
                await editor.Delete();
                
                await RefreshView();
            }
            catch (Exception em)
            {
                await DisplayAlert("Document deletion error", em.Message, "Continue");
            }

            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        }
    }
}