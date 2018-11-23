using CoreDRXLibrary;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Providers;
using CoreDynamic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
namespace CoreDRXEditor.Views
{
    public delegate Task OnDecryptedEventHandler(FileEditorProvider fileEditorProvider);

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DecryptionView : ContentPage
	{
        private IServiceProvider serviceProvider;
        private FileEditorProvider fileEditorProvider;
        private TimeSpan timeRemaining = TimeSpan.FromMinutes(1);
        private bool stopTimer = false;
        private bool timerPaused = false;
        private bool allowBackButton = false;
        public event OnDecryptedEventHandler OnDecrypted;

        private int PasswordAttemptsRemaining = 3;

		public DecryptionView(IServiceProvider serviceProvider, FileEditorProvider fileEditorProvider)
		{
			InitializeComponent();
            this.serviceProvider = serviceProvider;
            this.fileEditorProvider = fileEditorProvider;

            DecryptButton.Pressed += DecryptButton_Pressed;
            AbortButton.Pressed += AbortButton_Pressed;
            SecurityText.Text = String.Format("Security Restriction Level: {0}", fileEditorProvider.MasterFile.SecurityLevel.BuildDescription());

            Device.StartTimer(TimeSpan.FromMilliseconds(0.1), () =>
            {
                if (stopTimer == true) return false;

                if (!timerPaused)
                    timeRemaining = timeRemaining - TimeSpan.FromMilliseconds(10);

                if (timeRemaining.Ticks <= 0)
                {
                    Device.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("Decryption timed out", "Decryption for this file has timed out. This attempt has been logged. You will be returned to the file list.", "Abort");

                        if (Navigation.ModalStack.Count > 0)
                            await Navigation.PopModalAsync();
                    });

                    return false;
                }

                WarningText.Text = String.Format("If you click Abort, the decryption will be cancelled. The attempt will be reported. Decryption will time out in {0}.", timeRemaining.ToString(@"mm\:ss\:ff"));
                return true;
            });

            if (fileEditorProvider.MasterFile.FileEncryptionType == EncryptionType.Certificate)
            {
                pageLayout.Children.Remove(PasswordEntry);
                InstructionText.Text = "Click Decrypt to authenticate and decrypt the document.";
            }
		}

        private void AbortButton_Pressed(object sender, EventArgs e)
        {
            if (!timerPaused)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    allowBackButton = true;
                    OnBackButtonPressed();

                    await DisplayAlert("Decryption aborted", "Decryption for this document has been aborted. This attempt has been logged.", "Continue");
                });
            }
        }

        private async void DecryptButton_Pressed(object sender, EventArgs args)
        {
            if (fileEditorProvider.MasterFile.FileEncryptionType != EncryptionType.Certificate && String.IsNullOrEmpty(PasswordEntry.Text))
            {
                await DisplayAlert("Password not entered", "Please enter your password.", "Back");
                return;
            }

            if (fileEditorProvider.MasterFile.FileEncryptionType == EncryptionType.Certificate)
            {
                X509Certificate2 RevocationCheck = EncryptionProvider.GetCertificateFromSerial(fileEditorProvider.MasterFile.EncryptionSerial);
                if (RevocationCheck != null && RevocationCheck.Verify() == false)
                {
                    await DisplayAlert("Certificate alert", "The certificate being used has either been revoked, has expired or is not yet valid. Per policy, this document will be opened read-only. If the certificate has been revoked or has expired, please change the certificate immediately in Document Settings otherwise you will not be able to save your changes.", "Continue read-only");
                }
            }

            await Task.Run(() =>
            {
                try
                {
                    if (fileEditorProvider.MasterFile.FileEncryptionType == EncryptionType.Password)
                        fileEditorProvider.MasterFile.DecryptBodyPassword(PasswordEntry.Text);

                    if (fileEditorProvider.MasterFile.FileEncryptionType == EncryptionType.Certificate)
                    {
                        timerPaused = true;
                        fileEditorProvider.MasterFile.DecryptBodyCertificate();
                        timerPaused = false;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "File hash does not match. Decryption error or incorrect password?")
                    {
                        PasswordAttemptsRemaining--;

                        if (PasswordAttemptsRemaining <= 0)
                        {
                            IApplicationProvider applicationProvider = (IApplicationProvider) serviceProvider.GetService(typeof(IApplicationProvider));
                            applicationProvider.LockoutApplication();

                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                await Navigation.PushModalAsync(new LockoutView("crypto_threshold"));
                            });

                            return;
                        }
                        else
                        {
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                await DisplayAlert("Invalid decryption key", String.Format("An invalid password or decryption key was attempted. {0} attempts remaining until all local data is wiped. This attempt has been logged and will be reported.", PasswordAttemptsRemaining.ToString()), "Back");
                            });

                            return;
                        }
                    }

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("An error occurred", String.Format("An internal decryption error occured. Exception: {0}", e.Message), "Back");
                    });

                    timerPaused = false;
                    return;
                }

                stopTimer = true;
                timerPaused = true;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopModalAsync();
                    OnDecrypted?.Invoke(fileEditorProvider);
                });
            });
        }

        protected override bool OnBackButtonPressed()
        {
            if (!allowBackButton)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Security restriction", "Decryption is currently in progress. Use the Abort button to abort decryption.", "Back");
                });

                return true;
            }

            stopTimer = true;
            timerPaused = true;

            return base.OnBackButtonPressed();
        }
    }
}