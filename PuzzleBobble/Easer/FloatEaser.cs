using System;

namespace PuzzleBobble.Easer;

public class FloatEaser : Easer
{
    private float valueA;
    private float valueB;

    public FloatEaser(TimeSpan startTime) : base(startTime)
    {
    }

    public void SetValueA(float valueA)
    {
        this.valueA = valueA;
    }

    public void SetValueB(float valueB)
    {
        this.valueB = valueB;
    }

    public float GetValue(TimeSpan currTime)
    {
        return (float)(valueA + ((valueB - valueA) * GetEaseValue(currTime)));
    }
}
