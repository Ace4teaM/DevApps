using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevApps.Features
{
    internal static class Track
    {
        internal static string? elementName;
        public static void BeginTrackElement(string name)
        {
            elementName = name;
        }
        public static void EndTrackElement()
        {
            elementName = null;
        }
    }
}
