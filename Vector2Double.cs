
using Microsoft.Xna.Framework;

public struct Vector2Double
{
    public double X;
    public double Y;

    public Vector2Double(double x_, double y_)
    {
        X = x_;
        Y = y_;
    }

    public Vector2Double(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    public Vector2 Downcast()
    {
        return new Vector2((float)X, (float)Y);
    }
}