using PLATEAU.CityInfo;
using PLATEAU.RoadNetwork.Factory;
using PLATEAU.RoadNetwork.Mesh;
using PLATEAU.Util;
using PLATEAU.Util.GeoGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Assertions;

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
        private List<REdge> edges = new List<REdge>();

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
        public IReadOnlyList<REdge> Edges => edges;

        public RRoadTypeMask TypeMask
        {
            get
            {
                var ret = RRoadTypeMask.Empty;
                foreach (var edge in Edges)
                {
                    foreach (var poly in edge.Polygons)
                        ret |= poly.RoadType;
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
        /// <param name="keepLink"></param>
        public void DisConnect(bool keepLink)
        {
            var neighbors = GetNeighborVertices().ToList();
            foreach (var e in Edges)
            {
                e.DisConnect();
            }

            if (keepLink)
            {
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


    }


    /// <summary>
    /// ��
    /// </summary>
    [Serializable]
    public class REdge : ARnParts<REdge>
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
        private List<RPolygon> polygons = new List<RPolygon>();

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
        public IReadOnlyList<RPolygon> Polygons => polygons;

        /// <summary>
        /// �\�����_(2��)
        /// </summary>
        public IReadOnlyList<RVertex> Vertices => vertices;

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
        /// <param name="polygon"></param>
        public void AddPolygon(RPolygon polygon)
        {
            if (polygons.Contains(polygon))
                return;
            polygons.Add(polygon);
        }

        /// <summary>
        /// ��{�Ăяo���֎~. �ʂ̂Ȃ��������
        /// </summary>
        /// <param name="polygon"></param>
        public void RemovePolygon(RPolygon polygon)
        {
            polygons.Remove(polygon);
        }

        /// <summary>
        /// �����̐ڑ�����������
        /// </summary>
        public void DisConnect()
        {
            foreach (var v in vertices)
            {
                v?.RemoveEdge(this);
            }

            // �e�Ɏ����̐ڑ�����������悤�ɓ`����
            foreach (var p in polygons)
            {
                p?.RemoveEdge(this);
            }

            polygons.Clear();
            Array.Fill(vertices, null);
        }



        /// <summary>
        /// edge��v��2�ɕ�������
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="v"></param>
        public void SplitEdge(RVertex v)
        {
            var lastV1 = V1;
            SetVertex(VertexType.V1, v);
            var newEdge = new REdge(v, lastV1);
            foreach (var p in Polygons)
            {
                p.InsertEdge(newEdge, this);
            }
        }
    }

    /// <summary>
    /// ���p�`
    /// </summary>
    [Serializable]
    public class RPolygon : ARnParts<RPolygon>
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
        [field: SerializeField]
        public PLATEAUCityObjectGroup CityObjectGroup { get; set; }

        /// <summary>
        /// ���H�^�C�v
        /// </summary>
        [field: SerializeField]
        public RRoadTypeMask RoadType { get; set; }

        /// <summary>
        /// LodLevel
        /// </summary>
        [field: SerializeField]
        public int LodLevel { get; set; }

        /// <summary>
        /// �e�O���t
        /// </summary>
        public RGraph Graph { get; private set; }

        /// <summary>
        /// �\����
        /// </summary>
        private List<REdge> edges = new List<REdge>();

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �\����
        /// </summary>
        public IReadOnlyList<REdge> Edges => edges;

        // �L���ȃ|���S�����ǂ���
        public bool IsValid => Edges.Count >= 3;

        public RPolygon(RGraph graph, PLATEAUCityObjectGroup cityObjectGroup, RRoadTypeMask roadType, int lodLevel)
        {
            Graph = graph;
            CityObjectGroup = cityObjectGroup;
            RoadType = roadType;
            LodLevel = lodLevel;
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
            edge.AddPolygon(this);
        }

        /// <summary>
        /// ��{�ĂԂ̋֎~. edge��pos�̌��ɒǉ�����
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="pos"></param>
        public void InsertEdge(REdge edge, REdge pos)
        {
            if (edges.Contains(edge))
                return;
            var index = edges.IndexOf(pos);
            edges.Insert(index + 1, edge);
            edge.AddPolygon(this);
        }

        /// <summary>
        /// �Ӎ폜
        /// </summary>
        /// <param name="edge"></param>
        public void RemoveEdge(REdge edge)
        {
            edges.Remove(edge);
            edge.RemovePolygon(this);
        }

    }

    [Serializable]
    public class RGraph : ARnParts<RGraph>
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        private List<RPolygon> polygons = new List<RPolygon>();

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------
        /// <summary>
        /// ��
        /// </summary>
        public IReadOnlyList<RPolygon> Polygons => polygons;

        /// <summary>
        /// �SEdge���擾(�d��)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<REdge> GetAllEdge()
        {
            return Polygons.SelectMany(p => p.Edges).Distinct();
        }

        /// <summary>
        /// �SVertex���擾(�d��)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RVertex> GetAllVertex()
        {
            return GetAllEdge().SelectMany(e => e.Vertices).Distinct();
        }



        public void AddPolygon(RPolygon polygon)
        {
            polygons.Add(polygon);
        }

        public void RemovePolygon(RPolygon polygon)
        {
            polygons.Remove(polygon);
        }

        /// <summary>
        /// src��dst�Ƀ}�[�W����
        /// </summary>
        public void MergeVertex(RVertex src, RVertex dst)
        {
            // src�Ɍq�����Ă���ӂɕύX��ʒm����
            foreach (var e in src.Edges)
            {
                e.ChangeVertex(src, dst);
            }
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
            Dictionary<EdgeKey, REdge> edgeMap = new Dictionary<EdgeKey, REdge>();
            foreach (var cityObject in cityObjects)
            {
                var root = cityObject.CityObjects.rootCityObjects[0];
                var lodLevel = cityObject.CityObjectGroup.GetLodLevel();
                var roadType = root.GetRoadType();

                foreach (var mesh in cityObject.Meshes)
                {
                    var polygon = new RPolygon(graph, cityObject.CityObjectGroup, roadType, lodLevel);
                    var vertices = mesh.Vertices.Select(v => new RVertex(v)).ToList();
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
                                    polygon.AddEdge(e);
                            }
                        }
                    }

                    graph.AddPolygon(polygon);
                }
            }
            return graph;
        }

        public static void MergeVertices(this RGraph graph, float mergeEpsilon, int mergeCellLength)
        {
            var vertices = graph.GetAllVertex().ToList();

            var vertexTable = GeoGraphEx.MergeVertices(vertices.Select(v => v.Position), mergeEpsilon, mergeCellLength);
            var vertex2RVertex = vertexTable.Values.Distinct().ToDictionary(v => v, v => new RVertex(v));
            foreach (var v in vertices)
            {
                if (vertexTable.TryGetValue(v.Position, out var dst))
                {
                    graph.MergeVertex(v, vertex2RVertex[dst]);
                }
            }
        }
    }
}
