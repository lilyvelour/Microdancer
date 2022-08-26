using System;
using Dalamud.Game;
using NAudio.Wave;

namespace Microdancer
{
    public class AudioManager : IDisposable
    {
        private readonly Framework _framework;
        private readonly WasapiLoopbackCapture _capture;
        private bool _disposedValue;

        public float PeakValue { get; private set; }
        public float PreviousPeakValue { get; private set; }
        public float SmoothedPeakValue { get; private set; }
        public bool IsRecording { get; private set; }

        public float LinearToDecibel(float x)
        {
            return (float)Math.Log(x) * 20.0f;
        }

        public AudioManager(Framework framework, Service.Locator _)
        {
            _framework = framework;

            _capture = new();
            _capture.DataAvailable += OnDataAvailable;
            _framework.Update += OnFrameworkUpdate;
        }

        public void StartRecording()
        {
            if (IsRecording)
                return;

            PeakValue = 0;
            PreviousPeakValue = 0;
            SmoothedPeakValue = 0;
            _capture.StartRecording();
            IsRecording = true;
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;

            PeakValue = 0;
            PreviousPeakValue = 0;
            SmoothedPeakValue = 0;
            _capture.StopRecording();
            IsRecording = false;
        }

        public void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void OnDataAvailable(object? _, WaveInEventArgs args)
        {
            var peak = 0.0f;
            var buffer = new WaveBuffer(args.Buffer);

            for (var i = 0; i < args.BytesRecorded / 4; ++i)
            {
                peak = Math.Max(peak, Math.Abs(buffer.FloatBuffer[i]));
            }

            PreviousPeakValue = PeakValue;
            PeakValue = peak;
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            SmoothedPeakValue = MathExt.Lerp(
                SmoothedPeakValue,
                PeakValue,
                (float)framework.UpdateDelta.TotalSeconds * 5.0f
            );
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _capture.DataAvailable -= OnDataAvailable;
                    _framework.Update -= OnFrameworkUpdate;
                    _capture.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
