using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CoreDRXEditor
{
    public class DocumentTemplateGroup : ObservableCollection<DocumentTemplate>
    {
        public string Title;
        public string ShortName;
        public DocumentTemplateGroup(string title, string shortName)
        {
            Title = title;
            ShortName = shortName;
        }
    }

    public class DocumentTemplate
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public int Id { get; set; }
    }
}
