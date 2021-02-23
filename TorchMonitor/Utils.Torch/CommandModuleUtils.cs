using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Views;
using Utils.General;
using VRage.Game.ModAPI;
using VRageMath;

namespace Utils.Torch
{
    public static class CommandModuleUtils
    {
        public static void CatchAndReport(this CommandModule self, Action f)
        {
            try
            {
                f();
            }
            catch (Exception e)
            {
                ReportGenerator.LogAndRespond(self, e, m => self.Context.Respond(m, Color.Red));
            }
        }

        public static async void CatchAndReport(this CommandModule self, Func<Task> f)
        {
            try
            {
                await f();
            }
            catch (Exception e)
            {
                ReportGenerator.LogAndRespond(self, e, m => self.Context.Respond(m, Color.Red));
            }
        }

        public static void ShowCommands(this CommandModule self)
        {
            var level = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var commands = self.GetCommandMethods(level).ToArray();
            if (!commands.Any())
            {
                self.Context.Respond("No accessible commands found");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Commands:");
            foreach (var command in commands)
            {
                var name = command.Name;
                var description = command.Description.OrNull() ?? "no description";
                msgBuilder.AppendLine($"{name} -- {description}");
            }

            self.Context.Respond(msgBuilder.ToString());
        }

        static IEnumerable<CommandAttribute> GetCommandMethods(this CommandModule self, MyPromoteLevel maxLevel)
        {
            foreach (var method in self.GetType().GetMethods())
            {
                if (!method.TryGetAttribute<CommandAttribute>(out var command)) continue;
                if (!method.TryGetAttribute<PermissionAttribute>(out var permission)) continue;
                if (permission.PromoteLevel > maxLevel) continue;
                yield return command;
            }
        }

        public static void GetOrSetProperty(this CommandModule self, object config)
        {
            if (!self.Context.Args.TryGetFirst(out var propertyNameOrIndex))
            {
                self.ShowConfigurableProperties(config);
                return;
            }

            if (propertyNameOrIndex == "all")
            {
                self.ShowConfigurablePropertyValues(config);
                return;
            }

            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();

            var propertyName = propertyNameOrIndex;
            if (int.TryParse(propertyNameOrIndex, out var propertyIndex))
            {
                var maxPropertyIndex = properties.Length - 1;
                if (maxPropertyIndex < propertyIndex)
                {
                    self.Context.Respond($"Index out of bounds; max: {maxPropertyIndex}", Color.Red);
                    self.ShowConfigurableProperties(config);
                    return;
                }

                propertyName = properties[propertyIndex].Name;
            }

            if (!properties.TryGetFirst(p => p.Name == propertyName, out var property))
            {
                self.Context.Respond($"Property not found: \"{propertyName}\"", Color.Red);
                self.ShowConfigurableProperties(config);
                return;
            }

            if (property.TryGetAttribute(out ConfigPropertyAttribute prop) &&
                !prop.IsVisibleTo(promoLevel))
            {
                self.Context.Respond($"Property not visible: \"{propertyName}\"", Color.Red);
                self.ShowConfigurableProperties(config);
                return;
            }

            if (promoLevel == MyPromoteLevel.Admin &&
                self.Context.Args.TryGetElementAt(1, out var arg))
            {
                var newValue = ParsePrimitive(property.PropertyType, arg);
                property.SetValue(config, newValue);
            }

            var value = property.GetValue(config);
            self.Context.Respond($"> {propertyName}: {value}");
        }

        static void ShowConfigurablePropertyValues(this CommandModule self, object config)
        {
            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();
            if (!properties.Any())
            {
                self.Context.Respond("No configurable properties");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Config properties:");
            foreach (var property in properties)
            {
                var name = property.Name;
                var value = property.GetValue(config);
                msgBuilder.AppendLine($"> {name}: {value}");
            }

            msgBuilder.AppendLine("To update, either `config <index> <value>` or `config <name> <value>`.");

            self.Context.Respond(msgBuilder.ToString());
        }

        static void ShowConfigurableProperties(this CommandModule self, object config)
        {
            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();
            if (!properties.Any())
            {
                self.Context.Respond("No configurable properties");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Config properties:");
            foreach (var (property, index) in properties.Indexed())
            {
                var name = "no name";
                var description = "no description";

                if (property.TryGetAttribute<DisplayAttribute>(out var display))
                {
                    name = display.Name.OrNull() ?? name;
                    description = display.Description.OrNull() ?? description;
                }

                msgBuilder.AppendLine($"> {index} {property.Name} -- {name}; {description}");
            }

            msgBuilder.AppendLine("> all -- Show the value of all configurable properties");

            self.Context.Respond(msgBuilder.ToString());
        }

        static IEnumerable<PropertyInfo> GetConfigurableProperties(object config, MyPromoteLevel promoLevel)
        {
            var properties = config.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (!IsParseablePrimitive(property.PropertyType)) continue;
                if (property.GetSetMethod() == null) continue;
                if (property.HasAttribute<ConfigPropertyIgnoreAttribute>()) continue;

                if (property.TryGetAttribute(out ConfigPropertyAttribute prop))
                {
                    if (!prop.IsVisibleTo(promoLevel)) continue;
                }

                yield return property;
            }
        }

        static bool IsParseablePrimitive(Type type)
        {
            if (type == typeof(string)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(ulong)) return true;
            return false;
        }

        static object ParsePrimitive(Type type, string value)
        {
            if (type == typeof(string)) return value;
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value);
            if (type == typeof(double)) return double.Parse(value);
            if (type == typeof(long)) return long.Parse(value);
            if (type == typeof(ulong)) return ulong.Parse(value);
            throw new ArgumentException($"unsupported type: {type}");
        }
    }
}