using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoreDRXEditor.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FlagSelectionView : ContentPage
	{
        private ObservableCollection<DocumentFlag> availableFlags = new ObservableCollection<DocumentFlag>();
        public ObservableCollection<DocumentFlag> addedFlags = new ObservableCollection<DocumentFlag>();
        private DocumentFlag selectedFlag;

        private IServiceProvider serviceProvider;
        public FlagSelectionView(IServiceProvider serviceProvider, IList<DocumentFlag> documentFlags)
		{
			InitializeComponent();
            this.serviceProvider = serviceProvider;

            availableFlagsList.ItemsSource = availableFlags;
            addedFlagsList.ItemsSource = addedFlags;

            IDatabaseProvider databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
            foreach (DocumentFlag f in databaseProvider.GetAllFlags()) availableFlags.Add(f);
            foreach (DocumentFlag f in documentFlags)
            {
                if (availableFlags.Contains(f))
                    availableFlags.Remove(f);

                addedFlags.Add(f);
            }
        }

        private void AddedFlagSelected(object sender, SelectedItemChangedEventArgs e)
        {
            selectedFlag = e.SelectedItem as DocumentFlag;
            if (selectedFlag == null)
                return;

            availableFlags.Add(selectedFlag);
            addedFlags.Remove(selectedFlag);
        }

        private void AvailableFlagSelected(object sender, SelectedItemChangedEventArgs e)
        {
            selectedFlag = e.SelectedItem as DocumentFlag;
            if (selectedFlag == null)
                return;

            addedFlags.Add(selectedFlag);
            availableFlags.Remove(selectedFlag);
        }
    }
}