using CoreDRXEditor.Controls;
using CoreDRXLibrary.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoreDRXEditor.Views
{

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditorView : ContentPage
	{
        public bool UnsavedChanges = false;
        private bool Saving = false;

        private FileEditorProvider DocumentProvider;
        private IServiceProvider serviceProvider;
        private FileSelectionView selectionView;

        public ICommand GoBackCommand { get; private set; }
        public ICommand SaveDocumentCommand { get; private set; }
        public ICommand OptionsCommand { get; private set; }

        public EditorView(IServiceProvider serviceProvider, FileSelectionView parentView, FileEditorProvider fileEditorProvider)
		{
			InitializeComponent();

            selectionView = parentView;
            this.serviceProvider = serviceProvider;

            DocumentProvider = fileEditorProvider;
            editorControl.LoadDocument(serviceProvider, fileEditorProvider);
            editorControl.DocumentModified += EditorControl_DocumentModified;
            UnsavedChanges = false;

            backButton.Pressed += BackButton_Pressed;
            saveButton.Pressed += SaveButton_Pressed;
            optionsButton.Pressed += OptionsButton_Pressed;
		}

        private async void OptionsButton_Pressed(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DocumentSettingsView(serviceProvider, DocumentProvider, this));
        }

        private async void SaveButton_Pressed(object sender, EventArgs e)
        {
            Saving = true;
            await DocumentProvider.Save();
            Saving = false;
            UnsavedChanges = false;
        }

        private void BackButton_Pressed(object sender, EventArgs e)
        {
            OnBackButtonPressed();
        }

        private Task EditorControl_DocumentModified(object sender, string bodyText)
        {
            DocumentProvider.MasterFile.SetBodyContents(bodyText);

            UnsavedChanges = true;
            return Task.CompletedTask;
        }

        protected override bool OnBackButtonPressed()
        {
            if (Saving)
                return true;

            if (UnsavedChanges)
                Saving = true;

            Device.BeginInvokeOnMainThread(async () =>
            {
                if (UnsavedChanges)
                {
                    bool actionResult = await DisplayAlert("Unsaved changes", "Would you like to save your changes?", "Yes", "No");

                    if (actionResult)
                    {
                        await DocumentProvider.Save();
                        Saving = false;
                    }
                }

                await selectionView.RefreshView();

                if (Navigation.NavigationStack.Count > 0)
                    await Navigation.PopAsync();
            });

            return true;
        }
    }
}