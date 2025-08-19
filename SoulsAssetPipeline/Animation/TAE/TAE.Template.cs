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
            /// The game(s) this template is for.
            /// </summary>
            public TAEFormat Game;

            public bool BigEndian;

            /// <summary>
            /// Creates new empty template.
            /// </summary>
            public Template()
                : base()
            {

            }

            internal void SortByID()
            {
                var sorted = this.OrderBy(kvp => kvp.Key).ToList();
                Clear();
                foreach (var s in sorted)
                {
                    Add(s.Key, s.Value);
                }
            }

            public void WriteToXMLFile(string path)
            {
                var xmlBytes = WriteToXMLFileBytes();
                File.WriteAllBytes(path, xmlBytes);
            }

            private byte[] WriteToXMLFileBytes()
            {
                using (var testStream = new MemoryStream())
                {
                    using (var writer = XmlWriter.Create(testStream, new XmlWriterSettings()
                    {
                        Indent = true,
                        Encoding = Encoding.ASCII,
                    }))
                    {
                        writer.WriteStartDocument();

                        writer.WriteStartElement("action_template");
                        {
                            writer.WriteAttributeString("game", Game.ToString());
                            foreach (var kvp in this)
                            {
                                writer.WriteStartElement("action");
                                {
                                    kvp.Value.WriteXML(writer);
                                }
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();

                        writer.WriteEndDocument();
                    }

                    return testStream.ToArray();
                }
            }

            private Template(XmlDocument xml, bool disableErrorHandling)
                : base()
            {
                XmlNode templateNode = xml.SelectSingleNode("action_template");
                Game = (TAEFormat)Enum.Parse(typeof(TAEFormat), templateNode.Attributes["game"].InnerText);

                BigEndian = Game == TAEFormat.DES;

                ActionTemplate lastGoodActionTemplate = null;
                
                foreach (XmlNode actionNode in templateNode.SelectNodes("action"))
                {
                    try
                    {
                        var newAction = new ActionTemplate(actionNode, BigEndian);
                        if (ContainsKey(newAction.ID))
                        {
                            throw new Exception($"TAE Template has more than one action with ID {newAction.ID}.");
                        }
                        Add(newAction.ID, newAction);

                        lastGoodActionTemplate = newAction;
                    }
                    catch (Exception e) when (!disableErrorHandling)
                    {
                        if (lastGoodActionTemplate == null)
                        {
                            throw new Exception($"First TAE action template failed to read:\n\n{e}");
                        }
                        else
                        {
                            throw new Exception($"TAE action template failed to read.\n\nLast valid action ID read: {lastGoodActionTemplate.ID}\n\nMessage:\n{e}");
                        }
                    }
                        
                }

                SortByID();
            }

            

            /// <summary>
            /// Read a TAE template from an XML file.
            /// </summary>
            public static Template ReadXMLFile(string path, bool disableErrorHandling)
            {
                var xml = new XmlDocument();
                xml.Load(path);
                return new Template(xml, disableErrorHandling);
            }

            /// <summary>
            /// Read a TAE template from an XML string.
            /// </summary>
            public static Template ReadXMLText(string text, bool disableErrorHandling)
            {
                var xml = new XmlDocument();
                xml.LoadXml(text);
                return new Template(xml, disableErrorHandling);
            }

            /// <summary>
            /// Read a TAE template from an XML document.
            /// </summary>
            public static Template ReadXMLDoc(XmlDocument xml, bool disableErrorHandling)
            {
                return new Template(xml, disableErrorHandling);
            }
            
        }
    }
}
