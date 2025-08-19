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
            /// Info about an action in a TAE file.
            /// </summary>
            public class ActionTemplate : List<ParameterTemplate>
            {
                /// <summary>
                /// GUID used simply to check if templates have been edited at runtime and to refresh data accordingly.
                /// Call <see cref="RefreshGUID"/> to generate a new GUID.
                /// </summary>
                public string GUID { get; private set; } = Guid.NewGuid().ToString();

                public void RefreshGUID()
                {
                    GUID = Guid.NewGuid().ToString();
                }

                /// <summary>
                /// Whether it's big endian.
                /// </summary>
                public bool BigEndian;

                /// <summary>
                /// ID of this TAE action.
                /// </summary>
                public readonly int ID;

                /// <summary>
                /// Name of this TAE action.
                /// </summary>
                public string Name;


                public int GetParameterByteOffset(ParameterTemplate param)
                {
                    if (this.Contains(param))
                    {
                        int offset = 0;
                        foreach (var p in this)
                        {
                            if (p == param)
                                return offset;
                            else
                                offset += p.GetByteCount();
                        }
                    }
                    throw new ArgumentException($"Param field is not part of this action parameter struct.");
                }


                /// <summary>
                /// Gets the default byte array for this action, using the parameters'
                /// DefaultValue properties if applicable.
                /// </summary>
                public byte[] GetDefaultBytes()
                {
                    using (var memStream = new System.IO.MemoryStream())
                    {
                        var bw = new BinaryWriterEx(BigEndian, memStream);

                        foreach (var paramKvp in this)
                        {
                            var p = paramKvp;
                            paramKvp.WriteDefaultValue(bw);
                        }

                        return memStream.ToArray();
                    }
                }

                /// <summary>
                /// Gets the byte count of the entire list of parameters.
                /// </summary>
                public int GetAllParametersByteCount()
                {
                    int result = 0;
                    foreach (var paramKvp in this)
                    {
                        result += paramKvp.GetByteCount();
                    }
                    return result;
                }

                public void UpdateAllParamsUnkNames()
                {
                    int offset = 0;
                    foreach (var paramKvp in this)
                    {
                        if (paramKvp.NameIsUnk)
                            paramKvp.Name = $"Unk{offset:X2}";
                        offset += paramKvp.GetByteCount();
                    }
                }

                public void WriteXML(XmlWriter writer)
                {
                    writer.WriteAttributeString("id", ID.ToString());
                    writer.WriteAttributeString("name", Name);
                    foreach (var kvp in this)
                    {
                        writer.WriteStartElement(kvp.ParamType.ToString());
                        {
                            kvp.WriteXML(writer);
                        }
                        writer.WriteEndElement();
                    }
                }

                public ActionTemplate(int actionType, string name, bool bigEndian)
                {
                    BigEndian = bigEndian;
                    ID = actionType;
                    Name = name;
                }

                internal ActionTemplate(XmlNode actionNode, bool bigEndian)
                    : base()
                {
                    BigEndian = bigEndian;
                    ID = int.Parse(actionNode.Attributes["id"].InnerText);
                    Name = actionNode.Attributes["name"]?.InnerText ?? $"Action{ID}";
                    int i = 0;
                    int offset = 0;
                    foreach (XmlNode paramNode in actionNode.ChildNodes)
                    {
                        if (paramNode.Name == "#comment")
                            continue;
                        var newParam = new ParameterTemplate(ID, i++, paramNode, offset);
                        var paramSize = newParam.GetByteCount();
                        Add(newParam);
                        offset += paramSize;
                    }
                }
            }

        }
    }
}
