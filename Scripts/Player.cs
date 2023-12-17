using Godot;
using System;
using static Godot.TextServer;

namespace MC
{
    public partial class RayCastHitBlockInfo : GodotObject
    {
        public bool IsColliding { get; set; } = false;
        public Vector2I ChunkPos { get; set; }
        public Vector3I BlockLocalPos { get; set; }
        public Vector3 HitFaceNormal { get; set; }
    }

    public partial class Player : CharacterBody3D
    {
        [Signal] public delegate void LocalPlayerStateChangedEventHandler(int state);

        [Signal] public delegate void LocalPlayerMoveToNewChunkEventHandler(Vector2I newChunkPos);

        [Signal] public delegate void LocalPlayerBreakBlockEventHandler(RayCastHitBlockInfo info);

        [Export] public int Id { get; set; }
        [Export] public string NameTag { get; set; }

        Global _global;

        [Export] Camera3D _camera;
        [Export] RayCast3D _rayCast;
        [Export] Node3D _head;
        [Export] MeshInstance3D _selectionBox;

        [Export] float _speed = 4f;
        [Export] float _acceleration = 100f;
        [Export] float _jumpHeight = 1f;
        [Export] float _camSensitivity = 0.01f;

        [Export] float _checkChunkInterval = 0.3f;
        Vector2I _lastTimeChunkPos = new();

        bool _jumping = false;
        Vector2 _moveDirection = new();
        Vector2 _lookDirection = new();

        Vector3 _walkVelocity = new();
        Vector3 _gravityVelocity = new();
        Vector3 _jumpVelocity = new();

        RayCastHitBlockInfo _rayCastInfo = new();

        public override void _Ready()
        {
            _camera.ClearCurrent();

            if (!IsMultiplayerAuthority())
                return;

            _global = GetNode<Global>("/root/Global");
            _global.LocalPlayer = this;  // Should send signal

            _global.GameStateChanged += OnGameStateChanged;
        }

        public void Init()
        {
            if (!IsMultiplayerAuthority())
                return;

            GD.Print("Init local player!");

            _camera.Visible = true;
            _camera.MakeCurrent();

            NameTag = _global.GameStartInfo.PlayerName;
            Position = Global.PlayerSpawnPosition;

            // Generate selection box
            GenerateSelectionBoxMesh();
        }

        public override void _Process(double delta)
        {
            if (!IsMultiplayerAuthority() || !InGame())
                return;

            var chunkPos = World.WorldPosToChunkPos(Position);
            if (chunkPos != _lastTimeChunkPos)
            {
                _lastTimeChunkPos = chunkPos;
                EmitSignal(SignalName.LocalPlayerMoveToNewChunk, chunkPos);
            }

            _selectionBox.Visible = false;
            _rayCastInfo.IsColliding = false;

            if (_rayCast.IsColliding())
            {
                var collider = _rayCast.GetCollider();

                if (collider is Chunk chunk)
                {
                    _rayCastInfo.IsColliding = true;
                    _rayCastInfo.HitFaceNormal = _rayCast.GetCollisionNormal();

                    var worldPos = (_rayCast.GetCollisionPoint() - 0.5f * _rayCastInfo.HitFaceNormal);
                    var blockWorldPos = World.WorldPosToBlockWorldPos(worldPos);
                    _selectionBox.GlobalPosition = blockWorldPos - (_selectionBox.Scale - Vector3.One) / 2f;

                    _selectionBox.Visible = true;
                    _rayCastInfo.ChunkPos = World.WorldPosToChunkPos(worldPos);
                    _rayCastInfo.BlockLocalPos = World.BlockWorldPosToBlockLocalPos(blockWorldPos);
                }
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

            if (Input.IsActionJustPressed("Escape"))
            {
                if (_global.GameState == GameState.InGameActive)
                    _global.GameState = GameState.InGamePaused;
                else if (_global.GameState == GameState.InGamePaused)
                    _global.GameState = GameState.InGameActive;
            }
            
            if (_global.GameState != GameState.InGameActive)
                return;

            if (@event is InputEventMouseMotion mouseMotion)
            {
                _lookDirection = mouseMotion.Relative * _camSensitivity;
                var headRotation = _head.Rotation;
                headRotation.Y -= _lookDirection.X;
                headRotation.X = Mathf.Clamp(headRotation.X - _lookDirection.Y, -1.5f, 1.5f);
                _head.Rotation = headRotation;
            }

            if (Input.IsActionPressed("Jump"))
                _jumping = true;
            _moveDirection = Input.GetVector("Left", "Right", "Forward", "Back");

            if (Input.IsActionJustPressed("Break") && _rayCastInfo.IsColliding)
                EmitSignal(SignalName.LocalPlayerBreakBlock, _rayCastInfo);
        }

        bool InGame()
        {
            return _global.GameState == GameState.InGameActive || _global.GameState == GameState.InGamePaused;
        }

        void OnGameStateChanged(int state)
        {
            var gameState = (GameState)state;
            switch (gameState)
            {
                case GameState.InGameActive:
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    break;
                case GameState.InGamePaused:
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    break;
            }
        }

        Vector3 Walk(float delta)
        {
            var forward = _head.GlobalTransform.Basis * new Vector3(_moveDirection.X, 0, _moveDirection.Y);
            var walkDirection = new Vector3(forward.X, 0, forward.Z).Normalized();
            _walkVelocity = _walkVelocity.MoveToward(walkDirection * _speed * _moveDirection.Length(), _acceleration * delta);
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

        void GenerateSelectionBoxMesh()
        {
            var surfaceTool = new SurfaceTool();
            var material = new OrmMaterial3D();

            var indices = new int[]
            {
                0, 1, 0, 4, 1, 5, 4, 5,
                0, 2, 1, 3, 4, 6, 5, 7,
                2, 3, 2, 6, 3, 7, 6, 7
            };

            surfaceTool.Begin(Mesh.PrimitiveType.Lines);

            for (int i = 0; i < Cubic.Vertices.Length; i++)
                surfaceTool.AddVertex(Cubic.Vertices[i]);
            for (int i = 0; i < indices.Length; i++)
                surfaceTool.AddIndex(indices[i]);

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = new Color(0.2f, 0.2f, 0.2f);
            surfaceTool.SetMaterial(material);

            var mesh = surfaceTool.Commit();

            _selectionBox.Mesh = mesh;
            _selectionBox.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            _selectionBox.Visible = false;
        }
    }
}
