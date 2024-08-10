using PLATEAU.CityInfo;
using PLATEAU.RoadNetwork.Mesh;
using PLATEAU.Util;
using PLATEAU.Util.GeoGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// �s���Ȓl
        /// </summary>
        Undefined = 1 << 4,
        /// <summary>
        /// �S�Ă̒l
        /// </summary>
        All = ~0
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
                    foreach (var face in edge.Faces)
                        ret |= face.RoadTypes;
                }

                return ret;
            }
        }

        public IEnumerable<RFace> GetFaces()
        {
            foreach (var edge in Edges)
            {
                foreach (var face in edge.Faces)
                    yield return face;
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
        /// �אڂ��Ă���Edge���擾
        /// </summary>
        /// <returns></returns>
        public IEnumerable<REdge> GetNeighborEdges()
        {
            if (V0 != null)
            {
                foreach (var e in V0.Edges)
                {
                    if (e != this)
                        yield return e;
                }
            }

            if (V1 != null && V1 != V0)
            {
                foreach (var e in V1.Edges)
                {
                    if (e != this)
                        yield return e;
                }
            }
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
        /// v��2�ɕ�������, ����edge��V0->v, �V����Edge��v->V1�ɂȂ�. �V����Edge��Ԃ�
        /// </summary>
        /// <param name="v"></param>
        public REdge SplitEdge(RVertex v)
        {
            var lastV1 = V1;
            SetVertex(VertexType.V1, v);
            var newEdge = new REdge(v, lastV1);
            foreach (var p in Faces)
            {
                p.AddEdge(newEdge);
            }

            return newEdge;
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
        /// other�Ƌ��L���Ă��钸�_�����邩�ǂ���
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsShareAnyVertex(REdge other)
        {
            return IsShareAnyVertex(other, out _);
        }

        /// <summary>
        /// other�Ƌ��L���Ă��钸�_�����邩�ǂ���
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsShareAnyVertex(REdge other, out RVertex sharedVertex)
        {
            if (V0 == other.V0 || V1 == other.V0)
            {
                sharedVertex = other.V0;
                return true;
            }

            if (V0 == other.V1 || V1 == other.V1)
            {
                sharedVertex = other.V1;
                return true;
            }

            sharedVertex = null;
            return false;
        }

        /// <summary>
        /// vertex�Ɣ��Α��̒��_���擾����. vertex���܂܂�Ă��Ȃ��ꍇ��null��Ԃ�
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="opposite"></param>
        /// <returns></returns>
        public bool TryGetOppositeVertex(RVertex vertex, out RVertex opposite)
        {
            if (V0 == vertex)
            {
                opposite = V1;
                return true;
            }

            if (V1 == vertex)
            {
                opposite = V0;
                return true;
            }

            opposite = null;
            return false;
        }

        /// <summary>
        /// vertex�Ɣ��Α��̒��_���擾����. vertex���܂܂�Ă��Ȃ��ꍇ��null��Ԃ�
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public RVertex GetOppositeVertex(RVertex vertex)
        {
            if (TryGetOppositeVertex(vertex, out var opposite))
                return opposite;
            return null;
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
                        // �ӂ͑S�ē����Ȃ̂ō������̂��߈ړ������͍s��Ȃ�
                        queue[i].MergeTo(poly, false);
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
        private PLATEAUCityObjectGroup cityObjectGroup = null;

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
        private RGraph graph = null;

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
        public RGraph Graph => graph;

        /// <summary>
        /// �֘A����CityObjectGroup
        /// </summary>
        public PLATEAUCityObjectGroup CityObjectGroup => cityObjectGroup;

        // �L���ȃ|���S�����ǂ���
        public bool IsValid => edges.Count > 0;

        public RFace(RGraph graph, PLATEAUCityObjectGroup cityObjectGroup, RRoadTypeMask roadType, int lodLevel)
        {
            RoadTypes = roadType;
            LodLevel = lodLevel;
            this.cityObjectGroup = cityObjectGroup;
            this.graph = graph;
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
        /// �e�O���t�폜
        /// </summary>
        /// <param name="g"></param>
        public void RemoveGraph(RGraph g)
        {
            if (Graph == g)
                graph = null;
        }

        /// <summary>
        /// �e�O���t�����ւ�
        /// </summary>
        /// <param name="g"></param>
        public void SetGraph(RGraph g)
        {
            if (graph == g)
                return;

            graph?.RemoveFace(this);
            graph = g;
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
        /// moveEdge = false�̎��͎��g��Edges�͈ړ����Ȃ�.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="moveEdge"></param>
        public void MergeTo(RFace dst, bool moveEdge = true)
        {
            dst.RoadTypes |= RoadTypes;

            if (dst.CityObjectGroup && dst.CityObjectGroup != CityObjectGroup)
                throw new InvalidDataException("CityObjectGroup���قȂ�ꍇ�̓}�[�W�ł��܂���");
            dst.cityObjectGroup = CityObjectGroup;
            dst.LodLevel = Mathf.Max(dst.LodLevel, LodLevel);
            foreach (var e in Edges)
                dst.AddEdge(e);
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
            Graph?.RemoveFace(this);

            edges.Clear();
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
        public IEnumerable<REdge> GetAllEdges()
        {
            return Faces.SelectMany(p => p.Edges).Distinct();
        }

        /// <summary>
        /// �SVertex���擾(�d��)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RVertex> GetAllVertices()
        {
            return GetAllEdges().SelectMany(e => e.Vertices).Distinct();
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
            face.SetGraph(this);
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

    public class RFaceGroup
    {
        public RGraph Graph { get; }

        public PLATEAUCityObjectGroup CityObjectGroup { get; }

        public HashSet<RFace> Faces { get; } = new HashSet<RFace>();

        public RRoadTypeMask RoadTypes
        {
            get
            {
                return Faces.Aggregate((RRoadTypeMask)0, (a, f) => a | f.RoadTypes);
            }
        }

        public RFaceGroup(RGraph graph, PLATEAUCityObjectGroup cityObjectGroup, IEnumerable<RFace> faces)
        {
            Graph = graph;
            CityObjectGroup = cityObjectGroup;
            foreach (var face in faces)
                Faces.Add(face);
        }
    }

    public static class RVertexEx
    {
        /// <summary>
        /// self�ɑ΂��āA�w�肵��CityObjectGroup�ɕR�Â�Type���擾����
        /// </summary>
        /// <param name="self"></param>
        /// <param name="cog"></param>
        /// <returns></returns>
        public static RRoadTypeMask GetRoadType(this RVertex self, PLATEAUCityObjectGroup cog)
        {
            RRoadTypeMask roadType = RRoadTypeMask.Empty;
            foreach (var face in self.GetFaces().Where(f => f.CityObjectGroup == cog))
            {
                roadType |= face.RoadTypes;
            }
            return roadType;
        }

        public static RRoadTypeMask GetRoadType(this RVertex self, Func<RFace, bool> faceSelector)
        {
            RRoadTypeMask roadType = RRoadTypeMask.Empty;
            foreach (var face in self.GetFaces().Where(faceSelector))
            {
                roadType |= face.RoadTypes;
            }
            return roadType;
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
                if (V0.GetHashCode() > V1.GetHashCode())
                {
                    (V0, V1) = (V1, V0);
                }
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

        public static RGraph Create(List<SubDividedCityObject> cityObjects)
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
        /// ���_�����_�N�V��������
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mergeCellSize"></param>
        /// <param name="mergeCellLength"></param>
        /// <param name="midPointTolerance">a��c�Ƃ����ڑ����Ă��Ȃ��_b�ɑ΂��āAa-c�̒����Ƃ̋���������ȉ�����b���}�[�W����</param>
        public static void VertexReduction(this RGraph self, float mergeCellSize, int mergeCellLength, float midPointTolerance)
        {
            while (true)
            {
                var vertices = self.GetAllVertices().ToList();

                var vertexTable = GeoGraphEx.MergeVertices(vertices.Select(v => v.Position), mergeCellSize, mergeCellLength);
                var vertex2RVertex = vertexTable.Values.Distinct().ToDictionary(v => v, v => new RVertex(v));

                var afterCount = vertex2RVertex.Count +
                                 vertices.Count(v => vertexTable.ContainsKey(v.Position) == false);
                Debug.Log($"MergeVertices: {vertices.Count} -> {afterCount}");
                foreach (var v in vertices)
                {
                    if (vertexTable.TryGetValue(v.Position, out var dst))
                    {
                        v.MergeTo(vertex2RVertex[dst]);
                    }
                }
                if (vertices.Count == afterCount)
                    break;
            }
            // a-b-c�̂悤�Ȓ�����̒��_���폜����
            while (true)
            {
                var vertices = self.Faces
                    .SelectMany(f => f.Edges)
                    .SelectMany(e => e.Vertices)
                    .Where(v => v.Edges.Count == 2)
                    .Distinct()
                    .ToList();

                var sqrLen = midPointTolerance * midPointTolerance;
                var count = 0;
                foreach (var v in vertices)
                {
                    var neighbor = v.GetNeighborVertices().ToList();
                    if (neighbor.Count != 2)
                        continue;

                    // ���ԓ_�������Ă��قڒ����������ꍇ�͒��ԓ_�͍폜����
                    var segment = new LineSegment3D(neighbor[0].Position, neighbor[1].Position);
                    var p = segment.GetNearestPoint(v.Position);
                    if ((p - v.Position).sqrMagnitude < sqrLen)
                    {
                        v.MergeTo(neighbor[0]);
                        count++;
                    }
                }
                Debug.Log($"RemoveMidPoint : {vertices.Count} -> {vertices.Count - count}");
                if (count == 0)
                    break;
            }
#if false
            while (true)
            {
                var vertices = self.GetAllVertices().ToList();
                Dictionary<REdge, HashSet<RVertex>> edgeInsertMap = new();
                foreach (var v in vertices)
                {
                    var edges = v.Edges.ToList();
                    for (var i = 0; i < v.Edges.Count - 1; i++)
                    {
                        var e0 = edges[i];
                        var v0 = e0.GetOppositeVertex(v);
                        var s0 = new LineSegment3D(v.Position, v0.Position);
                        for (var j = i + 1; j < edges.Count; j++)
                        {
                            var e1 = edges[j];
                            var v1 = e1.GetOppositeVertex(v);
                            if (v0 == v1)
                                continue;

                            var s1 = new LineSegment3D(v.Position, v1.Position);
                            if (s0.Magnitude < s1.Magnitude)
                            {
                                var n = s1.GetNearestPoint(v0.Position);
                                if ((n - v0.Position).magnitude < 0.2f)
                                {
                                    edgeInsertMap.GetValueOrCreate(e1).Add(v0);
                                }
                            }
                            else
                            {
                                var n = s0.GetNearestPoint(v1.Position);
                                if ((n - v1.Position).magnitude < 0.2f)
                                {
                                    edgeInsertMap.GetValueOrCreate(e0).Add(v1);
                                }
                            }
                        }
                    }
                }
                if (edgeInsertMap.Any() == false)
                    break;
                foreach (var e in edgeInsertMap)
                {
                    var sortedV = e.Value.OrderBy(p => (p.Position - e.Key.V0.Position).sqrMagnitude).ToList();

                    var edge = e.Key;
                    foreach (var v in sortedV)
                    {
                        edge = edge.SplitEdge(v);
                    }
                }

                self.EdgeReduction();
            }
#endif
        }

        /// <summary>
        /// �ӂ̃��_�N�V���������i�������_�����ӂ��}�[�W����)
        /// </summary>
        /// <param name="self"></param>
        public static void EdgeReduction(this RGraph self)
        {
            var edges = self.GetAllEdges().ToList();
            var edgeTable = new Dictionary<EdgeKey, HashSet<REdge>>();

            foreach (var e in edges)
            {
                var key = new EdgeKey(e.V0, e.V1);
                edgeTable.GetValueOrCreate(key).Add(e);
            }
            Debug.Log($"MergeEdges: {edges.Count} -> {edgeTable.Count}");
            foreach (var e in edgeTable.Where(e => e.Value.Count > 1))
            {
                var dst = e.Value.First();
                var remove = e.Value.Skip(1).ToList();

                foreach (var r in remove)
                {
                    r.MergeTo(dst);
                }
            }
        }

        /// <summary>
        /// ����PLATEAUCityObjectGroup��Face��predicate�̃��[���ɏ]���Ĉ��RFace�ɂ���
        /// </summary>
        /// <param name="self"></param>
        /// <param name="isMatch"></param>
        public static List<RFaceGroup> GroupBy(this RGraph self, Func<RFace, RFace, bool> isMatch)
        {
            var ret = new List<RFaceGroup>(self.Faces.Count);
            foreach (var group in self.Faces.GroupBy(f => f.CityObjectGroup))
            {
                var faces = group.ToHashSet();
                var faceGroups = new List<RFaceGroup>();
                while (faces.Any())
                {
                    var queue = new Queue<RFace>();
                    queue.Enqueue(faces.First());
                    faces.Remove(faces.First());
                    var g = new HashSet<RFace>();
                    while (queue.Any())
                    {
                        var f0 = queue.Dequeue();
                        g.Add(f0);
                        foreach (var f1 in faces)
                        {
                            if (IsShareEdge(f0, f1) && isMatch(f0, f1))
                            {
                                g.Add(f1);
                                queue.Enqueue(f1);
                            }
                        }
                        foreach (var f in queue)
                            faces.Remove(f);
                    }

                    faceGroups.Add(new RFaceGroup(self, group.Key, g));
                }

                foreach (var f in faceGroups)
                    ret.Add(f);

            }
            return ret;
        }

        public static void Optimize(this RGraph self, float mergeCellSize, int mergeCellLength, float midPointTolerance)
        {
            self.VertexReduction(mergeCellSize, mergeCellLength, midPointTolerance);
            self.EdgeReduction();
            self.InsertVertexInNearEdge(midPointTolerance);
            self.EdgeReduction();
            self.SeparateFaces();
        }

        /// <summary>
        /// �e���_�ƕӂ̓����蔻��`�F�b�N. �����덷���e�ʂ�tolerance
        /// </summary>
        /// <param name="self"></param>
        /// <param name="tolerance"></param>
        public static void InsertVertexInNearEdge(this RGraph self, float tolerance)
        {
            var vertices = self.GetAllVertices().ToList();

            var comp = Comparer<float>.Default;
            int Compare(RVertex v0, RVertex v1)
            {
                var x = comp.Compare(v0.Position.x, v1.Position.x);
                if (x != 0)
                    return x;
                var z = comp.Compare(v0.Position.z, v1.Position.z);
                if (z != 0)
                    return z;
                return comp.Compare(v0.Position.y, v1.Position.y);
            }
            vertices.Sort(Compare);

            var queue = new HashSet<REdge>();

            Dictionary<REdge, HashSet<RVertex>> edgeInsertMap = new();
            Dictionary<Vector3, RVertex> vertexMap = new();

            var threshold = tolerance * tolerance;
            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                // �V�K�ǉ���
                var addEdges = new List<REdge>();
                var removeEdges = new HashSet<REdge>();
                foreach (var e in v.Edges)
                {
                    // v�Ɣ��Α��̓_������
                    var o = e.V0 == v ? e.V1 : e.V0;
                    var d = Compare(v, o);
                    // v���J�n�_�̕ӂ�ǉ�����
                    if (d < 0)
                        addEdges.Add(e);
                    // v���I���_�̕ӂ����o��
                    else if (d > 0)
                        removeEdges.Add(e);
                }
                foreach (var remove in removeEdges)
                    queue.Remove(remove);

                foreach (var e in queue)
                {
                    if (e.V0 == v || e.V1 == v)
                        continue;

                    var s = new LineSegment3D(e.V0.Position, e.V1.Position);
                    var near = s.GetNearestPoint(v.Position);
                    if ((near - v.Position).sqrMagnitude < threshold)
                    {
                        edgeInsertMap.GetValueOrCreate(e).Add(v);
                    }
                }

                foreach (var add in addEdges)
                    queue.Add(add);
            }

            foreach (var e in edgeInsertMap)
            {
                e.Key.InsertVertices(e.Value);
            }
        }

        /// <summary>
        /// ��������ӂ̌�_�ɒ��_��ǉ�����
        /// </summary>
        /// <param name="self"></param>
        /// <param name="heightTolerance">��_�����������ɂ���Ă������̋��e��</param>
        public static void InsertVerticesInEdgeIntersection(this RGraph self, float heightTolerance)
        {
            var vertices = self.GetAllVertices().ToList();

            var comp = Comparer<float>.Default;
            int Compare(RVertex v0, RVertex v1)
            {
                var x = comp.Compare(v0.Position.x, v1.Position.x);
                if (x != 0)
                    return x;
                var z = comp.Compare(v0.Position.z, v1.Position.z);
                if (z != 0)
                    return z;
                return comp.Compare(v0.Position.y, v1.Position.y);
            }
            vertices.Sort(Compare);

            var queue = new HashSet<REdge>();

            Dictionary<REdge, HashSet<RVertex>> edgeInsertMap = new();
            Dictionary<Vector3, RVertex> vertexMap = new();
            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                // �V�K�ǉ���
                var addEdges = new List<REdge>();
                var removeEdges = new HashSet<REdge>();
                foreach (var e in v.Edges)
                {
                    // v�Ɣ��Α��̓_������
                    var o = e.V0 == v ? e.V1 : e.V0;
                    var d = Compare(v, o);
                    // v���J�n�_�̕ӂ�ǉ�����
                    if (d < 0)
                        addEdges.Add(e);
                    // v���I���_�̕ӂ����o��
                    else if (d > 0)
                        removeEdges.Add(e);
                }
                bool NearlyEqual(float a, float b)
                {
                    return Mathf.Abs(a - b) < 1e-3f;
                }

                // ���񏜂����������m�̓`�F�b�N���Ȃ�(v�Ō������Ă��邩��)
                var targets = queue.Where(e =>
                {
                    // v��[�_�Ɏ��ӂ͖���
                    if (e.V0 == v || e.V1 == v)
                        return false;
                    return removeEdges.Contains(e) == false;
                }).ToList();
                var shareMid = 0.3f * 0.3f;
                foreach (var e0 in removeEdges)
                {
                    var s0 = new LineSegment3D(e0.V0.Position, e0.V1.Position);
                    foreach (var e1 in targets)
                    {
                        var s1 = new LineSegment3D(e1.V0.Position, e1.V1.Position);
                        // e0��e1�����L���Ă��钸�_������ꍇ�͖���
                        if (e0.IsShareAnyVertex(e1, out var shareV))
                        {
                            //var (sv, s) = s0.Magnitude < s1.Magnitude
                            //    ? (e0.GetOppositeVertex(shareV), s1)
                            //    : (e1.GetOppositeVertex(shareV), s0);
                            //if ((s.GetNearestPoint(sv.Position) - sv.Position).sqrMagnitude < shareMid)
                            //{
                            //    edgeInsertMap.GetValueOrCreate(e0).Add(sv);
                            //}

                            continue;
                        }

                        if (s0.TrySegmentIntersectionBy2D(s1, AxisPlane.Xz, heightTolerance, out var intersection,
                                out var t1, out var t2))
                        {
                            // ���݂��̒[�_�Ō������Ă���ꍇ�͖���
                            if ((NearlyEqual(t1, 0) || NearlyEqual(t1, 1)) && (NearlyEqual(t2, 0) || NearlyEqual(t2, 1)))
                                continue;
                            var p = vertexMap.GetValueOrCreate(intersection, k => new RVertex(k));
                            // #TODO : 0 or 1�Ō��������ꍇ���l��
                            edgeInsertMap.GetValueOrCreate(e0).Add(p);
                            edgeInsertMap.GetValueOrCreate(e1).Add(p);
                        }
                    }
                }

                foreach (var add in addEdges)
                    queue.Add(add);

                foreach (var remove in removeEdges)
                    queue.Remove(remove);
            }

            Debug.Log($"Add Vertex [{vertexMap.Count}]");
            foreach (var e in edgeInsertMap)
            {
                e.Key.InsertVertices(e.Value);
            }
        }

        /// <summary>
        /// �ӂ̊Ԃ�vertices��ǉ����ĕ�������. vertices��self.V0 ~ V1�̊Ԃɂ���O��
        /// </summary>
        /// <param name="self"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static List<REdge> InsertVertices(this REdge self, IEnumerable<RVertex> vertices)
        {
            var ret = new List<REdge>();
            var o = self.V0.Position;
            var edge = self;
            ret.Add(edge);
            // V0 -> V1�̏��ɕ��Ԃ悤�ɂ��ĕ�������.
            foreach (var v in vertices.OrderBy(v => (v.Position - o).sqrMagnitude))
            {
                edge = edge.SplitEdge(v);
                //DebugEx.DrawSphere(v.Position, 2f, color: Color.green, 30f);
                ret.Add(edge);
            }

            return ret;
        }

        public static void SeparateFaces(this RGraph self)
        {
            foreach (var p in self.Faces.ToList())
            {
                p.Separate();
            }
        }

        private static List<RVertex> ComputeOutlineVertices(IList<RFace> faces)
        {
            var vertices = faces
                .SelectMany(f => f.Edges.SelectMany(e => e.Vertices))
                .ToHashSet();
            var edges = faces
                .SelectMany(f => f.Edges)
                .ToHashSet();
            var res = GeoGraph2D.ComputeOutline(
                vertices
                , v => v.Position
                , AxisPlane.Xz
                , v => v.Edges.Where(e => edges.Contains(e)).Select(e => e.GetOppositeVertex(v)));
            return res.Outline ?? new List<RVertex>();
        }

        /// <summary>
        /// �A�E�g���C�����_���v�Z����
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static List<RVertex> ComputeOutlineVertices(this RFace self)
        {
            return ComputeOutlineVertices(new RFace[] { self });
        }

        /// <summary>
        /// faceGroup�̒���roadTypes�^�̃|���S���̃A�E�g���C�����_���v�Z����
        /// </summary>
        /// <param name="faceGroup"></param>
        /// <param name="roadTypes"></param>
        /// <returns></returns>
        public static List<RVertex> ComputeOutlineVertices(this RFaceGroup faceGroup, RRoadTypeMask roadTypes)
        {
            var faces = faceGroup.Faces.Where(f => f.RoadTypes.HasAnyFlag(roadTypes)).ToList();
            return ComputeOutlineVertices(faces);
        }

        /// <summary>
        /// CityObjectGroup�ɑ�����ʂ̃A�E�g���C�����_���v�Z����
        /// </summary>
        /// <param name="self"></param>
        /// <param name="cityObjectGroup"></param>
        /// <param name="roadTypes"></param>
        /// <returns></returns>
        public static List<RVertex> ComputeOutlineVerticesByCityObjectGroup(this RGraph self, PLATEAUCityObjectGroup cityObjectGroup, RRoadTypeMask roadTypes)
        {
            var faces = self
                .Faces
                .Where(f => f.CityObjectGroup == cityObjectGroup && f.RoadTypes.HasAnyFlag(roadTypes))
                .ToList();
            return ComputeOutlineVertices(faces);
        }

        /// <summary>
        /// a��b�̊Ԃɋ��ʂ̕ӂ����邩�ǂ���
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsShareEdge(RFace a, RFace b)
        {
            return a.Edges.SelectMany(e => e.Vertices).Any(v => b.Edges.SelectMany(e => e.Vertices).Contains(v));
        }

        /// <summary>
        /// self�̔�A���ȕ����𕪗�����
        /// </summary>
        /// <param name="self"></param>
        public static void Separate(this RFace self)
        {
            var edges = self.Edges.ToHashSet();
            if (edges.Any() == false)
                return;
            List<HashSet<REdge>> separatedEdges = new();
            while (edges.Any())
            {
                var queue = new Queue<REdge>();
                queue.Enqueue(edges.First());
                edges.Remove(edges.First());
                var subFace = new HashSet<REdge>();
                while (queue.Any())
                {
                    var edge = queue.Dequeue();
                    subFace.Add(edge);
                    foreach (var e in edge.GetNeighborEdges())
                    {
                        if (edges.Contains(e))
                        {
                            edges.Remove(e);
                            queue.Enqueue(e);
                        }
                    }
                }
                separatedEdges.Add(subFace);
            }

            if (separatedEdges.Count <= 1)
                return;

            foreach (var e in self.Edges.Where(e => separatedEdges[0].Contains(e) == false).ToList())
                self.RemoveEdge(e);

            for (var i = 1; i < separatedEdges.Count; i++)
            {
                var face = new RFace(self.Graph, self.CityObjectGroup, self.RoadTypes, self.LodLevel);
                foreach (var e in separatedEdges[i])
                    face.AddEdge(e);
                self.Graph.AddFace(face);
            }
        }
    }

}
