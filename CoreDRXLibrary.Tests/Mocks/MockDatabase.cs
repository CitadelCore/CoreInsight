using CoreDRXLibrary.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CoreDRXLibrary.Tests.Mocks
{
    class MockDatabase : DatabaseProvider
    {
        public MockDatabase(IServiceCollection serviceCollection) : base(serviceCollection) { }

        public override Task LoadDatabase() { return Task.CompletedTask; }
        protected override Task SaveDatabase() { return Task.CompletedTask; }
        public override Task UpdateAndSaveDatabase() { return Task.CompletedTask; }

        public override bool GetDRXExists(int id)
        {
            if (id != 101)
                throw new InvalidOperationException();

            return true;
        }

        public override string GetDRXFilePath(int id)
        {
            if (id != 101)
                throw new InvalidOperationException();

            return null;
        }

        public override void CreateDRXEntry(int id, string filename, string type, string title)
        {
            if(id != 101)
                throw new InvalidOperationException();
        }

        public override void DeleteDRXEntry(int id)
        {
            if (id != 101)
                throw new InvalidOperationException();
        }

        public override int GetNextID(int series)
        {
            if (series != 100)
                throw new InvalidOperationException();

            return 102;
        }

        public override Task<FileEditorProvider> OpenDRXFromIDAsync(int id)
        {
            if (id != 101)
                throw new InvalidOperationException();

            FileEditorProvider provider = new FileEditorProvider(base.serviceCollection.BuildServiceProvider());
            return Task.Run(() =>
            {
                return provider;
            });
        }

    }
}
