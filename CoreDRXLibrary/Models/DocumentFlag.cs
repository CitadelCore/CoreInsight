using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace CoreDRXLibrary.Models
{
    public class DocumentFlag
    {
        public string FlagId { get; set; }
        public string Description { get; set; }
        public Classification SecurityLevel { get => SecurityLevelInt; set { SecurityLevelInt = value; } }
        private Classification SecurityLevelInt = Classification.Public;
        public Color FlagColour { get => FlagColourInt; set { FlagColourInt = value; } }
        public Color FlagColourInt = Color.Gray;
    }
}
