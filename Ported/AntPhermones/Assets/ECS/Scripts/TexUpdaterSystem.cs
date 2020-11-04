﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class TexUpdaterSystem : SystemBase
{
    const float dt = 1.0f / 60;
    const float randomSteering = 0.14f;
    const float decay = 0.9985f;
    const float trailAddSpeed = 0.3f;
    const float excitementWhenLookingForFood = 0.3f;
    const float excitementWhenGoingBackToNest = 1.0f;

    Entity textureSingleton; //Use a singleton entity for the buffer entity

    Color[] colors = new Color[RefsAuthoring.TexSize * RefsAuthoring.TexSize];

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<TexSingleton>();
    }

    static void DropPheromones(float x, float y, Vector2 bounds, NativeArray<PheromonesBufferData> localPheromones, float speed, float dt, int TexSize, bool test)
    {
        Vector2 texelCoord = new Vector2(0.5f * (-x / bounds.x) + 0.5f, 0.5f * (-y / bounds.y) + 0.5f);
        int index = (int)(texelCoord.y * TexSize) * TexSize + (int)(texelCoord.x * TexSize);

        float excitement = .3f;
        if (test)
        {
            excitement = 1f;
        }
        excitement *= speed / 0.2f;
        var strength = excitement;

        var p = (float)localPheromones[index];
        p += (trailAddSpeed * strength * dt) * (1f - p);
        if (p > 1f)
        {
            p = 1f;
        }

        localPheromones[index] = p;
    }
    
    protected override void OnUpdate()
    {
        int TexSize = RefsAuthoring.TexSize;

        //Get the pheremones data 
        EntityQuery doesTextInitExist = GetEntityQuery(typeof(TexInitialiser));
        if(!doesTextInitExist.IsEmpty)
        {
            return;
        }

        Vector2 bounds = AntMovementSystem.bounds;
        var localPheromones = EntityManager.GetBuffer<PheromonesBufferData>(GetSingletonEntity<TexSingleton>());
        
        Entities
            .WithNativeDisableParallelForRestriction(localPheromones)
            .WithoutBurst()
            .ForEach((int entityInQueryIndex, ref Translation translation, ref Direction direction, ref RandState rand, in Speed speed, in AntLookingForFood dummy) =>
            {
                DropPheromones(translation.Value.x, translation.Value.z, bounds, localPheromones.AsNativeArray(), speed.Value, dt, TexSize, false);
               
            })
            .Run();

        Entities
            .WithNativeDisableParallelForRestriction(localPheromones)
            .WithoutBurst()
            .ForEach((int entityInQueryIndex, ref Translation translation, ref Direction direction, ref RandState rand, in Speed speed, in AntLookingForNest dummy) =>
            {
                DropPheromones(translation.Value.x, translation.Value.z, bounds, localPheromones.AsNativeArray(), speed.Value, dt, TexSize, true);
            })
            .Run();

        Entities
            .ForEach((Refs map) =>
            {
                for (int i = 0; i < TexSize; ++i)
                {
                    for (int j = 0; j < TexSize; ++j)
                    {
                        localPheromones[j * TexSize + i] *= decay;
                        colors[j * TexSize + i].r = localPheromones[j * TexSize + i]; ;
                        colors[j * TexSize + i].g = 0;
                        colors[j * TexSize + i].b = 0;
                    }
                }
                map.PheromoneMap.SetPixels(0, 0, TexSize, TexSize, colors);
                map.PheromoneMap.Apply();
            })
            .WithoutBurst()
            .Run();
        
    }

    
    
}