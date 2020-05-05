using System;
using System.Linq;
using System.Numerics;

namespace SFAnimExtensions.Havok
{
    public class RootMotionData
    {
        //public Matrix CurrentAbsoluteRootMotion = Matrix.Identity;

        public readonly Vector4 Up;
        public readonly Vector4 Forward;
        public readonly float Duration;
        public readonly Vector4[] Frames;

        /// <summary>
        /// The accumulative root motion delta applied by playing the entire anim from the beginning to the end.
        /// </summary>
        public readonly Vector4 LoopDeltaForward;

        /// <summary>
        /// The accumulative root motion delta applied by playing the entire anim in reverse from the end to the beginning.
        /// </summary>
        public readonly Vector4 LoopDeltaBackward;

        public RootMotionData(HKX.HKADefaultAnimatedReferenceFrame refFrame) : this(refFrame.Up, refFrame.Forward, refFrame.Duration, refFrame.ReferenceFrameSamples.GetArrayData().Elements.Select(hkxVector => hkxVector.Vector).ToArray())
        {
        }

        public RootMotionData(Vector4 up, Vector4 forward, float duration, Vector4[] frames)
        {
            Up = up;
            Forward = forward;
            Duration = duration;
            Frames = frames;

            LoopDeltaForward = frames[frames.Length - 1] - frames[0];
            LoopDeltaBackward = frames[0] - frames[frames.Length - 1];
        }


        private Matrix4x4 GetMatrixFromSample(Vector4 sample)
        {
            return Matrix4x4.CreateRotationY(sample.W) *
                Matrix4x4.CreateWorld(
                    new Vector3(sample.X, sample.Y, sample.Z),
                    new Vector3(Forward.X, Forward.Y, -Forward.Z),
                    new Vector3(Up.X, Up.Y, Up.Z));
        }

        public Vector4 GetSample(float time)
        {
            if (time > Duration)
            {
                throw new ArgumentException($"Argument time {time} is bigger than duration of the animation {Duration}");
            }

            float frame = (Frames.Length - 1) * time / Duration;

            float frameFloor = (float)Math.Floor(frame % (Frames.Length));
            Vector4 sample = Frames[(int)frameFloor];

            if (frame != frameFloor)
            {
                float frameMod = frame % 1;

                Vector4 nextFrameRootMotion;

                //if (frame >= Frames.Length - 1)
                //    nextFrameRootMotion = Frames[0];
                //else
                //    nextFrameRootMotion = Frames[(int)(frameFloor + 1)];

                nextFrameRootMotion = Frames[(int)(frameFloor + 1)];

                sample = Vector4.Lerp(sample, nextFrameRootMotion, frameMod);
            }

            return sample;
        }

        private static Vector4 AddRootMotion(Vector4 start, Vector4 toAdd, float direction, bool dontAddRotation = false)
        {
            if (!dontAddRotation)
                start.W += toAdd.W;

            Vector3 displacement = Vector3.Transform(new Vector3(toAdd.X, toAdd.Y, toAdd.Z), Matrix4x4.CreateRotationY(direction));
            start.X += displacement.X;
            start.Y += displacement.Y;
            start.Z += displacement.Z;
            return start;
        }

        private Vector4 ExtractRootMotionInternal(float previousTimeSanitized, float nextTimeSanitized)
        {
            System.Diagnostics.Contracts.Contract.Requires(previousTimeSanitized >= 0 && previousTimeSanitized <= Duration);
            System.Diagnostics.Contracts.Contract.Requires(nextTimeSanitized >= 0 && nextTimeSanitized <= Duration);

            var previousData = GetSample(previousTimeSanitized);

            var nextData = GetSample(nextTimeSanitized);

            return nextData - previousData;
        }

        private float ConvertToLoopTime(float time)
        {
            return ((time % Duration) + Duration) % Duration;
        }

        private Vector4 ExtractExtraRootMotionInternal(float previousTime, float nextTime)
        {
            float timeUntilEnd = ConvertToLoopTime(previousTime);
            float timeFromStart = ConvertToLoopTime(nextTime);

            return ExtractRootMotionInternal(timeUntilEnd, Duration) + ExtractRootMotionInternal(0, timeFromStart);
        }

        public (Vector3 positionChange, float directionChange) ExtractRootMotion(float previousTime, float nextTime)
        {
            float timeChange = nextTime - previousTime;

            int fullLoops = Math.Sign(timeChange) * (int)(Math.Abs(timeChange) / Duration);

            Vector4 fullLoopRootMotion = Math.Abs(fullLoops) * (fullLoops > 0 ? LoopDeltaForward : LoopDeltaBackward);

            int fullLoopsInPreviousTime = (int)Math.Floor(previousTime / Duration);
            int fullLoopsInNextTime = (int)Math.Floor(nextTime / Duration);

            Vector4 extraRootMotion;

            if (fullLoopsInPreviousTime != fullLoopsInNextTime)
            {
                // the extracted root motion is split between two instances of this animation
                if (fullLoopsInPreviousTime < fullLoopsInNextTime)
                {
                    // ||---P-------------||-----------------||----N------------||
                    extraRootMotion = ExtractExtraRootMotionInternal(previousTime, nextTime);
                }
                else
                {
                    // ||---N-------------||-----------------||----P------------||
                    extraRootMotion = ExtractExtraRootMotionInternal(nextTime, previousTime);
                }
            }
            else
            {
                // ||---P--------N----|| or ||---N--------P----||
                float extractionStart = ((previousTime % Duration) + Duration) % Duration;
                float extractionEnd = ((nextTime % Duration) + Duration) % Duration;

                extraRootMotion = ExtractRootMotionInternal(extractionStart, extractionEnd);
            }


            Vector4 extractedRootMotion = extraRootMotion + fullLoopRootMotion;

            return (new Vector3(extractedRootMotion.X, extractedRootMotion.Y, extractedRootMotion.Z), extractedRootMotion.W);
        }

        public (Vector4 Motion, float Direction) UpdateRootMotion(Vector4 currentRootMotion, float currentDirection, float currentFrame, int loopCountDelta)
        {
            var currentData = GetSample(currentFrame);

            var changedRootMotion = AddRootMotion(currentRootMotion, currentData, currentDirection);

            return (changedRootMotion, currentDirection);
        }
    }
}
