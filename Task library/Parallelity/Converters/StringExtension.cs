using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parallelity.Converters
{
    public static class StringExtension
    {
        public static String ToCapital(this String text)
        {
            return new String(text
                .Select((c, i) => i == 0 ? char.ToUpper(c) : c)
                .ToArray());
        }
    }
}
