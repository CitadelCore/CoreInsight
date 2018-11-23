using CoreDRXEditor.Controls;
using CoreDRXEditor.UWP.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(RichTextEditorControl), typeof(RichTextEditorRenderer))]
namespace CoreDRXEditor.UWP.Renderers
{
    public delegate Task DocumentModifiedEventHandler(object sender, EventArgs e);
    class RichTextEditorRenderer : ViewRenderer<RichTextEditorControl, RichEditBox>
    {
        private RichTextEditorControl currentControl = null;
        public string RtfDocument { get {
                Control.Document.GetText(TextGetOptions.FormatRtf, out string rtfText);
                return rtfText;
            } set {
                Control.Document.SetText(TextSetOptions.FormatRtf, value);
            } }

        public RichTextEditorRenderer() {
            DocumentModified += RichTextEditorRenderer_DocumentModified;
        }

        private Task RichTextEditorRenderer_DocumentModified(object sender, EventArgs e)
        {
            currentControl.InvokeDocumentModified(RtfDocument);
            return Task.CompletedTask;
        }

        public event DocumentModifiedEventHandler DocumentModified;

        protected override void OnElementChanged(ElementChangedEventArgs<RichTextEditorControl> e)
        {
            if (e.NewElement == null)
                return;

            base.OnElementChanged(e);
            currentControl = e.NewElement;

            if (Control == null)
            {
                RichEditBox richTextEditor = new RichEditBox();
                SetNativeControl(richTextEditor);

                richTextEditor.TextChanged += RichTextEditor_TextChanged;
            }

            if (e.NewElement.RtfDocument != null)
                RtfDocument = e.NewElement.RtfDocument;
        }

        private void RichTextEditor_TextChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DocumentModified?.Invoke(this, EventArgs.Empty);
        }

        public void SetFont(Font font)
        {
            Control.FontSize = font.FontSize;
            Control.FontStyle = (FontStyle) font.FontAttributes;
        }
    }
}
