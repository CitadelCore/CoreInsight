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
	public partial class LockoutView : ContentPage
	{
		public LockoutView(string lockoutCause)
		{
			InitializeComponent();

            // Convert the cause to a hexadecimal string.
            lockoutId.Text = String.Format("Security lockout ID: {0}", BitConverter.ToString(Encoding.Default.GetBytes(lockoutCause)).Replace("-", String.Empty));
		}

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}