using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class GameObject
{
    public GameObject? Parent = null;
    public List<GameObject> Children = [];
    private List<GameObject> _childrenToAdd = [];

    public Vector2 Position; // Local position

    public Vector2 GlobalPosition
    {
        get { if (Parent is null) return Position; else return Position + Parent.Position; }
    }
    public Vector2 ScreenPosition
    {
        get { return GlobalPosition + Game1.WindowCenter; }
    }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    public Vector2 Velocity { get; set; }

    public readonly string Name;

    // Is this object being updated in main game loop?
    public bool IsActive { get; set; }

    // Is this object being drawn in main game loop?
    public bool IsVisible { get; set; }

    public bool Destroyed { get; private set; }

    // We treat GameObject contructor like Initialize method
    public GameObject(string name)
    {
        Position = Vector2.Zero;
        Rotation = 0.0f;
        Scale = Vector2.One;
        Velocity = Vector2.Zero;
        Name = name;
        IsActive = true;
        IsVisible = true;
        Destroyed = false;
    }

    public virtual void LoadContent(ContentManager content)
    {
        foreach (var child in Children)
        {
            child.LoadContent(content);
        }
    }

    public virtual void Update(GameTime gameTime)
    {
        Children.RemoveAll(child => child.Destroyed);
        var i = Children.RemoveAll(child => child.Parent != this);
        // if (i > 0) System.Console.WriteLine($"{i} children removed from {Name}");
        Children.AddRange(_childrenToAdd);
        // if (_childrenToAdd.Count > 0) System.Console.WriteLine($"{_childrenToAdd.Count} children added to {Name}");
        _childrenToAdd.Clear();

        foreach (var child in Children)
        {
            child.Update(gameTime);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var child in Children)
        {
            child.Draw(spriteBatch, gameTime);
        }
    }

    /// <summary>
    /// This also destroys all children of the object.
    /// </summary>
    public void Destroy()
    {
        foreach (var child in Children)
        {
            child.Destroy();
        }
        Destroyed = true;
    }

    public void AddChild(GameObject child)
    {
        child.Position = child.GlobalPosition - GlobalPosition;
        child.Parent = this;
        Children.Add(child);
    }

    public void AddChildren(IEnumerable<GameObject> children)
    {
        foreach (var child in children)
        {
            child.Position = child.GlobalPosition - GlobalPosition;
            child.Parent = this;
            Children.Add(child);
        }
    }

    /// <summary>
    /// Call this if the object is created in update loop
    /// </summary>
    public void AddChildDeferred(GameObject child)
    {
        child.Position = child.GlobalPosition - GlobalPosition;
        child.Parent = this;
        _childrenToAdd.Add(child);
    }

    /// <summary>
    /// Call this if the objects are created in update loop
    /// </summary>
    public void AddChildrenDeferred(IEnumerable<GameObject> children)
    {
        foreach (var child in children)
        {
            child.Position = child.GlobalPosition - GlobalPosition;
            child.Parent = this;
            _childrenToAdd.Add(child);
        }
    }

    public void ClearChildren()
    {
        foreach (var child in Children)
        {
            child.Destroy();
        }
        Children.Clear();
    }

}
