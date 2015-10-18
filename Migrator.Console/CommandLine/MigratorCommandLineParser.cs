using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Migrator.Console.CommandLine
{
    public class MigratorCommandLineParser<TModel> : ICommandLineParser<TModel> where TModel : new()
    {
        public TModel Parse(ICollection<string> args)
        {
            var model = new TModel();

            foreach (var attributedProperty in GetAttributedProperties())
            {
                ReadAndAssignProperty(model, args, attributedProperty);
            }

            return model;
        }

        private static void ReadAndAssignProperty(TModel model, IEnumerable<string> args, AttributedProperty attributedProperty)
        {
            var alias = attributedProperty.Attribute.Alias;
            var property = attributedProperty.Property;

            if (property.PropertyType.IsAssignableFrom(typeof (bool)))
            {
                property.SetValue(model, DoesAliasExist(args, alias), null);
            }
            else
            {
                string text;
                
                if (!TryGetAliasValue(args, alias, out text))
                {
                    return;
                }

                var value = ConvertStringToType(property.PropertyType, text);

                property.SetValue(model, value, null);
            }
        }

        private static IEnumerable<AttributedProperty> GetAttributedProperties()
        {
            return typeof (TModel)
                .GetProperties()
                .Select(p => new {Property = p, Attributes = p.GetCustomAttributes(typeof (CommandLineAliasAttribute), true)})
                .Where(p => p.Attributes.Length == 1)
                .Select(p => new AttributedProperty{ Property = p.Property, Attribute = (CommandLineAliasAttribute) p.Attributes.First()});
        }

        private static bool DoesAliasExist(IEnumerable<string> args, string alias)
        {
            return args.Any(a => ("/" + alias).Equals(a, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetAliasValue(IEnumerable<string> args, string alias, out string text)
        {
            var regex = new Regex("/" + Regex.Escape(alias) + "=(.+)", RegexOptions.IgnoreCase);

            var match = args.Select(arg => regex.Match(arg)).FirstOrDefault(m => m.Success);

            if (match == null)
            {
                text = null;
                return false;
            }

            text = match.Groups[1].Value;
            return true;
        }

        private static object ConvertStringToType(Type type, string text)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromInvariantString(text);
        }

        private class AttributedProperty
        {
            public PropertyInfo Property { get; set; }
            public CommandLineAliasAttribute Attribute { get; set; }
        }
    }
}