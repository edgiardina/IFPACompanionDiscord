using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFPACompanionDiscord.Extensions
{
    internal static class StringExtensions
    {
        public static string RemoveCharactersAfterLastOccurrence(this string s, char c)
        {
            int idx = s.LastIndexOf(c);

            if (idx != -1)
            {
                return s[..idx];
            }
            else return s;
        }

    }
}
