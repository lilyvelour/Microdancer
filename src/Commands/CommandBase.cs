using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;

namespace Microdancer
{
    [PluginInterface]
    public abstract class CommandBase : IDisposable
    {
        protected Configuration Configuration { get; private set; }
        private readonly CommandManager _commandManager;

        private readonly (string, CommandInfo)[] _commands = Array.Empty<(string, CommandInfo)>();

        private bool _disposedValue;

        public CommandBase(CommandManager commandManager, Configuration configuration)
        {
            _commandManager = commandManager;
            Configuration = configuration;

            _commands = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommands)
                .ToArray();

            foreach (var (command, commandInfo) in _commands)
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
                foreach (var (command, _) in _commands)
                {
                    _commandManager.RemoveHandler(command);
                }
            }

            _disposedValue = true;
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

            CommandInfo.HandlerDelegate? handlerDelegate = null;

            var parameterLength = method.GetParameters().Length;

            switch(parameterLength)
            {
                case 0:
                    handlerDelegate = new CommandInfo.HandlerDelegate(
                        (_, _) => method.Invoke(this, null));
                    break;
                case 1:
                    var parameter = method.GetParameters()[0];
                    if (parameter.ParameterType == typeof(string[]))
                    {
                        handlerDelegate = new CommandInfo.HandlerDelegate(
                            (_, args) => method.Invoke(this, new[]
                            {
                                Regex.Matches((args ?? string.Empty).Trim(), @"[\""].+?[\""]|[^ ]+")
                                    .Cast<Match>()
                                    .Select(x => x.Value.Trim('"')).ToArray(),
                            }));
                    }
                    else if (parameter.ParameterType == typeof(string))
                    {
                        handlerDelegate = new CommandInfo.HandlerDelegate(
                            (_, args) => method.Invoke(this, new[] { args ?? string.Empty }));
                    }
                    else
                    {
                        PluginLog.LogError(
                            $"Invalid parameter type for {cmd} - must be a string or string array");
                        yield break;
                    }
                    break;
                default:
                    PluginLog.LogError(
                        $"Invalid method for {cmd} - must have a single string or string array argument");
                    yield break;
            }

            var commandInfo = new CommandInfo(handlerDelegate!)
            {
                HelpMessage = command.HelpMessage ?? string.Empty,
                ShowInHelp = command.ShowInHelp,
            };

            yield return (cmd, commandInfo);

            var commands = new List<(string, CommandInfo)>
            {
                (cmd, commandInfo)
            };

            foreach(var alias in aliases)
            {
                yield return (alias, new CommandInfo(handlerDelegate!)
                {
                    HelpMessage = $"Alias for {cmd}.",
                    ShowInHelp = command.ShowInHelp,
                });
            }
        }
    }
}