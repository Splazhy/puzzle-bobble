// https://stackoverflow.com/a/20679895/3623350
using System.Collections.Generic;
using System.Linq;

namespace PuzzleBobble;
public class FrameCounter
{
    public long TotalFrames { get; private set; }
    public double TotalSeconds { get; private set; }
    public double AverageFramesPerSecond { get; private set; }
    public double CurrentFramesPerSecond { get; private set; }

    public const int MaximumSamples = 100;

    private readonly Queue<double> _sampleBuffer = new();

    public void Update(double deltaTime)
    {
        CurrentFramesPerSecond = 1.0f / deltaTime;

        _sampleBuffer.Enqueue(CurrentFramesPerSecond);

        if (_sampleBuffer.Count > MaximumSamples)
        {
            _sampleBuffer.Dequeue();
            AverageFramesPerSecond = _sampleBuffer.Average(i => i);
        }
        else
        {
            AverageFramesPerSecond = CurrentFramesPerSecond;
        }

        TotalFrames++;
        TotalSeconds += deltaTime;
    }
}
