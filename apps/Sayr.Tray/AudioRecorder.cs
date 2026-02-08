using NAudio.Wave;

namespace Sayr.Tray;

internal sealed class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _buffer;
    private WaveFileWriter? _writer;
    private TaskCompletionSource<byte[]>? _stopTcs;

    public bool IsRecording { get; private set; }

    public void Start()
    {
        if (IsRecording)
        {
            return;
        }

        _buffer = new MemoryStream();
        _writer = new WaveFileWriter(_buffer, new WaveFormat(16000, 1));

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1),
            BufferMilliseconds = 100
        };

        _waveIn.DataAvailable += (_, args) =>
        {
            _writer?.Write(args.Buffer, 0, args.BytesRecorded);
            _writer?.Flush();
        };

        _waveIn.RecordingStopped += (_, _) =>
        {
            _writer?.Dispose();
            _writer = null;

            if (_buffer is null)
            {
                _stopTcs?.TrySetException(new InvalidOperationException("No audio buffer available."));
                return;
            }

            var data = _buffer.ToArray();
            _buffer.Dispose();
            _buffer = null;
            _stopTcs?.TrySetResult(data);
        };

        _stopTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waveIn.StartRecording();
        IsRecording = true;
    }

    public Task<byte[]> StopAsync()
    {
        if (!IsRecording || _waveIn is null || _stopTcs is null)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        IsRecording = false;

        return _stopTcs.Task;
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
        _writer?.Dispose();
        _buffer?.Dispose();
    }
}
