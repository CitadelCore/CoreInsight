using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDRXLibrary.Models
{
    public class Classification
    {
        public static readonly Classification Public = new Classification() { ClassificationId = "PLC", FriendlyName = "Public" };
        public static readonly Classification Confidential = new Classification() { ClassificationId = "CNF", FriendlyName = "Confidential" };
        public static readonly Classification Secret = new Classification() { ClassificationId = "SCT", FriendlyName = "Secret" };
        public static readonly Classification TopSecret = new Classification() { ClassificationId = "TSCT", FriendlyName = "Top Secret" };
        public static readonly Classification Thaumiel = new Classification() { ClassificationId = "THL", FriendlyName = "Thaumiel" };
        public static readonly Classification BlackGold = new Classification() { ClassificationId = "BG", FriendlyName = "BlackGold" };
        public static readonly Classification StormVault = new Classification() { ClassificationId = "SV", FriendlyName = "StormVault" };
        public static readonly Classification DITVault = new Classification() { ClassificationId = "DIT", FriendlyName = "DIT Vault" };

        public string ClassificationId;
        public string FriendlyName;

        public static Classification ParseFromId(string Id)
        {
            switch (Id)
            {
                case "PLC":
                    return Public;
                case "CNF":
                    return Confidential;
                case "SCT":
                    return Secret;
                case "TSCT":
                    return TopSecret;
                case "THL":
                    return Thaumiel;
                case "BG":
                    return BlackGold;
                case "SV":
                    return StormVault;
                case "DIT":
                    return DITVault;
                default:
                    throw new InvalidOperationException("Cannot convert to classification: invalid ID.");
            }
        }

        public static Classification ParseFromDescriptive(string Descriptive)
        {
            return ParseFromId(Descriptive.Split(' ')[0]);
        }

        public string BuildDescription()
        {
            return String.Format("{0} ({1})", ClassificationId, FriendlyName);
        }
    }
}
