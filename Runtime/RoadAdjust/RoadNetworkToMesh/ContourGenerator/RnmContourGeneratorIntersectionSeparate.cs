using PLATEAU.RoadNetwork.Structure;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PLATEAU.RoadAdjust.RoadNetworkToMesh
{
    /// <summary>
    /// 交差点の輪郭線を生成します。レーンは分割します。
    /// </summary>
    internal class RnmContourGeneratorIntersectionSeparate: IRnmContourGenerator
    {
        
        public RnmContourMeshList Generate(IRrTarget target)
        {
            var cMeshes = new RnmContourMeshList();
            foreach (var inter in target.Intersections())
            {
                var targetObjs = new[] { new RoadReproduceSource(inter) };
                foreach (var c in GenerateContours(inter))
                {
                    cMeshes.Add(new RnmContourMesh(targetObjs, c));
                }
            }
            return cMeshes;
        }

        public IEnumerable<RnmContour> GenerateContours(RnIntersection inter)
        {
            // 車道
            yield return CarContour(inter);
            
            // 歩道
            foreach (var sideWalk in inter.SideWalks)
            {
                foreach (var contour in SidewalkContour(sideWalk))
                {
                    yield return contour;
                }
            }

            // 外側
            // 今までは交差点のOutsideSidewalkがないケースがあったために必要としていた処理ですが、
            // 今は歩道がある場合はOutsideSidewalkが存在するため、不要となりました。
            // foreach (var outer in NonSidewalkOuters(inter))
            // {
            //     yield return outer;
            // }
        }

        /// <summary> 車道 </summary>
        private RnmContour CarContour(RnIntersection inter)
        {
            var edges = inter.Edges.Select(e => e.Border);
            var calc = new RnmContourCalculator(RnmMaterialType.IntersectionCarLane);
            foreach (var edge in edges)
            {
                calc.AddLine(edge, Vector2.zero, Vector2.zero); // FIXME UV1は未実装
            }
            return calc.Calculate();
        }

        /// <summary> 歩道 </summary>
        private IEnumerable<RnmContour> SidewalkContour(RnSideWalk sideWalk)
        {
            // 歩道部
            var calc = new RnmContourCalculator(RnmMaterialType.SideWalk);
            var inside = sideWalk.InsideWay;
            var outside = sideWalk.OutsideWay;
            if (inside == null || outside == null) yield break;
            
            var curbBoundary = new RnWay(sideWalk.InsideWay);
            new RnmModelAdjuster().MoveToward(curbBoundary, sideWalk.OutsideWay, RnmContourGeneratorSidewalk.CurbWidth, 0, 0);
            
            // 歩道の内側の線を追加
            calc.AddLine(inside, Vector2.zero, Vector2.zero);
            
            // 歩道の外側の線を追加
            calc.AddLine(outside, Vector2.zero, Vector2.zero);
            
            // Edgeを追加。
            var edges = sideWalk.EdgeWays;
            foreach (var line in edges.Where(l => l != null))
            {
                calc.AddLine(line, Vector2.zero, Vector2.zero); // FIXME UV1は未実装
            }

            var contour = calc.Calculate();
            var modifier = new RnmTessModifierPileUp(contour.Vertices.Select(v => v.Position).ToArray(), RnmContourGeneratorSidewalk.PileUpHeightSideWalk);
            contour.AddModifier(modifier);
            yield return contour;
            
            // 縁石部（歩道の端）
            // 縁石部を生成
            var calc2 = new RnmContourCalculator(RnmMaterialType.MedianLane);
            calc2.AddLine(curbBoundary, Vector2.zero, Vector2.zero); // FIXME UV1は未実装
            calc2.AddLine(sideWalk.InsideWay , Vector2.zero, Vector2.zero);
            var contour2 = calc2.Calculate();
            // 段差
            var modifier2 =
                new RnmTessModifierPileUp(contour2.Vertices.Select(v => v.Position).ToArray(), RnmContourGeneratorSidewalk.PileUpHeightCurb);
            contour2.AddModifier(modifier2);

            yield return contour2;
        }

        /// <summary>
        /// 2つの線について、端点が近いものが1つでもあるかどうかを返します。
        /// </summary>
        private bool IsLinesHasNearEdge(RnWay w1, RnWay w2)
        {
            if (w1.Count == 0) return false;
            if (w1.Count == 1) return IsPointOnLineEdge(w1[0], w2);
            return IsPointOnLineEdge(w1[0], w2) || IsPointOnLineEdge(w1[^1], w2);
        }
        
        /// <summary>
        /// 点が線の端点付近にあるときにtrueを返します。
        /// </summary>
        private bool IsPointOnLineEdge(Vector3 p, RnWay l)
        {
            const float EdgeThreshold = 1f;
            if (l.Count == 0) return false;
            if (l.Count == 1) return Vector3.Distance(p, l[0]) < EdgeThreshold;
            return Vector3.Distance(p, l[0]) < EdgeThreshold || Vector3.Distance(p, l[^1]) < EdgeThreshold;   
        }

        /// <summary> ネットワーク上では歩道判定でないが車道の外側の部分 </summary>
        private IEnumerable<RnmContour> NonSidewalkOuters(RnIntersection inter)
        {
            // 交差点のカーブの白線に相当する部分のうち、歩道と重ならない部分
            var outsideBorderEdges = new OutsideBorderEdges(inter).Collect().ToArray();
            
            // 交差点に隣接する道路の歩道の端
            var neighborSidewalkEdges = new NeighborSidewalkEdges(inter).Collect().ToArray();
            
            // 交差点の歩道の端
            var sidewalkEdges = inter.SideWalks.SelectMany(w => w.EdgeWays);
            
            var sumSideEdges = neighborSidewalkEdges.Concat(sidewalkEdges).ToArray();
            
            foreach (var border in outsideBorderEdges)
            {
                // outsideBorderEdgesに近いsideEdgesを対象にします。
                var matchingSideEdges = new HashSet<RnWay>();
                foreach (var sideEdge in sumSideEdges)
                {
                    // 追加したいsideEdgeの条件1: outsideBorderに接している
                    if (ContourGeneratorCommon.NearestDist(border, sideEdge) < 1f)
                    {
                        matchingSideEdges.Add(sideEdge);
                        
                    }
                }

                if (matchingSideEdges.Count > 0)
                {
                    var calc = new RnmContourCalculator(RnmMaterialType.SideWalk);
                    var lines = new List<IEnumerable<Vector3>> { border };
                    lines.AddRange(matchingSideEdges);
                    foreach (var line in lines)
                    {
                        calc.AddLine(line, Vector2.zero, Vector2.zero); // FIXME UV1は未実装
                    }
                    var contour = calc.Calculate();
                    var modifier = new RnmTessModifierPileUp(contour.Vertices.Select(v => v.Position).ToArray(), RnmContourGeneratorSidewalk.PileUpHeightSideWalk);
                    contour.AddModifier(modifier);
                    yield return contour;
                }
            }
        }
        
        
        /// <summary>
        /// 交差点について、nonBorderEdge（交差点のカーブ部分の白線に相当）のうち歩道と重ならないものを返します。 </summary>
        private class OutsideBorderEdges : IRnWayCollector
        {
            private readonly RnIntersection inter;

            public OutsideBorderEdges(RnIntersection inter)
            {
                this.inter = inter;
            }

            public IEnumerable<RnWay> Collect()
            {
                foreach (var nonBorder in inter.Edges.Where(e => !e.IsBorder).Select(e => e.Border))
                {
                    // bool isBorderOutside = true;
                    // 条件: 歩道のinsideWayと重ならない
                    var sideWalks = inter.SideWalks;
                    var rnInsideWays = sideWalks.Select(sw => sw.InsideWay).Where(w => w != null);
                    var insideWays = rnInsideWays.SelectMany(w => w.Vertices);
                    var rnmLine = new RnmLine(nonBorder, Vector2.zero, Vector2.zero); // FIXME UV1は未実装
                    var nonBorder2 = rnmLine.SubtractSeparate(insideWays, 1);
                    foreach (var b in nonBorder2)
                        yield return new RnWay(new RnLineString(b.Vertices.Select(v => new RnPoint(v.Position))));

                }
            }
        }
        
        /// <summary> 交差点に隣接する道路の歩道の端を取得します。 </summary>
        private class NeighborSidewalkEdges : IRnWayCollector
        {
            private const float NearThreshold = 1f;
            private readonly RnIntersection inter;

            public NeighborSidewalkEdges(RnIntersection inter)
            {
                this.inter = inter;
            }

            public IEnumerable<RnWay> Collect()
            {
                // 隣接する道路について
                foreach (var neighbor in inter.Borders.Where(n => n.Road != null))
                {
                    // 隣の道路のもっとも近く、十分な近さである StartEdgeWay, EndEdgeWay を選択
                    var carBoarders = new CarBorder(inter).Collect().ToArray();
                    var nSidewalks = neighbor.Road.SideWalks;
                    var nStarts = nSidewalks.Select(sw => sw.StartEdgeWay).Where(w => w != null).ToArray();
                    var nEnds = nSidewalks.Select(sw => sw.EndEdgeWay).Where(w => w != null).ToArray();
                    if (nStarts.Length != 0)
                    {
                        int nearestID = ContourGeneratorCommon.FindCorrespondWayIDMinDist(nStarts, neighbor.Border);
                        var nearest = nStarts[nearestID];
                        float minDist = carBoarders.Min(cb =>
                            ContourGeneratorCommon.NearestDist(nearest, cb));
                        if (minDist < NearThreshold)
                        {
                            yield return nearest;
                        }
                    }

                    if (nEnds.Length != 0)
                    {
                        int nearestID = ContourGeneratorCommon.FindCorrespondWayIDMinDist(nEnds, neighbor.Border);
                        var nearest = nEnds[nearestID];
                        float minDist = carBoarders.Min(cb =>
                            ContourGeneratorCommon.NearestDist(nearest, cb));
                        if (minDist < NearThreshold)
                        {
                            yield return nEnds[nearestID];
                        }
                    }
                }
            }
        }
        
        /// <summary> 交差点の車道のボーダーを返します </summary>
        private class CarBorder : IRnWayCollector
        {
            private readonly RnIntersection inter;

            public CarBorder(RnIntersection inter)
            {
                this.inter = inter;
            }

            public IEnumerable<RnWay> Collect()
            {
                return inter.Edges
                    .Where(e => e.IsBorder)
                    .Select(e => e.Border);
            }
        }
    }
}