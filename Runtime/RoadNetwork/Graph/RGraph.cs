using PLATEAU.CityInfo;
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
    /// ���_����
    /// </summary>
    [Flags]
    public enum RVertexType
    {
        /// <summary>
        /// ���������тƗא�
        /// </summary>
        Median = 1 << 0,
        /// <summary>
        /// �����Ɨא�
        /// </summary>
        SideWalk = 1 << 2,
    }

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
    public class RVertex
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        [SerializeField]
        private List<REdge> edges = new List<REdge>();

        /// <summary>
        /// �ʒu
        /// </summary>
        [field: SerializeField]
        public Vector3 Position { get; set; }

        /// <summary>
        /// ���_����
        /// </summary>
        [field: SerializeField]
        public RVertexType Types { get; set; }

        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyList<REdge> Edges => edges;

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
        /// ���_����type���L�����ǂ�����ݒ肷��
        /// </summary>
        /// <param name="type"></param>
        /// <param name="enable"></param>
        public void SetAttributeEnable(RVertexType type, bool enable)
        {
            if (enable)
            {
                Types |= type;
            }
            else
            {
                Types &= ~type;
            }
        }

        /// <summary>
        /// ���_����type���L�����ǂ������擾����
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetAttributeEnable(RVertexType type)
        {
            return (Types & type) != 0;
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
    public class REdge
    {
        public enum VertexType
        {
            V0,
            V1,
        }

        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        [SerializeField]
        private List<RPolygon> polygons = new List<RPolygon>();

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
        /// �ڑ����_(2��)
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
        }
    }

    /// <summary>
    /// ���p�`
    /// </summary>
    [Serializable]
    public class RPolygon
    {
        //----------------------------------
        // start: �t�B�[���h
        //----------------------------------
        /// <summary>
        /// �\����\��
        /// </summary>
        [field: SerializeField]
        public bool Visible { get; set; }

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
        [field: SerializeField]
        public RGraph Graph { get; private set; }

        /// <summary>
        /// �\����
        /// </summary>
        [SerializeField]
        private List<REdge> edges = new List<REdge>();

        //----------------------------------
        // end: �t�B�[���h
        //----------------------------------

        /// <summary>
        /// �\����
        /// </summary>
        public IReadOnlyList<REdge> Edges => edges;

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
        /// �Ӎ폜
        /// </summary>
        /// <param name="edge"></param>
        public void RemoveEdge(REdge edge)
        {
            edges.Remove(edge);
            edge.RemovePolygon(this);
        }

    }

    public class RGraph
    {
        /// <summary>
        /// ���_
        /// </summary>
        public List<RVertex> Vertices { get; set; }

        /// <summary>
        /// ��
        /// </summary>
        public List<REdge> Edges { get; set; }

        /// <summary>
        /// ��
        /// </summary>
        public List<RPolygon> Faces { get; set; }
    }
}
