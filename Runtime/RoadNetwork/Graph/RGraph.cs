using PLATEAU.CityInfo;
using PLATEAU.RoadNetwork.Factory;
using PLATEAU.RoadNetwork.Mesh;
using PLATEAU.Util;
using PLATEAU.Util.GeoGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace PLATEAU.RoadNetwork.Graph
{
    /// <summary>
    /// ���H�^�C�v
    /// </summary>
    [Flags]
    public enum RRoadTypeMask
    {
        /// <summary>
        /// �����Ȃ�
        /// </summary>
        Empty = 0,
        /// <summary>
        /// �ԓ�
        /// </summary>
        Road = 1 << 0,
        /// <summary>
        /// ����
        /// </summary>
        SideWalk = 1 << 1,
        /// <summary>
        /// ����������
        /// </summary>
        Median = 1 << 2,
        /// <summary>
        /// �������H
        /// </summary>
        HighWay = 1 << 3,
    }

    public static class RRoadTypeEx
    {
        /// <summary>
        /// �ԓ�����
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsRoad(this RRoadTypeMask self)
        {
            return (self & RRoadTypeMask.Road) != 0;
        }

        /// <summary>
        /// ��ʓ��H
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsHighWay(this RRoadTypeMask self)
        {
            return (self & RRoadTypeMask.HighWay) != 0;
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsSideWalk(this RRoadTypeMask self)
        {
            return (self & RRoadTypeMask.SideWalk) != 0;
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsMedian(this RRoadTypeMask self)
        {
            return (self & RRoadTypeMask.Median) != 0;
        }

        /// <summary>
        /// self��flag�̂ǂꂩ�������Ă��邩�ǂ���
        /// </summary>
        /// <param name="self"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool HasAnyFlag(this RRoadTypeMask self, RRoadTypeMask flag)
        {
            return (self & flag) != 0;
        }
    }

    /// <summary>
    /// �ړ_
    /// </summary>
    [Serializable]
    public class RVertex : ARnParts<RVertex>
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �ڑ���
        /// </summary>
        private HashSet<REdge> edges = new HashSet<REdge>();

        /// <summary>
        /// �ʒu
        /// </summary>
        [field: SerializeField]
        public Vector3 Position { get; set; }

        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyCollection<REdge> Edges => edges;

        public RRoadTypeMask TypeMask
        {
            get
            {
                var ret = RRoadTypeMask.Empty;
                foreach (var edge in Edges)
                {
                    foreach (var poly in edge.Faces)
                        ret |= poly.RoadTypes;
                }

                return ret;
            }
        }

        public RVertex(Vector3 v)
        {
            Position = v;
        }

        /// <summary>
        /// ��{�Ăяo���֎~. �ڑ��Ӓǉ�
        /// </summary>
        /// <param name="edge"></param>
        public void AddEdge(REdge edge)
        {
            if (edges.Contains(edge))
                return;

            edges.Add(edge);
        }

        /// <summary>
        /// ��{�Ăяo���֎~. �ڑ��Ӎ폜
        /// </summary>
        /// <param name="edge"></param>
        public void RemoveEdge(REdge edge)
        {
            edges.Remove(edge);
        }

        /// <summary>
        /// �������g���O��.
        /// keepLink��true�̎��͎��������Ȃ��Ȃ��Ă��ڑ����_���m��Edge��\���Đڑ��������Ȃ��悤�ɂ���
        /// </summary>
        public void DisConnect()
        {
            // �����������Ă���ӂ��폜����
            foreach (var e in Edges.ToList())
                e.RemoveVertex(this);
        }

        /// <summary>
        /// �������g���������邤����, ���܂ł������Ȃ���͎c���悤�ɂ���
        /// </summary>
        public void DisConnectWithKeepLink()
        {
            var neighbors = GetNeighborVertices().ToList();

            // �����ƌq�����Ă���ӂ͈�U�폜
            foreach (var e in Edges.ToList())
                e.DisConnect();

            // �\��Ȃ���
            for (var i = 0; i < neighbors.Count; i++)
            {
                var v0 = neighbors[i];
                if (v0 == null)
                    continue;
                for (var j = i; j < neighbors.Count; ++j)
                {
                    var v1 = neighbors[j];
                    if (v1 == null)
                        continue;
                    if (v0.IsNeighbor(v1))
                        continue;
                    // �V�����ӂ��쐬����
                    var _ = new REdge(v0, v1);
                }
            }
        }

        /// <summary>
        /// �אڒ��_���擾
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RVertex> GetNeighborVertices()
        {
            foreach (var edge in Edges)
            {
                if (edge.V0 == this)
                {
                    Assert.IsTrue(edge.V1 != this);
                    yield return edge.V1;
                }
                else
                {
                    Assert.IsTrue(edge.V0 != this);
                    yield return edge.V0;
                }
            }
        }

        /// <summary>
        /// other�Ƃ̒��ڂ̕ӂ������Ă��邩
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsNeighbor(RVertex other)
        {
            return Edges.Any(e => e.Vertices.Contains(other));
        }


        /// <summary>
        /// ���g��dst�Ƀ}�[�W����
        /// </summary>
        public void MergeTo(RVertex dst, bool checkEdgeMerge = true)
        {
            // src�Ɍq�����Ă���ӂɕύX��ʒm����
            foreach (var e in Edges.ToList())
            {
                e.ChangeVertex(this, dst);
            }
            // �����̐ڑ��͉�������
            DisConnect();
            if (checkEdgeMerge == false)
                return;
            // �������_�������Ă���ӂ��}�[�W����
            var queue = dst.Edges.ToList();
            while (queue.Any())
            {
                var edge = queue[0];
                queue.RemoveAt(0);
                for (var i = 0; i < queue.Count;)
                {
                    if (edge.IsSameVertex(queue[i]))
                    {
                        queue[i].MergeTo(edge);
                        queue.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
    }


    /// <summary>
    /// ��
    /// </summary>
    [Serializable]
    public class REdge
        : ARnParts<REdge>
    {
        public enum VertexType
        {
            V0,
            V1,
        }

        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �ڑ���
        /// </summary>
        private HashSet<RFace> faces = new HashSet<RFace>();

        /// <summary>
        /// �\�����_(2��)
        /// </summary>
        [SerializeField]
        private RVertex[] vertices = new RVertex[2];

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �J�n�_
        /// </summary>
        public RVertex V0 => GetVertex(VertexType.V0);

        /// <summary>
        /// �I���_
        /// </summary>
        public RVertex V1 => GetVertex(VertexType.V1);

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyCollection<RFace> Faces => faces;

        /// <summary>
        /// �\�����_(2��)
        /// </summary>
        public IReadOnlyList<RVertex> Vertices => vertices;

        /// <summary>
        /// �L���ȕӂ��ǂ���. 2�̒��_�����݂��āA���قȂ邩�ǂ���
        /// </summary>
        public bool IsValid => V0 != null && V1 != null && V0 != V1;

        public REdge(RVertex v0, RVertex v1)
        {
            SetVertex(VertexType.V0, v0);
            SetVertex(VertexType.V1, v1);
        }

        /// <summary>
        /// ���_�m�[�h�擾
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public RVertex GetVertex(VertexType type)
        {
            return vertices[(int)type];
        }


        /// <summary>
        /// ���_�m�[�h�������ւ�
        /// </summary>
        /// <param name="type"></param>
        /// <param name="vertex"></param>
        public void SetVertex(VertexType type, RVertex vertex)
        {
            var old = vertices[(int)type];
            if (old == vertex)
                return;

            old?.RemoveEdge(this);
            vertices[(int)type] = vertex;
            vertex?.AddEdge(this);
        }

        /// <summary>
        /// ���_from -> to�ɕύX����
        /// from�������Ă��Ȃ��ꍇ�͖���
        /// �ύX�������ʗ����Ƃ�to�ɂȂ�ꍇ�͐ڑ������������
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ChangeVertex(RVertex from, RVertex to)
        {
            if (V0 == from)
            {
                // �����Ƃ�to�ɂȂ�ꍇ�͐ڑ������������
                if (V1 == to)
                    DisConnect();
                else
                    SetVertex(VertexType.V0, to);
            }

            if (V1 == from)
            {
                // �����Ƃ�to�ɂȂ�ꍇ�͐ڑ������������
                if (V0 == to)
                    DisConnect();
                else
                    SetVertex(VertexType.V1, to);
            }
        }

        /// <summary>
        /// ��{�Ăяo���֎~. �אږʒǉ�
        /// </summary>
        /// <param name="face"></param>
        public void AddFace(RFace face)
        {
            if (faces.Contains(face))
                return;
            faces.Add(face);
        }

        /// <summary>
        /// ��{�Ăяo���֎~. �ʂ̂Ȃ��������
        /// </summary>
        /// <param name="face"></param>
        public void RemoveFace(RFace face)
        {
            faces.Remove(face);
        }

        /// <summary>
        /// ���_���폜����
        /// </summary>
        /// <param name="vertex"></param>
        public void RemoveVertex(RVertex vertex)
        {
            if (V0 == vertex)
                SetVertex(VertexType.V0, null);
            if (V1 == vertex)
                SetVertex(VertexType.V1, null);
        }

        /// <summary>
        /// �����̐ڑ�����������
        /// </summary>
        public void DisConnect()
        {
            // �q�Ɏ����̐ڑ�����������悤�ɓ`����
            foreach (var v in vertices.ToList())
            {
                v?.RemoveEdge(this);
            }

            // �e�Ɏ����̐ڑ�����������悤�ɓ`����
            foreach (var p in faces.ToList())
            {
                p?.RemoveEdge(this);
            }

            faces.Clear();
            Array.Fill(vertices, null);
        }

        /// <summary>
        /// v��2�ɕ�������
        /// </summary>
        /// <param name="v"></param>
        public void SplitEdge(RVertex v)
        {
            var lastV1 = V1;
            SetVertex(VertexType.V1, v);
            var newEdge = new REdge(v, lastV1);
            foreach (var p in Faces)
            {
                p.AddEdge(newEdge);
            }
        }

        /// <summary>
        /// �������_���Q�Ƃ��Ă��邩�ǂ���. (�����͖��Ȃ�)
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public bool IsSameVertex(RVertex v0, RVertex v1)
        {
            return (V0 == v0 && V1 == v1) || (V0 == v1 && V1 == v0);
        }

        public bool IsSameVertex(REdge other)
        {
            return IsSameVertex(other.V0, other.V1);
        }

        /// <summary>
        /// ���g��dst�Ƀ}�[�W����
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="checkFaceMerge"></param>
        public void MergeTo(REdge dst, bool checkFaceMerge = true)
        {
            var addFaces =
                Faces.Where(poly => dst.Faces.Contains(poly) == false).ToList();
            foreach (var p in addFaces)
            {
                p.ChangeEdge(this, dst);
            }

            // �Ō�Ɏ����̐ڑ��͉�������
            DisConnect();

            if (checkFaceMerge == false)
                return;
            var queue = dst.Faces.ToList();
            while (queue.Any())
            {
                var poly = queue[0];
                queue.RemoveAt(0);
                for (var i = 0; i < queue.Count;)
                {
                    if (poly.IsSameEdges(queue[i]))
                    {
                        queue[i].MergeTo(poly);
                        queue.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// �ӂ̏W��
    /// </summary>
    [Serializable]
    public class RFace : ARnParts<RFace>
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        /// <summary>
        /// �\����\��
        /// </summary>
        [field: SerializeField]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// �Ή�����CityObjectGroup
        /// </summary>
        [SerializeField]
        private List<PLATEAUCityObjectGroup> cityObjectGroups = new List<PLATEAUCityObjectGroup>();

        /// <summary>
        /// ���H�^�C�v
        /// </summary>
        [field: SerializeField]
        public RRoadTypeMask RoadTypes { get; set; }

        /// <summary>
        /// LodLevel
        /// </summary>
        [field: SerializeField]
        public int LodLevel { get; set; }

        /// <summary>
        /// �e�O���t
        /// </summary>
        private HashSet<RGraph> graphs = new HashSet<RGraph>();

        /// <summary>
        /// �\����
        /// </summary>
        private HashSet<REdge> edges = new HashSet<REdge>();

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �\����
        /// </summary>
        public IReadOnlyCollection<REdge> Edges => edges;

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyCollection<RGraph> Graphs => graphs;

        /// <summary>
        /// �֘A����CityObjectGroup
        /// </summary>
        public IReadOnlyList<PLATEAUCityObjectGroup> CityObjectGroups => cityObjectGroups;

        // �L���ȃ|���S�����ǂ���
        public bool IsValid => edges.Count > 0;

        public RFace(RGraph graph, PLATEAUCityObjectGroup cityObjectGroup, RRoadTypeMask roadType, int lodLevel)
        {
            RoadTypes = roadType;
            LodLevel = lodLevel;
            AddCityObjectGroup(cityObjectGroup);
            AddGraph(graph);
        }

        public void AddCityObjectGroup(PLATEAUCityObjectGroup cityObjectGroup)
        {
            if (cityObjectGroups.Contains(cityObjectGroup))
                return;
            cityObjectGroups.Add(cityObjectGroup);
        }

        /// <summary>
        /// �Ӓǉ�
        /// </summary>
        /// <param name="edge"></param>
        public void AddEdge(REdge edge)
        {
            if (edges.Contains(edge))
                return;

            edges.Add(edge);
            edge.AddFace(this);
        }

        /// <summary>
        /// �e�O���t�Q�ƒǉ�
        /// </summary>
        /// <param name="graph"></param>
        public void AddGraph(RGraph graph)
        {
            if (graphs.Contains(graph))
                return;
            graphs.Add(graph);
        }

        /// <summary>
        /// �e�O���t�폜
        /// </summary>
        /// <param name="graph"></param>
        public void RemoveGraph(RGraph graph)
        {
            graphs.Remove(graph);
        }

        ///// <summary>
        ///// ��{�ĂԂ̋֎~. edge��pos�̌��ɒǉ�����
        ///// </summary>
        ///// <param name="edge"></param>
        ///// <param name="pos"></param>
        //public void InsertEdge(REdge edge, REdge pos)
        //{
        //    if (edges.Contains(edge))
        //        return;
        //    var index = edges.IndexOf(pos);
        //    edges.Insert(index + 1, edge);
        //    edge.AddFace(this);
        //}

        /// <summary>
        /// �Ӎ폜
        /// </summary>
        /// <param name="edge"></param>
        public void RemoveEdge(REdge edge)
        {
            edges.Remove(edge);
            // �q���玩�����폜
            edge.RemoveFace(this);
        }

        /// <summary>
        /// �ӂ̕ύX
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ChangeEdge(REdge from, REdge to)
        {
            RemoveEdge(from);
            AddEdge(to);
        }

        /// <summary>
        /// ����Edge�ō\������Ă��邩�ǂ���
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSameEdges(RFace other)
        {
            if (edges.Count != other.edges.Count)
                return false;
            return other.Edges.All(e => Edges.Contains(e));
        }

        /// <summary>
        /// ��{�Ăяo���֎~. ���g��dst�Ƀ}�[�W����
        /// </summary>
        /// <param name="dst"></param>
        public void MergeTo(RFace dst)
        {
            dst.RoadTypes |= RoadTypes;
            foreach (var co in cityObjectGroups)
            {
                dst.AddCityObjectGroup(co);
            }
            dst.LodLevel = Mathf.Max(dst.LodLevel, LodLevel);
            DisConnect();
        }


        /// <summary>
        /// �����̐ڑ�����������
        /// </summary>
        public void DisConnect()
        {
            // �q�Ɏ����̐ڑ�����������悤�ɓ`����
            foreach (var e in Edges)
                e.RemoveFace(this);

            // �e�Ɏ����̐ڑ�����������悤�ɓ`����
            foreach (var g in graphs)
                g.RemoveFace(this);
            edges.Clear();
            graphs.Clear();
        }

    }

    [Serializable]
    public class RGraph : ARnParts<RGraph>
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        private HashSet<RFace> faces = new HashSet<RFace>();

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------
        /// <summary>
        /// ��
        /// </summary>
        public IReadOnlyCollection<RFace> Faces => faces;

        /// <summary>
        /// �SEdge���擾(�d��)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<REdge> GetAllEdge()
        {
            return Faces.SelectMany(p => p.Edges).Distinct();
        }

        /// <summary>
        /// �SVertex���擾(�d��)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RVertex> GetAllVertex()
        {
            return GetAllEdge().SelectMany(e => e.Vertices).Distinct();
        }

        /// <summary>
        /// �eFace�ǉ�
        /// </summary>
        /// <param name="face"></param>
        public void AddFace(RFace face)
        {
            if (face == null)
                return;
            if (faces.Contains(face))
                return;
            faces.Add(face);
            face.AddGraph(this);
        }

        /// <summary>
        /// �eFace�폜
        /// </summary>
        /// <param name="face"></param>
        public void RemoveFace(RFace face)
        {
            faces.Remove(face);
            face?.RemoveGraph(this);
        }
    }

    public static class RGraphEx
    {
        public readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public RVertex V0 { get; }
            public RVertex V1 { get; }

            public EdgeKey(RVertex v0, RVertex v1)
            {
                V0 = v0;
                V1 = v1;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(V0, V1);
            }

            public bool Equals(EdgeKey other)
            {
                // V0/V1���t�ł������Ƃ݂Ȃ�
                if (Equals(V0, other.V0) && Equals(V1, other.V1))
                    return true;

                if (Equals(V0, other.V1) && Equals(V1, other.V0))
                    return true;

                return false;
            }
        }

        public static RGraph Create(List<ConvertedCityObject> cityObjects)
        {
            var graph = new RGraph();
            Dictionary<Vector3, RVertex> vertexMap = new Dictionary<Vector3, RVertex>();
            Dictionary<EdgeKey, REdge> edgeMap = new Dictionary<EdgeKey, REdge>();
            foreach (var cityObject in cityObjects)
            {
                var root = cityObject.CityObjects.rootCityObjects[0];
                var lodLevel = cityObject.CityObjectGroup.GetLodLevel();
                var roadType = root.GetRoadType();

                foreach (var mesh in cityObject.Meshes)
                {
                    var face = new RFace(graph, cityObject.CityObjectGroup, roadType, lodLevel);
                    var vertices = mesh.Vertices.Select(v =>
                    {
                        return vertexMap.GetValueOrCreate(v, k => new RVertex(k));
                    }).ToList();
                    foreach (var s in mesh.SubMeshes)
                    {
                        var separated = s.Separate();

                        foreach (var m in separated)
                        {
                            for (var i = 0; i < m.Triangles.Count; i += 3)
                            {
                                var e0 = edgeMap.GetValueOrCreate(new EdgeKey(vertices[m.Triangles[i]], vertices[m.Triangles[i + 1]])
                                    , e => new REdge(e.V0, e.V1));
                                var e1 = edgeMap.GetValueOrCreate(new EdgeKey(vertices[m.Triangles[i + 1]], vertices[m.Triangles[i + 2]])
                                    , e => new REdge(e.V0, e.V1));
                                var e2 = edgeMap.GetValueOrCreate(new EdgeKey(vertices[m.Triangles[i + 2]], vertices[m.Triangles[i]])
                                    , e => new REdge(e.V0, e.V1));
                                var edges = new[] { e0, e1, e2 };
                                foreach (var e in edges)
                                    face.AddEdge(e);
                            }
                        }
                    }

                    graph.AddFace(face);
                }
            }
            return graph;
        }

        /// <summary>
        /// ���_���}�[�W����
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="mergeEpsilon"></param>
        /// <param name="mergeCellLength"></param>
        public static void MergeVertices(this RGraph graph, float mergeEpsilon, int mergeCellLength)
        {
            var vertices = graph.GetAllVertex().ToList();

            var vertexTable = GeoGraphEx.MergeVertices(vertices.Select(v => v.Position), mergeEpsilon, mergeCellLength);
            var vertex2RVertex = vertexTable.Values.Distinct().ToDictionary(v => v, v => new RVertex(v));
            Debug.Log($"MergeVertices: {vertices.Count} -> {vertex2RVertex.Count + vertices.Count(v => vertexTable.ContainsKey(v.Position) == false)}");
            var hoge = vertices.Where(v => (v.Position.Xz() - new Vector2(-439.77f, -16.70f)).sqrMagnitude < 0.1f).ToList();
            foreach (var v in vertices)
            {
                if (vertexTable.TryGetValue(v.Position, out var dst))
                {
                    v.MergeTo(vertex2RVertex[dst]);
                }
            }
        }

        /// <summary>
        /// �A�E�g���C�����_���v�Z����
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static List<RVertex> ComputeOutlineVertices(this RFace face)
        {
            return GeoGraph2D.ComputeOutline(
                face.Edges.SelectMany(e => e.Vertices).Distinct()
                , v => v.Position.GetTangent(AxisPlane.Xz)
                , v => v.GetNeighborVertices().Where(b => b.Edges.Any(e => e.Faces.Contains(face))),
                false);
        }
    }
}
