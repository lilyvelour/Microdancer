using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public abstract class CommandBase : IDisposable
    {
        private readonly CommandManager _commandManager;
        private readonly ChatGui _chatGui;
        protected Dictionary<string, CommandInfo> CommandInfo { get; } = new();

        private bool _disposedValue;

        public CommandBase()
        {
            _commandManager = CustomService.Get<CommandManager>();
            _chatGui = CustomService.Get<ChatGui>();

            CommandInfo = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommands)
                .ToDictionary(t => t.Item1, t => t.Item2);

            foreach (var (command, commandInfo) in CommandInfo)
            {
                _commandManager.AddHandler(command, commandInfo);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                foreach (var (command, _) in CommandInfo)
                {
                    _commandManager.RemoveHandler(command);
                }
            }

            _disposedValue = true;
        }

        protected CommandInfo GetCommandInfo(string cmd)
        {
            if (!cmd.StartsWith("/"))
            {
                cmd = $"/{cmd}";
            }

            return CommandInfo[cmd];
        }

        protected void Print(string cmd, string msg)
        {
            if (!cmd.StartsWith("/"))
            {
                cmd = $"/{cmd}";
            }

            _chatGui.Print($"{cmd}: {msg}");
        }

        protected void PrintError(string cmd, string msg)
        {
            _chatGui.PrintError($"{cmd}: {msg}");
        }

        private IEnumerable<(string, CommandInfo)> GetCommands(MethodInfo method)
        {
            var command = method.GetCustomAttribute<CommandAttribute>();
            if (command == null)
            {
                yield break;
            }

            var cmd = command.Command;
            if (!cmd.StartsWith("/"))
            {
                cmd = $"/{cmd}";
            }

            var aliases = command.Aliases?.Select(alias =>
            {
                if (!alias.StartsWith("/"))
                {
                    alias = $"/{alias}";
                }

                return alias;
            }) ?? Array.Empty<string>();

            var parameter = method.GetParameters().FirstOrDefault();
            var commandInfo = new CommandInfo(GetHandler(cmd, command, method))
            {
                HelpMessage = command.HelpMessage ?? string.Empty,
                ShowInHelp = command.ShowInHelp,
            };

            yield return (cmd, commandInfo);

            foreach(var alias in aliases)
            {
                yield return (alias, new CommandInfo(GetHandler(alias, command, method))
                {
                    HelpMessage = $"Alias for {cmd}.",
                    ShowInHelp = command.ShowInHelp,
                });
            }
        }

        private CommandInfo.HandlerDelegate GetHandler(string cmd, CommandAttribute command, MethodInfo method)
        {
            return new CommandInfo.HandlerDelegate((_, args) =>
            {
                var parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    ExecCommand(method);
                    return;
                }

                if (command.Raw)
                {
                    ExecCommand(method, new[] { args });
                    return;
                }

                var argsArray = Regex.Matches((args ?? string.Empty).Trim(), @"[\""].+?[\""]|[^ ]+")
                    .Cast<Match>()
                    .Select(x => x.Value.Trim('"'))
                    .ToArray();

                if (parameters.Length == 1 && typeof(IEnumerable<string>).IsAssignableFrom(parameters[0].ParameterType))
                {
                    ExecCommand(method, new object[] { argsArray });
                    return;
                }

                if (argsArray.Length < parameters.Where(p => !p.HasDefaultValue).Count())
                {
                    PrintError(
                        cmd,
                        $"Invalid parameter count. Command must have at least {parameters.Length} parameter(s).");
                }

                var convertedParams = new List<object?>();

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;

                    if (i < argsArray.Length)
                    {
                        var arg = argsArray[i];

                        try
                        {
                            convertedParams.Add(ChangeType(arg, parameterType));
                        }
                        catch (Exception e)
                        {
                            PrintError(
                                cmd,
                                $"Invalid parameter type at position {i + 1} ({arg})."
                                + $" Type must be '{parameterType.Name}'\n{e.Message}."
                            );
                        }
                    }
                    else
                    {
                        convertedParams.Add(parameters[i].DefaultValue);
                    }
                }

                ExecCommand(method, convertedParams.ToArray());
            });
        }

        private void ExecCommand(MethodInfo method, object?[]? parameters = null)
        {
            if (method.ReturnType == typeof(Task))
            {
                Task.Run(() => (Task)method.Invoke(this, parameters)!);
            }
            else
            {
                method.Invoke(this, parameters);
            }
        }

        private static object? ChangeType(string arg, Type conversion)
        {
            object value =
                arg.ToLowerInvariant() == "on" ? true
                : arg.ToLowerInvariant() == "off" ? false
                : arg;

            if (value is string str)
            {
                if (bool.TryParse(str, out var b))
                {
                    value = b;
                }
                else if (float.TryParse(str, out var f))
                {
                    value = f;
                }
                else if (int.TryParse(str, out var ii))
                {
                    value = ii;
                }
            }

            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t)!;
            }

            return Convert.ChangeType(value, t);
        }
    }
}