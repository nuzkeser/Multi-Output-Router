using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;

namespace MultiOutputRouter
{
    public class AudioEngine
    {
        private WasapiLoopbackCapture _capture;
        private List<WasapiOut> _outputs = new List<WasapiOut>();
        private List<BufferedWaveProvider> _buffers = new List<BufferedWaveProvider>();
        
        private Dictionary<string, VolumeSampleProvider> _volumeProviders = new Dictionary<string, VolumeSampleProvider>();
        private Dictionary<string, float> _deviceVolumes = new Dictionary<string, float>();

        public void SetDeviceVolume(string deviceId, float volume)
        {
            _deviceVolumes[deviceId] = volume;
            if (_volumeProviders.ContainsKey(deviceId))
            {
                _volumeProviders[deviceId].Volume = volume;
            }
        }

        public void StartRouting(MMDevice sourceDevice, List<MMDevice> destinationDevices)
        {
            StopRouting();

            _capture = new WasapiLoopbackCapture(sourceDevice);
            var format = _capture.WaveFormat;

            foreach (var dest in destinationDevices)
            {
                var buffer = new BufferedWaveProvider(format)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };
                _buffers.Add(buffer);

                var sampleProvider = buffer.ToSampleProvider();
                var volumeProvider = new VolumeSampleProvider(sampleProvider);
                if (_deviceVolumes.ContainsKey(dest.ID))
                {
                    volumeProvider.Volume = _deviceVolumes[dest.ID];
                }
                else
                {
                    volumeProvider.Volume = 1.0f;
                }
                
                _volumeProviders[dest.ID] = volumeProvider;

                var wasapiOut = new WasapiOut(dest, AudioClientShareMode.Shared, true, 25);
                wasapiOut.Init(volumeProvider);
                wasapiOut.Play();
                _outputs.Add(wasapiOut);
            }

            _capture.DataAvailable += (s, a) =>
            {
                foreach (var buffer in _buffers)
                {
                    // ANTI-LATENCY MEASURE
                    if (buffer.BufferedDuration.TotalMilliseconds > 15)
                    {
                        buffer.ClearBuffer();
                    }

                    try 
                    {
                        buffer.AddSamples(a.Buffer, 0, a.BytesRecorded);
                    }
                    catch { } // ignore
                }
            };

            _capture.RecordingStopped += (s, a) =>
            {
                StopRouting();
            };

            _capture.StartRecording();
        }

        public void StopRouting()
        {
            if (_capture != null)
            {
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }

            foreach (var output in _outputs)
            {
                output.Stop();
                output.Dispose();
            }
            _outputs.Clear();
            _buffers.Clear();
            _volumeProviders.Clear();
        }
    }
}
