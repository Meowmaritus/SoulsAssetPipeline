using SoulsAssetPipeline.Animation;
using SoulsAssetPipeline.Animation.SIBCAM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation.SIBCAM
{
    public class SibcamPlayer
    {
        public bool IsPlaying = true;
        public bool IsLoop = false;
        public bool IsFinish = false;
        public float Time { get; private set; } = 0;

        public SIBCAM Sibcam;

        public View[] BakedFrames;

        private void BakeSibcam()
        {
            int hkxFrameCount = (int)Sibcam.NumFrames;
            BakedFrames = new View[hkxFrameCount];

            int lastKeyIndex = -1;
            var lastKeyValue_Motion = NewBlendableTransform.Identity;
            for (int frame = 0; frame < Sibcam.CameraAnimation.Count; frame++)
            //foreach (var keyPos in CurrentCutSibcam.CameraAnimation)
            {
                var keyPos = Sibcam.CameraAnimation[frame];
                //int frame = (int)keyPos.Index;

                var currentKeyValue_Motion = SibcamAnimFrameToTransform(keyPos);

                if (frame >= 0 && frame < BakedFrames.Length)
                    BakedFrames[frame].MoveMatrix = currentKeyValue_Motion;

                // Fill in from the last keyframe to this one
                for (int f = Math.Max(lastKeyIndex + 1, 0); f <= Math.Min(frame - 1, BakedFrames.Length - 1); f++)
                {
                    float lerpS = 1f * (f - lastKeyIndex) / (frame - lastKeyIndex);
                    var blendFrom = lastKeyValue_Motion;
                    var blendTo = currentKeyValue_Motion;

                    var blended = NewBlendableTransform.Lerp(blendFrom, blendTo, lerpS);

                    BakedFrames[f].MoveMatrix = blended;
                }
                lastKeyIndex = frame;
                lastKeyValue_Motion = currentKeyValue_Motion;
            }
            // Fill in from last key to end of animation.
            for (int f = Math.Max(lastKeyIndex + 1, 0); f <= BakedFrames.Length - 1; f++)
            {
                BakedFrames[f].MoveMatrix = lastKeyValue_Motion;
            }



            lastKeyIndex = -1;
            float lastKeyValue_Fov = Sibcam.InitialFoV;
            foreach (var keyPos in Sibcam.FoVDataList)
            {
                int frame = (int)keyPos.FrameIdx;

                float currentKeyValue_Fov = keyPos.FoV;

                if (frame >= 0 && frame < BakedFrames.Length)
                    BakedFrames[frame].Fov = currentKeyValue_Fov;

                // Fill in from the last keyframe to this one
                for (int f = Math.Max(lastKeyIndex + 1, 0); f <= Math.Min(frame - 1, BakedFrames.Length - 1); f++)
                {
                    float lerpS = 1f * (f - lastKeyIndex) / (frame - lastKeyIndex);
                    var blendFrom = lastKeyValue_Fov;
                    var blendTo = currentKeyValue_Fov;
                    var blended = SapMath.Lerp(blendFrom, blendTo, lerpS);

                    BakedFrames[f].Fov = blended;
                }
                lastKeyIndex = frame;
                lastKeyValue_Fov = currentKeyValue_Fov;
            }
            // Fill in from last key to end of animation.
            for (int f = Math.Max(lastKeyIndex + 1, 0); f <= BakedFrames.Length - 1; f++)
            {
                BakedFrames[f].Fov = lastKeyValue_Fov;
            }
        }

        public struct View
        {
            public NewBlendableTransform MoveMatrix;
            public float Fov;
            public static View Default => new View()
            {
                MoveMatrix = NewBlendableTransform.Identity,
                Fov = 1,
            };
        }

        public View CurrentView = View.Default;

        public SibcamPlayer(SIBCAM sibcam)
        {
            Sibcam = sibcam;
            BakeSibcam();
        }
        const float FRAME = 0.033333333333f;

        public void UpdatePlayback(float deltaTime)
        {
            if (IsPlaying)
            {
                var newTime = Time + deltaTime;
                if (newTime > (BakedFrames.Length - 1) * FRAME)
                    newTime = (BakedFrames.Length - 1) * FRAME;
                SetTime(newTime);
            }
        }

        public void SetTime(float time)
        {
            if (IsLoop)
            {
                time %= Sibcam.NumFrames * FRAME;
            }
            else
            {
                if (time > Sibcam.NumFrames * FRAME)
                {
                    time = Sibcam.NumFrames * FRAME;
                    IsFinish = true;
                }
            }

            Time = time;

            var frame = (Time / 0.033333333333f);

            if (BakedFrames.Length > 0)
            {

                if (frame < 0)
                    frame = 0;

                if (frame >= BakedFrames.Length)
                    frame = BakedFrames.Length - 1;

                var curFrame = BakedFrames[(int)Math.Floor(frame)].MoveMatrix;
                var curFov = BakedFrames[(int)Math.Floor(frame)].Fov;

                var nextFrame = curFrame;
                var nextFov = curFov;

                if (frame >= Sibcam.NumFrames - 1)
                {
                    nextFrame = IsLoop ? BakedFrames[0].MoveMatrix : curFrame;
                    nextFov = IsLoop ? BakedFrames[0].Fov : curFov;
                }
                else
                {
                    nextFrame = BakedFrames[(int)Math.Ceiling(frame)].MoveMatrix;
                    nextFov = BakedFrames[(int)Math.Ceiling(frame)].Fov;
                }
                var s = frame % 1;
                var finalTransform = NewBlendableTransform.Lerp(curFrame, nextFrame, s);
                CurrentView.MoveMatrix = finalTransform;
            }
        }

        public static NewBlendableTransform SibcamAnimFrameToTransform(SIBCAM.CameraFrame f)
        {
            return new NewBlendableTransform()
            {
                Translation = f.Position * new System.Numerics.Vector3(1, 1, -1),
                Scale = f.Scale,
                Rotation = Quaternion.CreateFromRotationMatrix(

                    Matrix4x4.CreateRotationX(-(f.Rotation.X + SapMath.Pi * 0.5f)) *
                    Matrix4x4.CreateRotationZ(f.Rotation.Z) *
                    Matrix4x4.CreateRotationY(-f.Rotation.Y)
                    ),
            };
        }
    }
}
