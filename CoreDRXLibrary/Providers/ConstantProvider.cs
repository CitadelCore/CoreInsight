using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreDRXLibrary.Providers
{
    class ConstantProvider
    {
        public static Dictionary<string, string> Flags = new Dictionary<string, string>()
        {
            { "A (Anomaly)", "PLC" },
            { "AR (Arrest)", "SCT" },
            { "C (Cinematic)", "PLC" },
            { "CH (Chase)", "CNF" },
            { "CN (Country)", "PLC" },
            { "D (Drain)", "CNF" },
            { "DE (Death)", "CNF" },
            { "DM (Inf.Damage)", "SCT" },
            { "PDM (Prop.Damage)", "PLC" },
            { "DR (Dread)", "CNF" },
            { "DV (Deja Vu)", "PLC" },
            { "E (Exertus)", "THL" },
            { "EM (PersE)", "TSCT" },
            { "F (Fluid)", "PLC" },
            { "FDD (FD Drive)", "BG" },
            { "G (Game)", "PLC" },
            { "IC (Inception)", "PLC" },
            { "L (Lucid - Int.SA)", "CNF" },
            { "LUB (Lucid - Int.SB)", "CNF" },
            { "LUC (Lucid - Int.SC)", "CNF" },
            { "LUD (Lucid - Int.SD)", "CNF" },
            { "LE (Levitation)", "SCT" },
            { "M (Motor)", "PLC" },
            { "MA (Machine)", "PLC" },
            { "OOBE (OOB EX)", "PLC" },
            { "P (Portal)", "SCT" },
            { "PO (Pool)", "CNF" },
            { "PU (Pump)", "CNF" },
            { "I (Injury)", "TSCT" },
            { "IM (Impossible)", "CNF" },
            { "PZ (Puzzle)", "SCT" },
            { "R (RBT)", "THL" },
            { "RC (RealityCheck)", "PLC"},
            { "RE (Relational)", "PLC" },
            { "RTR (Reactor)", "PLC" },
            { "S (Scenario)", "CNF" },
            { "SC (School)", "PLC" },
            { "SH (SPRHMN)", "SCT" },
            { "SS (Scene Split)", "PLC" },
            { "SU (Surreal)", "PLC"},
            { "T (Transition)", "PLC" },
            { "TR (Transport)", "PLC" },
            { "U (Unrealistic)", "PLC" },
            { "X (Explicit)", "BG" }
        };

        public static Dictionary<string, int> SecurityNums = new Dictionary<string, int>()
        {
            { "PLC", 0 },
            { "CNF", 1 },
            { "SCT", 2 },
            { "TSCT", 3 },
            { "THL", 4 },
            { "BG", 5 },
            { "SV", 6 },
            { "DIT", 7 },
        };
    }
}
