using Godot;
using System;

namespace MC
{
    public partial class AnimatedCharacter : Node3D
    {
        [Export] Skeleton3D _skeleton;
        [Export] AnimationTree _animationTree;

        [Export] string _rootBoneName = "Root";
        [Export] string _headBoneName = "Neck";
        [Export] float _idleWalkBlendDuration = 0.3f;

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

        [Export] public float SkeletonYawAngle
        {
            get { return _skeletonYawAngle; }
            set
            {
                _skeletonYawAngle = value;
                _skeleton.Rotate(Vector3.Up, value);
            }
        }
        float _skeletonYawAngle;

        [Export] public Vector3 HeadGlobalLookAtVector
        {
            get { return _headGlobalLookAtVector; }
            set
            {
                _headGlobalLookAtVector = value;
                var localVector = (_skeleton.GlobalTransform.Basis.Inverse() * value).Normalized();

                //GD.Print($"local vector: {localVector}");

                _headLocalForwardVector = (new Vector3(localVector.X, 0f, localVector.Z)).Normalized();
                var dot = Vector2.Right.Dot((new Vector2(localVector.X, localVector.Z)).Normalized());

                //GD.Print($"dot: {dot}");

                if (dot < 0)
                {
                    var theta = Mathf.Acos(dot);
                    var rotDir = (localVector.Z > 0 ? -1 : 1);
                    SkeletonYawAngle = (theta - Mathf.Pi / 2f) * rotDir;
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


        [Export]
        public Vector3 WalkDirection
        {
            get { return _walkDirection; }
            set
            {
                _walkDirection = value;

            }
        }
        Vector3 _walkDirection = new();
        Vector3 _walkDirectionFinalValue = new();

        public override void _Ready()
        {
            _headBoneId = _skeleton.FindBone(_headBoneName);
            _rootBoneId = _skeleton.FindBone(_rootBoneName);
        }

        public void StartIdleWalkBlendAnimation(float finalValue)
        {
            if (finalValue == _idleWalkBlendParamFinalValue)
                return;

            _idleWalkTween?.Kill();
            _idleWalkTween = CreateTween();
            _idleWalkTween.TweenProperty(this, nameof(IdleWalkBlendParam), finalValue, _idleWalkBlendDuration * Mathf.Abs(IdleWalkBlendParam - finalValue));
            _idleWalkTween.TweenCallback(Callable.From(() => { _idleWalkBlendParamFinalValue = -1f; }));

            _idleWalkBlendParamFinalValue = finalValue;
        }

        public void StartWalkDirectionChangeAnimation(Vector3 finalGlobalDirection)
        {
            var finalLocalDirection = (_skeleton.GlobalTransform.Basis.Inverse() * finalGlobalDirection).Normalized();
            if (_headLocalForwardVector.Dot(finalLocalDirection) < 0)
                finalLocalDirection = -finalLocalDirection;

            if (finalLocalDirection == _walkDirectionFinalValue)
                return;

            _walkDirectionTween?.Kill();
            _walkDirectionTween = CreateTween();
            //_walkDirectionTween.TweenProperty(this, nameof());
        }

    }

}