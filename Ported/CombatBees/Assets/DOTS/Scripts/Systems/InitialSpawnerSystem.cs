using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using UnityCamera = UnityEngine.Camera;
using UnityGameObject = UnityEngine.GameObject;
using UnityInput = UnityEngine.Input;
using UnityKeyCode = UnityEngine.KeyCode;
using UnityMeshRenderer = UnityEngine.MeshRenderer;
using UnityMonoBehaviour = UnityEngine.MonoBehaviour;
using UnityRangeAttribute = UnityEngine.RangeAttribute;

public class InitialSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var random = new Random(1234);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        var spawnerEntity = GetSingletonEntity<InitialSpawner>();
        var spawner = GetComponent<InitialSpawner>(spawnerEntity);

        var arena = GetSingletonEntity<IsArena>();
        var resourcesBounds = GetComponent<Bounds>(arena).Value;
        resourcesBounds.Extents.z *= 0.3f;
        resourcesBounds.Extents.y *= 0.3f;

        //Spawn bees
        Entities
            .ForEach((Entity entity, in Bounds bounds, in Team team) =>
            {

                for (int i = 0; i < spawner.BeeCount/2; ++i)
                {
                    var instance = ecb.Instantiate(spawner.BeePrefab);
                    ecb.AddComponent(instance, team);

                    ecb.AddComponent<IsBee>(instance);
                    ecb.AddComponent(instance, new Speed { Value = random.NextFloat(0, spawner.MaxSpeed) });

                    var translation = new Translation { Value = bounds.Value.Center };
                    ecb.SetComponent(instance, translation);

                    ecb.SetComponent(instance, new URPMaterialPropertyBaseColor
                    {
                        Value = GetComponent<URPMaterialPropertyBaseColor>(entity).Value
                    });
                }
            }).Run();

        //Spawn resources
        for (int i = 0; i < spawner.ResourceCount; ++i)
        {
            var instance = ecb.Instantiate(spawner.ResourcePrefab);
            ecb.AddComponent<IsResource>(instance);
            ecb.AddComponent<HasGravity>(instance);

            var translation = new Translation { Value = Utils.BoundedRandomPosition(resourcesBounds,ref random) };
            ecb.SetComponent(instance, translation);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        Enabled = false;
    }
}
