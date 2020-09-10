﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ArrowPlacingSystem : SystemBase
{
    EntityQuery tilesQuery;
    EntityQuery arrowsQuery;
    EntityQuery arrowPrefabQuery;
    EntityCommandBufferSystem ECBSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        tilesQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<PositionXZ>(),
                ComponentType.ReadOnly<Tile>(),
            }
        });

        arrowsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Arrow>(),
                ComponentType.ReadOnly<Translation>(),
            }
        });

        arrowPrefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Arrow>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Prefab>()
            }
        });

        ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var tilePositions = tilesQuery.ToComponentDataArrayAsync<PositionXZ>(Allocator.TempJob, out var tilePositionsHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, tilePositionsHandle);
        var tiles = tilesQuery.ToComponentDataArrayAsync<Tile>(Allocator.TempJob, out var tilesHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, tilesHandle);
        var tileEntities = tilesQuery.ToEntityArrayAsync(Allocator.TempJob, out var tileEntitiesHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, tileEntitiesHandle);
        var tileAccessor = GetComponentDataFromEntity<Tile>();

        var arrowPositions = arrowsQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out var arrowPositionHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, arrowPositionHandle);
        var arrows = arrowsQuery.ToComponentDataArrayAsync<Arrow>(Allocator.TempJob, out var arrowsHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, arrowsHandle);
        var arrowEntities = arrowsQuery.ToEntityArrayAsync(Allocator.TempJob, out var arrowEntitiesHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, arrowEntitiesHandle);
        //var arrowAccessor = GetComponentDataFromEntity<Arrow>();

        var ecb = ECBSystem.CreateCommandBuffer().AsParallelWriter();

        var arrowPrefab = arrowPrefabQuery.GetSingletonEntity();

        Entities
            .WithDisposeOnCompletion(tilePositions)
            .WithDisposeOnCompletion(tiles)
            .WithDisposeOnCompletion(tileEntities)
            .WithDisposeOnCompletion(arrowPositions)
            .WithDisposeOnCompletion(arrows)
            .WithDisposeOnCompletion(arrowEntities)
            .WithChangeFilter<PlaceArrowEvent>()
            .WithNativeDisableParallelForRestriction(tileAccessor)
            .ForEach((
                int entityInQueryIndex,
                in Entity placeArrowEventEntity,
                in PositionXZ position,
                in PlaceArrowEvent placeArrowEvent,
                in Direction direction) =>
            {
                var newArrowPosition = (int2)position.Value;
                for (int i = 0; i < tileEntities.Length; i++)
                {
                    var tilePosition = (int2)tilePositions[i].Value;
                    if (math.any(tilePosition != newArrowPosition))
                        continue;

                    var tileValue = tiles[i].Value;

                    if ((tileValue & Tile.Attributes.ArrowAny) != 0) // Target tile is an Arrow
                    {
                        for (int j = 0; j < arrowEntities.Length; j++)
                        {
                            var arrowPosition = (int2)arrowPositions[j].Value.xz;
                            if (math.any(tilePosition != arrowPosition))
                                continue;

                            if (arrows[j].Owner == placeArrowEvent.Player) // Placer owns the arrow, remove
                            {
                                ecb.DestroyEntity(entityInQueryIndex, arrowEntities[j]);
                                tileAccessor[tileEntities[i]] = new Tile
                                {
                                    Value = (Tile.Attributes)((int)tiles[i].Value & ~(int)Tile.Attributes.ArrowAny)
                                };
                            }
                        }
                    }
                    else if ((tileValue & Tile.Attributes.ObstacleAny) != 0) // Tile is either a Hole or a Goal
                    {

                    }
                    else // Tile is free to place New Arrow
                    {
                        tileAccessor[tileEntities[i]] = new Tile
                        {
                            Value = (Tile.Attributes)(((int)tiles[i].Value & ~(int)Tile.Attributes.ArrowAny) | (int)direction.Value << (int)Tile.Attributes.ArrowShiftCount)
                        };
                        var newArrow = ecb.Instantiate(entityInQueryIndex, arrowPrefab);

                        var playerColor = GetComponent<Color>(placeArrowEvent.Player);
                        ecb.SetComponent(entityInQueryIndex, newArrow, new Arrow { Owner = placeArrowEvent.Player });
                        ecb.SetComponent(entityInQueryIndex, newArrow, new Translation { Value = new float3(position.Value.x, 0, position.Value.y) });
                        ecb.SetComponent(entityInQueryIndex, newArrow, new Rotation { Value = quaternion.Euler(0, AnimalMovementSystem.RadiansFromDirection(direction.Value), 0) });
                        ecb.AddComponent(entityInQueryIndex, newArrow, playerColor);
                    }
                }

                ecb.DestroyEntity(entityInQueryIndex, placeArrowEventEntity);
            }).ScheduleParallel();
        ECBSystem.AddJobHandleForProducer(Dependency);
    }
}