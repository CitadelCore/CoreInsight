using CoreDRXEditor.Views;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Providers;
using CoreDynamic.Interfaces;
using CoreDynamic.Providers;
using CoreDynamic.Views;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CoreDRXEditor
{
	public partial class HomePage : ContentPage
	{
        private IServiceProvider serviceProvider;
        private IDatabaseProvider databaseProvider;
        private PleaseWaitPage pleaseWait;

        public HomePage()
        {
            InitializeComponent();
        }

        public void Loaded()
        {
            Task.Run(() =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();

                IPlatformParameters PlatformSpecificParameters = null;

                // Conditional selection of the ADAL platform parameters depending on the application platform.
                // This will be injected into the CoreDynamic library code via DI.
                
#if __MOBILE__
#if __IOS__ || __TVOS__ || __WATCHOS__
            PlatformSpecificParameters = new PlatformParameters(UIKit.UIApplication.SharedApplication.KeyWindow.RootViewController, false, PromptBehavior.Auto);
#endif

#if __ANDROID__
            PlatformSpecificParameters = new PlatformParameters((Android.App.Activity) Forms.Context, false, PromptBehavior.Auto);
#endif
#else
                PlatformSpecificParameters = new PlatformParameters(PromptBehavior.Auto, false);
#endif

                serviceCollection.Add(new ServiceDescriptor(typeof(IPlatformParameters), PlatformSpecificParameters));
                serviceCollection.Add(new ServiceDescriptor(typeof(ILog), LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)));
                serviceCollection.Add(new ServiceDescriptor(typeof(IAdfsServiceProvider), p => new AdfsServiceProvider(serviceCollection), ServiceLifetime.Singleton));
                serviceCollection.Add(new ServiceDescriptor(typeof(IApplicationProvider), p => new ApplicationProvider(serviceCollection, "CoreDRXEditor", "DRX Editor", true), ServiceLifetime.Singleton));
                serviceCollection.Add(new ServiceDescriptor(typeof(IDatabaseProvider), p => new DatabaseProvider(), ServiceLifetime.Singleton));

                serviceProvider = serviceCollection.BuildServiceProvider();

                IApplicationProvider provider = serviceProvider.GetRequiredService<IApplicationProvider>();
                databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
                databaseProvider.DatabaseLoaded += DatabaseProvider_DatabaseLoaded;
                provider.AuthorizationFinished += Provider_AuthorizationFinished;
                openDocumentButton.Clicked += OpenDocumentButton_Clicked;
                newDocument.Clicked += NewDocument_Clicked;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    pleaseWait = new PleaseWaitPage();
                    await Navigation.PushModalAsync(pleaseWait);
                    pleaseWait.SetStatus("Waiting for server authentication to succeed.");

                    await Task.Run(async () =>
                    {
                        try
                        {
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                await provider.AuthenticateToNetwork();
                            });   
                        }
                        catch (AdfsServiceException e)
                        {
                            pleaseWait.SetStatus("Authentication error occurred.");
                            await DisplayAlert("Authentication error", "An authentication error occurred while contacting the ADFS service. Please ensure you are connected to the TOWER internal network either directly or over a VPN. Error: " + e.Message, "Continue without CoreServer");
                        }
                    });
                });
            });
        }

        private async void OpenDocumentButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FileSelectionView(serviceProvider));
        }

        private async void NewDocument_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DocumentSettingsView(serviceProvider));
        }

        private void DatabaseProvider_DatabaseLoaded(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopModalAsync();
            });
        }

        private void Provider_AuthorizationFinished(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                pleaseWait.SetStatus("Downloading the database from CoreServer.");
            });

            Task.Run(() =>
            {
                databaseProvider.LoadDatabase(serviceProvider);
            });
        }
    }
}
