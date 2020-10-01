using SoulsAssetPipeline.XmlStructs;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static SoulsFormats.FLVER2;

namespace SoulsAssetPipeline
{
    public class FLVER2MaterialInfoBank
    {
        public List<MaterialDef> MaterialDefs = new List<MaterialDef>();
        public Dictionary<string, XmlStructDef> GXItemStructs = new Dictionary<string, XmlStructDef>();

        public class VertexBufferDeclaration
        {
            public List<BufferLayout> Buffers = new List<BufferLayout>();

            internal void ReadXML(XmlNode node)
            {
                var bufferLayoutNodes = node.SelectNodes("vertex_buffer");
                int bufferIndex = 0;
                Buffers.Clear();
                foreach (XmlNode bln in bufferLayoutNodes)
                {
                    var bufferLayout = new BufferLayout();
                    foreach (XmlNode memberNode in bln.ChildNodes)
                    {
                        FLVER.LayoutType memberType = (FLVER.LayoutType)Enum.Parse(typeof(FLVER.LayoutType), memberNode.Name);
                        FLVER.LayoutSemantic memberSemantic = FLVER.LayoutSemantic.Position;
                        var memberIndex = 0;
                        var memberBufferIndex = bufferIndex;



                        // Try to parse Semantic[Index] lol
                        int memberSemanticLeftBracketIndex = memberNode.InnerText.IndexOf('[');
                        int memberSemanticRightBracketIndex = memberNode.InnerText.IndexOf(']');
                        int memberSemanticIndexStrLength = memberSemanticRightBracketIndex - memberSemanticLeftBracketIndex - 1;
                        // If it has [] brackets with text inbetween them, parse index from within brackets and semantic from before brackets.
                        if (memberSemanticLeftBracketIndex >= 0 && memberSemanticRightBracketIndex >= 0 && memberSemanticIndexStrLength > 0)
                        {
                            memberIndex = int.Parse(memberNode.InnerText.Substring(memberSemanticLeftBracketIndex + 1, memberSemanticIndexStrLength));
                            memberSemantic = (FLVER.LayoutSemantic)Enum.Parse(typeof(FLVER.LayoutSemantic), 
                                memberNode.InnerText.Substring(0, memberSemanticLeftBracketIndex));
                        }
                        // Otherwise entire string parsed as semantic and index is 0.
                        else
                        {
                            memberIndex = 0;
                            memberSemantic = (FLVER.LayoutSemantic)Enum.Parse(typeof(FLVER.LayoutSemantic), memberNode.InnerText);
                        }


                        var bufferIndexText = memberNode.Attributes["from_buffer_index"]?.InnerText;
                        if (bufferIndexText != null && int.TryParse(bufferIndexText, out int specifiedBufferIndex))
                        {
                            memberBufferIndex = specifiedBufferIndex;
                        }


                        bufferLayout.Add(new FLVER.LayoutMember(memberType, memberSemantic, memberIndex, memberBufferIndex));
                    }
                    Buffers.Add(bufferLayout);
                    bufferIndex++;
                }
            }


        }

        public class GXItemDef
        {
            public string GXID;
            public int Unk04;
            public int DataLength;

            public void ReadXML(XmlNode node)
            {
                GXID = node.SafeGetAttribute("gxid");
                Unk04 = node.SafeGetInt32Attribute("unk04");
                DataLength = node.SafeGetInt32Attribute("data_length");
            }
        }

        public class MaterialDef
        {
            public string MTD;
            public List<VertexBufferDeclaration> AcceptableVertexBufferDeclarations 
                = new List<VertexBufferDeclaration>();
            public List<GXItemDef> GXItems = new List<GXItemDef>();
            

            public List<string> TextureChannels = new List<string>();

            public void ReadXML(XmlNode node)
            {
                MTD = node.Attributes["mtd"].InnerText;


                AcceptableVertexBufferDeclarations.Clear();

                var vertBufferDeclarations = node.SelectNodes("acceptable_vertex_buffer_declarations/vertex_buffer_declaration");
                foreach (XmlNode vbnode in vertBufferDeclarations)
                {
                    var vb = new VertexBufferDeclaration();
                    vb.ReadXML(vbnode);
                    AcceptableVertexBufferDeclarations.Add(vb);
                }


                TextureChannels.Clear();

                var texChannelNodes = node.SelectNodes("texture_channel_list/texture_channel");
                foreach (XmlNode tcn in texChannelNodes)
                {
                    TextureChannels.Add(tcn.InnerText);
                }


                GXItems.Clear();

                var gxItemNodes = node.SelectNodes("gx_item_list/gx_item");
                foreach (XmlNode gin in gxItemNodes)
                {
                    var g = new GXItemDef();
                    g.ReadXML(gin);
                    GXItems.Add(g);
                }
            }
        }

        public void ReadXML(string xmlFile)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlFile);
            var materialDefNodes = xml.SelectNodes("material_info_bank/material_def_list/material_def");

            MaterialDefs.Clear();

            List<string> mtdsAlreadyDefined = new List<string>();

            foreach (XmlNode mdn in materialDefNodes)
            {
                var mat = new MaterialDef();
                mat.ReadXML(mdn);
                MaterialDefs.Add(mat);
                if (mtdsAlreadyDefined.Contains(mat.MTD))
                    Console.WriteLine("FATCAT");
                else
                    mtdsAlreadyDefined.Add(mat.MTD);
            }

            GXItemStructs.Clear();

            var gxItemNodes = xml.SelectNodes("material_info_bank/gx_item_struct_list/gx_item_struct");
            foreach (XmlNode gin in gxItemNodes)
            {
                string gxid = gin.SafeGetAttribute("gxid");
                var structDef = new XmlStructDef(gin);
                GXItemStructs.Add(gxid, structDef);
            }
        }


    }
}
