using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using CoreDRXEditor.Controls;
using CoreDRXEditor.iOS.Renderers;

[assembly: ExportRenderer(typeof(RichTextEditorControl), typeof(RichTextEditorRenderer))]
namespace CoreDRXEditor.iOS.Renderers
{
    class RichTextEditorRenderer : ViewRenderer
    {
        public RichTextEditorRenderer() { }
    }
}