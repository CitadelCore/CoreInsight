using CoreDynamic.Interfaces;
using CoreDynamic.Providers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoreDynamic.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SignInPage : ContentPage
    {
        IServiceCollection serviceCollection;
		public SignInPage(IServiceCollection serviceCollection)
		{
            this.serviceCollection = serviceCollection;
			InitializeComponent();

            signInTowerButton.Clicked += SignInTowerButton_Clicked;
		}

        private void SignInTowerButton_Clicked(object sender, EventArgs e)
        {
            AttemptSignIn();
        }

        private void AttemptSignIn()
        {
            IApplicationProvider provider = serviceCollection.BuildServiceProvider().GetRequiredService<IApplicationProvider>();

            Device.BeginInvokeOnMainThread(async () => {
                try { await provider.AuthenticateToNetwork(); }
                catch (AdfsServiceException) { }
            });
        }
	}
}