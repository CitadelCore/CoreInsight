using CoreDRXLibrary.Interfaces;
using CoreDynamic.Interfaces;
using CoreDynamic.Providers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreDRXLibrary.Providers
{
    class MasterProvider
    {
        private IApplicationProvider Provider;
        private IDatabaseProvider Database;

        public MasterProvider(IServiceCollection collection, string[] args)
        {
            IServiceProvider serviceProvider = collection.BuildServiceProvider();
            this.Provider = serviceProvider.GetRequiredService<IApplicationProvider>();
            this.Database = serviceProvider.GetRequiredService<IDatabaseProvider>();

            if (this.Database.IsValid == true)
            {
                //CoreToastClient ctc = new CoreToastClient(this.Provider);
                //ctc.ShowMessage("DRX Editor has finished loading the secure store.");

                if (args.Length > 0)
                {
                    List<string> argc = new List<string>();

                    foreach (string argb in args)
                    {
                        string[] split = argb.Split(' ');
                        foreach (string s in split)
                        {
                            argc.Add(s);
                        }
                    }

                    args = argc.ToArray();
                    string arg;

                    int i = 0;
                    foreach (string argv in args)
                    {
                        arg = argv.Replace("/", "");

                        switch (arg)
                        {
                            case "id":
                                try
                                {
                                    int drxid = Convert.ToInt32(args[i + 1]);
                                    Database.OpenDRXFromIDAsync(drxid);
                                }
                                catch (Exception)
                                {
                                    // TODO: log something here
                                }

                                break;
                            case "open":
                            case "edit":
                                try
                                {

                                    int drxid = Convert.ToInt32(args[i + 1].Split(new string[] { "DRX_" }, StringSplitOptions.None)[1].Split(new string[] { ".drx" }, StringSplitOptions.None)[0]);
                                    Database.OpenDRXFromIDAsync(drxid);
                                }
                                catch (Exception)
                                {
                                    // TODO: log something here
                                }
                                break;
                            default:
                                break;
                        }

                        i++;
                    }
                }
            }
        }
    }
}
