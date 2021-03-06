﻿using System.Text;

namespace NetTopologySuite.Encodings
{
    public static class EncodingEx
    {
        private static readonly IEncodingRegistry EncodingRegistry = new EncodingRegistry();

        public static Encoding GetASCII()
        {
            return EncodingRegistry.ASCII;
        }

        /*
        private static Func<Encoding, int> GetAccessor()
        {
            var pex = Expression.Parameter(typeof(Encoding));
            MemberInfo member =
                typeof(Encoding).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic |
                                               BindingFlags.GetProperty).First(a => a.Name == "CodePage");

            return Expression.Lambda<Func<Encoding, int>>(
                Expression.MakeMemberAccess(pex, member), pex
                ).Compile();
        }
        */

        public static int CodePage(this Encoding self)
        {
            if (self is UTF8Encoding)
                return 65001;

            return EncodingRegistry.GetCodePage(self);
            //return Accessor(self);
        }
    }
}