﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sledge.Common.Mediator;
using Sledge.DataStructures.GameData;
using Sledge.DataStructures.MapObjects;
using Sledge.Editor.Documents;

namespace Sledge.Editor.UI
{
    public partial class MapTreeWindow : HotkeyForm, IMediatorListener
    {
        public Document Document { get; set; }

        public MapTreeWindow(Document document)
        {
            InitializeComponent();
            Document = document;
            Mediator.Subscribe(EditorMediator.DocumentActivated, this);
        }

        protected override void OnLoad(EventArgs e)
        {
            RefreshNodes();
        }

        private void DocumentActivated(Document document)
        {
            Document = document;
            RefreshNodes();
        }

        private void RefreshNodes()
        {
            MapTree.Nodes.Clear();
            if (Document == null) return;
            LoadMapNode(null, Document.Map.WorldSpawn);
        }

        private void LoadMapNode(TreeNode parent, MapObject obj)
        {
            var text = GetNodeText(obj);
            var node = new TreeNode(obj.GetType().Name + text) { Tag = obj };
            if (obj is World)
            {
                var w = (World)obj;
                node.Nodes.AddRange(GetEntityNodes(w.EntityData).ToArray());
            }
            else if (obj is Entity)
            {
                var e = (Entity)obj;
                node.Nodes.AddRange(GetEntityNodes(e.EntityData).ToArray());
            }
            else if (obj is Solid)
            {
                var s = (Solid)obj;
                node.Nodes.AddRange(GetFaceNodes(s.Faces).ToArray());
            }
            foreach (var mo in obj.Children)
            {
                LoadMapNode(node, mo);
            }
            if (parent == null) MapTree.Nodes.Add(node);
            else parent.Nodes.Add(node);
        }

        private string GetNodeText(MapObject mo)
        {
            if (mo is Solid)
            {
                return " (" + ((Solid)mo).Faces.Count + " faces)";
            }
            if (mo is Group)
            {
                return " (" + mo.Children + " children)";
            }
            var ed = mo.GetEntityData();
            if (ed != null)
            {
                var targetName = ed.GetPropertyValue("targetname");
                return ": " + ed.Name + (String.IsNullOrWhiteSpace(targetName) ? "" : " (" + targetName + ")");
            }
            return "";
        }

        private IEnumerable<TreeNode> GetEntityNodes(EntityData data)
        {
            yield return new TreeNode("Flags: " + data.Flags);
        }

        private IEnumerable<TreeNode> GetFaceNodes(IEnumerable<Face> faces)
        {
            var c = 0;
            foreach (var face in faces)
            {
                var fnode = new TreeNode("Face " + c);
                c++;
                var pnode = fnode.Nodes.Add("Plane: " + face.Plane.Normal + " * " + face.Plane.DistanceFromOrigin);
                pnode.Nodes.Add("Normal: " + face.Plane.Normal);
                pnode.Nodes.Add("Distance: " + face.Plane.DistanceFromOrigin);
                pnode.Nodes.Add("A: " + face.Plane.A);
                pnode.Nodes.Add("B: " + face.Plane.B);
                pnode.Nodes.Add("C: " + face.Plane.C);
                pnode.Nodes.Add("D: " + face.Plane.D);
                var tnode = fnode.Nodes.Add("Texture: " + face.Texture.Name);
                tnode.Nodes.Add("U Axis: " + face.Texture.UAxis);
                tnode.Nodes.Add("V Axis: " + face.Texture.VAxis);
                tnode.Nodes.Add(String.Format("Scale: X = {0}, Y = {1}", face.Texture.XScale, face.Texture.YScale));
                tnode.Nodes.Add(String.Format("Offset: X = {0}, Y = {1}", face.Texture.XShift, face.Texture.YShift));
                tnode.Nodes.Add("Rotation: " + face.Texture.Rotation);
                var vnode = fnode.Nodes.Add("Vertices: " + face.Vertices.Count);
                var d = 0;
                foreach (var vertex in face.Vertices)
                {
                    var cnode = vnode.Nodes.Add("Vertex " + d + ": " + vertex.Location);
                    d++;
                    cnode.Nodes.Add("Texture U: " + vertex.TextureU);
                    cnode.Nodes.Add("Texture V: " + vertex.TextureV);
                }
                yield return fnode;
            }
        }

        public void Notify(string message, object data)
        {
            Mediator.ExecuteDefault(this, message, data);
        }

        private void SelectionChanged(object sender, TreeViewEventArgs e)
        {
            RefreshSelectionProperties();
        }

        private void RefreshSelectionProperties()
        {
            Properties.Items.Clear();
            if (MapTree.SelectedNode != null && MapTree.SelectedNode.Tag != null)
            {
                var list = GetTagProperties(MapTree.SelectedNode.Tag);
                foreach (var kv in list)
                {
                    Properties.Items.Add(new ListViewItem(new[] {kv.Item1, kv.Item2}));
                }
                Properties.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private IEnumerable<Tuple<string, string>> GetTagProperties(object tag)
        {
            var list = new List<Tuple<string, string>>();
            if (tag is MapObject)
            {
                var mo = (MapObject) tag;
                var ed = mo.GetEntityData();
                if (ed != null)
                {
                    var gd = Document.GameData.Classes.FirstOrDefault(x => String.Equals(x.Name, ed.Name, StringComparison.InvariantCultureIgnoreCase));
                    foreach (var prop in ed.Properties)
                    {
                        var gdp = gd != null ? gd.Properties.FirstOrDefault(x => String.Equals(x.Name, prop.Key, StringComparison.InvariantCultureIgnoreCase)) : null;
                        var key = gdp != null && !String.IsNullOrWhiteSpace(gdp.ShortDescription) ? gdp.ShortDescription : prop.Key;
                        list.Add(Tuple.Create(key, prop.Value));
                    }
                }
            }
            return list;
        }
    }
}
