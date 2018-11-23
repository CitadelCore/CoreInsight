using CoreDRXLibrary.Providers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CoreDRXEditor.Controls
{
    public delegate Task DocumentModifiedEventHandler(object sender, string bodyText);
    public class RichTextEditorControl : View
    {
        private FileEditorProvider FileEditorProvider;

        public event DocumentModifiedEventHandler DocumentModified;
        public string RtfDocument = null;

        public void LoadDocument(IServiceProvider serviceProvider, FileEditorProvider fileEditorProvider)
        {
            FileEditorProvider = fileEditorProvider;
            RtfDocument = fileEditorProvider.MasterFile.GetBodyContents();
        }

        public void InvokeDocumentModified(string bodyText)
        {
            DocumentModified.Invoke(this, bodyText);
        }
    }
}
