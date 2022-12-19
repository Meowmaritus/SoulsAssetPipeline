using Assimp;
using SoulsAssetPipeline.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SoulsAssetPipeline.AnimationImporting
{
    public class ImportedAnimation : HavokAnimationData
    {
        public ImportedAnimation()
            : base ("SAP Custom Animation", null, null, null)
        {

        }

        public class Frame
        {
            public System.Numerics.Vector4 RootMotion => 
                new System.Numerics.Vector4(
                    RootMotionTranslation.X, RootMotionTranslation.Y,
                    RootMotionTranslation.Z, RootMotionRotation);

            public System.Numerics.Vector3 RootMotionTranslation;
            public float RootMotionRotation;

            public List<NewBlendableTransform> BoneTransforms = new List<NewBlendableTransform>();

            public static Frame Lerp(Frame a, Frame b, float s)
            {
                if (a.BoneTransforms.Count != b.BoneTransforms.Count)
                    throw new InvalidOperationException("Cannot lerp frames with two different skeletons.");
                var result = new Frame();
                result.RootMotionRotation = SapMath.Lerp(a.RootMotionRotation, b.RootMotionRotation, s);
                result.RootMotionTranslation = System.Numerics.Vector3.Lerp(a.RootMotionTranslation, b.RootMotionTranslation, s);
                result.BoneTransforms = new List<NewBlendableTransform>();
                for (int t = 0; t < a.BoneTransforms.Count; t++)
                {
                    result.BoneTransforms.Add(NewBlendableTransform.Lerp(a.BoneTransforms[t], b.BoneTransforms[t], s));
                }
                return result;
            }
        }

        public List<string> TransformTrackNames = new List<string>();
        public Dictionary<string, int> TransformTrackToBoneIndices = new Dictionary<string, int>();
        public List<Frame> Frames = new List<Frame>();

        public Frame GetSampledFrame(float frame)
        {
            if (frame < 0 || frame > (Frames.Count - 1))
            {
                throw new InvalidOperationException("Meh");
            }
            else if (frame == (int)frame)
            {
                return Frames[(int)frame];
            }
            else
            {
                var a = Frames[(int)Math.Floor(frame)];
                var b = Frames[(int)Math.Ceiling(frame)];
                return Frame.Lerp(a, b, frame % 1);
            }
        }

        //public double Duration;

        //public double Framerate = 60;
        //public double DeltaTime => 1.0 / Framerate;

        public byte[] WriteToSplineCompressedHKX2010Bytes(SplineCompressedAnimation.RotationQuantizationType rotationQuantizationType, float rotationTolerance, string applicationDir)
        {
            var newGuid = Guid.NewGuid().ToString();

            string uncompressed = $@"{applicationDir}\SapResources\CompressAnim\{newGuid}.uncompressed.xml";
            string compressed = $@"{applicationDir}\SapResources\CompressAnim\{newGuid}.compressed.hkx";

            WriteToHavok2010InterleavedUncompressedXMLFile(uncompressed);

            string args = $"\"{Path.GetFileName(uncompressed)}\" \"{Path.GetFileName(compressed)}\" {((int)rotationQuantizationType)} {rotationTolerance}";

            var xx = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($@"{applicationDir}\SapResources\CompressAnim\CompressAnim.exe")
            {
                Arguments = args,
                CreateNoWindow = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = $@"{applicationDir}\SapResources\CompressAnim",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            xx.WaitForExit();

            string standardOutput = xx.StandardOutput.ReadToEnd();
            string standardError = xx.StandardError.ReadToEnd();

            byte[] compressedHkx = File.ReadAllBytes(compressed);

            if (File.Exists(uncompressed))
                File.Delete(uncompressed);

            if (File.Exists(compressed))
                File.Delete(compressed);

            return compressedHkx;
        }

        public byte[] WriteToHavok2010InterleavedUncompressedXML()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Duration = Math.Max(Duration, FrameDuration);

            using (var testStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(testStream, new XmlWriterSettings()
                {
                    Indent = true,
                    Encoding = Encoding.ASCII,
                }))
                {
                    writer.WriteStartDocument();
                    {
                        writer.WriteStartElement("hkpackfile");
                        {
                            writer.WriteAttributeString("classversion", "8");
                            writer.WriteAttributeString("contentsversion", "hk_2010.2.0-r1");
                            writer.WriteAttributeString("toplevelobject", "#0040");

                            writer.WriteStartElement("hksection");
                            {
                                writer.WriteAttributeString("name", "__data__");

                                writer.WriteStartElement("hkobject");
                                {
                                    writer.WriteAttributeString("name", "#0043");
                                    writer.WriteAttributeString("class", "hkaDefaultAnimatedReferenceFrame");
                                    writer.WriteAttributeString("signature", "0x6d85e445");

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "up");
                                        writer.WriteString("(0.000000 1.000000 0.000000 0.000000)");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "forward");
                                        writer.WriteString("(0.000000 0.000000 1.000000 0.000000)");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "duration");
                                        writer.WriteString(Duration.ToString("0.000000"));
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "referenceFrameSamples");
                                        writer.WriteAttributeString("numelements", (Frames.Count).ToString());
                                        var sb = new StringBuilder();
                                        for (int i = 0; i < Frames.Count; i++)
                                        {
                                            sb.Append("\n\t\t\t\t");
                                            sb.Append($"({Frames[i].RootMotion.X:0.000000} {Frames[i].RootMotion.Y:0.000000} {Frames[i].RootMotion.Z:0.000000} {Frames[i].RootMotion.W:0.000000})");
                                        }
                                        sb.Append("\n\t\t\t");
                                        writer.WriteString(sb.ToString());
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();


                                writer.WriteStartElement("hkobject");
                                {
                                    writer.WriteAttributeString("name", "#0042");
                                    writer.WriteAttributeString("class", "hkaInterleavedUncompressedAnimation");
                                    writer.WriteAttributeString("signature", "0x930af031");

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "type");
                                        writer.WriteString("HK_INTERLEAVED_ANIMATION");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "duration");
                                        writer.WriteString(Duration.ToString("0.000000"));
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "numberOfTransformTracks");
                                        writer.WriteString(TransformTrackNames.Count.ToString());
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "numberOfFloatTracks");
                                        writer.WriteString("0");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "extractedMotion");
                                        writer.WriteString("#0043");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "annotationTracks");
                                        writer.WriteAttributeString("numelements", TransformTrackNames.Count.ToString());
                                        foreach (var t in TransformTrackNames)
                                        {
                                            writer.WriteStartElement("hkobject");
                                            {
                                                writer.WriteStartElement("hkparam");
                                                {
                                                    writer.WriteAttributeString("name", "trackName");
                                                    writer.WriteString(t);
                                                }
                                                writer.WriteEndElement();

                                                writer.WriteStartElement("hkparam");
                                                {
                                                    writer.WriteAttributeString("name", "annotations");
                                                    writer.WriteAttributeString("numelements", "0");
                                                    writer.WriteString("");
                                                }
                                                writer.WriteEndElement();
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "floats");
                                        writer.WriteAttributeString("numelements", "0");
                                        writer.WriteString("");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "transforms");

                                        var sb = new StringBuilder();

                                        int transformCount = 0;

                                        for (int f = 0; f < Frames.Count; f++)
                                        {
                                            for (int t = 0; t < TransformTrackNames.Count; t++)
                                            {
                                                if (f > 0)
                                                {
                                                    // Use dot product to check for anti-podals
                                                    var dot = System.Numerics.Quaternion.Dot(Frames[f - 1].BoneTransforms[t].Rotation, Frames[f].BoneTransforms[t].Rotation);
                                                    if (dot < 0)
                                                    {
                                                        var tr = Frames[f].BoneTransforms[t];
                                                        tr.Rotation = -tr.Rotation;
                                                        Frames[f].BoneTransforms[t] = tr;
                                                    }
                                                }

                                                var r = Frames[f].BoneTransforms[t].Rotation;

                                                sb.Append("\n\t\t\t\t");
                                                sb.Append($"({Frames[f].BoneTransforms[t].Translation.X:0.000000} " +
                                                    $"{Frames[f].BoneTransforms[t].Translation.Y:0.000000} " +
                                                    $"{Frames[f].BoneTransforms[t].Translation.Z:0.000000})");
                                                sb.Append($"({r.X:0.000000} " +
                                                    $"{r.Y:0.000000} " +
                                                    $"{r.Z:0.000000} " +
                                                    $"{r.W:0.000000})");
                                                sb.Append($"({Frames[f].BoneTransforms[t].Scale.X:0.000000} " +
                                                    $"{Frames[f].BoneTransforms[t].Scale.Y:0.000000} " +
                                                    $"{Frames[f].BoneTransforms[t].Scale.Z:0.000000})");
                                                transformCount++;
                                            }
                                        }

                                        sb.Append("\n\t\t\t");

                                        writer.WriteAttributeString("numelements", Frames.Count.ToString());

                                        writer.WriteString(sb.ToString());
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();


                                writer.WriteStartElement("hkobject");
                                {
                                    writer.WriteAttributeString("name", "#0041");
                                    writer.WriteAttributeString("class", "hkaAnimationBinding");
                                    writer.WriteAttributeString("signature", "0x66eac971");

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "originalSkeletonName");
                                        writer.WriteString(TransformTrackNames[0]);
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "animation");
                                        writer.WriteString("#0042");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "transformTrackToBoneIndices");
                                        writer.WriteAttributeString("numelements", $"{TransformTrackToBoneIndices.Count}");
                                        writer.WriteString(string.Join(" ", TransformTrackToBoneIndices.Select(kvp => kvp.Value)));
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "floatTrackToFloatSlotIndices");
                                        writer.WriteAttributeString("numelements", "0");
                                        writer.WriteString("");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "blendHint");
                                        if (BlendHint == HKX.AnimationBlendHint.NORMAL)
                                            writer.WriteString("NORMAL");
                                        else if (BlendHint == HKX.AnimationBlendHint.ADDITIVE)
                                            writer.WriteString("ADDITIVE");
                                        else if (BlendHint == HKX.AnimationBlendHint.ADDITIVE_DEPRECATED)
                                            writer.WriteString("ADDITIVE");
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();

                                writer.WriteStartElement("hkobject");
                                {
                                    writer.WriteAttributeString("name", "#0044");
                                    writer.WriteAttributeString("class", "hkaAnimationContainer");
                                    writer.WriteAttributeString("signature", "0x8dc20333");

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "skeletons");
                                        writer.WriteAttributeString("numelements", "0");
                                        writer.WriteString("");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "animations");
                                        writer.WriteAttributeString("numelements", "1");
                                        writer.WriteString("#0042");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "bindings");
                                        writer.WriteAttributeString("numelements", "1");
                                        writer.WriteString("#0041");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "attachments");
                                        writer.WriteAttributeString("numelements", "0");
                                        writer.WriteString("");
                                    }
                                    writer.WriteEndElement();

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "skins");
                                        writer.WriteAttributeString("numelements", "0");
                                        writer.WriteString("");
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();

                                writer.WriteStartElement("hkobject");
                                {
                                    writer.WriteAttributeString("name", "#0040");
                                    writer.WriteAttributeString("class", "hkRootLevelContainer");
                                    writer.WriteAttributeString("signature", "0x2772c11e");

                                    writer.WriteStartElement("hkparam");
                                    {
                                        writer.WriteAttributeString("name", "namedVariants");
                                        writer.WriteAttributeString("numelements", "1");
                                        writer.WriteStartElement("hkobject");
                                        {

                                            writer.WriteStartElement("hkparam");
                                            {
                                                writer.WriteAttributeString("name", "name");
                                                writer.WriteString("Merged Animation Container");
                                            }
                                            writer.WriteEndElement();

                                            writer.WriteStartElement("hkparam");
                                            {
                                                writer.WriteAttributeString("name", "className");
                                                writer.WriteString("hkaAnimationContainer");
                                            }
                                            writer.WriteEndElement();

                                            writer.WriteStartElement("hkparam");
                                            {
                                                writer.WriteAttributeString("name", "variant");
                                                writer.WriteString("#0044");
                                            }
                                            writer.WriteEndElement();

                                        }
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndDocument();
                }

                return testStream.ToArray();
            }
        }

        public void WriteToHavok2010InterleavedUncompressedXMLFile(string fileName)
        {
            Duration = Math.Max(Duration, FrameDuration);


            if (File.Exists(fileName))
                File.Delete(fileName);

            byte[] fileData = WriteToHavok2010InterleavedUncompressedXML();
            File.WriteAllBytes(fileName, fileData);
        }

        public override NewBlendableTransform GetTransformOnFrame(int transformTrackIndex, float frame, bool enableLooping)
        {
            var frameRatio = frame % 1;

            if (enableLooping)
                frame = frame % FrameCount;
            else
                frame = Math.Min(frame, FrameCount);

            if (frame < 0)
                frame = 0;

            if (frameRatio != 0)
            {
                var blendFrom = Frames[(int)Math.Floor(frame)].BoneTransforms[transformTrackIndex];
                var blendTo = Frames[(int)Math.Ceiling(frame)].BoneTransforms[transformTrackIndex];
                return NewBlendableTransform.Lerp(blendFrom, blendTo, frameRatio);
            }
            else
            {
                return Frames[(int)frame].BoneTransforms[transformTrackIndex];
            }
            
        }
    }
}
