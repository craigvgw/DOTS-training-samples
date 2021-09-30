using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BeeSpawnerSystem : SystemBase
{
    private Random random;
    protected override void OnCreate()
    {
        base.OnCreate();
        random = new Random((uint)System.DateTime.Now.Ticks);
    }
    
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        WorldBounds bounds = GetSingleton<WorldBounds>();
        Prefabs prefabs = GetSingleton<Prefabs>();
        uint seed = random.NextUInt();
        
        Entities
            .ForEach((Entity entity, int entityInQueryIndex, in BeeSpawner spawner) =>
            {
                Random random = new Random((uint)entityInQueryIndex + seed);

                // Destroying the current entity is a classic ECS pattern,
                // when something should only be processed once then forgotten.
                ecb.DestroyEntity(entity);
                float3 blueSpawnMax = bounds.AABB.Min;
                blueSpawnMax.x += bounds.HiveOffset;
                blueSpawnMax.yz = bounds.AABB.Max.yz;

                for (int i = 0; i < spawner.BlueBeeCount; ++i)
                {
                    float3 position = random.NextFloat3(bounds.AABB.Min, blueSpawnMax);

                    var instance = ecb.Instantiate(prefabs.BlueBeePrefab);
                    var translation = new Translation { Value = position };
                    ecb.SetComponent(instance, translation);
                }

                float3 redSpawnMin = bounds.AABB.Max;
                redSpawnMin.x -= bounds.HiveOffset;
                redSpawnMin.yz = bounds.AABB.Min.yz;

                for (int i = 0; i < spawner.RedBeeCount; ++i)
                {
                    float3 position = random.NextFloat3(redSpawnMin, bounds.AABB.Max);

                    var instance = ecb.Instantiate(prefabs.RedBeePrefab);
                    var translation = new Translation { Value = position };
                    ecb.SetComponent(instance, translation);
                }
            }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}