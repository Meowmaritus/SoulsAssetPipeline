using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace SoulsAssetPipeline.Animation
{
    public class ArmIKSolver
    {
        public Func<int, NewBlendableTransform> GetBoneLocal;
        public Action<int, NewBlendableTransform> SetBoneLocal;

        public Func<int, NewBlendableTransform> GetBoneFK;
        public Action<int, NewBlendableTransform> SetBoneFK;
        public Func<int, Quaternion, NewBlendableTransform> RotateBoneLocal;

        public int upperArmIndex;
        public int forearmIndex;
        public int handIndex;
        public NewBlendableTransform elbow;
        public NewBlendableTransform target;

        public NewBlendableTransform upperArm
        {
            get => GetBoneFK(upperArmIndex);
            set => SetBoneFK(upperArmIndex, value);
        }

        public NewBlendableTransform forearm
        {
            get => GetBoneFK(forearmIndex);
            set => SetBoneFK(forearmIndex, value);
        }

        public NewBlendableTransform hand
        {
            get => GetBoneFK(handIndex);
            set => SetBoneFK(handIndex, value);
        }

        public NewBlendableTransform upperArm_Local
        {
            get => GetBoneLocal(upperArmIndex);
            set => SetBoneLocal(upperArmIndex, value);
        }

        public NewBlendableTransform forearm_Local
        {
            get => GetBoneLocal(forearmIndex);
            set => SetBoneLocal(forearmIndex, value);
        }

        public NewBlendableTransform hand_Local
        {
            get => GetBoneLocal(handIndex);
            set => SetBoneLocal(handIndex, value);
        }


        public Quaternion uppperArm_OffsetRotation = Quaternion.Identity;
        public Quaternion forearm_OffsetRotation = Quaternion.Identity;
        public Quaternion hand_OffsetRotation = Quaternion.Identity;

        //public NewBlendableTransform upperArm_Parent;
        //public NewBlendableTransform forearm_Parent;
        //public NewBlendableTransform hand_Parent;


        public bool handMatchesTargetRotation = true;

        float angle;
        float upperArm_Length;
        float forearm_Length;
        float arm_Length;
        float targetDistance;
        float adyacent;

        public void IterateTowardTarget(float slerpRatio)
        {

            //var h = hand;
            //h.Rotation = SapMath.GetDeltaQuaternionWithDirectionVectors(
            //    Vector3.Normalize(hand.GetForward()), Vector3.Normalize(target.Translation - hand.Translation)) * h.Rotation;
            //hand = h;



            var f = forearm;
            f.Rotation = Quaternion.Slerp(Quaternion.Identity, SapMath.GetDeltaQuaternionWithDirectionVectors(
                Vector3.Normalize(hand.Translation - forearm.Translation), Vector3.Normalize(target.Translation - forearm.Translation)), slerpRatio) * f.Rotation;
            forearm = f;


            var u = upperArm;
            u.Rotation = Quaternion.Slerp(Quaternion.Identity, SapMath.GetDeltaQuaternionWithDirectionVectors(
                Vector3.Normalize(hand.Translation - upperArm.Translation), Vector3.Normalize(target.Translation - upperArm.Translation)), slerpRatio) * u.Rotation;
            upperArm = u;




            //upperArm = upperArm.FKLookAt(target, elbow.Translation - upperArm.Translation);

            //upperArm = RotateBoneLocal(upperArmIndex, uppperArm_OffsetRotation);

            //Vector3 cross = Vector3.Cross(elbow.Translation - upperArm.Translation, forearm.Translation - upperArm.Translation);

            //upperArm_Length = Vector3.Distance(upperArm.Translation, forearm.Translation);

            //forearm_Length = Vector3.Distance(forearm.Translation, hand.Translation);
            //arm_Length = upperArm_Length + forearm_Length;
            //targetDistance = Vector3.Distance(upperArm.Translation, target.Translation);
            //targetDistance = (float)Math.Min(targetDistance, arm_Length - arm_Length * 0.001f);

            //adyacent = ((upperArm_Length * upperArm_Length) - (forearm_Length * forearm_Length) + (targetDistance * targetDistance)) / (targetDistance * 2);

            //angle = (float)Math.Acos(adyacent / upperArm_Length);
            //var upArm = upperArm;
            //upArm = upArm.FKRotateAround(upArm.Translation, cross, -angle);
            ////upArm.Rotation *= Quaternion.CreateFromAxisAngle(cross, -angle);
            //upperArm = upArm;


            //forearm = forearm.FKLookAt(target, cross);

            //forearm = RotateBoneLocal(forearmIndex, forearm_OffsetRotation);


            //if (handMatchesTargetRotation)
            //{
            //    var h = hand;
            //    h.Rotation = target.Rotation;
            //    hand = h;

            //    hand = RotateBoneLocal(handIndex, hand_OffsetRotation);
            //}
        }
    }
}