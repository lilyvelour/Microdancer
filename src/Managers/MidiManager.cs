using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NAudio.Midi;

namespace Microdancer
{
    public class MidiManager : IDisposable
    {
        private bool disposedValue;
        private static MidiIn? _midiIn;
        private readonly Configuration? _config;
        private readonly MicroManager _microManager;
        private readonly LibraryManager _library;
        private readonly IPluginLog _pluginLog;
        private int _lastValue = -1;

        public MidiManager(
            DalamudPluginInterface pluginInterface,
            IPluginLog pluginLog,
            Service.Locator serviceLocator
        )
        {
            _config = pluginInterface.GetPluginConfig() as Configuration;
            _microManager = serviceLocator.Get<MicroManager>();
            _library = serviceLocator.Get<LibraryManager>();
            _pluginLog = pluginLog;

            var midiIn = GetMidiIn();

            if (midiIn == null) return;

            midiIn.MessageReceived -= OnMessageReceived;
            midiIn.MessageReceived += OnMessageReceived;
            midiIn.Start();
        }

        public static MidiIn? GetMidiIn()
        {
            try
            {
                if (_midiIn != null)
                {
                    return _midiIn;
                }

                var deviceIndex = -1;
                for(var i = 0; i < MidiOut.NumberOfDevices; ++i)
                {
                    try
                    {
                        var info = MidiIn.DeviceInfo(i);
                        //_pluginLog.Info(info.ProductName);
                        if (info.ProductName == "loopMIDI Port")
                        {
                            deviceIndex = i;
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (deviceIndex < 0)
                {
                    //_pluginLog.Error("Unable to initialize MidiIn device!");
                    return null;
                }

                _midiIn = new MidiIn(deviceIndex);
                //_pluginLog.Info("Initialized MidiIn device");
                return _midiIn;
            }
            catch (Exception e)
            {
                //_pluginLog.Error($"Unable to initialize MidiIn device! {e.Message}");
                return null;
            }
        }

        private void OnMessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            try
            {
                var midiEvent = e.MidiEvent;

                if (midiEvent.CommandCode != MidiCommandCode.ControlChange) return;

                var controlChangeEvent = (ControlChangeEvent)midiEvent;

                var controllerValue = controlChangeEvent.ControllerValue;
                if (controllerValue == _lastValue) return;
                _lastValue = controllerValue;

                _pluginLog.Info($"MIDI CC Event: {controlChangeEvent.Controller} | {controlChangeEvent.ControllerValue}");

                if (controlChangeEvent.Controller != MidiController.Modulation) return;

                MicroInfo? microInfo = null;

                if (_microManager.Current == null)
                {
                    var selection = _library.Find<Micro>(_config?.LibrarySelection ?? Guid.Empty);
                    if (selection != null) {
                        microInfo = new MicroInfo(selection);
                    }
                }
                else
                {
                    microInfo = new MicroInfo(_microManager.Current.Micro);
                }

                if (microInfo == null) return;

                if (microInfo.AllRegions.Length <= controllerValue) return;

                var regionName = microInfo.AllRegions[controllerValue].Name;

                _pluginLog.Info($"Executing region {regionName} from MIDI...");

                _microManager.StartMicro(microInfo.Micro, regionName);
            }
            catch (Exception ex)
            {
                _pluginLog.Error(ex, ex.Message);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _midiIn?.Stop();
                    }
                    finally
                    {
                        _midiIn?.Dispose();
                        _midiIn = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}