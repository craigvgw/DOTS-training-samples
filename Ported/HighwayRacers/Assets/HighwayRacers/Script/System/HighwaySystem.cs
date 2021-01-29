﻿using System;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Mathematics;
using UnityEngine.UI;
using  System.Collections.Generic;
using UnityEngine.Rendering;

[AlwaysUpdateSystem]
public class HighwaySystem : SystemBase
{
    private TrackSettings CurrentTrackSettings;
    private float CurrentTrackSize;
    private float CurrentTrackCorners;
    private float StraightPieceLength = -1;
    private EntityQuery ToBeDeleted;
    private RoundRect TrackRect;
    
    
    // // we use prefabs
    // public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    // {
    //     referencedPrefabs.Add(PiecePrefab);
    // }
    
    
    protected override void OnCreate()
    {
        TrackRect = RoundRect.Instance;
        
        CurrentTrackSettings = UIValues.GetTrackSettings();
        
        ToBeDeleted = GetEntityQuery(typeof(HighwayPiece));

    }
    
    
    
    protected override void OnUpdate()
    {
        if (UIValues.GetModified() == CurrentTrackSettings.Iteration && CurrentTrackSettings.Iteration > 0)
        {
            return;
        }

        CurrentTrackSettings = UIValues.GetTrackSettings();
        if (CurrentTrackSize == CurrentTrackSettings.TrackSize &&
            CurrentTrackCorners == CurrentTrackSettings.CornerRadius)
        {
            return;
        }

        CurrentTrackSize = CurrentTrackSettings.TrackSize;
        CurrentTrackCorners = CurrentTrackSettings.CornerRadius;
        
        RoundRect _rect = TrackRect;
        _rect.Size = CurrentTrackSize;
        _rect.Radius = CurrentTrackCorners;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // lazy man's sentinel value
        // in unset (= first time) grab the setup data from the
        // trackInfo
        float _straightPieceLength = StraightPieceLength;
        if (_straightPieceLength < 0)
        {
            var tInfo = GetSingletonEntity<TrackInfo>();
            var tInfoComp = GetComponent<TrackInfo>(tInfo);
            
            // these could really be constants but it's good
            // to make them data driven
            StraightPieceLength = _straightPieceLength = tInfoComp.SegmentLength;
        }

        // Delete the pre-existing track
        EntityManager.DestroyEntity(ToBeDeleted);

        int SegmentCount = Mathf.CeilToInt(_rect.Perimeter / _straightPieceLength) * 4;
        RoundRect.RectData rectData = RoundRect.Instance.GetRectData();
        
        Entities
            .WithoutBurst()
            .ForEach((Entity entity, in HighwayPrefabs hw) =>
            {
                
                for (int i = 0; i < SegmentCount; i++)
                {
                    float d = i / (SegmentCount * 1f);
                    var seg = ecb.Instantiate(hw.StraightPiecePrefab);

                    float3 pos = new float3();
                    float yaw = 0;
                    RoundRect.Interpolate(d, rectData, out pos, out yaw );
                    var trans = new Translation
                    {
                        Value =  pos
                    };
                    ecb.SetComponent(seg, trans);

                    var rot = new Rotation
                    {
                        Value = quaternion.AxisAngle(Vector3.up, yaw)
                    };
                    ecb.SetComponent(seg, rot);
                    ecb.AddComponent(seg, new HighwayPiece());
                }
            }).Run();
                
        ecb.Playback(EntityManager);
       

    }
    
    
    
}
