using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class GameObject
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    public Vector2 Velocity { get; set; }

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
    }

    protected void UpdatePosition(GameTime gameTime)
    {
        Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    protected void UpdateChildren(GameTime gameTime, Vector2 parentTranslate)
    {

        foreach (var child in children)
        {
            child.Update(gameTime, parentTranslate + Position);
        }
    }

    protected void UpdatePendingAndDestroyedChildren()
    {
        Debug.Assert(content is not null);

        children.RemoveAll(obj => obj.Destroyed);
        // NOTE: we need to load content for every new game objects,
        // not sure if this is a design flaw or not.
        pendingChildren.ForEach(obj => obj.LoadContent(content));
        children.AddRange(pendingChildren);
        pendingChildren.Clear();
    }

    protected void DrawChildren(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        foreach (var child in children)
        {
            child.Draw(spriteBatch, gameTime, parentTranslate + Position);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
    }

    public void Destroy()
    {
        Destroyed = true;
    }
}
