using Godot;
using System;
using System.Collections.Generic;
using static Godot.TextServer;

namespace MC
{
    public partial class RayCastHitBlockInfo : GodotObject
    {
        public bool IsColliding { get; set; } = false;
        public Vector2I ChunkPos { get; set; }
        public Vector3 BlockWorldPos { get; set; }
        public Vector3I BlockLocalPos { get; set; }
        public Vector3 HitFaceNormal { get; set; }
    }

    public partial class Player : CharacterBody3D
    {
        [Signal] public delegate void LocalPlayerStateChangedEventHandler(int state);

        [Signal] public delegate void LocalPlayerMoveToNewChunkEventHandler(Vector2I newChunkPos);

        [Signal] public delegate void LocalPlayerBreakBlockEventHandler(RayCastHitBlockInfo info);

        [Signal] public delegate void LocalPlayerPlaceBlockEventHandler(RayCastHitBlockInfo info);

        [Export] public int Id { get; set; }
        [Export] public string NameTag 
        { 
            get { return _nameTag; }
            set
            {
                _nameTag = value;
                _nameLabel.Text = value;
            }
        }
        string _nameTag;

        public Vector2I CurrentChunkPos { get; set; }

        public HashSet<Vector3I> OccupiedBlockWorldPositions = new();

        Global _global;

        [Export] Camera3D _camera;
        [Export] RayCast3D _rayCast;
        [Export] Node3D _head;
        [Export] MeshInstance3D _selectionBox;
        [Export] AnimatedCharacter _animatedCharacter;
        [Export] Label3D _nameLabel;

        [Export] float _speed = 4f;
        [Export] float _acceleration = 100f;
        [Export] float _jumpHeight = 1f;
        [Export] float _camSensitivity = 0.01f;
        [Export] float _playerHeight = 1.8f;
        [Export] float _headPitchMaxAngleRadian = 1.55f;

        bool _jumping = false;
        Vector2 _moveDirection = new();
        Vector2 _lookDirection = new();

        Vector3 _walkVelocity = new();
        Vector3 _gravityVelocity = new();
        Vector3 _jumpVelocity = new();

        RayCastHitBlockInfo _rayCastInfo = new();

        [Export] float _breakBlockInterval = 0.25f;
        [Export] float _placeBlockInterval = 0.25f;
        Timer _breakBlockTimer = null;
        Timer _placeBlockTimer = null;

        Vector3 HeadRotation
        {
            get { return _head.Rotation; }
            set
            {
                _head.Rotation = value;
                _animatedCharacter.HeadGlobalLookAtVector = _head.GlobalTransform.Basis * Vector3.Forward;
            }
        }

        Vector3 WalkDirection
        {
            get { return _walkDirection; }
            set
            {
                if (_walkDirection == value) return;
                _walkDirection = value;
                _animatedCharacter.StartWalkDirectionChangeAnimation(value);
            }
        }
        Vector3 _walkDirection = new();


        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");

            if (!IsMultiplayerAuthority())
                return;

            _global.LocalPlayer = this;  // Should send signal

            _animatedCharacter = GetNode<AnimatedCharacter>("%AnimatedCharacter");
        }

        public void Init()
        {
            if (!IsMultiplayerAuthority())
                return;

            GD.Print("Init local player!");

            NameTag = _global.GameStartInfo.PlayerName;
            Position = Global.PlayerSpawnPosition;

            _breakBlockTimer = new Timer();
            AddChild(_breakBlockTimer);
            _breakBlockTimer.OneShot = true;

            _placeBlockTimer = new Timer();
            AddChild(_placeBlockTimer);
            _placeBlockTimer.OneShot = true;

            _animatedCharacter.SetInvisible();
            HeadRotation = new Vector3();

            _nameLabel.Hide();
        }

        public override void _Process(double delta)
        {
            if (!InGame())
                return;

            UpdateOccupiedBlockPositions();

            if (!IsMultiplayerAuthority())
                return;

            if (Position.Y < 0)
            {
                Position = Global.PlayerSpawnPosition;
                _gravityVelocity = Vector3.Zero;
                _jumpVelocity = Vector3.Zero;
            }

            var chunkPos = World.WorldPosToChunkPos(Position);
            CurrentChunkPos = chunkPos;

            _selectionBox.Visible = false;
            _rayCastInfo.IsColliding = false;

            if (_rayCast.IsColliding())
            {
                var collider = _rayCast.GetCollider() as Node;

                if (collider.IsInGroup(Global.WorldGroup))
                {
                    _rayCastInfo.IsColliding = true;
                    _rayCastInfo.HitFaceNormal = _rayCast.GetCollisionNormal();

                    var worldPos = (_rayCast.GetCollisionPoint() - 0.5f * _rayCastInfo.HitFaceNormal);
                    var blockWorldPos = World.WorldPosToBlockWorldPos(worldPos);
                    _selectionBox.GlobalPosition = blockWorldPos - (_selectionBox.Scale - Vector3.One) / 2f + new Vector3(0.5f, 0.5f, 0.5f);

                    _selectionBox.Visible = true;
                    _rayCastInfo.ChunkPos = World.WorldPosToChunkPos(worldPos);
                    _rayCastInfo.BlockWorldPos = blockWorldPos;
                    _rayCastInfo.BlockLocalPos = World.BlockWorldPosToBlockLocalPos(blockWorldPos);
                }
            }

            if (_global.GameState != GameState.InGameActive)
                return;

            if (Input.IsActionPressed("Break") && _breakBlockTimer.TimeLeft <= 0)
            {
                _breakBlockTimer.Start(_breakBlockInterval);
                _animatedCharacter.InteractCount += 1; 
                
                if (_rayCastInfo.IsColliding)
                    EmitSignal(SignalName.LocalPlayerBreakBlock, _rayCastInfo);

            }
            else if (Input.IsActionPressed("Place") && _placeBlockTimer.TimeLeft <= 0)
            {
                _placeBlockTimer.Start(_placeBlockInterval);
                _animatedCharacter.InteractCount += 1;

                if (_rayCastInfo.IsColliding)
                    EmitSignal(SignalName.LocalPlayerPlaceBlock, _rayCastInfo);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsMultiplayerAuthority() || !InGame())
                return;

            Velocity = Walk((float)delta) + Gravity((float)delta) + Jump((float)delta);
            
            MoveAndSlide();
        }

        public override void _Input(InputEvent @event)
        {
            if (!IsMultiplayerAuthority() || !InGame())
                return;

            _jumping = false;
            _moveDirection = Vector2.Zero;
            
            if (_global.GameState != GameState.InGameActive)
            {
                _animatedCharacter.StartIdleWalkBlendAnimation(_moveDirection.Length());
                return;
            }

            if (@event is InputEventMouseMotion mouseMotion)
            {
                _lookDirection = mouseMotion.Relative * _camSensitivity;
                var headRotation = HeadRotation;
                headRotation.Y -= _lookDirection.X;
                headRotation.X = Mathf.Clamp(headRotation.X - _lookDirection.Y, -_headPitchMaxAngleRadian, _headPitchMaxAngleRadian);
                HeadRotation = headRotation;
            }

            if (Input.IsActionPressed("Jump"))
                _jumping = true;
            _moveDirection = Input.GetVector("Left", "Right", "Forward", "Back");
            _animatedCharacter.StartIdleWalkBlendAnimation(_moveDirection.Length());
        }

        public void MakeCameraCurrent()
        {
            _camera.MakeCurrent();
        }

        bool InGame()
        {
            return _global.GameState == GameState.InGameActive || _global.GameState == GameState.InGamePaused || _global.GameState == GameState.InGameOptionsPage;
        }

        Vector3 Walk(float delta)
        {
            var forward = _head.GlobalTransform.Basis * new Vector3(_moveDirection.X, 0, _moveDirection.Y);
            WalkDirection = new Vector3(forward.X, 0, forward.Z).Normalized();
            _walkVelocity = _walkVelocity.MoveToward(WalkDirection * _speed * _moveDirection.Length(), _acceleration * delta);
            return _walkVelocity;
        }

        Vector3 Gravity(float delta)
        {
            _gravityVelocity = IsOnFloor() ? Vector3.Zero : _gravityVelocity.MoveToward(new Vector3(0, Velocity.Y - Global.Gravity, 0), Global.Gravity * delta);
            return _gravityVelocity;
        }

        Vector3 Jump(float delta)
        {
            if (_jumping && IsOnFloor())
                _jumpVelocity = new Vector3(0, Mathf.Sqrt(4 * _jumpHeight * Global.Gravity), 0);
            else
                _jumpVelocity = IsOnFloor() ? Vector3.Zero : _jumpVelocity.MoveToward(Vector3.Zero, Global.Gravity * delta);
            return _jumpVelocity;
        }

        void UpdateOccupiedBlockPositions()
        {
            OccupiedBlockWorldPositions.Clear();
            var headOccupiedBlockWorldPos = World.WorldPosToBlockWorldPos(Position + new Vector3(0, _playerHeight / 2f, 0));
            var footOccupiedBlockWorldPos = World.WorldPosToBlockWorldPos(Position - new Vector3(0, _playerHeight / 2f, 0));
            for (int y = footOccupiedBlockWorldPos.Y; y <= headOccupiedBlockWorldPos.Y; y++)
                OccupiedBlockWorldPositions.Add(new Vector3I(headOccupiedBlockWorldPos.X, y, headOccupiedBlockWorldPos.Z));
        }
    }
}
