using CoreDRXEditor.Controls;
using CoreDRXEditor.iOS.Renderers;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(RichTextEditorControl), typeof(RichTextEditorRenderer))]
namespace CoreDRXEditor.iOS.Renderers
{
    class RichTextEditorRenderer : ViewRenderer<RichTextEditorControl, UITextView>
    {
        public RichTextEditorRenderer() { }

        protected override void OnElementChanged(ElementChangedEventArgs<RichTextEditorControl> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                UITextView textView = new UITextView();
                SetNativeControl(textView);
            }
        }
    }
}
