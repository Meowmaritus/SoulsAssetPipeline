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

        public SIBCAM2 Sibcam;

        public List<View> BakedFrames;

        private void BakeSibcam()
        {
            float lastKeyValue_Fov = 0.750492f; //43 degrees lol

            BakedFrames = new List<View>();
            foreach (var c in Sibcam.Cams)
            {
                int hkxFrameCount = c.Animation.Frames.Count;
                View[] thisCutBakedFrames = new View[hkxFrameCount];

                int lastKeyIndex = -1;
                var lastKeyValue_Motion = NewBlendableTransform.Identity;
                for (int frame = 0; frame < c.Animation.Frames.Count; frame++)
                //foreach (var keyPos in CurrentCutSibcam.CameraAnimation)
                {
                    var frameInfo = c.Animation.Frames[frame];
                    //int frame = (int)keyPos.Index;

                    var currentKeyValue_Motion = SibcamAnimFrameToTransform(frameInfo.Baked, c, c.Animation);

                    if (frame >= 0 && frame < thisCutBakedFrames.Length)
                        thisCutBakedFrames[frame].MoveMatrix = currentKeyValue_Motion;

                    // Fill in from the last keyframe to this one
                    for (int f = Math.Max(lastKeyIndex + 1, 0); f <= Math.Min(frame - 1, thisCutBakedFrames.Length - 1); f++)
                    {
                        float lerpS = 1f * (f - lastKeyIndex) / (frame - lastKeyIndex);
                        var blendFrom = lastKeyValue_Motion;
                        var blendTo = currentKeyValue_Motion;

                        var blended = NewBlendableTransform.Lerp(blendFrom, blendTo, lerpS);

                        thisCutBakedFrames[f].MoveMatrix = blended;
                    }
                    lastKeyIndex = frame;
                    lastKeyValue_Motion = currentKeyValue_Motion;
                }
                // Fill in from last key to end of animation.
                for (int f = Math.Max(lastKeyIndex + 1, 0); f <= thisCutBakedFrames.Length - 1; f++)
                {
                    thisCutBakedFrames[f].MoveMatrix = lastKeyValue_Motion;
                }



                lastKeyIndex = -1;
                if (c.Fov != null)
                {
                    lastKeyValue_Fov = c.Fov.DefaultFov;
                    foreach (var keyPos in c.Fov.Samples)
                    {
                        int frame = (int)keyPos.FrameIndex;

                        float currentKeyValue_Fov = keyPos.Fov;

                        if (frame >= 0 && frame < thisCutBakedFrames.Length)
                            thisCutBakedFrames[frame].Fov = currentKeyValue_Fov;

                        // Fill in from the last keyframe to this one
                        for (int f = Math.Max(lastKeyIndex + 1, 0); f <= Math.Min(frame - 1, thisCutBakedFrames.Length - 1); f++)
                        {
                            float lerpS = 1f * (f - lastKeyIndex) / (frame - lastKeyIndex);
                            var blendFrom = lastKeyValue_Fov;
                            var blendTo = currentKeyValue_Fov;
                            var blended = SapMath.Lerp(blendFrom, blendTo, lerpS);

                            thisCutBakedFrames[f].Fov = blended;
                        }
                        lastKeyIndex = frame;
                        lastKeyValue_Fov = currentKeyValue_Fov;
                    }

                    //// Fill in from last key to end of animation.
                    //for (int f = Math.Max(lastKeyIndex + 1, 0); f <= thisCutBakedFrames.Length - 1; f++)
                    //{
                    //    thisCutBakedFrames[f].Fov = lastKeyValue_Fov;
                    //}
                }

                // Fill in from last key to end of animation.
                for (int f = Math.Max(lastKeyIndex + 1, 0); f <= thisCutBakedFrames.Length - 1; f++)
                {
                    thisCutBakedFrames[f].Fov = lastKeyValue_Fov;
                }


                BakedFrames.AddRange(thisCutBakedFrames);
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

        public SibcamPlayer(SIBCAM2 sibcam)
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
                if (newTime > (Sibcam.LastFrame) * FRAME)
                    newTime = (Sibcam.LastFrame) * FRAME;
                SetTime(newTime);
            }
        }

        public void SetTime(float time)
        {
            if (IsLoop)
            {
                time %= (Sibcam.LastFrame + 1) * FRAME;
            }
            else
            {
                if (time >= (Sibcam.LastFrame) * FRAME)
                {
                    time = (Sibcam.LastFrame) * FRAME;
                    IsFinish = true;
                }
            }

            Time = time;

            var frame = (Time / 0.033333333333f);

            if (BakedFrames.Count > 0)
            {

                if (frame < 0)
                    frame = 0;

                if (frame >= BakedFrames.Count)
                    frame = BakedFrames.Count - 1;

                var curFrame = BakedFrames[(int)Math.Floor(frame)].MoveMatrix;
                var curFov = BakedFrames[(int)Math.Floor(frame)].Fov;

                var nextFrame = curFrame;
                var nextFov = curFov;

                if (frame >= BakedFrames.Count - 1)
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

        public static NewBlendableTransform SibcamAnimFrameToTransform(SIBCAM2.CamAnimation.FrameInfo.BakedData f, SIBCAM2.Cam c, SIBCAM2.CamAnimation ca)
        {
            return new NewBlendableTransform()
            {
                Translation = (f.Position + (c.Translation * new Vector3(1, 1, 0))) * new System.Numerics.Vector3(1, 1, -1),
                Scale = f.Scale,
                Rotation = Quaternion.CreateFromRotationMatrix(

                    Matrix4x4.CreateRotationX(-(f.Rotation.X + (-c.Rotation.X) + (-ca.Rotation1.X * 0))) *
                    Matrix4x4.CreateRotationZ(f.Rotation.Z + (c.Rotation.Z) + (ca.Rotation1.Z * 0)) *
                    Matrix4x4.CreateRotationY(-f.Rotation.Y + (-c.Rotation.Y) + (-ca.Rotation1.Y * 0))
                    ),
            };
        }
    }
}
