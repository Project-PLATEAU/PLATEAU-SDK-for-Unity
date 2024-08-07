﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace PLATEAU.Util.GeoGraph
{
    public static class GeoGraph2D
    {
        public const float Epsilon = 1e-5f;


        public class Vector2Equitable : IEqualityComparer<Vector2>
        {
            public float Tolerance { get; set; }

            public Vector2Equitable(float tolerance)
            {
                Tolerance = tolerance;
            }
            public bool Equals(Vector2 x, Vector2 y)
            {
                return (x - y).sqrMagnitude < Tolerance;
            }

            public int GetHashCode(Vector2 obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Vector3の頂点をtoVec2で2Dに射影したうえで凸包を構成する頂点を返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="toVec2"></param>
        /// <returns></returns>
        public static List<Vector3> ComputeConvexVolume(IEnumerable<Vector3> vertices, Func<Vector3, Vector2> toVec2)
        {
            // リストの最後の辺が時計回りになっているかを確認
            bool IsLastClockwise(List<Vector3> list)
            {
                if (list.Count <= 2)
                    return true;
                return Vector2Util.Cross(toVec2(list[^1] - list[^2]), toVec2(list[^2] - list[^3])) > 0;
            }
            var compare = new Vector2Equitable(Epsilon);
            var sortedVertices = vertices.OrderBy(v => v.x).ThenBy(v => v.y).ToList();
            for (var i = 0; i < sortedVertices.Count - 1;)
            {
                if (compare.Equals(toVec2(sortedVertices[i]), toVec2(sortedVertices[i + 1])))
                    sortedVertices.RemoveAt(i + 1);
                else
                    i++;
            }
            if (sortedVertices.Count <= 2)
                return new List<Vector3>();

            // 上方の凸形状計算
            var ret = new List<Vector3> { sortedVertices[0], sortedVertices[1] };

            for (var i = 2; i < sortedVertices.Count; i++)
            {
                ret.Add(sortedVertices[i]);
                while (IsLastClockwise(ret) == false)
                    ret.RemoveAt(ret.Count - 2);
            }

            // 下方の凸形状計算
            ret.Add(sortedVertices[^2]);
            for (var i = sortedVertices.Count - 3; i >= 0; --i)
            {
                ret.Add(sortedVertices[i]);
                while (IsLastClockwise(ret) == false)
                    ret.RemoveAt(ret.Count - 2);
            }

            return ret;
        }

        /// <summary>
        /// 凸多角形を計算して返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static List<Vector2> ComputeConvexVolume(IEnumerable<Vector2> vertices)
        {
            // リストの最後の辺が時計回りになっているかを確認
            bool IsLastClockwise(List<Vector2> list)
            {
                if (list.Count <= 2)
                    return true;
                return Vector2Util.Cross(list[^1] - list[^2], list[^2] - list[^3]) > 0;
            }

            var sortedVertices = vertices.OrderBy(v => v.x).ThenBy(v => v.y).Distinct().ToList();
            var compare = new Vector2Equitable(Epsilon);
            for (var i = 0; i < sortedVertices.Count - 1;)
            {
                if (compare.Equals(sortedVertices[i], sortedVertices[i + 1]))
                    sortedVertices.RemoveAt(i + 1);
                else
                    i++;
            }
            if (sortedVertices.Count <= 2)
                return new List<Vector2>();

            // 上方の凸形状計算
            var ret = new List<Vector2> { sortedVertices[0], sortedVertices[1] };
            for (var i = 2; i < sortedVertices.Count; i++)
            {
                ret.Add(sortedVertices[i]);
                while (IsLastClockwise(ret) == false)
                    ret.RemoveAt(ret.Count - 2);
            }

            // 下方の凸形状計算
            ret.Add(sortedVertices[^2]);
            for (var i = sortedVertices.Count - 3; i >= 0; --i)
            {
                ret.Add(sortedVertices[i]);
                while (IsLastClockwise(ret) == false)
                    ret.RemoveAt(ret.Count - 2);
            }

            return ret;
        }

        public static Dictionary<Vector3, Vector3> MergeVertices(IEnumerable<Vector3> vertices, Func<Vector3, Vector2> toVec2, float epsilon = 0.1f)
        {
            var compare = new Vector2Equitable(epsilon);
            var ret = new Dictionary<Vector3, Vector3>();

            vertices.OrderBy(v => v.x).ThenBy(v => v.y);
            foreach (var v in vertices)
            {
                var found = ret.Keys.FirstOrDefault(k => compare.Equals(k.Xy(), v.Xy()));
                if (found != null)
                    ret[found] = v;
                else
                    ret[v] = v;
            }
            return ret;
        }

        /// <summary>
        /// meshのアウトラインを計算する
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="toVec2"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static List<Vector3> ComputeMeshOutlineVertices(Mesh mesh, Func<Vector3, Vector2> toVec2, float epsilon = 0.1f)
        {
            Vector3 ToCell(Vector3 v)
            {
                var x = (int)(v.x / epsilon);
                var y = (int)(v.y / epsilon);
                var z = (int)(v.z / epsilon);
                return new Vector3(x * epsilon, y * epsilon, z * epsilon);
            }

            // key   : 頂点
            // value : その頂点とつながっている頂点のリスト
            var vertices = new Dictionary<Vector3, HashSet<Vector3>>();
            for (var i = 0; i < mesh.triangles.Length; i += 3)
            {
                var p0 = ToCell(mesh.vertices[mesh.triangles[i]]);
                var p1 = ToCell(mesh.vertices[mesh.triangles[i + 1]]);
                var p2 = ToCell(mesh.vertices[mesh.triangles[i + 2]]);

                if (vertices.ContainsKey(p0) == false)
                    vertices[p0] = new HashSet<Vector3>();
                if (vertices.ContainsKey(p1) == false)
                    vertices[p1] = new HashSet<Vector3>();
                if (vertices.ContainsKey(p2) == false)
                    vertices[p2] = new HashSet<Vector3>();

                vertices[p0].Add(p1);
                vertices[p0].Add(p2);
                vertices[p1].Add(p2);
                vertices[p1].Add(p0);
                vertices[p2].Add(p0);
                vertices[p2].Add(p1);
            }

            if (vertices.Count <= 2)
                return new List<Vector3>();

            var comp = Comparer<float>.Default;

            var keys = vertices.Keys.ToList();
            keys.Sort((a, b) =>
            {
                var a2 = toVec2(a);
                var b2 = toVec2(b);
                var x = comp.Compare(a2.x, b2.x);
                var y = comp.Compare(a2.y, b2.y);
                if (x != 0)
                    return x;
                return y;
            });

            //
            var dir = Vector2.down;
            void Eval(Vector2 axis, Vector2 a, out float ang, out float sqrLen)
            {
                ang = Vector2.SignedAngle(axis, a);
                if (ang < 0f)
                    ang += 360f;
                sqrLen = a.sqrMagnitude;
            }

            // 比較
            var ret = new List<Vector3> { keys[0] };
            while (ret.Count < vertices.Count)
            {
                var last = toVec2(ret[^1]);
                var neighbors = vertices[ret[^1]];
                if (neighbors.Count == 0)
                    break;
                Vector3 next = neighbors.First();
                Eval(dir, toVec2(next) - last, out var ang, out var sqrLen);
                foreach (var v in neighbors.Skip(1))
                {
                    // 最も外側に近い点を返す
                    Eval(dir, toVec2(v) - last, out var ang2, out var sqrLen2);
                    var x = -comp.Compare(ang2, ang);
                    if (x == 0)
                        x = comp.Compare(sqrLen2, sqrLen);
                    if (x < 0)
                    {
                        next = v;
                        ang = ang2;
                        sqrLen = sqrLen2;
                    }
                }

                if (ret.Contains(next))
                    break;
                ret.Add(next);
                dir = last - toVec2(next);
            }

            return ret;
        }

        /// <summary>
        /// ポリゴンを構成する頂点配列を渡すと, そのポリゴンが時計回りなのか反時計回りなのかを返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static bool IsClockwise(IEnumerable<Vector2> vertices)
        {
            var total = GeoGraphEx.GetEdges(vertices, true).Sum(item => Vector2Util.Cross(item.Item1, item.Item2));
            return total < 0;
        }

        /// <summary>
        /// verticesで表される多角形が点pを内包するかどうか
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool Contains(IEnumerable<Vector2> vertices, Vector2 p)
        {
            // https://www.nttpc.co.jp/technology/number_algorithm.html
            bool Check(Vector2 c, Vector2 v)
            {
                // 上向きの辺。点Pがy軸方向について、始点と終点の間にある。ただし、終点は含まない。(ルール1)
                // 下向きの辺。点Pがy軸方向について、始点と終点の間にある。ただし、始点は含まない。(ルール2)
                if (((c.y <= p.y) && (v.y > p.y)) || ((c.y > p.y) && (v.y <= p.y)))
                {
                    // ルール1,ルール2を確認することで、ルール3も確認できている。
                    // 辺は点pよりも右側にある。ただし、重ならない。(ルール4)
                    // 辺が点pと同じ高さになる位置を特定し、その時のxの値と点pのxの値を比較する。
                    var vt = (p.y - c.y) / (v.y - c.y);
                    if (p.x < (c.x + (vt * (v.x - c.x))))
                    {
                        return true;
                    }
                }

                return false;
            }

            var cnt = GeoGraphEx.GetEdges(vertices, true).Count(item => Check(item.Item1, item.Item2));
            return (cnt % 2) == 1;
        }

        /// <summary>
        /// 頂点verticesで構成されるポリゴン(isLoop = falseの時は開いている)と半直線rayとの交点を返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="ray"></param>
        /// <param name="intersection"></param>
        /// <param name="t"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        public static bool PolygonHalfLineIntersection(IEnumerable<Vector2> vertices, Ray2D ray, out Vector2 intersection, out float t, bool isLoop = true)
        {
            var ret = GeoGraphEx.GetEdges(vertices, isLoop)
                .Select(p =>
                {
                    var success = LineUtil.HalfLineSegmentIntersection(ray, p.Item1, p.Item2, out Vector2 intersection,
                        out float f1,
                        out float f2);
                    return new { success, intersection, f1, f2 };
                })
                .Where(p => p.success)
                .TryFindMin(p => p.f1, out var o);

            intersection = o?.intersection ?? Vector2.zero;
            t = o?.f1 ?? 0f;
            return ret;
        }

        /// <summary>
        /// 頂点verticesで構成されるポリゴン(isLoop = falseの時は開いている)と半直線rayとの交点を返す.
        /// ただし、y座標は無視してXz平面だけで当たり判定を行う
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="ray"></param>
        /// <param name="intersection"></param>
        /// <param name="t"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        public static bool PolygonHalfLineIntersectionXZ(IEnumerable<Vector3> vertices, Ray ray,
            out Vector3 intersection, out float t, bool isLoop = true)
        {
            var ret = PolygonHalfLineIntersection(vertices.Select(v => v.Xz()),
                new Ray2D(ray.origin.Xz(), ray.direction.Xz()), out Vector2 _, out float f1, isLoop);
            if (ret == false)
            {
                intersection = Vector3.zero;
                t = 0f;
            }
            else
            {
                intersection = ray.origin + ray.direction * f1;
                t = f1;
            }
            return ret;
        }


        /// <summary>
        /// 頂点verticesで構成されるポリゴン(isLoop = falseの時は開いている)と半直線rayとの交点を返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="ray"></param>
        /// <param name="intersection"></param>
        /// <param name="t"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        public static bool PolygonSegmentIntersection(IEnumerable<Vector2> vertices, Vector2 st, Vector2 en, out Vector2 intersection, out float t, bool isLoop = true)
        {
            var ret = GeoGraphEx.GetEdges(vertices, isLoop)
                .Select(p =>
                {
                    var success = LineUtil.SegmentIntersection(st, en, p.Item1, p.Item2, out Vector2 intersection,
                        out float f1,
                        out float f2);
                    return new { success, intersection, f1, f2 };
                })
                .Where(p => p.success)
                .TryFindMin(p => p.f1, out var o);

            intersection = o.intersection;
            t = o.f1;
            return ret;
        }

        /// <summary>
        /// 頂点verticesで構成されるポリゴン(isLoop = falseの時は開いている)と半直線rayとの交点を返す.
        /// ただし、y座標は無視してXz平面だけで当たり判定を行う
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="en"></param>
        /// <param name="intersection"></param>
        /// <param name="t"></param>
        /// <param name="isLoop"></param>
        /// <param name="st"></param>
        /// <returns></returns>
        public static bool PolygonSegmentIntersectionXZ(IEnumerable<Vector3> vertices, Vector3 st, Vector3 en,
            out Vector3 intersection, out float t, bool isLoop = true)
        {
            var ret = PolygonHalfLineIntersection(vertices.Select(v => v.Xz()),
                new Ray2D(st.Xz(), en.Xz()), out Vector2 _, out float f1, isLoop);
            if (ret == false)
            {
                intersection = Vector3.zero;
                t = 0f;
            }
            else
            {
                intersection = Vector3.Lerp(st, en, f1);
                t = f1;
            }
            return ret;
        }

        public static void RemoveSelfCrossing<T>(List<T> self, Func<T, Vector2> selector, Func<T, T, T, T, Vector2, float, float, T> creater)
        {
            for (var i = 0; i < self.Count - 2; ++i)
            {
                var p1 = selector(self[i]);
                var p2 = selector(self[i + 1]);
                for (var j = i + 2; j < self.Count - 1;)
                {
                    var p3 = selector(self[j]);
                    var p4 = selector(self[j + 1]);

                    if (LineUtil.SegmentIntersection(p1, p2, p3, p4, out var intersection, out var f1, out var f2))
                    {
                        var newNode = creater(self[i], self[i + 1], self[j], self[j + 1], intersection, f1, 2);
                        self.RemoveRange(i + 1, j - i);
                        self.Insert(i + 1, newNode);
                        // もう一回最初から検索しなおす
                        j = i + 2;
                    }
                    else
                    {
                        ++j;
                    }
                }
            }
        }

        //public static Ray2D LerpRay2(LineSegment2D segA, LineSegment2D segB, float p)
        //{
        //    // segAの直線に射影する
        //    Vector2 Convert(Vector2 v)
        //    {
        //        var d = v - segA.Start;
        //        var x = Vector2.Dot(d, segA.Direction);
        //        var y = Mathf.Sqrt(d.sqrMagnitude - x * x);
        //        return new Vector2(x, y) / segA.Magnitude;
        //    }
        //    var bS = Convert(segB.Start);
        //    var bE = Convert(segB.End);

        //    var points = new List<Vector2> { Vector2.zero, bS, bE, Vector2.right };
        //    points.Sort((a, b) => Comparer<float>.Default.Compare(a.x, b.x));
        //    var p1 = points[1];
        //    var p2 = points[2];

        //    p1
        //}

        /// <summary>
        /// 直線l上の点から直線a,bへの距離がp : 1-pとなるような直線lを返す
        /// 0.5だと中間の角度が返る
        /// \ p  |1-p /
        ///  \   |   /
        ///   \  |  / 
        ///  a \ | / b
        ///     \ /
        /// </summary>
        /// <param name="rayA"></param>
        /// <param name="rayB"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Ray2D LerpRay(Ray2D rayA, Ray2D rayB, float p)
        {
            // 2線が平行の時は交点が無いので特別処理
            if (LineUtil.LineIntersection(rayA, rayB, out var intersection, out var t1, out var t2) == false)
            {
                var aPos = Vector2.Dot(rayB.origin - rayA.origin, rayA.direction) * rayA.direction + rayA.origin;
                var origin = Vector2.Lerp(aPos, rayB.origin, p);
                return new Ray2D(origin, rayA.direction);
            }

            var dirA = rayA.direction;
            var dirB = rayB.direction;

            var radX = Mathf.Deg2Rad * Vector2.Angle(dirA, dirB);
            var siX = Mathf.Sin(radX);
            var coX = Mathf.Cos(radX);
            // a-l間の角度A
            // l-b間の角度(x - A)
            // sin(A) : sin(B) = p : (1-p)
            // B = X - A
            // sin(A) : sin(X - A) = p : (1-p)
            // Sin(A) : sin(X)cos(A) - cos(X)sin(A) = p : (1-p)
            // (1-p)Sin(A) = p ( sin(X)cos(A) - cos(X)sin(A))
            // ((1-p) + p * cos(X))sin(A) = p*sin(X)cos(A)
            // tan(A) = p*sin(X) / ((1-p) + p * cos(X))
            var radA = Mathf.Atan2(p * siX, 1 - p + p * coX);
            return new Ray2D(intersection, Vector2Util.RotateTo(dirA, dirB, radA));
        }

        public class BorderParabola2D
        {
            public float A { get; set; }

            public float B { get; set; }

            public float C0 { get; set; }

            public float P { get; set; }

            public float Y0 { get; set; }

            // p=0.5以外の時は条件を満たすxには限界がある
            public float? RangeX { get; set; }
            public Vector2 Origin { get; set; }

            public Vector2 AxisX { get; set; }

            public Vector2 AxisY { get; set; }

            public BorderParabola2D(Vector2 origin, Vector2 axisX, Vector2 axisY, float y0, float p)
            {
                // 以下の方程式を解く
                // x^2 + (y0 - y)^2 : y^2 = (1-p)^2 : p^2
                Origin = origin;
                AxisX = axisX;
                AxisY = axisY;
                P = p;
                Y0 = y0;
                var p2 = p * p;
                A = 2 * p - 1;
                B = 2 * Y0 * p2;
                C0 = p2 * Y0 * Y0;
                // 以下の二次方程式になる
                // ay^2 + 2by + c0 + p^2x^2 = 0

                //var c0 = 2 * p2 * y0;
                //var c1 = 4 * p4 * y02 - 4 * d * p2 * y02;
                //var c2 = -4 * d * p2;
                // y = (c0 ± √(c1 + c2*x^2)) / 2d

                var c = C0 + P * P * 0 * 0;
                var d = B * B - 4 * A * C0;
                if (d > 0f && A > 0f)
                {
                    RangeX = Mathf.Sqrt(d / p2);
                }

            }

            float GetY(float x)
            {
                if (Mathf.Abs(A) < GeoGraph2D.Epsilon)
                    return x * x / (2 * Y0) + Y0 / 2f;
                var c = C0 + P * P * x * x;
                var d = B * B - 4 * A * c;
                return (B - Mathf.Sqrt(d)) / (2 * A);
            }

            public Vector2 GetPoint(float x)
            {
                var local = new Vector2(x, GetY(x));
                return local.x * AxisX + local.y * AxisY + Origin;
            }

        }


        /// <summary>
        /// vと直線rayをp : 1-pで分割する放物線を返す
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="v"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static BorderParabola2D GetBorderParabola(Ray2D ray, Vector2 v, float p)
        {
            p = Mathf.Clamp01(p);
            var origin = ray.GetNearestPoint(v, out var t);
            //var origin = Vector2.Lerp(near, v, p);
            //
            var dir = (v - origin);
            var rot = Vector2.SignedAngle(Vector2.up, dir);
            var y0 = (v - origin).magnitude;// * (Vector2Util.Cross(ray.direction, v - origin) < 0f ? -1f : 1f);
            return new BorderParabola2D(origin, ray.direction, dir.normalized, y0, p);
        }

        [Serializable]
        public class DebugOption
        {
            public int showLeftIndex = -1;
            public int showRightIndex = -1;
            public bool showParabolaStart = true;
            public bool showParabolaEnd = true;
            public bool showRemoveSegment = true;
            public bool showReturnSegments = true;
        }

        public class NearestPointInfo
        {
            public int Index { get; set; }

            public float T { get; set; }

            public float Distance { get; set; }

            public static int Compare(NearestPointInfo a, NearestPointInfo b)
            {
                var ret = Comparer<int>.Default.Compare(a.Index, b.Index);
                if (ret != 0)
                    return ret;
                return Comparer<float>.Default.Compare(a.T, b.T);
            }

            public override string ToString()
            {
                return $"{Index}/{T:F2}";
            }
        }

        public class InnerSegment
        {
            public LineSegment2D Segment { get; set; }

            public int LeftIndex { get; }

            public int RightIndex { get; }

            public NearestPointInfo LeftNearestStartInfo { get; set; }

            public NearestPointInfo LeftNearestEndInfo { get; set; }

            public NearestPointInfo RightNearestStartInfo { get; set; }

            public NearestPointInfo RightNearestEndInfo { get; set; }

            public bool IsStartLeft { get; set; }

            public bool IsEndLeft { get; set; }

            // Left:Right = P : (1-P)となるP
            public float P { get; set; }

            // 輪郭線と交差しているかどうか
            public bool IsCrossed { get; set; }

            public float GetDistEval(bool isStart)
            {
                var (left, right) = isStart ? (LeftNearestStartInfo, RightNearestStartInfo) : (LeftNearestEndInfo, RightNearestEndInfo);

                var leftDist = left.Distance / P;
                var rightDist = right.Distance / P;
                return Mathf.Min(leftDist, rightDist);
            }

            public NearestPointInfo GetNearestInfo(bool isStart)
            {
                return isStart ? LeftNearestStartInfo : LeftNearestEndInfo;
            }


            public InnerSegment(LineSegment2D segment, int leftIndex, int rightIndex, bool isStartLeft, bool isEndLeft, float p)
            {
                Segment = segment;
                LeftIndex = leftIndex;
                RightIndex = rightIndex;
                P = p;
                IsStartLeft = isStartLeft;
                IsEndLeft = isEndLeft;
            }

            public override string ToString()
            {
                //return $"{LeftIndex}-{RightIndex}(L={LeftNearestStartInfo}-{LeftNearestEndInfo},R={RightNearestStartInfo}-{RightNearestEndInfo}";
                return $"{LeftIndex}-{RightIndex}";
            }

            public static int Compare(InnerSegment a, InnerSegment b)
            {
                var ret = NearestPointInfo.Compare(a.LeftNearestStartInfo, b.LeftNearestStartInfo);
                if (ret != 0)
                    return ret;
                return NearestPointInfo.Compare(a.RightNearestStartInfo, b.RightNearestStartInfo);
            }
        }

        /// <summary>
        /// selfの左右の道を横幅p : (1-p)で分割した線分を返す. p=[0,1]
        /// 例) 0.5だと中央線が返る, 0だとLeftが返る, 1だとRightが返る. 
        /// </summary>
        /// <param name="seg1"></param>
        /// <param name="p"></param>
        /// <param name="op"></param>
        /// <param name="seg0"></param>
        /// <returns></returns>
        public static List<InnerSegment> GetInnerLerpSegments(IReadOnlyList<Vector2> seg0, IReadOnlyList<Vector2> seg1, float p, DebugOption op = null)
        {
            p = Mathf.Clamp01(p);
            var lefts = GeoGraphEx.GetEdges(seg0, false).Select(v => new LineSegment2D(v.Item1, v.Item2)).ToList();
            var rights = GeoGraphEx.GetEdges(seg1, false).Select(v => new LineSegment2D(v.Item1, v.Item2)).ToList();

            var floatComparer = Comparer<float>.Default;

            var innerSegments = new List<InnerSegment>();


            var q = from m in Enumerable.Range(0, lefts.Count)
                    from n in Enumerable.Range(0, rights.Count)
                    select new { leftIndex = m, rightIndex = n };

            void ChangeSegment(InnerSegment ev, LineSegment2D segment)
            {
                var isHit = false;
                NearestPointInfo FindNearestPoint(List<LineSegment2D> edges, Vector2 pos)
                {
                    Enumerable.Range(0, edges.Count)
                        .Select(i =>
                        {
                            var e = edges[i];
                            var near = e.GetNearestPoint(pos, out var t);
                            if (e.TrySegmentIntersection(segment, out var inter, out var t1, out var t2) && t2 > Epsilon && t2 < segment.Magnitude - Epsilon)
                            {
                                isHit = true;
                            }
                            return new { dist = (near - pos).magnitude, index = i, t = t, isHit = isHit };
                        }).TryFindMin(x => x.dist, out var x);
                    return new NearestPointInfo() { Index = x.index, T = x.t, Distance = x.dist, };
                }

                ev.Segment = segment;
                ev.LeftNearestStartInfo = FindNearestPoint(lefts, segment.Start);
                ev.LeftNearestEndInfo = FindNearestPoint(lefts, segment.End);
                ev.RightNearestStartInfo = FindNearestPoint(rights, segment.Start);
                ev.RightNearestEndInfo = FindNearestPoint(rights, segment.End);
                ev.IsCrossed = isHit;
            }

#if true
            {

                var leftIndex = 0;
                var rightIndex = 0;
                while (leftIndex < lefts.Count && rightIndex < rights.Count)
                {
                    var l = lefts[leftIndex];
                    var r = rights[rightIndex];
                    var centerRay = GeoGraph2D.LerpRay(l.Ray, r.Ray, p);
                    var dirL = new Vector2(l.Direction.y, -l.Direction.x);
                    var dirR = new Vector2(r.Direction.y, -r.Direction.x);
                    var points = new[]
                        {
                        new { ray = new Ray2D(l.Start, dirL), isLeft = true },
                        new { ray = new Ray2D(l.End, dirL), isLeft = true },
                        new { ray = new Ray2D(r.Start, dirR), isLeft = false },
                        new { ray = new Ray2D(r.End, dirR), isLeft = false }
                    }
                        .Select(x =>
                        {
                            var hit = LineUtil.LineIntersection(centerRay, x.ray, out var inter, out var t1, out var t2);
                            var other = x.isLeft ? r : l;
                            var nearPos = other.GetNearestPoint(x.ray.origin);
                            return new
                            {
                                isHit = hit,
                                inter = inter,
                                tCenterRay = t1,
                                tRay = t2,
                                isLeft = x.isLeft,
                                origin = x.ray.origin
                            };
                        }).Where(x => x.isHit)
                        .ToList();

                    points.Sort((a, b) => floatComparer.Compare(a.tCenterRay, b.tCenterRay));

                    // ベースラインの切り替えが走ったかどうか
                    if (points.Any() == false)
                        break;

                    // 重ならない時は無視
                    if (points.Count > 1 && points[0].isLeft != points[1].isLeft)
                    {
                        // left-rightが重ならない時は無視
                        //if (baseRef.HasValue && points.Count == 4 && points[0].Item3 != points[1].Item3)
                        void Add()
                        {
                            var begSeg = points[1];
                            var endSeg = points[2];
                            if (begSeg.tCenterRay < 0 && endSeg.tCenterRay < 0)
                                return;

                            var segment = new LineSegment2D(points[1].inter, points[2].inter);
                            var ev = new InnerSegment(segment, leftIndex, rightIndex, begSeg.isLeft, endSeg.isLeft, p);
                            ChangeSegment(ev, segment);
                            innerSegments.Add(ev);
                            if (op?.showReturnSegments ?? false)
                            {
                                DebugEx.DrawString(ev.ToString(), ev.Segment.Start);
                            }
                        }

                        Add();
                    }


                    // より遠いのが左の場合右の線分を進める
                    if (points.Last().isLeft)
                    {
                        rightIndex++;
                    }
                    else
                    {
                        leftIndex++;
                    }
                }
            }
            innerSegments.Sort(InnerSegment.Compare);
            return innerSegments;
#else
            foreach (var s in q)
            {
                var leftIndex = s.leftIndex;
                var rightIndex = s.rightIndex;
                var l = lefts[leftIndex];
                var r = rights[rightIndex];
                var centerRay = GeoGraph2D.LerpRay(l.Ray, r.Ray, p);
                var dirL = new Vector2(l.Direction.y, -l.Direction.x);
                var dirR = new Vector2(r.Direction.y, -r.Direction.x);
                var points = new[]
                    {
                        new { ray = new Ray2D(l.Start, dirL), isLeft = true },
                        new { ray = new Ray2D(l.End, dirL), isLeft = true },
                        new { ray = new Ray2D(r.Start, dirR), isLeft = false },
                        new { ray = new Ray2D(r.End, dirR), isLeft = false }
                    }
                    .Select(x =>
                    {
                        var hit = LineUtil.LineIntersection(centerRay, x.ray, out var inter, out var t1, out var t2);
                        var other = x.isLeft ? r : l;
                        var nearPos = other.GetNearestPoint(x.ray.origin);
                        return new
                        {
                            isHit = hit,
                            inter = inter,
                            tCenterRay = t1,
                            tRay = t2,
                            isLeft = x.isLeft,
                            origin = x.ray.origin
                        };
                    }).Where(x => x.isHit)
                    .ToList();

                points.Sort((a, b) => floatComparer.Compare(a.tCenterRay, b.tCenterRay));

                // ベースラインの切り替えが走ったかどうか
                if (points.Any() == false)
                    continue;
                // 重ならない時は無視
                if (points[0].isLeft == points[1].isLeft)
                    continue;
                if (points.Count != 4)
                    continue;

                // left-rightが重ならない時は無視
                //if (baseRef.HasValue && points.Count == 4 && points[0].Item3 != points[1].Item3)
                var begSeg = points[1];
                var endSeg = points[2];
                if (begSeg.tCenterRay < 0 && endSeg.tCenterRay < 0)
                    continue;

                var segment = new LineSegment2D(points[1].inter, points[2].inter);
                var ev = new InnerSegment(segment, leftIndex, rightIndex, begSeg.isLeft, endSeg.isLeft, p);
                ChangeSegment(ev, segment);
                innerSegments.Add(ev);
            }
            void CreateBorderParabola(InnerSegment self, bool isBeg)
            {
                var isLeft = isBeg ? self.IsStartLeft : self.IsEndLeft;
                var (l, r) = (lefts[self.LeftIndex], rights[self.RightIndex]);
                var (targetSeg, otherSeg) = isLeft ? (l, r) : (r, l);

                var pos = isBeg ? targetSeg.Start : targetSeg.End;
                var nearPos = otherSeg.GetNearestPoint(pos);
                var pp = isLeft ? (1 - p) : p;
                var begBorder = GeoGraph2D.GetBorderParabola(otherSeg.Ray, pos, pp);

                var orig = begBorder.GetPoint(0);
                if (!innerSegments.Any(x => (x.Segment.Start - orig).magnitude < Epsilon || (x.Segment.End - orig).magnitude < Epsilon))
                    return;

                {
                    DebugEx.DrawString($"{self.LeftIndex}-{self.RightIndex}[{self.IsStartLeft}/{isBeg}]", begBorder.GetPoint(0f));
                    //DebugEx.DrawArrow(begBorder.Origin.Xya(), begBorder.GetPoint(0f).Xya(), bodyColor: Color.black);
                    DebugEx.DrawBorderParabola2D(begBorder, -10f, 10f);
                }
            }

            void ForeachEvents(Func<InnerSegment, bool, InnerSegment, List<InnerSegment>> action)
            {
                innerSegments.Sort(InnerSegment.Compare);
                // Item1 : InnerSegment
                // Item2 : 開始地点か終了地点か
                var events = Enumerable.Range(0, innerSegments.Count * 2)
                    .Select(i => new { ev = innerSegments[i / 2], isStart = (i % 2) == 0 })
                    .ToList();

                void Sort()
                {
                    events.Sort((aIndex, bIndex) =>
                    {
                        var a = aIndex.ev.GetNearestInfo(aIndex.isStart);
                        var b = bIndex.ev.GetNearestInfo(bIndex.isStart);
                        return NearestPointInfo.Compare(a, b);
                    });
                }
                Sort();
                int debugLoopNum = 0;
                var now = new HashSet<InnerSegment>();
                while (events.Any() && debugLoopNum++ < 1000)
                {
                    var e = events.First();
                    var a = e.ev;
                    var isAdded = false;
                    try
                    {
                        foreach (var b in now)
                        {
                            if (b == a)
                                continue;
                            var adds = action(a, e.isStart, b) ?? new List<InnerSegment>();
                            adds = adds.Where(x =>
                                NearestPointInfo.Compare(x.LeftNearestStartInfo, x.LeftNearestEndInfo) < 0)
                                .ToList();
                            if (adds.Any())
                            {
                                foreach (var x in adds)
                                {
                                    innerSegments.Add(x);
                                    events.Add(new { ev = x, isStart = true });
                                    events.Add(new { ev = x, isStart = false });
                                }

                                isAdded = true;
                            }
                        }

                        if (e.isStart)
                        {
                            now.Add(a);
                        }
                        else
                        {
                            now.Remove(a);
                        }

                    }
                    finally
                    {
                        events.RemoveAt(0);
                        if (isAdded)
                            Sort();
                    }
                }
            }

            // 逆走 or 全く進まないような線を削除する
            innerSegments.RemoveAll(x =>
            {
                var ret = NearestPointInfo.Compare(x.LeftNearestStartInfo, x.LeftNearestEndInfo) >= 0;
                if (ret && (op?.showRemoveSegment ?? false))
                    DebugEx.DrawLineSegment2D(x.Segment, color: DebugEx.GetDebugColor(15, 16, 0.3f));
                return ret;
            });

            //交差する線分を分割する
            ForeachEvents((a, isStart, b) =>
            {
                if (isStart == false)
                    return null;
                var addSegments = new List<InnerSegment>();
                if (b.Segment.TrySegmentIntersection(a.Segment, out var inter, out var t1, out var t2) && t1 is > 0f and < 1f && t2 is > 0f and < 1f)
                {
                    void Split(InnerSegment s)
                    {
                        var s1 = s.Segment;
                        s1.End = inter;
                        var s2 = s.Segment;
                        s2.Start = inter;
                        var newS = new InnerSegment(s2, s.LeftIndex, s.RightIndex, s.IsStartLeft,
                            s.IsEndLeft, p);
                        ChangeSegment(s, s1);
                        ChangeSegment(newS, s2);
                        addSegments.Add(newS);
                    }
                    Split(a);
                    Split(b);
                }

                return addSegments;
            });

            // 逆走 or 全く進まないような線を削除する
            innerSegments.RemoveAll(x =>
            {
                var ret = x.IsCrossed;
                if (ret && (op?.showRemoveSegment ?? false))
                    DebugEx.DrawLineSegment2D(x.Segment, color: DebugEx.GetDebugColor(1, 16, 0.3f));
                return ret;
            });

            innerSegments.RemoveAll(x =>
                x.LeftNearestEndInfo.Distance < Epsilon ||
                x.LeftNearestStartInfo.Distance < Epsilon ||
                x.RightNearestStartInfo.Distance < Epsilon ||
                x.RightNearestEndInfo.Distance < Epsilon
            );

            ForeachEvents((a, isStart, b) =>
            {
                //return null;
                // 同一店の場合は無視する
                var addSegments = new List<InnerSegment>();
                if (isStart)
                {
                    if ((a.Segment.Start - b.Segment.Start).sqrMagnitude > Epsilon)
                    {
                        var aInfo = a.LeftNearestStartInfo;
                        var pos = lefts[aInfo.Index].GetPoint(aInfo.T);

                        var near = b.Segment.GetNearestPoint(pos, out var t);
                        if (t > 0f && t < b.Segment.Magnitude)
                        {
                            var s2 = b.Segment;
                            s2.End = near;
                            ChangeSegment(b, s2);

                            var s1 = b.Segment;
                            s1.Start = pos;
                            var newSeg = new InnerSegment(s1, b.LeftIndex, b.RightIndex, b.IsStartLeft, b.IsEndLeft, b.P);
                            ChangeSegment(newSeg, s1);
                            addSegments.Add(newSeg);
                        }

                    }
                }
                else
                {
                    if ((a.Segment.End - b.Segment.End).sqrMagnitude > Epsilon)
                    {
                        var aInfo = a.LeftNearestEndInfo;
                        var pos = lefts[aInfo.Index].GetPoint(aInfo.T);

                        var near = b.Segment.GetNearestPoint(pos, out var t);
                        if (t > 0f && t < b.Segment.Magnitude)
                        {
                            var s2 = b.Segment;
                            s2.End = near;
                            ChangeSegment(b, s2);
                            var s1 = b.Segment;
                            s1.End = pos;
                            var newSeg = new InnerSegment(s1, b.LeftIndex, b.RightIndex, b.IsStartLeft, b.IsEndLeft, b.P);
                            ChangeSegment(newSeg, s1);
                            //addSegments.Add(newSeg);
                        }

                    }
                }
                return addSegments;
            });

            var deleted = new HashSet<InnerSegment>();
            ForeachEvents((a, isStart, b) =>
            {
                if (isStart == false)
                    return null;

                var ret = Comparer<float>.Default.Compare(a.GetDistEval(true), b.GetDistEval(true));
                if (ret == 0)
                    ret = Comparer<float>.Default.Compare(a.GetDistEval(false), b.GetDistEval(false));
                if (ret < 0)
                {
                    deleted.Add(b);
                }
                else
                {
                    deleted.Add(a);
                }

                return null;
            });

            foreach (var a in deleted)
            {
                innerSegments.Remove(a);
                if (op?.showRemoveSegment ?? false)
                {
                    DebugEx.DrawLineSegment2D(a.Segment, color: DebugEx.GetDebugColor(5, 16, 0.3f));

                }
            }

            innerSegments.Sort(InnerSegment.Compare);
            var debugIndex = 0;
            if (op?.showReturnSegments ?? false)
            {
                foreach (var seg in innerSegments)
                {
                    DebugEx.DrawString($"[{debugIndex}] {seg}",
                        (seg.Segment.Start.Xya() + seg.Segment.End.Xya()) * 0.5f, color: Color.blue, fontSize: 15);
                    DebugEx.DrawLineSegment2D(seg.Segment, color: DebugEx.GetDebugColor(debugIndex, 16));
                    if (seg != innerSegments[0])
                    {
                        if (op.showParabolaStart)
                            CreateBorderParabola(seg, true);
                    }
                    if (op.showParabolaEnd)
                        CreateBorderParabola(seg, false);

                    debugIndex++;
                }
            }
            
            return innerSegments;
#endif
        }

        [Serializable]
        public class DebugFindOppositeOption
        {
            public bool showCenterLine = false;
            public Color showCenterLineColor = Color.blue;
            public int pattern = 0;
        }

        /// <summary>
        /// verticesを始点終点から見ていき,お互い中心線を使って比較しながら中心の辺を表すインデックス配列を返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="toleranceAngleDegForMidEdge">中心線と同一直線と判断する線分の角度[deg]</param>
        /// <param name="skipAngleDeg">開始線分との角度がこれ以下の間は絶対に中心線にならない</param>
        /// <param name="op"></param>
        /// <returns></returns>
        public static List<int> FindMidEdge(IReadOnlyList<Vector2> vertices, float toleranceAngleDegForMidEdge = 20f, float skipAngleDeg = 20f, DebugFindOppositeOption op = null)
        {
            var edges = GeoGraphEx.GetEdges(vertices, false).Select(v => new LineSegment2D(v.Item1, v.Item2)).ToList();

            // 開始線分が中心線扱いにならないようにskipAngleDegを使ってスキップする
            var (startIndex, endIndex) = (0, edges.Count - 1);
            if (skipAngleDeg > 0f)
            {
                while (startIndex < edges.Count - 1 && Mathf.Abs(Vector2.Angle(edges[startIndex].Direction, edges[startIndex + 1].Direction)) < skipAngleDeg)
                    startIndex++;
                while (endIndex > 0 && Mathf.Abs(Vector2.Angle(edges[endIndex].Direction, edges[endIndex - 1].Direction)) < skipAngleDeg)
                    endIndex--;
            }

            var leftIndex = startIndex;
            var rightIndex = endIndex;
            var fComp = Comparer<float>.Default;

            // left ~ rightの間にエッジが1つしかなくなるまで続ける
            //   -> その残った一つがエッジ
            while (leftIndex < rightIndex - 2)
            {
                var l = edges[leftIndex];
                // 0 ~ edge.Countまでつながっているような線なので逆順の線分の方向を逆にする
                var r = edges[rightIndex].Reversed();
                // 中心線を計算
                var centerRay = GeoGraph2D.LerpRay(l.Ray, r.Ray, 0.5f);
                var dirL = new Vector2(l.Direction.y, -l.Direction.x);
                var dirR = new Vector2(r.Direction.y, -r.Direction.x);
                var points = new[]
                    {
                        new { ray = new Ray2D(l.Start, dirL), isLeft = true },
                        new { ray = new Ray2D(l.End, dirL), isLeft = true },
                        new { ray = new Ray2D(r.Start, dirR), isLeft = false },
                        new { ray = new Ray2D(r.End, dirR), isLeft = false }
                    }
                    .Select(x =>
                    {
                        var hit = LineUtil.LineIntersection(centerRay, x.ray, out var inter, out var t1, out var t2);
                        var other = x.isLeft ? r : l;
                        return new
                        {
                            isHit = hit,
                            inter,
                            tCenterRay = t1,
                            tRay = t2,
                            x.isLeft,
                            x.ray.origin
                        };
                    }).Where(x => x.isHit)
                    .ToList();

                points.Sort((a, b) => fComp.Compare(a.tCenterRay, b.tCenterRay));

                // 重ならない時は無視
                if (points.Any() == false)
                {
                    leftIndex++;
                    continue;
                }
#if UNITY_EDITOR
                var isCross = points.Count == 4 && points[0].isLeft != points[1].isLeft;
                if (isCross)
                {
                    if (op?.showCenterLine ?? false)
                        DebugEx.DrawArrow(points[1].inter, points[2].inter, bodyColor: op.showCenterLineColor);
                }
#endif
                // より遠いのが左の場合右の線分を進める
                if (points.Last().isLeft)
                {
                    rightIndex--;
                }
                else
                {
                    leftIndex++;
                }
            }

            // ここに来る段階でleftIndex == rightIndex - 2のはず
            var edgeBaseIndex = (leftIndex + rightIndex) / 2;
            var ret = new List<int> { edgeBaseIndex };
            var stop = new[] { false, false };
            while (stop.Contains(false) && ret.Count < edges.Count - 1)
            {
                // 0 : left用
                // 1 : right用
                var infos = new[]
                {
                    new {now=ret.First(), d = -1 },
                    new {now=ret.Last() , d = +1 }
                };
                // 差が小さいほうから見る
                var es = Enumerable.Range(0, 2)
                    // すでに停止している or 最後まで進んだら無視
                    .Where(i => stop[i] == false && startIndex <= infos[i].now + infos[i].d && infos[i].now + infos[i].d <= endIndex)
                    .Select(j =>
                    {
                        var info = infos[j];
                        var e0 = edges[edgeBaseIndex];
                        var e1 = edges[info.now + info.d];
                        return new { i = j, index = info.now + info.d, ang = Vector2.Angle(e0.Direction, e1.Direction) };
                    })
                    .OrderBy(x => x.ang)
                    .ToList();
                if (es.Count == 0)
                    break;
                foreach (var e in es)
                {
                    if (e.ang > toleranceAngleDegForMidEdge)
                    {
                        stop[e.i] = true;
                        continue;
                    }

                    if (e.i == 0)
                    {
                        ret.Insert(0, e.index);
                    }
                    else
                    {
                        ret.Add(e.index);
                    }
                }
            }
            // edge -> 頂点の配列に戻すために最後のインデックスを足す
            ret.Add(ret.Last() + 1);
            return ret;
        }
#if false
        public static Dictionary<Vector2, List<Tuple<Vector2, Vector2>>> ComputeIntersections(IEnumerable<Tuple<Vector2, Vector2>> originalSegments)
        {
            // key   : index
            // value : 線分
            var segments = originalSegments
                .Distinct()
                .Select((v, i) => new { v, i })
                .ToDictionary(x => x.i, x => x.v);

            var comparer = new Vector2Comparer();
            // key   : 端点 or 交点
            // value : keyを上端に持つ線分のリスト
            var eventQueue = new SortedDictionary<Vector2, List<int>>(comparer);
            foreach (var x in segments)
            {
                var refer = eventQueue.GetValueOrCreate(x.Value.Item1);
                refer.Add(x.Key);

                eventQueue.GetValueOrCreate(x.Value.Item2);
            }

            var lastP = Vector2.zero;

            float GetInterY(Tuple<Vector2, Vector2> a)
            {
                var p = (a.Item2.x - lastP.x) / (a.Item2.x - a.Item1.x);
                return Vector2.Lerp(a.Item1, a.Item2, p).y;
            }

            var tauComparer = Comparer<int>.Create(new Comparison<int>((x, y) => GetInterY(segments[x]).CompareTo(GetInterY(segments[y]))));
            var states = new SortedList<int, int>(tauComparer);
            var lowers = new HashSet<int>();
            var combines = new HashSet<int>();
            while (eventQueue.Count > 0)
            {
                var q = eventQueue.First();


                var upper = q.Value;

                lastP = q.Key;

                //states.IndexOfKey()


                foreach (var c in combines)
                    states.Remove(c);

                foreach (var l in lowers)
                    states.Remove(l);

                foreach (var u in q.Value)
                    states.Remove(u);

                while (true)
                {

                }
            }

        }
#endif
    }
}