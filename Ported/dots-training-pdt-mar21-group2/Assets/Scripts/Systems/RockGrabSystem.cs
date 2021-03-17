﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// System that will attribute a rock to an hand
/// </summary>
public class RockGrabSystem : SystemBase
{
    private EntityQuery m_AvailableRocksQuery;

    protected override void OnCreate()
    {
        m_AvailableRocksQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Rock>(),
            ComponentType.ReadOnly<Available>());
    }
    
    protected override void OnUpdate()
    {
        var availableRocks = GetComponentDataFromEntity<Available>();

        EntityCommandBufferSystem sys = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

        var ecb = sys.CreateCommandBuffer();
        
        // Job can't be in parallel! Since it needs to solve race conditions between
        // arms trying to reach for the same rock
        Entities
            .WithAll<HandGrabbingRock>()
            .ForEach((Entity entity, ref TargetRock targetRock) =>
            {
                // leave grabbing state
                ecb.RemoveComponent<HandGrabbingRock>(entity);
                
                if (availableRocks.HasComponent(targetRock.RockEntity))
                {
                    // grab successful, time to throw
                    ecb.AddComponent<HandThrowingRock>(entity);
                    
                    // exclusive ownership of the rock, it can't be grabbed anymore
                    // by other arms
                    ecb.RemoveComponent<Available>(targetRock.RockEntity);
                    
                    // just a hack for now : we just stop the rock
                    ecb.RemoveComponent<Velocity>(targetRock.RockEntity);
                }
                else
                {
                    // grab failed, we must try again
                    ecb.AddComponent<HandIdle>(entity);
                }

            }).Schedule();

        sys.AddJobHandleForProducer(Dependency);
    }
}