using System;

namespace MigratorConsole
{
    public static class StringExtensions
    {
        public static string Truncate(this string subject, int maxLength)
        {
            if (subject == null)
            {
                throw new ArgumentNullException("subject");
            }

            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException("maxLength");
            }

            return subject.Length <= maxLength ? subject : subject.Substring(0, maxLength);
        }

        public static string FormatWith(this string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");    
            }

            return string.Format(format, args);
        }
    }
}
