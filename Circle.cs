using Microsoft.Xna.Framework;

namespace PuzzleBobble;

public class Circle
{
    public Vector2 position;
    public float radius;

    public Circle(Vector2 position, float radius)
    {
        this.position = position;
        this.radius = radius;
    }

    public float Overlap(Circle other)
    {
        float distance = Vector2.Distance(position, other.position);
        return radius + other.radius - distance;
    }
}
