using System;

namespace JosephM.Xrm.Autonumber.Core
{
    public static class GuidExtentions
    {
        public static string ToMatchString(this Guid guid)
        {
            var toString = guid.ToString().ToLower();
            if (!toString.StartsWith("{"))
                toString = "{" + toString + "}";
            return toString;
        }
    }
}