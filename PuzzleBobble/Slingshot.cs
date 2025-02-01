using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Slingshot : GameObject
{

    private readonly float firerate; // shots per second
    private float _timeSinceLastFired;
    public BallData? Data { get; private set; }
    public BallData? NextData { get; private set; }
    private BallData.Assets? _ballAssets = null;
    public float BallSpeed = 1000.0f / 3; // IDEA: make this property upgradable

    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = MathF.PI * -80.0f / 180.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 80.0f / 180.0f;

    private const float MAX_RECOIL = 30.0f / 3;
    private const float RECOIL_RECOVERY = 100.0f / 3;
    private float visualRecoilOffset = 0.0f;

    public event BallFiredHandler? BallFired;
    public delegate void BallFiredHandler(Ball ball);

    private readonly SlingshotStaff _staff;
    private SlingshotBall? _ball;
    private SlingshotBall? _nextBall;

    private bool _lastFrameRightClick = false;
    public bool CheckNextData = true;

    private GameState _state = GameState.Playing;

    public Slingshot(Game game) : base("slingshot")
    {
        Position = new Vector2(0, 100);
        firerate = 3.0f;
        _timeSinceLastFired = 1 / firerate;

        _staff = new SlingshotStaff();
        children.Add(_staff);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _staff.LoadContent(content);

        _ballAssets = new BallData.Assets(content);
        Data?.LoadAnimation(_ballAssets);
        NextData?.LoadAnimation(_ballAssets);
    }
    private static readonly Vector2 NEXT_BALL_POSITION = new(33, 6);

    public void SetNextData(GameTime gameTime, BallData? data)
    {
        if (_nextBall is not null)
        {
            _nextBall.TargetPosition = NEXT_BALL_POSITION + new Vector2(0, 10);
            _nextBall.FadeAway(gameTime);
        }
        Debug.Assert(_ballAssets is not null, "Ball assets are not loaded.");
        Debug.Assert(content is not null, "ContentManager is not initialized.");
        NextData = data;
        NextData?.LoadAnimation(_ballAssets);
        if (NextData is BallData bd)
        {
            _nextBall = new SlingshotBall(gameTime, _ballAssets, bd, NEXT_BALL_POSITION);
            _nextBall.LoadContent(content);
            children.Add(_nextBall);
        }
        CheckNextData = false;
    }

    private void SwapDatas()
    {
        (NextData, Data) = (Data, NextData);
        CheckNextData = true;
        (_ball, _nextBall) = (_nextBall, _ball);
        SetDisplayBallPositions();
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        // TODO: Implement `IsJustPressed` method for new InputManager class
        // This code executes multiple times per a short key press,
        // resulting in undesired behavior.
        //
        // if (Keyboard.GetState().IsKeyDown(Keys.H))
        //     IsActive = !IsActive;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeSinceLastFired += deltaTime;
        visualRecoilOffset = Math.Max(0.0f, visualRecoilOffset - RECOIL_RECOVERY * deltaTime);

        if (!IsActive) return;

        MouseState mouseState = Mouse.GetState();
        Vector2 direction = new Vector2(mouseState.X, mouseState.Y) - ScreenPosition;


        if (mouseState.RightButton == ButtonState.Pressed && !_lastFrameRightClick)
        {
            if (_state == GameState.Playing && Data is not null && NextData is not null) SwapDatas();
        }
        _lastFrameRightClick = mouseState.RightButton == ButtonState.Pressed;

        // +90 degrees to adjust for texture orientation
        direction.Rotate(MathF.PI / 2.0f);

        Rotation = MathF.Atan2(direction.Y, direction.X);
        Rotation = MathHelper.Clamp(Rotation, MIN_ROTATION, MAX_ROTATION);

        _staff.TargetPosition = new Vector2(MathF.Cos(Rotation + (MathF.PI / 2)), MathF.Sin(Rotation + (MathF.PI / 2)));
        _staff.TargetPosition.Normalize();
        _staff.TargetPosition *= 30f / 3;
        _staff.TargetRotation = -Rotation * 0.2f;

        if (_state == GameState.Playing)
        {
            if (Data is BallData bd && mouseState.LeftButton == ButtonState.Pressed && _timeSinceLastFired > 1 / firerate)
            {
                var staffTargetPos2 = new Vector2(MathF.Cos(Rotation - (MathF.PI / 2)), MathF.Sin(Rotation - (MathF.PI / 2)));
                staffTargetPos2.Normalize();
                staffTargetPos2 *= 20f / 3;
                _staff.TargetPosition2 = staffTargetPos2;
                _staff.TargetRotation2 = 0;
                _staff.ChangeUntil = gameTime.TotalGameTime + TimeSpan.FromSeconds(0.1);

                // Rotate back to 0 degrees
                float targetRotation = Rotation - MathF.PI / 2.0f;
                Ball newBall = new(bd, Ball.State.Moving)
                {
                    Position = Position,
                    Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * BallSpeed,
                };
                // I'm thinking `BallFactory` class
                // then maybe `AbstractBallFactory` class
                // then maybe `AbstractBallFactorySingleton` class
                // then maybe `AbstractBallFactorySingletonBuilder` class
                // then burn the whole project to the ground
                Debug.Assert(content is not null, "ContentManager is not initialized.");
                newBall.LoadContent(content);

                BallFired?.Invoke(newBall);

                _timeSinceLastFired = 0.0f;

                Data = null;
                Debug.Assert(_ball is not null, "Slingshot ball GameObject is null.");
                _ball.Destroy();

                visualRecoilOffset = MAX_RECOIL;
            }

            if (Data is null && NextData is not null) SwapDatas();
        }

        SetDisplayBallPositions();

        UpdateChildren(gameTime);
        UpdatePendingAndDestroyedChildren();
        if (_ball is not null && _ball.Destroyed) _ball = null;
        if (_nextBall is not null && _nextBall.Destroyed) _nextBall = null;
    }

    private void SetDisplayBallPositions()
    {
        if (_ball is not null) { _ball.TargetPosition = new Vector2(0, 0); }
        if (_nextBall is not null) { _nextBall.TargetPosition = NEXT_BALL_POSITION; }
    }



    public void Fail(GameTime gameTime)
    {
        _state = GameState.Fail;
        _ball?.FadeAway(gameTime);
        _nextBall?.FadeAway(gameTime);
    }

    public void Success(GameTime gameTime)
    {
        _state = GameState.Success;
        _ball?.FadeAway(gameTime);
        _nextBall?.FadeAway(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_ballAssets is not null, "Ball assets are not loaded.");

        DrawChildren(spriteBatch, gameTime);

        _staff.Draw(spriteBatch, gameTime);
    }

}
