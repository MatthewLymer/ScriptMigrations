using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MigratorConsole.CommandLine
{
    public class MigratorCommandLineParser<TModel> : ICommandLineParser<TModel> where TModel : new()
    {
        public TModel Parse(ICollection<string> args)
        {
            var model = new TModel();

            foreach (var attributedProperty in GetAttributedProperties())
            {
                var alias = attributedProperty.Attribute.Alias;
                var property = attributedProperty.Property;

                if (property.PropertyType.IsAssignableFrom(typeof (bool)))
                {
                    var found = args.Any(a => ("/" + alias).Equals(a, StringComparison.OrdinalIgnoreCase));
                    property.SetValue(model, found, null);
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

                    property.SetValue(model, value, null);
                }
            }

            return model;
        }

        private static IEnumerable<AttributedProperty> GetAttributedProperties()
        {
            var items = typeof (TModel)
                .GetProperties()
                .Select(p => new {Property = p, Attributes = p.GetCustomAttributes(typeof (CommandLineAliasAttribute), true)})
                .Where(p => p.Attributes.Length == 1)
                .Select(p => new AttributedProperty{ Property = p.Property, Attribute = (CommandLineAliasAttribute) p.Attributes.First()});

            return items;
        }

        private class AttributedProperty
        {
            public PropertyInfo Property { get; set; }
            public CommandLineAliasAttribute Attribute { get; set; }
        }
    }
}