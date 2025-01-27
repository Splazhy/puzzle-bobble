using System;

namespace PuzzleBobble.Easer;

// hoped to make this class generic, but turns out c# doesn't support
// restricting generics over types with specific operators implemented
// so this is just straight out of 
// https://github.com/NutchapolSal/javagameproject/blob/main/Tetris/data/easer/Easer.java
// with types changed to work better with c#
public abstract class Easer
{
    private TimeSpan startTime;
    private bool easeAToB = false;
    private bool differentEaseBToA = false;
    private Func<double, double> easeBToAFunction = x => x;
    private Func<double, double> easeAToBFunction = x => x;
    private bool differentLengthBToA = false;
    private TimeSpan timeLengthAToB = TimeSpan.FromMilliseconds(1);
    private TimeSpan timeLengthBToA = TimeSpan.FromMilliseconds(1);

    protected Easer(TimeSpan startTime)
    {
        this.startTime = startTime;
    }

    private void UpdateStartTime(TimeSpan oldTimeLength, TimeSpan currTime)
    {
        double rawEaseValue = (currTime - startTime) / oldTimeLength;
        rawEaseValue = Math.Max(0.0, Math.Min(rawEaseValue, 1.0));

        if (easeAToB)
        {
            startTime = currTime - (timeLengthAToB * rawEaseValue);
        }
        else
        {
            startTime = currTime - (timeLengthBToA * rawEaseValue);
        }
    }

    public void SetTimeLength(TimeSpan duration, TimeSpan currTime)
    {
        var oldTimeLength = timeLengthAToB;
        timeLengthAToB = duration;
        if (!differentLengthBToA)
        {
            timeLengthBToA = duration;
        }
        if (easeAToB || !differentLengthBToA)
        {
            UpdateStartTime(oldTimeLength, currTime);
        }
    }

    public void SetTimeLengthBToA(TimeSpan duration, TimeSpan currTime)
    {
        var oldTimeLength = timeLengthBToA;

        differentLengthBToA = true;
        timeLengthBToA = duration;

        if (!easeAToB)
        {
            UpdateStartTime(oldTimeLength, currTime);
        }
    }

    public void UnsetTimeLengthBToA(TimeSpan currTime)
    {
        differentLengthBToA = false;
        SetTimeLength(timeLengthAToB, currTime);
    }

    protected double GetEaseValue(TimeSpan currTime)
    {
        var currTimeLength = easeAToB ? timeLengthAToB : timeLengthBToA;
        var deltaTime = currTime - startTime;
        if (currTimeLength <= deltaTime)
        {
            return easeAToB ? 1.0 : 0.0;
        }
        if (deltaTime.TotalSeconds <= 0)
        {
            return easeAToB ? 0.0 : 1.0;
        }
        double rawEaseValue = deltaTime / currTimeLength;
        if (easeAToB)
        {
            return easeAToBFunction(rawEaseValue);
        }
        else
        {
            return 1 - easeBToAFunction(rawEaseValue);
        }
    }

    public void SetEaseFunction(Func<double, double> func)
    {
        easeAToBFunction = func;
        if (!differentEaseBToA)
        {
            easeBToAFunction = func;
        }
    }

    public void SetEaseBToAFunction(Func<double, double> func)
    {
        differentEaseBToA = true;
        easeBToAFunction = func;
    }

    public void UnsetEaseBToAFunction()
    {
        differentEaseBToA = false;
        easeBToAFunction = easeAToBFunction;
    }

    public void StartEase(TimeSpan startTime, bool easeAToB)
    {
        this.startTime = startTime;
        this.easeAToB = easeAToB;
    }
}
