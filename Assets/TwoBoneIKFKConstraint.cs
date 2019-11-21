using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent, AddComponentMenu ("Animation Rigging/Custom/Two Bone IK FK Constraint")]
public class TwoBoneIKFKConstraint : RigConstraint<TwoBoneIKFKConstraintJob, TwoBoneIKFKConstraintData, TwoBoneIKFKConstraintBinder> { }

[BurstCompile]
public struct TwoBoneIKFKConstraintJob : IWeightedAnimationJob {

    public ReadWriteTransformHandle Root;
    public ReadWriteTransformHandle Mid;
    public ReadWriteTransformHandle Tip;

    public ReadOnlyTransformHandle IK_Target;
    public ReadOnlyTransformHandle IK_Hint;

    public ReadOnlyTransformHandle FK_Root;
    public ReadOnlyTransformHandle FK_Mid;
    public ReadOnlyTransformHandle FK_Tip;

    public ReadWriteTransformHandle Slider;

    public Vector2 LinkLengths;

    public FloatProperty jobWeight { get; set; }

    public void ProcessRootMotion (AnimationStream stream) { }

    public void ProcessAnimation (AnimationStream stream) {
        float w = jobWeight.Get (stream);

        var sliderPos = Slider.GetLocalPosition (stream);
        var t = Mathf.Clamp01 (sliderPos.y);
        Slider.SetLocalPosition (stream, new Vector3 (0, t, 0));

        if (w > 0f) {
            var rootRot = Root.GetRotation (stream);
            var midRot = Mid.GetRotation (stream);
            var tipRot = Tip.GetRotation (stream);

            var rootRotFK = Quaternion.Lerp (rootRot, FK_Root.GetRotation (stream), w);
            var midRotFK = Quaternion.Lerp (midRot, FK_Mid.GetRotation (stream), w);
            var tipRotFK = tipRot;

            AnimationRuntimeUtils.SolveTwoBoneIK (
                stream, Root, Mid, Tip, IK_Target, IK_Hint,
                posWeight : 1f * w,
                rotWeight : 0 * w,
                hintWeight : 1f * w,
                limbLengths : LinkLengths,
                targetOffset : AffineTransform.identity
            );
            var rootRotIK = Root.GetRotation (stream);
            var midRotIK = Mid.GetRotation (stream);
            var tipRotIK = Tip.GetRotation (stream);

            Root.SetRotation (stream, Quaternion.Lerp (rootRotFK, rootRotIK, t));
            Mid.SetRotation (stream, Quaternion.Lerp (midRotFK, midRotIK, t));
            Tip.SetRotation (stream, Quaternion.Lerp (tipRotFK, tipRotIK, t));
        }
    }
}

[System.Serializable]
public struct TwoBoneIKFKConstraintData : IAnimationJobData {

    public Transform Root;
    public Transform Mid;
    public Transform Tip;

    [SyncSceneToStream] public Transform IK_Target;
    [SyncSceneToStream] public Transform IK_Hint;

    [SyncSceneToStream] public Transform FK_Root;
    [SyncSceneToStream] public Transform FK_Mid;

    [SyncSceneToStream] public Transform Slider;

    public bool IsValid () => !(Tip == null || Mid == null || Root == null || IK_Target == null || FK_Root == null || FK_Mid == null || Slider == null);

    public void SetDefaultValues () {
        Root = null;
        Mid = null;
        Tip = null;
        IK_Target = null;
        IK_Hint = null;
        FK_Root = null;
        FK_Mid = null;
        Slider = null;
    }
}

public class TwoBoneIKFKConstraintBinder : AnimationJobBinder<TwoBoneIKFKConstraintJob, TwoBoneIKFKConstraintData> {

    public override TwoBoneIKFKConstraintJob Create (Animator animator, ref TwoBoneIKFKConstraintData data, Component component) {
        var job = new TwoBoneIKFKConstraintJob ();

        job.Root = ReadWriteTransformHandle.Bind (animator, data.Root);
        job.Mid = ReadWriteTransformHandle.Bind (animator, data.Mid);
        job.Tip = ReadWriteTransformHandle.Bind (animator, data.Tip);

        job.IK_Target = ReadOnlyTransformHandle.Bind (animator, data.IK_Target);
        if (data.IK_Hint != null)
            job.IK_Hint = ReadOnlyTransformHandle.Bind (animator, data.IK_Hint);

        job.FK_Root = ReadOnlyTransformHandle.Bind (animator, data.FK_Root);
        job.FK_Mid = ReadOnlyTransformHandle.Bind (animator, data.FK_Mid);

        job.Slider = ReadWriteTransformHandle.Bind (animator, data.Slider);

        job.LinkLengths[0] = Vector3.Distance (data.Root.position, data.Mid.position);
        job.LinkLengths[1] = Vector3.Distance (data.Mid.position, data.Tip.position);

        return job;
    }

    public override void Destroy (TwoBoneIKFKConstraintJob job) { }
}
