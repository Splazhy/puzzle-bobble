using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class GameObject
{
    public Vector2 ParentTranslate { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 ScreenPosition => Position + ParentTranslate;
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    public Vector2 Velocity;

    public readonly string Name;

    // Is this object being updated in main game loop?
    public bool IsActive { get; set; }

    // Is this object being drawn in main game loop?
    public bool IsVisible { get; set; }

    public bool Destroyed { get; private set; }

    protected List<GameObject> children = [];
    protected List<GameObject> pendingChildren = [];
    protected ContentManager? content;


    // We treat GameObject contructor like Initialize method
    public GameObject(string name)
    {
        ParentTranslate = Vector2.Zero;
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
        this.content = content;
        foreach (var child in children)
        {
            child.LoadContent(content);
        }
    }

    public virtual void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        ParentTranslate = parentTranslate;
    }

    protected void UpdatePosition(GameTime gameTime)
    {
        Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    protected void UpdateChildren(GameTime gameTime)
    {
        foreach (var child in children)
        {
            child.Update(gameTime, ScreenPosition);
        }
    }

    protected void UpdatePendingAndDestroyedChildren()
    {
        Debug.Assert(content is not null);

        children.RemoveAll(obj => obj.Destroyed);

        // NOTE: we need to load content for every new game objects,
        // not sure if this is a design flaw or not.
        pendingChildren.ForEach(obj =>
        {
            obj.LoadContent(content);
            obj.ParentTranslate = ScreenPosition;
        });
        children.AddRange(pendingChildren);
        pendingChildren.Clear();

        // if (pendingChildren.Count > 0) System.Console.WriteLine($"{pendingChildren.Count} children added to {Name}");
    }

    protected void DrawChildren(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var child in children)
        {
            child.Draw(spriteBatch, gameTime);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
    }

    /// <summary>
    /// This also destroys all children of the object.
    /// </summary>
    public void Destroy()
    {
        foreach (var child in children)
        {
            child.Destroy();
        }
        Destroyed = true;
    }

    public void AddChild(GameObject child)
    {
        child.Position = child.Position - Position;
        children.Add(child);
    }

    public void AddChildren(IEnumerable<GameObject> childrenEnum)
    {
        foreach (var child in childrenEnum)
        {
            child.Position = child.Position - Position;
            children.Add(child);
        }
    }

    /// <summary>
    /// Call this if the object is created in update loop
    /// </summary>
    public void AddChildDeferred(GameObject child)
    {
        child.Position = child.Position - Position;
        pendingChildren.Add(child);
    }

    /// <summary>
    /// Call this if the objects are created in update loop
    /// </summary>
    public void AddChildrenDeferred(IEnumerable<GameObject> children)
    {
        foreach (var child in children)
        {
            child.Position = child.Position - Position;
            pendingChildren.Add(child);
        }
    }

    public void ClearChildren()
    {
        foreach (var child in children)
        {
            child.Destroy();
        }
        children.Clear();
    }

}
