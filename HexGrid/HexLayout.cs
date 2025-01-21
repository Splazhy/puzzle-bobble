using Microsoft.Xna.Framework;
namespace PuzzleBobble.HexGrid;

// https://www.redblobgames.com/grids/hexagons/implementation.html
public readonly struct HexLayout
{
    public readonly HexOrientation orientation;
    public readonly Vector2Double size;
    public readonly Vector2Double origin;

    public HexLayout(HexOrientation orientation_, Vector2Double size_, Vector2Double origin_)
    {
        orientation = orientation_;
        size = size_;
        origin = origin_;
    }

    public readonly Vector2Double HexToCenterPixel(Hex h)
    {
        double x = (orientation.f0 * h.Q + orientation.f1 * h.R) * size.X;
        double y = (orientation.f2 * h.Q + orientation.f3 * h.R) * size.Y;
        return new Vector2Double(x + origin.X, y + origin.Y);
    }

    public readonly Vector2Double HexToOriginPixel(Hex h)
    {
        double x = (orientation.f0 * h.Q + orientation.f1 * h.R) * size.X;
        double y = (orientation.f2 * h.Q + orientation.f3 * h.R) * size.Y;
        return new Vector2Double(x, y);
    }

    public readonly HexFrac PixelToHex(Vector2Double p)
    {
        Vector2Double pt = new((p.X - origin.X) / size.X,
                                (p.Y - origin.Y) / size.Y);
        double q = orientation.b0 * pt.X + orientation.b1 * pt.Y;
        double r = orientation.b2 * pt.X + orientation.b3 * pt.Y;
        return new HexFrac(q, r, -q - r);
    }

    public HexFrac PixelToHex(Vector2 p)
    {
        return PixelToHex(new Vector2Double(p));
    }
};