using Godot;
using System;
using System.Reflection.Metadata;

namespace MC
{
    public partial class AnimatedCharacter : Node3D
    {
        [Export] Skeleton3D _skeleton;
        [Export] AnimationTree _animationTree;

        [Export] string _rootBoneName = "Root";
        [Export] string _headBoneName = "Neck";
        [Export] float _idleWalkBlendDuration = 0.3f;
        [Export] float _walkDirectionChangeSpeed = 0.3f;

        int _headBoneId = -1;
        int _rootBoneId = -1;

        Tween _idleWalkTween;
        Tween _walkDirectionTween;

        [Export] public Quaternion HeadQuaternion
        {
            get { return _headQuaternion; }
            set
            {
                _headQuaternion = value.Normalized();
                _skeleton.SetBonePoseRotation(_headBoneId, _headQuaternion);
            }
        }
        Quaternion _headQuaternion;

        [Export] public Vector3 HeadGlobalLookAtVector
        {
            get { return _headGlobalLookAtVector; }
            set
            {
                _headGlobalLookAtVector = value;
                var localVector = (_skeleton.GlobalTransform.Basis.Inverse() * value).Normalized();

                _headLocalForwardVector = (new Vector3(localVector.X, 0f, localVector.Z)).Normalized();

                var angle = GetAngleToRightVector(_headLocalForwardVector);

                var delta = Mathf.Abs(angle) - Mathf.Pi / 2f;
                if (delta >= 0)
                {
                    _skeleton.RotateY(delta * Mathf.Sign(angle));
                }

                HeadQuaternion = (new Quaternion(Vector3.Right, localVector)).Normalized();
            }
        }
        Vector3 _headGlobalLookAtVector;
        Vector3 _headLocalForwardVector = new();

        [Export] public float IdleWalkBlendParam
        {
            get { return _idleWalkBlendParam; }
            set
            {
                _idleWalkBlendParam = value;
                _animationTree.Set("parameters/IdleWalkBlend/blend_amount", value);
            }
        }
        float _idleWalkBlendParam;
        float _idleWalkBlendParamFinalValue = -1f;

        [Export] public int InteractCount
        {
            get { return _interactCount; }
            set
            {
                _interactCount = value;
                _animationTree.Set("parameters/InteractOneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
            }
        }
        int _interactCount = 0;

        float WalkDirectionValue
        {
            get { return _walkDirectionValue; }
            set
            {
                _skeleton.RotateY(value - _walkDirectionValue);
                _walkDirectionValue = value;
                HeadGlobalLookAtVector = _headGlobalLookAtVector;
            }
        }
        float _walkDirectionValue;

        public override void _Ready()
        {
            _headBoneId = _skeleton.FindBone(_headBoneName);
            _rootBoneId = _skeleton.FindBone(_rootBoneName);

            _skeleton = GetNode<Skeleton3D>("%Skeleton3D");
            _animationTree = GetNode<AnimationTree>("%AnimationTree");
        }

        public void SetInvisible()
        {
            var meshs = _skeleton.GetChildren();
            foreach (var mesh in meshs)
            {
                if (mesh is MeshInstance3D mesh3d)
                {
                    mesh3d.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
                }
            }
        }

        public void StartIdleWalkBlendAnimation(float finalValue)
        {
            if (finalValue == _idleWalkBlendParamFinalValue)
                return;

            _idleWalkTween?.Kill();
            _idleWalkTween = CreateTween();
            _idleWalkTween.TweenProperty(this, nameof(IdleWalkBlendParam), finalValue, _idleWalkBlendDuration * Mathf.Abs(IdleWalkBlendParam - finalValue));

            _idleWalkBlendParamFinalValue = finalValue;
        }

        public void StartWalkDirectionChangeAnimation(Vector3 finalGlobalDirection)
        {
            var finalLocalDirection = (_skeleton.GlobalTransform.Basis.Inverse() * finalGlobalDirection).Normalized();
            if (_headLocalForwardVector.Dot(finalLocalDirection) < 0)
                finalLocalDirection = -finalLocalDirection;
            var finalValue = GetAngleToRightVector(finalLocalDirection);

            _walkDirectionTween?.Kill();
            _walkDirectionTween = CreateTween();
            _walkDirectionValue = 0f;
            float duration = _walkDirectionChangeSpeed * Mathf.Abs(finalValue);
            _walkDirectionTween.TweenProperty(this, nameof(WalkDirectionValue), finalValue, duration);
        }

        float GetAngleToRightVector(Vector3 vector)
        {
            return Vector3.Right.SignedAngleTo(vector, Vector3.Up);
        }
    }

}