﻿using System;
using UnityEngine;

namespace PLATEAU.RoadNetwork.Data
{
    [Serializable, RoadNetworkSerializeData(typeof(RoadNetworkBlock))]
    public class RoadNetworkDataBlock : IPrimitiveData
    {
        // 自分自身を表すId
        [field: SerializeField]
        [RoadNetworkSerializeMember(nameof(RoadNetworkBlock.MyId))]
        public RnID<RoadNetworkDataBlock> MyId { get; set; }

        [field: SerializeField]
        [RoadNetworkSerializeMember(nameof(RoadNetworkBlock.ParentLink))]
        public RnID<RoadNetworkDataLink> ParentLink { get; set; }

        [field: SerializeField]
        [RoadNetworkSerializeMember(nameof(RoadNetworkBlock.LaneType))]
        public int LaneType { get; set; }
    }
}