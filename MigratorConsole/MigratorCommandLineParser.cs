using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MigratorConsole
{
    public class MigratorCommandLineParser
    {
        public T Parse<T>(ICollection<string> args) where T : new()
        {
            var obj = new T();

            var items = typeof (T).GetProperties()
                .Select(p => new {Property = p, Attributes = p.GetCustomAttributes(typeof (CommandLineAliasAttribute), true)})
                .Where(p => p.Attributes.Length == 1)
                .Select(p => new {p.Property, Attribute = (CommandLineAliasAttribute) p.Attributes.First()});

            foreach (var item in items)
            {
                string alias = item.Attribute.Alias;
                var property = item.Property;

                if (property.PropertyType.IsAssignableFrom(typeof (bool)))
                {
                    var found = args.Any(a => ("/" + alias).Equals(a, StringComparison.OrdinalIgnoreCase));
                    property.SetValue(obj, found, null);
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(property.PropertyType);

                    var escapedPropertyName = Regex.Escape(alias);

                    var regex = new Regex("/" + escapedPropertyName + "=(.+)", RegexOptions.IgnoreCase);

                    var match = args.Select(arg => regex.Match(arg)).FirstOrDefault(m => m.Success);

                    if (match == null)
                    {
                        continue;
                    }

                    var value = converter.ConvertFromInvariantString(match.Groups[1].Value);

                    property.SetValue(obj, value, null);
                }
            }

            return obj;
        }
    }
}