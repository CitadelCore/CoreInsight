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
	public partial class PleaseWaitPage : ContentPage
	{
		public PleaseWaitPage()
		{
			InitializeComponent();
		}

        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
	}
}