using PLATEAU.CityInfo;
using System;
using System.Collections.Generic;
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
        // ���������тƗא�
        Median = 1 << 0,
        // �����Ɨא�
        SideWalk = 1 << 2,
    }

    /// <summary>
    /// �ړ_
    /// </summary>
    public class RVertex
    {
        private List<REdge> edges = new List<REdge>();

        /// <summary>
        /// �ʒu
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyList<REdge> Edges => edges;

        /// <summary>
        /// ���_����
        /// </summary>
        public RVertexType Types { get; set; }

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
                if (edge.Start == this)
                {
                    Assert.IsTrue(edge.End != this);
                    yield return edge.End;
                }
                else
                {
                    Assert.IsTrue(edge.Start != this);
                    yield return edge.Start;
                }
            }
        }
    }


    /// <summary>
    /// ��
    /// </summary>
    public class REdge
    {
        public enum VertexType
        {
            Start,
            End,
        }

        private List<RPolygon> polygons = new List<RPolygon>();

        private RVertex[] vertices = new RVertex[2];

        /// <summary>
        /// �J�n�_
        /// </summary>
        public RVertex Start => GetVertex(VertexType.Start);

        /// <summary>
        /// �I���_
        /// </summary>
        public RVertex End => GetVertex(VertexType.End);

        /// <summary>
        /// �ڑ���
        /// </summary>
        public IReadOnlyList<RPolygon> Polygons => polygons;

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
    }

    /// <summary>
    /// ���p�`
    /// </summary>
    public class RPolygon
    {
        readonly List<RVertex> vertices = new List<RVertex>();

        readonly List<REdge> edges = new List<REdge>();

        private readonly PLATEAUCityObjectGroup cityObjectGroup = null;

        /// <summary>
        /// �\����. ���v���ɐڑ���
        /// </summary>
        public IReadOnlyList<REdge> Edges => edges;

        /// <summary>
        /// �\�����_
        /// </summary>
        public IReadOnlyList<RVertex> Vertices => vertices;

        /// <summary>
        /// �Ή�����CityObjectGroup
        /// </summary>
        public PLATEAUCityObjectGroup CityObjectGroup => cityObjectGroup;

        /// <summary>
        /// �e�O���t
        /// </summary>
        public RGraph Graph { get; set; }
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
