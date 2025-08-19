using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SoulsFormats;

namespace SoulsAssetPipeline.Animation
{
    public partial class TAE
    {
        /// <summary>
        /// Template for the parameters in all actions.
        /// </summary>
        public partial class Template : Dictionary<long, Template.ActionTemplate>
        {
            /// <summary>
            /// Info about a parameter supplied to a TAE action.
            /// </summary>
            public class ParameterTemplate
            {
                /// <summary>
                /// Gets the byte count of a specific value type.
                /// </summary>
                public int GetByteCount()
                {
                    switch (ParamType)
                    {
                        case ParamTypes.s8:
                        case ParamTypes.u8:
                        case ParamTypes.x8:
                        case ParamTypes.b:
                            return 1;
                        case ParamTypes.s16:
                        case ParamTypes.u16:
                        case ParamTypes.x16:
                            return 2;
                        case ParamTypes.s32:
                        case ParamTypes.u32:
                        case ParamTypes.x32:
                        case ParamTypes.f32:
                            return 4;
                        case ParamTypes.f32grad:
                            return 8;
                        case ParamTypes.s64:
                        case ParamTypes.u64:
                        case ParamTypes.x64:
                        case ParamTypes.f64:
                            return 8;
                        case ParamTypes.aob:
                            return AobLength;
                        default: throw new ArgumentException("Not a valid ParamType");
                    }
                }

                /// <summary>
                /// Gets the System.Type of this parameter's value.
                /// </summary>
                public System.Type GetValueObjectType()
                {
                    switch (ParamType)
                    {
                        case ParamTypes.aob: return typeof(string);
                        case ParamTypes.u8: case ParamTypes.x8: return typeof(byte);
                        case ParamTypes.s8: return typeof(sbyte);
                        case ParamTypes.u16: case ParamTypes.x16: return typeof(ushort);
                        case ParamTypes.s16: return typeof(short);
                        case ParamTypes.u32: case ParamTypes.x32: return typeof(uint);
                        case ParamTypes.s32: return typeof(int);
                        case ParamTypes.u64: case ParamTypes.x64: return typeof(ulong);
                        case ParamTypes.s64: return typeof(long);
                        case ParamTypes.f32: return typeof(float);
                        case ParamTypes.f32grad: return typeof(System.Numerics.Vector2);
                        case ParamTypes.f64: return typeof(double);
                        case ParamTypes.b: return typeof(byte);
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                /// <summary>
                /// Converts a string to a value based on this ParameterTemplate's type.
                /// </summary>
                public object StringToValue(string str)
                {
                    if (str == null)
                        return null;

                    IEnumerable<string> GetArrayFromSingleLineString(string s)
                    {
                        return s.Split(' ')
                            .Where(st => !string.IsNullOrWhiteSpace(st))
                            .Select(st => st.Trim());
                    }

                    List<string> GetArrayFromString(string s)
                    {
                        List<string> result = new List<string>();
                        var lines = s.Split('\n');
                        foreach (var l in lines)
                            result.AddRange(GetArrayFromSingleLineString(l.Replace("\r", "").Replace("\n", "").Replace("\t", "")));
                        return result;
                    }

                    // Convert a string enum value to the actual numeric value.
                    if (EnumEntries != null)
                    {
                        var match = FindEnumEntry(str);
                        if (match != null)
                            str = match.Value.ToString();
                    }

                    switch (ParamType)
                    {
                        case ParamTypes.aob: return GetArrayFromString(str).Select(b => byte.Parse(b, System.Globalization.NumberStyles.HexNumber)).ToArray();
                        case ParamTypes.u8: return byte.Parse(str);
                        case ParamTypes.x8: return byte.Parse(str, System.Globalization.NumberStyles.HexNumber);
                        case ParamTypes.s8: return sbyte.Parse(str);
                        case ParamTypes.u16: return ushort.Parse(str);
                        case ParamTypes.x16: return ushort.Parse(str, System.Globalization.NumberStyles.HexNumber);
                        case ParamTypes.s16: return short.Parse(str);
                        case ParamTypes.u32: return uint.Parse(str);
                        case ParamTypes.x32: return uint.Parse(str, System.Globalization.NumberStyles.HexNumber);
                        case ParamTypes.s32: return int.Parse(str);
                        case ParamTypes.u64: return ulong.Parse(str);
                        case ParamTypes.x64: return ulong.Parse(str, System.Globalization.NumberStyles.HexNumber);
                        case ParamTypes.s64: return long.Parse(str);
                        case ParamTypes.f32: return float.Parse(str);
                        case ParamTypes.f32grad:
                            var floatSplit = str.Split('|');
                            float gradStart = float.Parse(floatSplit[0]);
                            float gradEnd = float.Parse(floatSplit[1]);
                            return new System.Numerics.Vector2(gradStart, gradEnd);
                        case ParamTypes.f64: return double.Parse(str);
                        case ParamTypes.b:
                            string toLower = str.ToLower().Trim();
                            if (toLower == "true")
                                return true;
                            else if (toLower == "false")
                                return false;
                            else
                                return byte.Parse(str);
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                /// <summary>
                /// Converts a value to a string based on this ParameterTemplate's type.
                /// </summary>
                public string ValueToString(object val, bool replaceWithEnumNames = true)
                {
                    if (EnumEntries != null && replaceWithEnumNames)
                    {
                        if (EnumEntries.Any(e => e.Value.Equals(val)))
                        {
                            return EnumEntries.First(x => x.Value.Equals(val)).Key;
                        }
                    }

                    switch (ParamType)
                    {
                        case ParamTypes.aob: return string.Join(" ", ((byte[])val).Select(b => b.ToString("X2")));
                        case ParamTypes.x8: return ((byte)val).ToString("X2");
                        case ParamTypes.x16: return ((ushort)val).ToString("X4");
                        case ParamTypes.x32: return ((uint)val).ToString("X8");
                        case ParamTypes.x64: return ((ulong)val).ToString("X16");
                        case ParamTypes.b:
                            var asByte = (byte)val;
                            if (asByte == 1)
                                return "True";
                            else if (asByte == 0)
                                return "False";
                            else
                                return asByte.ToString();
                        case ParamTypes.f32grad: return $"{((System.Numerics.Vector2)val).X}|{((System.Numerics.Vector2)val).Y}";
                        default: return val.ToString();
                    }
                }

                public void WriteValue(BinaryWriterEx bw, object value)
                {
                    switch (ParamType)
                    {
                        case ParamTypes.aob: bw.WriteBytes((byte[])value); break;
                        case ParamTypes.b: bw.WriteByte((byte)value); break;
                        case ParamTypes.u8: case ParamTypes.x8: bw.WriteByte((byte)value); break;
                        case ParamTypes.s8: bw.WriteSByte((sbyte)value); break;
                        case ParamTypes.u16: case ParamTypes.x16: bw.WriteUInt16((ushort)value); break;
                        case ParamTypes.s16: bw.WriteInt16((short)value); break;
                        case ParamTypes.u32: case ParamTypes.x32: bw.WriteUInt32((uint)value); break;
                        case ParamTypes.s32: bw.WriteInt32((int)value); break;
                        case ParamTypes.u64: case ParamTypes.x64: bw.WriteUInt64((ulong)value); break;
                        case ParamTypes.s64: bw.WriteInt64((long)value); break;
                        case ParamTypes.f32: bw.WriteSingle((float)value); break;
                        case ParamTypes.f32grad:
                            bw.WriteSingle(((System.Numerics.Vector2)value).X);
                            bw.WriteSingle(((System.Numerics.Vector2)value).Y);
                            break;
                        case ParamTypes.f64: bw.WriteDouble((double)value); break;
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                public object ReadValue(BinaryReaderEx br)
                {
                    switch (ParamType)
                    {
                        case ParamTypes.aob: return br.ReadBytes(AobLength);
                        case ParamTypes.b: return br.ReadByte();
                        case ParamTypes.u8: case ParamTypes.x8: return br.ReadByte();
                        case ParamTypes.s8: return br.ReadSByte();
                        case ParamTypes.u16: case ParamTypes.x16: return br.ReadUInt16();
                        case ParamTypes.s16: return br.ReadInt16();
                        case ParamTypes.u32: case ParamTypes.x32: return br.ReadUInt32();
                        case ParamTypes.s32: return br.ReadInt32();
                        case ParamTypes.u64: case ParamTypes.x64: return br.ReadUInt64();
                        case ParamTypes.s64: return br.ReadInt64();
                        case ParamTypes.f32: return br.ReadSingle();
                        case ParamTypes.f32grad:
                            var gradStart = br.ReadSingle();
                            var gradEnd = br.ReadSingle();
                            return new System.Numerics.Vector2(gradStart, gradEnd);
                        case ParamTypes.f64: return br.ReadDouble();
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                public void AssertValue(BinaryReaderEx br)
                {
                    switch (ParamType)
                    {
#if DEBUG
                        case ParamTypes.aob:
                            var assertAob = (byte[])ValueToAssert;
                            for (int i = 0; i < AobLength; i++)
                            {
                                br.AssertByte(assertAob[i]);
                            }
                            break;
                        case ParamTypes.b: br.AssertByte((byte)ValueToAssert); break;
                        case ParamTypes.u8: case ParamTypes.x8: br.AssertByte((byte)ValueToAssert); break;
                        case ParamTypes.s8: br.AssertSByte((sbyte)ValueToAssert); break;
                        case ParamTypes.u16: case ParamTypes.x16: br.AssertUInt16((ushort)ValueToAssert); break;
                        case ParamTypes.s16: br.AssertInt16((short)ValueToAssert); break;
                        case ParamTypes.u32: case ParamTypes.x32: br.AssertUInt32((uint)ValueToAssert); break;
                        case ParamTypes.s32: br.AssertInt32((int)ValueToAssert); break;
                        case ParamTypes.u64: case ParamTypes.x64: br.AssertUInt64((ulong)ValueToAssert); break;
                        case ParamTypes.s64: br.AssertInt64((long)ValueToAssert); break;
                        case ParamTypes.f32: br.AssertSingle((float)ValueToAssert); break;
                        case ParamTypes.f32grad:
                            br.AssertSingle(((System.Numerics.Vector2)ValueToAssert).X);
                            br.AssertSingle(((System.Numerics.Vector2)ValueToAssert).Y);
                            break;
                        case ParamTypes.f64: br.AssertDouble((double)ValueToAssert); break;
#else
                        case ParamTypes.aob:
                            var assertAob = (byte[])ValueToAssert;
                            br.Position += assertAob.Length;
                            break;
                        case ParamTypes.b: br.Position += 1; break;
                        case ParamTypes.u8: br.Position += 1; break;
                        case ParamTypes.s8: br.Position += 1; break;
                        case ParamTypes.u16: br.Position += 2; break;
                        case ParamTypes.s16: br.Position += 2; break;
                        case ParamTypes.u32: br.Position += 4; break;
                        case ParamTypes.s32: br.Position += 4; break;
                        case ParamTypes.u64: case ParamTypes.x64: br.Position += 8; break;
                        case ParamTypes.s64: br.Position += 8; break;
                        case ParamTypes.f32: br.Position += 4; break;
                        case ParamTypes.f32grad:
                            br.Position += 8; break;
                            break;
                        case ParamTypes.f64: br.Position += 8; break;
#endif
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                internal void WriteAssertValue(BinaryWriterEx bw)
                {
                    switch (ParamType)
                    {
                        case ParamTypes.aob:
                            var assertAob = (byte[])ValueToAssert;
                            bw.WriteBytes(assertAob);
                            break;
                        case ParamTypes.b: bw.WriteByte((byte)ValueToAssert); break;
                        case ParamTypes.u8: case ParamTypes.x8: bw.WriteByte((byte)ValueToAssert); break;
                        case ParamTypes.s8: bw.WriteSByte((sbyte)ValueToAssert); break;
                        case ParamTypes.u16: case ParamTypes.x16: bw.WriteUInt16((ushort)ValueToAssert); break;
                        case ParamTypes.s16: bw.WriteInt16((short)ValueToAssert); break;
                        case ParamTypes.u32: case ParamTypes.x32: bw.WriteUInt32((uint)ValueToAssert); break;
                        case ParamTypes.s32: bw.WriteInt32((int)ValueToAssert); break;
                        case ParamTypes.u64: case ParamTypes.x64: bw.WriteUInt64((ulong)ValueToAssert); break;
                        case ParamTypes.s64: bw.WriteInt64((long)ValueToAssert); break;
                        case ParamTypes.f32: bw.WriteSingle((float)ValueToAssert); break;
                        case ParamTypes.f32grad:
                            bw.WriteSingle(((System.Numerics.Vector2)ValueToAssert).X);
                            bw.WriteSingle(((System.Numerics.Vector2)ValueToAssert).Y);
                            break;
                        case ParamTypes.f64: bw.WriteDouble((double)ValueToAssert); break;
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                public object GetDefaultValue()
                {
                    switch (ParamType)
                    {
                        case ParamTypes.aob: return new byte[AobLength];
                        case ParamTypes.b: case ParamTypes.u8: case ParamTypes.x8: return (byte)(0);
                        case ParamTypes.s8: return (sbyte)(0);
                        case ParamTypes.u16: case ParamTypes.x16: return (ushort)(0);
                        case ParamTypes.s16: return (short)(0);
                        case ParamTypes.u32: case ParamTypes.x32: return (uint)(0);
                        case ParamTypes.s32: return (int)(0);
                        case ParamTypes.u64: case ParamTypes.x64: return (ulong)(0);
                        case ParamTypes.s64: return (long)(0);
                        case ParamTypes.f32: return (float)(0); 
                        case ParamTypes.f32grad: return new System.Numerics.Vector2(0, 0);
                        case ParamTypes.f64: return (double)(0);
                        default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                    }
                }

                internal void WriteDefaultValue(BinaryWriterEx bw)
                {
                    if (ValueToAssert != null)
                    {
                        WriteAssertValue(bw);
                    }
                    else if (DefaultValue == null)
                    {
                        switch (ParamType)
                        {
                            case ParamTypes.aob:
                                for (int i = 0; i < AobLength; i++)
                                    bw.WriteByte(0);
                                break;
                            case ParamTypes.b: case ParamTypes.u8: case ParamTypes.x8: bw.WriteByte(0); break;
                            case ParamTypes.s8: bw.WriteSByte(0); break;
                            case ParamTypes.u16: case ParamTypes.x16: bw.WriteUInt16(0); break;
                            case ParamTypes.s16: bw.WriteInt16(0); break;
                            case ParamTypes.u32: case ParamTypes.x32: bw.WriteUInt32(0); break;
                            case ParamTypes.s32: bw.WriteInt32(0); break;
                            case ParamTypes.u64: case ParamTypes.x64: bw.WriteUInt64(0); break;
                            case ParamTypes.s64: bw.WriteInt64(0); break;
                            case ParamTypes.f32: bw.WriteSingle(0); break;
                            case ParamTypes.f32grad:
                                bw.WriteSingle(0);
                                bw.WriteSingle(0);
                                break;
                            case ParamTypes.f64: bw.WriteDouble(0); break;
                            default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                        }
                    }
                    else
                    {
                        switch (ParamType)
                        {
                            case ParamTypes.aob:
                                var assertAob = (byte[])DefaultValue;
                                bw.WriteBytes(assertAob);
                                break;
                            case ParamTypes.b: case ParamTypes.u8: case ParamTypes.x8: bw.WriteByte((byte)DefaultValue); break;
                            case ParamTypes.s8: bw.WriteSByte((sbyte)DefaultValue); break;
                            case ParamTypes.u16: case ParamTypes.x16: bw.WriteUInt16((ushort)DefaultValue); break;
                            case ParamTypes.s16: bw.WriteInt16((short)DefaultValue); break;
                            case ParamTypes.u32: case ParamTypes.x32: bw.WriteUInt32((uint)DefaultValue); break;
                            case ParamTypes.s32: bw.WriteInt32((int)DefaultValue); break;
                            case ParamTypes.u64: case ParamTypes.x64: bw.WriteUInt64((ulong)DefaultValue); break;
                            case ParamTypes.s64: bw.WriteInt64((long)DefaultValue); break;
                            case ParamTypes.f32: bw.WriteSingle((float)DefaultValue); break;
                            case ParamTypes.f32grad:
                                bw.WriteSingle(((System.Numerics.Vector2)DefaultValue).X);
                                bw.WriteSingle(((System.Numerics.Vector2)DefaultValue).Y);
                                break;
                            case ParamTypes.f64: bw.WriteDouble((double)DefaultValue); break;
                            default: throw new Exception($"Invalid ParamTemplate ParamType: {ParamType.ToString()}");
                        }
                    }
                    
                }

                public ParameterTemplate GetCopy()
                {
                    var p = new ParameterTemplate();
                    p.AobLength = AobLength;
                    p.DefaultValue = DefaultValue;
                    if (EnumEntries != null)
                    {
                        p.EnumEntries = new List<EnumEntry>();
                        foreach (var e in EnumEntries)
                        {
                            p.EnumEntries.Add(new EnumEntry(e.Key, e.Value));
                        }
                    }
                    else
                        p.EnumEntries = null;
                    p.Name = Name;
                    p.NameGroup = NameGroup;
                    p.NameIsUnk = NameIsUnk;
                    p.ParamType = ParamType;
                    p.ValueToAssert = ValueToAssert;
                    return p;
                }

                /// <summary>
                /// The value type of this parameter.
                /// </summary>
                public ParamTypes ParamType;

                /// <summary>
                /// The name of this parameter.
                /// </summary>
                public string Name;

                /// <summary>
                /// Set to true when name is not specified in file and an UnkXX name is autogenerated.
                /// </summary>
                public bool NameIsUnk = false;

                /// <summary>
                /// The name of the group this parameter is in.
                /// Leave null to place outside of any groups.
                /// </summary>
                public string NameGroup = null;

                public string GetKeyString()
                {
                    if (NameGroup != null)
                        return $"{NameGroup}::{Name}";
                    else 
                        return Name;
                }

                /// <summary>
                /// (Optional) The value which should be asserted on this parameter.
                /// </summary>
                public object ValueToAssert = null;

                /// <summary>
                /// (Optional) The default value to set when creating a new action of
                /// this type from scratch. Otherwise a 0 value will be used in such a case.
                /// </summary>
                public object DefaultValue = null;

                /// <summary>
                /// (Only applies if Type == ParamType.aob)
                /// The length of the array of bytes.
                /// </summary>
                public int AobLength = -1;

                public class EnumEntry
                {
                    public string Key;
                    public object Value;
                    public EnumEntry()
                    {

                    }
                    public EnumEntry(string key, object value)
                    {
                        Key = key;
                        Value = value;
                    }
                }

                /// <summary>
                /// Possible values if this is an enum, otherwise it's null.
                /// </summary>
                public List<EnumEntry> EnumEntries { get; set; } = null;

                public EnumEntry FindEnumEntry(string entryName)
                {
                    return EnumEntries.FirstOrDefault(e => e.Key == entryName);
                }

                public void EnsureEnumEntry(object entryValue)
                {
                    if (EnumEntries == null)
                        EnumEntries = new List<EnumEntry>();
                    var v = Convert.ToInt32(entryValue);
                    if (!EnumEntries.Any(a => Convert.ToInt32(a.Value) == v))
                        EnumEntries.Add(new EnumEntry($"{v}: <Unmapped Value>", v));
                }

                /// <summary>
                /// Sorts the enum entries by key.
                /// </summary>
                public void SortEnumEntries()
                {
                    EnumEntries = EnumEntries.OrderBy(kvp => kvp.Key)
                        .ToList();
                }

                public void WriteXML(XmlWriter writer)
                {
                    if (!NameIsUnk)
                        writer.WriteAttributeString("name", Name);

                    if (NameGroup != null)
                        writer.WriteAttributeString("group", NameGroup);

                    if (ValueToAssert != null)
                        writer.WriteAttributeString("assert", ValueToString(ValueToAssert));

                    if (DefaultValue != null)
                        writer.WriteAttributeString("default", ValueToString(DefaultValue));

                    if (AobLength > 0 && ParamType == ParamTypes.aob)
                        writer.WriteAttributeString("length", AobLength.ToString());

                    if (EnumEntries != null && EnumEntries.Count > 0)
                    {
                        foreach (var en in EnumEntries)
                        {
                            writer.WriteStartElement("entry");
                            {
                                writer.WriteAttributeString("name", en.Key);
                                writer.WriteAttributeString("value", ValueToString(en.Value, replaceWithEnumNames: false));
                            }
                            writer.WriteEndElement();
                        }
                    }

                }

                public ParameterTemplate()
                {

                }

                internal ParameterTemplate(long actionId, long paramIndex, XmlNode paramNode, int offset)
                {
                    ParamType = (ParamTypes)Enum.Parse(typeof(ParamTypes), paramNode.Name);

                    NameGroup = paramNode.Attributes["group"]?.InnerText;

                    var nameStr = paramNode.Attributes["name"]?.InnerText;
                    if (nameStr != null)
                    {
                        Name = nameStr;
                        NameIsUnk = false;
                    }
                    else
                    {
                        Name = $"Unk{offset:X2}";
                        NameIsUnk = true;
                    }
                    
                    // Load enum entries before doing default value so you can make the default value an enum entry.
                    var enumNodes = paramNode.SelectNodes("entry");
                    if (enumNodes.Count > 0)
                    {
                        EnumEntries = new List<EnumEntry>();
                        foreach (XmlNode entryNode in paramNode.SelectNodes("entry"))
                        {
                            var entryName = entryNode.Attributes["name"].InnerText;
                            var entryValue = StringToValue(entryNode.Attributes["value"].InnerText);
                            EnumEntries.Add(new EnumEntry(entryName, entryValue));
                        }
                    }

                    if (paramNode.HasChildNodes)
                    {
                        var valueNode = paramNode.SelectSingleNode("assert");
                        if (valueNode != null)
                        {
                            ValueToAssert = StringToValue(valueNode.InnerText);
                        }

                        var defaultValueNode = paramNode.SelectSingleNode("default");
                        if (defaultValueNode != null)
                        {
                            DefaultValue = StringToValue(defaultValueNode.InnerText);
                        }
                    }

                    try
                    {
                        if (ValueToAssert == null)
                            ValueToAssert = StringToValue(paramNode.Attributes["assert"]?.InnerText);
                    }
                    catch (Exception ex)
                    {
                        // Placeholder.
                        if (paramNode.Attributes["assert"]?.InnerText != null)
                            ValueToAssert = GetDefaultValue();
                        //throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")}\n    Failed to read 'assert' attribute of parameter.\n\n\n{ex}");
                    }
                    
                    try
                    {
                        if (DefaultValue == null)
                            DefaultValue = StringToValue(paramNode.Attributes["default"]?.InnerText);
                    }
                    catch (Exception ex)
                    {
                        if (EnumEntries != null && EnumEntries.Count > 0)
                            throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")}\n    Failed to read 'default' attribute of parameter. Note: default value must be an integer on enums.\n\n\n{ex}");
                        else
                            throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")}\n    Failed to read 'default' attribute of parameter.\n\n\n{ex}");
                    }

                    var lengthAttribute = paramNode.Attributes["length"];
                    if (lengthAttribute != null)
                    {
                        try
                        {
                            AobLength = int.Parse(lengthAttribute.InnerText);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")}\n    Failed to read 'length' attribute of parameter.\n\n\n{ex}");
                        }
                    }
                    else
                    {
                        if (ParamType == ParamTypes.aob)
                        {
                            throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")} was an " +
                                $"array of bytes but no length was specified");
                        }
                    }

                    if (ParamType == ParamTypes.aob && ValueToAssert != null)
                    {
                        var aob = (byte[])ValueToAssert;
                        if (aob.Length != AobLength)
                        {
                            throw new Exception($"Action {actionId} -> Parameter {(Name != null ? $"'{Name}'" : $"{paramIndex}")}: " +
                                $"AoB assert value length was {aob.Length} but 'length' " +
                                $"attribute was set to {AobLength}.");
                        }
                    }
                    
                }
            }

            
            /// <summary>
            /// Possible types for values in an action parameter.
            /// </summary>
            public enum ParamTypes
            {
                /// <summary>
                /// Single-byte boolean value.
                /// </summary>
                b,

                /// <summary>
                /// Unsigned byte.
                /// </summary>
                u8,

                /// <summary>
                /// Unsigned byte, display as hex.
                /// </summary>
                x8,

                /// <summary>
                /// Signed byte.
                /// </summary>
                s8,

                /// <summary>
                /// Unsigned short.
                /// </summary>
                u16,

                /// <summary>
                /// Unsigned short, display as hex.
                /// </summary>
                x16,

                /// <summary>
                /// Signed short.
                /// </summary>
                s16,

                /// <summary>
                /// Unsigned int.
                /// </summary>
                u32,

                /// <summary>
                /// Unsigned int, display as hex.
                /// </summary>
                x32,

                /// <summary>
                /// Signed int.
                /// </summary>
                s32,

                /// <summary>
                /// Unsigned long.
                /// </summary>
                u64,

                /// <summary>
                /// Unsigned long, display as hex.
                /// </summary>
                x64,

                /// <summary>
                /// Signed long.
                /// </summary>
                s64,

                /// <summary>
                /// Single-precision float.
                /// </summary>
                f32,

                /// <summary>
                /// Single-precision float gradient. Two float values to blend over time from the start of the event to the end of the event.
                /// </summary>
                f32grad,

                /// <summary>
                /// Double-precision float.
                /// </summary>
                f64,

                /// <summary>
                /// Array of bytes.
                /// </summary>
                aob,
            }

            
        }
    }
}
