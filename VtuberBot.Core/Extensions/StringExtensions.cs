using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace VtuberBot.Core.Extensions
{
    public static class StringExtensions
    {
        public static string UrlEncode(this string value)
        {
            var limit = 32760;
            var sb = new StringBuilder();
            var loops = value.Length / limit;
            for (var i = 0; i <= loops; i++)
            {
                sb.Append(i < loops
                    ? Uri.EscapeDataString(value.Substring(limit * i, limit))
                    : Uri.EscapeDataString(value.Substring(limit * i)));
            }

            return sb.ToString();
        }
        public static string RandomString
        {
            get
            {
                var b = new byte[4];
                new RNGCryptoServiceProvider().GetBytes(b);
                var r = new Random(BitConverter.ToInt32(b, 0));
                var ret = string.Empty;
                var str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                for (var i = 0; i < 48; i++)
                {
                    ret += str.Substring(r.Next(0, str.Length - 1), 1);
                }

                return ret;
            }
        }

        public static bool EqualsIgnoreCase(this string text, string text2)
        {
            return string.Equals(text, text2, StringComparison.CurrentCultureIgnoreCase);
        }

        public static IEnumerable<Cookie> ToCookies(this string cookie, string domain)
        {
            return from item in cookie.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                let kv = item.Split('=')
                let name = kv.FirstOrDefault().Trim()
                let value = kv.Length > 1 ? kv.LastOrDefault() : string.Empty
                select new Cookie(name, value) { Domain = domain };
        }

        public static CookieContainer ToCookieContainer(this string cookie, string domain)
        {
            var result = new CookieContainer();
            foreach (var sub in cookie.ToCookies(domain).Reverse())
                result.Add(sub);
            return result;
        }

        public static string ToMd5(this string text)
        {
            return BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
        }
        public static string GetFileSizeString(this long size)
        {
            if (size < 1024) return size + "B";
            if (size < 1024 * 1024) return (size / 1024D).ToString("f2") + "KB";
            if (size < 1024 * 1024 * 1024) return (size / 1024D / 1024D).ToString("f2") + "MB";
            return (size / 1024D / 1024D / 1024D).ToString("f2") + "GB";
        }

    }
}
