using CoreDRXLibrary;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Models;
using CoreDRXLibrary.Providers;
using CoreDynamic.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoreDRXEditor.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DocumentSettingsView : ContentPage
	{
        private IServiceProvider serviceProvider;
        private FileEditorProvider fileEditorProvider;
        private EditorView editorView;
        private FlagSelectionView selectionView;

        private bool Creating = false;

		public DocumentSettingsView(IServiceProvider serviceProvider, FileEditorProvider fileEditorProvider = null, EditorView parentView = null)
		{
			InitializeComponent();

            if (fileEditorProvider == null)
            {
                Creating = true;

                IFileProvider newFile = new CoreDRXLibrary.Providers.FileProvider(serviceProvider);
                fileEditorProvider = new FileEditorProvider(serviceProvider, newFile);

                fileEditorProvider.Initialize();
                fileEditorProvider.Create();

                fileEditorProvider.MasterFile.IsValid = true;

                saveChangesButton.Text = "Create Document";
            }

            if (!fileEditorProvider.MasterFile.IsValid || (fileEditorProvider.MasterFile.EncryptionEnabled && !fileEditorProvider.MasterFile.HasBeenDecrypted))
                throw new Exception("Document either has not yet been loaded or it is still encrypted.");

            this.serviceProvider = serviceProvider;
            this.fileEditorProvider = fileEditorProvider;
            this.editorView = parentView;

            IFileProvider file = fileEditorProvider.MasterFile;

            drxId.Text = String.Format("DRX ID: {0}", file.Id.ToString());
            drxTitleEntry.Text = file.FriendlyName;

            settingEntry.Text = file.Setting;
            securityLevelPicker.SelectedItem = file.SecurityLevel.BuildDescription();
            saveChangesButton.Clicked += SaveChangesButton_Clicked;
            setFlagsButton.Clicked += SetFlagsButton_Clicked;
        }

        private void AddedFlags_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            fileEditorProvider.MasterFile.Flags = selectionView.addedFlags.ToList();
        }

        private void SetFlagsButton_Clicked(object sender, EventArgs e)
        {
            if (selectionView == null)
            {
                selectionView = new FlagSelectionView(serviceProvider, fileEditorProvider.MasterFile.Flags);
                selectionView.addedFlags.CollectionChanged += AddedFlags_CollectionChanged;
            }
            
            Navigation.PushModalAsync(this.selectionView);
        }
        
        private void SaveChangesButton_Clicked(object sender, EventArgs e)
        {
            IDatabaseProvider provider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            PleaseWaitPage pleaseWait = new PleaseWaitPage();
            pleaseWait.SetStatus("Saving document preferences.");
            Navigation.PushModalAsync(pleaseWait);

            Task.Run(async () =>
            {
                // Serialize values and load them back into the document
                fileEditorProvider.MasterFile.FriendlyName = drxTitleEntry.Text;
                fileEditorProvider.MasterFile.Setting = settingEntry.Text;
                fileEditorProvider.MasterFile.SecurityLevel = Classification.ParseFromDescriptive((string)securityLevelPicker.SelectedItem);
                if (editorView != null) editorView.UnsavedChanges = false;

                bool didNotError = await fileEditorProvider.Save();

                if (didNotError && Creating)
                {
                    await provider.UpdateAndSaveDatabase();
                }

                Device.BeginInvokeOnMainThread(async () => { await Navigation.PopModalAsync(); await Navigation.PopAsync(); });
            });
        }
    }
}