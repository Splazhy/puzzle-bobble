using System;

public readonly struct HexOrientation
{
    public readonly float f0, f1, f2, f3;
    public readonly float b0, b1, b2, b3;
    public readonly float start_angle; // in multiples of 60°
    public HexOrientation(float f0_, float f1_, float f2_, float f3_,
                float b0_, float b1_, float b2_, float b3_,
                float start_angle_)
    {
        f0 = f0_;
        f1 = f1_;
        f2 = f2_;
        f3 = f3_;
        b0 = b0_;
        b1 = b1_;
        b2 = b2_;
        b3 = b3_;
        start_angle = start_angle_;
    }

    public static readonly HexOrientation POINTY = new(MathF.Sqrt(3.0f), MathF.Sqrt(3.0f) / 2.0f, 0.0f, 3.0f / 2.0f,
                MathF.Sqrt(3.0f) / 3.0f, -1.0f / 3.0f, 0.0f, 2.0f / 3.0f,
                0.5f);

    public static readonly HexOrientation FLAT = new(3.0f / 2.0f, 0.0f, MathF.Sqrt(3.0f) / 2.0f, MathF.Sqrt(3.0f),
                2.0f / 3.0f, 0.0f, -1.0f / 3.0f, MathF.Sqrt(3.0f) / 3.0f,
                0.0f);

};