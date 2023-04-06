using System;

namespace Morix
{
    public static class Ensure
    {
        public static void NotNull(object obj, string argumentName)
        {
            if (obj == null)
                throw new ArgumentNullException(argumentName);
        }
    }
}