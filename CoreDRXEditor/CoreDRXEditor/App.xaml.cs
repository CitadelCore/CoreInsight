using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace CoreDRXEditor
{
	public partial class App : Application
	{
        HomePage page;
        public App()
		{
			InitializeComponent();

            page = new HomePage();
            MainPage = new NavigationPage(page);
        }

		protected override void OnStart()
		{
            // Handle when your app starts
            page.Loaded();
        }

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
