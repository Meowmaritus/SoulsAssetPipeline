using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SoulsAssetPipeline.Animation
{
    
    public abstract class HavokAnimationData
    {
        public long ID { get; internal set; }
        public string Name { get; internal set;  }

        public RootMotionData RootMotion { get; internal set; }

        public HKX.AnimationBlendHint BlendHint = HKX.AnimationBlendHint.NORMAL;

        public bool IsAdditiveBlend => BlendHint == HKX.AnimationBlendHint.ADDITIVE_CHILD_SPACE ||
            BlendHint == HKX.AnimationBlendHint.ADDITIVE_PARENT_SPACE;

        public HKX.HKASkeleton hkaSkeleton;

        public float Duration;
        public float FrameDuration;
        public int FrameCount;

        // Index into array = hkx bone index, result = transform track index.
        public int[] HkxBoneIndexToTransformTrackMap;

        public int[] TransformTrackIndexToHkxBoneMap;

        /// <summary>
        /// Gives dictionary of (index into this anims transform tracks, bone name they're mapped to).
        /// Any tracks not mapped to bones are excluded.
        /// </summary>
        public Dictionary<int, string> GetTransformTrackBoneNameMapping(IEnumerable<string> boneNameWhitelist = null)
        {
            var res = new Dictionary<int, string>();
            var bones = hkaSkeleton.Bones.GetArrayData().Elements;
            for (int i = 0; i < TransformTrackIndexToHkxBoneMap.Length; i++)
            {
                var b = TransformTrackIndexToHkxBoneMap[i];
                if (b >= 0 && b < bones.Count)
                {
                    var bn = bones[b].Name.GetString();

                    if (boneNameWhitelist == null || boneNameWhitelist.Contains(bn))
                        res.Add(i, bn);
                }
            }
            return res;
        }

        public HavokAnimationData(long id, string name)
        {
            ID = id;
            Name = name;
        }

        public HavokAnimationData(long id, string name, HKX.HKASkeleton skeleton, HKX.HKADefaultAnimatedReferenceFrame refFrame, HKX.HKAAnimationBinding binding)
        {
            ID = id;
            Name = name;

            hkaSkeleton = skeleton;

            if (refFrame != null)
            {
                RootMotion = new RootMotionData(refFrame);
            }

            BlendHint = binding?.BlendHint ?? HKX.AnimationBlendHint.NORMAL;
        }

        public NewBlendableTransform GetTransformOnFrameByBone(int hkxBoneIndex, float frame, bool enableLooping)
        {
            var track = HkxBoneIndexToTransformTrackMap[hkxBoneIndex];

            if (track == -1)
            {
                var skeleTransform = hkaSkeleton.Transforms[hkxBoneIndex];

                NewBlendableTransform defaultBoneTransformation = new NewBlendableTransform();

                defaultBoneTransformation.Scale.X = skeleTransform.Scale.Vector.X;
                defaultBoneTransformation.Scale.Y = skeleTransform.Scale.Vector.Y;
                defaultBoneTransformation.Scale.Z = skeleTransform.Scale.Vector.Z;

                defaultBoneTransformation.Rotation = new Quaternion(
                    skeleTransform.Rotation.Vector.X,
                    skeleTransform.Rotation.Vector.Y,
                    skeleTransform.Rotation.Vector.Z,
                    skeleTransform.Rotation.Vector.W);

                defaultBoneTransformation.Translation.X = skeleTransform.Position.Vector.X;
                defaultBoneTransformation.Translation.Y = skeleTransform.Position.Vector.Y;
                defaultBoneTransformation.Translation.Z = skeleTransform.Position.Vector.Z;

                return defaultBoneTransformation;
            }

            return GetTransformOnFrame(track, frame, enableLooping);
        }
        public abstract NewBlendableTransform GetTransformOnFrame(int transformIndex, float frame, bool enableLooping);

    }

    public class HavokAnimationData_Dummy : HavokAnimationData
    {
        public HavokAnimationData_Dummy(long id, string name)
            : base(id, name)
        {
            
        }

        public override NewBlendableTransform GetTransformOnFrame(int transformIndex, float frame, bool enableLooping)
        {
            return NewBlendableTransform.Identity;
        }
    }

    public class HavokAnimationData_SplineCompressed : HavokAnimationData
    {
        public HavokAnimationData_SplineCompressed(long id, string name, HKX.HKASplineCompressedAnimation anim)
           : base(id, name)
        {
            Duration = anim.Duration;// Math.Max(anim.Duration, anim.FrameDuration * anim.FrameCount);
            FrameCount = anim.FrameCount;

            FrameDuration = anim.FrameDuration;

            BlockCount = anim.BlockCount;
            NumFramesPerBlock = anim.FramesPerBlock;

            Tracks = SplineCompressedAnimation.ReadSplineCompressedAnimByteBlock(
                isBigEndian: false, anim.GetData(), anim.TransformTrackCount, anim.BlockCount);
        }


        public HavokAnimationData_SplineCompressed(long id, string name, HKX.HKASkeleton skeleton,
           HKX.HKADefaultAnimatedReferenceFrame refFrame, HKX.HKAAnimationBinding binding, HKX.HKASplineCompressedAnimation anim)
           : base(id, name, skeleton, refFrame, binding)
        {
            Duration = anim.Duration;// Math.Max(anim.Duration, anim.FrameDuration * anim.FrameCount);
            FrameCount = anim.FrameCount;

            FrameDuration = anim.FrameDuration;

            BlockCount = anim.BlockCount;
            NumFramesPerBlock = anim.FramesPerBlock;

            HkxBoneIndexToTransformTrackMap = new int[skeleton.Bones.Size];
            TransformTrackIndexToHkxBoneMap = new int[binding.TransformTrackToBoneIndices.Size];

            for (int i = 0; i < binding.TransformTrackToBoneIndices.Size; i++)
            {
                TransformTrackIndexToHkxBoneMap[i] = -1;
            }

            for (int i = 0; i < skeleton.Bones.Size; i++)
            {
                HkxBoneIndexToTransformTrackMap[i] = -1;
            }

            for (int i = 0; i < binding.TransformTrackToBoneIndices.Size; i++)
            {
                short boneIndex = binding.TransformTrackToBoneIndices[i].data;
                if (boneIndex >= 0)
                {
                    if (boneIndex >= HkxBoneIndexToTransformTrackMap.Length)
                        Array.Resize(ref HkxBoneIndexToTransformTrackMap, boneIndex + 1);
                    HkxBoneIndexToTransformTrackMap[boneIndex] = i;
                }
                TransformTrackIndexToHkxBoneMap[i] = boneIndex;
            }

            Tracks = SplineCompressedAnimation.ReadSplineCompressedAnimByteBlock(
                isBigEndian: false, anim.GetData(), anim.TransformTrackCount, anim.BlockCount);
        }

        public List<SplineCompressedAnimation.TransformTrack[]> Tracks;

        public int BlockCount = 1;
        public int NumFramesPerBlock = 255;

        public int GetBlock(float frame)
        {
            return (int)((frame % (FrameCount - 1)) / (NumFramesPerBlock - 1));
        }

        private NewBlendableTransform GetTransformOnSpecificBlockAndFrame(int transformIndex, int block, float frame, bool enableLooping)
        {
            //if (transformIndex == 172)
            //{
            //    Console.WriteLine("teste");
            //}
            ////Bruh
            //if (frame < 1000)
            //    frame = 0;

            //while (frame < 0)
            //    frame += FrameCount;

            //if (frame < 0)
            //{
            //    var loopMult = MathF.Ceiling(Math.Abs(frame) / FrameCount);
            //    frame += (FrameCount) * loopMult;
            //}

            if (block < 0)
                block = 0;

            frame = (enableLooping ? (frame % (FrameCount - 1)) : (Math.Min(frame, FrameCount))) % (NumFramesPerBlock - 1);
            
            NewBlendableTransform result = NewBlendableTransform.Identity;
            var track = Tracks[block][transformIndex];
            var skeleTransform = hkaSkeleton != null ? hkaSkeleton.Transforms[TransformTrackIndexToHkxBoneMap[transformIndex]] : new HKX.Transform() { Scale = new HKX.HKVector4(new Vector4(1,1,1,1)) };

            //result.Scale.X = track.SplineScale?.ChannelX == null
            //    ? (IsAdditiveBlend ? 1 : track.StaticScale.X) : track.SplineScale.GetValueX(frame);
            //result.Scale.Y = track.SplineScale?.ChannelY == null
            //    ? (IsAdditiveBlend ? 1 : track.StaticScale.Y) : track.SplineScale.GetValueY(frame);
            //result.Scale.Z = track.SplineScale?.ChannelZ == null
            //    ? (IsAdditiveBlend ? 1 : track.StaticScale.Z) : track.SplineScale.GetValueZ(frame);

            if (track.SplineScale != null)
            {
                result.Scale.X = track.SplineScale.GetValueX(frame, FrameCount)
                    ?? (IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.X);

                result.Scale.Y = track.SplineScale.GetValueY(frame, FrameCount)
                    ?? (IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.Y);

                result.Scale.Z = track.SplineScale.GetValueZ(frame, FrameCount)
                    ?? (IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.Z);
            }
            else
            {
                if (track.Mask.ScaleTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticX))
                    result.Scale.X = track.StaticScale.X;
                else
                    result.Scale.X = IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.X;

                if (track.Mask.ScaleTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticY))
                    result.Scale.Y = track.StaticScale.Y;
                else
                    result.Scale.Y = IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.Y;

                if (track.Mask.ScaleTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticZ))
                    result.Scale.Z = track.StaticScale.Z;
                else
                    result.Scale.Z = IsAdditiveBlend ? 1 : skeleTransform.Scale.Vector.Z;
            }

            //if (IsAdditiveBlend)
            //{
            //    result.Scale.X *= skeleTransform.Scale.Vector.X;
            //    result.Scale.Y *= skeleTransform.Scale.Vector.Y;
            //    result.Scale.Z *= skeleTransform.Scale.Vector.Z;
            //}

            //if (result.Scale.LengthSquared() > (Vector3.One * 1.1f).LengthSquared())
            //{
            //    Console.WriteLine(":fatoof:");
            //}


            if (track.SplineRotation != null)//track.HasSplineRotation)
            {
                result.Rotation = track.SplineRotation.GetValue(frame, FrameCount);
            }
            else if (track.HasStaticRotation)
            {
                // We actually need static rotation or Gael hands become unbent among others
                result.Rotation = track.StaticRotation;
            }
            else
            {
                result.Rotation = Quaternion.Identity;
                //result.Rotation = IsAdditiveBlend ? Quaternion.Identity : new Quaternion(
                //    skeleTransform.Rotation.Vector.X,
                //    skeleTransform.Rotation.Vector.Y,
                //    skeleTransform.Rotation.Vector.Z,
                //    skeleTransform.Rotation.Vector.W);
            }

            //if (IsAdditiveBlend)
            //{
            //    result.Rotation = new Quaternion(
            //        skeleTransform.Rotation.Vector.X,
            //        skeleTransform.Rotation.Vector.Y,
            //        skeleTransform.Rotation.Vector.Z,
            //        skeleTransform.Rotation.Vector.W) * result.Rotation;
            //}

            

            if (track.SplinePosition != null)
            {
                //result.Translation.X = track.SplinePosition.GetValueX(frame)
                //    ?? (IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.X);

                //result.Translation.Y = track.SplinePosition.GetValueY(frame)
                //    ?? (IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.Y);

                //result.Translation.Z = track.SplinePosition.GetValueZ(frame)
                //    ?? (IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.Z);

                result.Translation.X = track.SplinePosition.GetValueX(frame, FrameCount) ?? 0;

                result.Translation.Y = track.SplinePosition.GetValueY(frame, FrameCount) ?? 0;

                result.Translation.Z = track.SplinePosition.GetValueZ(frame, FrameCount) ?? 0;
            }
            else
            {
                //if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticX))
                //    result.Translation.X = track.StaticPosition.X;
                //else
                //    result.Translation.X = IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.X;

                //if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticY))
                //    result.Translation.Y = track.StaticPosition.Y;
                //else
                //    result.Translation.Y = IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.Y;

                //if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticZ))
                //    result.Translation.Z = track.StaticPosition.Z;
                //else
                //    result.Translation.Z = IsAdditiveBlend ? 0 : skeleTransform.Position.Vector.Z;

                if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticX))
                    result.Translation.X = track.StaticPosition.X;
                else
                    result.Translation.X = 0;

                if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticY))
                    result.Translation.Y = track.StaticPosition.Y;
                else
                    result.Translation.Y = 0;

                if (track.Mask.PositionTypes.Contains(SplineCompressedAnimation.FlagOffset.StaticZ))
                    result.Translation.Z = track.StaticPosition.Z;
                else
                    result.Translation.Z = 0;
            }

            //result.Translation.X = track.SplinePosition?.GetValueX(frame) ?? (IsAdditiveBlend ? 0 : track.StaticPosition.X);
            //result.Translation.Y = track.SplinePosition?.GetValueY(frame) ?? (IsAdditiveBlend ? 0 : track.StaticPosition.Y);
            //result.Translation.Z = track.SplinePosition?.GetValueZ(frame) ?? (IsAdditiveBlend ? 0 : track.StaticPosition.Z);

            //if (!IsAdditiveBlend && (!track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.SplineX) &&
            //    !track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.StaticX)))
            //{
            //    result.Translation.X = skeleTransform.Position.Vector.X;
            //}

            //if (!IsAdditiveBlend && (!track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.SplineY) &&
            //    !track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.StaticY)))
            //{
            //    result.Translation.Y = skeleTransform.Position.Vector.Y;
            //}

            //if (!IsAdditiveBlend && (!track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.SplineZ) &&
            //    !track.Mask.PositionTypes.Contains(Havok.SplineCompressedAnimation.FlagOffset.StaticZ)))
            //{
            //    result.Translation.Z = skeleTransform.Position.Vector.Z;
            //}

            //if (IsAdditiveBlend)
            //{
            //    result.Translation.X += skeleTransform.Position.Vector.X;
            //    result.Translation.Y += skeleTransform.Position.Vector.Y;
            //    result.Translation.Z += skeleTransform.Position.Vector.Z;
            //}

            if (float.IsNaN(result.Scale.X))
                result.Scale.X = 1;
            if (float.IsNaN(result.Scale.Y))
                result.Scale.Y = 1;
            if (float.IsNaN(result.Scale.Z))
                result.Scale.Z = 1;



            return result;
        }

        public override NewBlendableTransform GetTransformOnFrame(int transformTrackIndex, float frame, bool enableLooping)
        {
            if (frame < 0)
            {
                var loopMult = MathF.Ceiling(Math.Abs(frame) / FrameCount);
                frame += (FrameCount) * loopMult;

                // Just in case??
                if (frame < 0)
                {
                    frame += FrameCount;
                }
            }

            

            int blockIndex = GetBlock(frame);



            float frameInBlock = (enableLooping ? (frame % (FrameCount - 1)) : (Math.Min(frame, FrameCount))) % (NumFramesPerBlock - 1);

            NewBlendableTransform currentFrame = GetTransformOnSpecificBlockAndFrame(transformTrackIndex,
                    block: blockIndex, frame, enableLooping);
            return currentFrame;

            //if (frame >= FrameCount - 1)
            //{
            //    NewBlendableTransform currentFrame = GetTransformOnSpecificBlockAndFrame(track, 
            //        block: CurrentBlock, frame: (float)Math.Floor(frame));
            //    NewBlendableTransform nextFrame = GetTransformOnSpecificBlockAndFrame(track, block: 0, frame: 0);
            //    currentFrame = NewBlendableTransform.Lerp(frame % 1, currentFrame, nextFrame);
            //    return currentFrame;
            //}
            //// Regular frame
            //else
            //{
            //    NewBlendableTransform currentFrame = GetTransformOnSpecificBlockAndFrame(track, 
            //        block: CurrentBlock, frame);
            //    return currentFrame;
            //}
        }
    }

    public class HavokAnimationData_InterleavedUncompressed : HavokAnimationData
    {
        public int TransformTrackCount { get; }
        public List<NewBlendableTransform> Transforms { get; }

        public HavokAnimationData_InterleavedUncompressed(long id, string name, HKX.HKAInterleavedUncompressedAnimation anim)
            : base(id, name)
        {
            Duration = anim.Duration;// Math.Max(anim.Duration, anim.FrameDuration * anim.FrameCount);
            TransformTrackCount = anim.TransformTrackCount;
            FrameCount = ((int)anim.Transforms.Size / anim.TransformTrackCount);

            FrameDuration = Duration / (FrameCount - 1);

            //Duration += FrameDuration;

            Transforms = new List<NewBlendableTransform>((int)anim.Transforms.Capacity);

            foreach (var t in anim.Transforms.GetArrayData().Elements)
            {
                Transforms.Add(new NewBlendableTransform()
                {
                    Translation = new Vector3(t.Position.Vector.X, t.Position.Vector.Y, t.Position.Vector.Z),
                    Scale = new Vector3(t.Scale.Vector.X, t.Scale.Vector.Y, t.Scale.Vector.Z),
                    Rotation = new Quaternion(t.Rotation.Vector.X, t.Rotation.Vector.Y, t.Rotation.Vector.Z, t.Rotation.Vector.W)
                });
            }
        }

        public HavokAnimationData_InterleavedUncompressed(long id, string name, HKX.HKASkeleton skeleton,
           HKX.HKADefaultAnimatedReferenceFrame refFrame, HKX.HKAAnimationBinding binding, HKX.HKAInterleavedUncompressedAnimation anim)
           : base(id, name, skeleton, refFrame, binding)
        {
            Duration = anim.Duration;// Math.Max(anim.Duration, anim.FrameDuration * anim.FrameCount);
            TransformTrackCount = anim.TransformTrackCount;
            FrameCount = ((int)anim.Transforms.Size / anim.TransformTrackCount);

            FrameDuration = Duration / (FrameCount - 1);

            HkxBoneIndexToTransformTrackMap = new int[skeleton.Bones.Size];
            TransformTrackIndexToHkxBoneMap = new int[binding.TransformTrackToBoneIndices.Size];

            for (int i = 0; i < binding.TransformTrackToBoneIndices.Size; i++)
            {
                TransformTrackIndexToHkxBoneMap[i] = -1;
            }

            for (int i = 0; i < skeleton.Bones.Size; i++)
            {
                HkxBoneIndexToTransformTrackMap[i] = -1;
            }

            for (int i = 0; i < binding.TransformTrackToBoneIndices.Size; i++)
            {
                short boneIndex = binding.TransformTrackToBoneIndices[i].data;
                if (boneIndex >= 0 && boneIndex < skeleton.Bones.Size)
                    HkxBoneIndexToTransformTrackMap[boneIndex] = i;
                TransformTrackIndexToHkxBoneMap[i] = boneIndex;
            }

            Transforms = new List<NewBlendableTransform>((int)anim.Transforms.Capacity);

            foreach (var t in anim.Transforms.GetArrayData().Elements)
            {
                Transforms.Add(new NewBlendableTransform()
                {
                    Translation = new Vector3(t.Position.Vector.X, t.Position.Vector.Y, t.Position.Vector.Z),
                    Scale = new Vector3(t.Scale.Vector.X, t.Scale.Vector.Y, t.Scale.Vector.Z),
                    Rotation = new Quaternion(t.Rotation.Vector.X, t.Rotation.Vector.Y, t.Rotation.Vector.Z, t.Rotation.Vector.W)
                });
            }
        }

        public override NewBlendableTransform GetTransformOnFrame(int transformTrackIndex, float frame, bool enableLooping)
        {
            frame = (enableLooping ? frame % (FrameCount -1) : frame);

            int curFrameIndex = (int)Math.Floor(frame);
            int nextFrameIndex = (int)Math.Ceiling(frame);

            if (enableLooping)
            {
                if (curFrameIndex >= FrameCount - 1)
                    curFrameIndex = 0;
                else if (curFrameIndex < 0)
                    curFrameIndex = FrameCount - 1;

                if (nextFrameIndex >= FrameCount - 1)
                    nextFrameIndex = 0;
                else if (nextFrameIndex < 0)
                    nextFrameIndex = FrameCount - 1;
            }
            else
            {
                if (curFrameIndex > FrameCount - 1)
                    curFrameIndex = FrameCount - 1;
                else if (curFrameIndex < 0)
                    curFrameIndex = 0;

                if (nextFrameIndex > FrameCount - 1)
                    nextFrameIndex = FrameCount - 1;
                else if (nextFrameIndex < 0)
                    nextFrameIndex = 0;
            }

            

            NewBlendableTransform currentFrame = Transforms[(TransformTrackCount * curFrameIndex) + transformTrackIndex];
            NewBlendableTransform nextFrame = Transforms[(TransformTrackCount * nextFrameIndex) + transformTrackIndex];
            return NewBlendableTransform.Lerp(currentFrame, nextFrame, frame % 1);
        }
    }
}
