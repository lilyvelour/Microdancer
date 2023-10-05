using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public abstract class CommandBase : IDisposable
    {
        private readonly ICommandManager _commandManager;
        private readonly IChatGui _chatGui;
        private readonly LicenseChecker _license;

        protected Dictionary<string, CommandInfo> CommandInfo { get; } = new();

        private bool _disposedValue;

        protected CommandBase(Service.Locator serviceLocator)
        {
            _commandManager = serviceLocator.Get<ICommandManager>();
            _chatGui = serviceLocator.Get<IChatGui>();
            _license = serviceLocator.Get<LicenseChecker>();

            CommandInfo = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommands)
                .ToDictionary(t => t.Command, t => t.Info);

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
            return CommandInfo[cmd];
        }

        protected void Print(string msg)
        {
            _chatGui.Print($"{GetPrefix()}{msg}");
        }

        protected void PrintError(string msg)
        {
            _chatGui.PrintError($"{GetPrefix()}{msg}");
        }

        private string GetPrefix()
        {
            var prefix = string.Empty;

            var st = new StackTrace(1, true);
            var frames = st.GetFrames();

            foreach (var frame in frames)
            {
                var attr = frame.GetMethod()?.GetCustomAttribute<CommandAttribute>();
                if (attr == null)
                {
                    continue;
                }

                prefix = $"{attr.Command}: ";
            }

            return prefix;
        }

        private IEnumerable<(string Command, CommandInfo Info)> GetCommands(MethodInfo method)
        {
            var command = method.GetCustomAttribute<CommandAttribute>();
            if (command == null)
            {
                yield break;
            }

            var helpMessage = command.HelpMessage ?? "<No Description>";
            if (command.Aliases.Length > 0)
            {
                helpMessage = $"{string.Join(" → ", command.Aliases)} → {helpMessage}";
            }

            var commandInfo = new CommandInfo(GetHandler(command, method))
            {
                HelpMessage = helpMessage,
                ShowInHelp = command.ShowInHelp,
            };

            yield return (command.Command, commandInfo);

            foreach (var alias in command.Aliases)
            {
                yield return (
                    alias,
                    new CommandInfo(GetHandler(command, method))
                    {
                        HelpMessage = $"Alias for {command.Command}.",
                        ShowInHelp = false,
                    }
                );
            }
        }

        private CommandInfo.HandlerDelegate GetHandler(CommandAttribute command, MethodInfo method)
        {
            return new CommandInfo.HandlerDelegate(
                (_, args) =>
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

                    var argsArray = Regex
                        .Matches((args ?? string.Empty).Trim(), @"[\""].+?[\""]|[^ ]+")
                        .Cast<Match>()
                        .Select(x => x.Value.Trim('"'))
                        .ToArray();

                    if (
                        parameters.Length == 1
                        && typeof(IEnumerable<string>).IsAssignableFrom(parameters[0].ParameterType)
                    )
                    {
                        ExecCommand(method, new object[] { argsArray });
                        return;
                    }

                    var minParameterCount = parameters.Count(p => !p.HasDefaultValue);
                    if (argsArray.Length < minParameterCount)
                    {
                        _chatGui.PrintError(
                            $"{command.Command}: Invalid parameter count. Command must have {(minParameterCount < parameters.Length ? "at least" : "")} {minParameterCount} parameter(s)."
                        );
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
                                _chatGui.PrintError(
                                    $"{command.Command}: Invalid parameter type at position {i + 1} ({arg})."
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
                }
            );
        }

        private void ExecCommand(MethodInfo method, object?[]? parameters = null)
        {
            if (_license.IsValidLicense == null)
            {
                _chatGui.PrintError("Microdancer is not yet initialized. Please wait before using any commands.");
                return;
            }

            if (_license.IsValidLicense != true)
            {
                _chatGui.PrintError(
                    "Microdancer is not currently licensed for this character. Please contact Dance Mom for access!"
                );
                return;
            }

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
            object value = string.Equals(arg, "on", StringComparison.InvariantCultureIgnoreCase)
              ? true
              : string.Equals(arg, "off", StringComparison.InvariantCultureIgnoreCase)
                  ? false
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
                else if (double.TryParse(str, out var d))
                {
                    value = d;
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
