using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDRXLibrary
{
    public class ParserUtilities
    {
        public static string FlagToNonDescriptive(string Flag)
        {
            return Flag.Split(' ')[0];
        }

        public static string SecurityFlagToNonDescriptive(string Flag)
        {
            return Flag.Split(' ')[0];
        }

        public static int IdToSeries(int id)
        {
            return Convert.ToInt32(Convert.ToString(id).ToCharArray()[0] + "00");
        }

        public static int SecurityFlagToIndex(string Flag)
        {
            switch (Flag)
            {
                case @"PLC":
                    return 0;
                case @"CNF":
                    return 1;
                case @"SCT":
                    return 2;
                case @"TSCT":
                    return 3;
                case @"THL":
                    return 4;
                case @"BG":
                    return 5;
                case @"SV":
                    return 6;
                case @"DIT":
                    return 7;
                default:
                    throw new Exception("Invalid parameter!");
            }
        }

        public static int SeriesFlagToIndex(string Flag)
        {
            switch (Flag)
            {
                case @"0":
                    return 0;
                case @"100":
                    return 1;
                case @"200":
                    return 2;
                case @"300":
                    return 3;
                case @"400":
                    return 4;
                case @"500":
                    return 5;
                case @"600":
                    return 6;
                case @"700":
                    return 7;
                case @"800":
                    return 8;
                case @"900":
                    return 9;
                default:
                    throw new Exception("Invalid parameter!");
            }
        }

        public static string VRELToString(int v, int r, int e, int l)
        {
            StringBuilder sb = new StringBuilder();
            if (v == -1) { sb.Append("?" + "-"); } else { sb.Append(v.ToString() + "-"); }
            if (r == -1) { sb.Append("?" + "-"); } else { sb.Append(r.ToString() + "-"); }
            if (e == -1) { sb.Append("?" + "-"); } else { sb.Append(e.ToString() + "-"); }
            if (l == -1) { sb.Append("?"); } else { sb.Append(l.ToString()); }

            return sb.ToString();
        }
    }
}
