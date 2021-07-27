using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class AntSpawnerSystem : SystemBase
{
    const int mapSize = 128;

    protected override void OnUpdate()
    {
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            Random rand = new Random(1234);

            Entities
                .ForEach((Entity entity, in AntSpawner spawner) =>
                {
                    // Add logic for creating the ants
                    ecb.DestroyEntity(entity);
                    
                    for (int i = 0; i < spawner.AntCount; i++) 
                    {
                        var spawnedEntity = ecb.Instantiate(spawner.AntPrefab);
                        ecb.SetComponent(spawnedEntity, new Translation
                        {
                            Value = new float3(rand.NextFloat(-5f,5f)+mapSize*.5f,rand.NextFloat(-5f,5f) + mapSize * .5f, 0)
                        });
                        ecb.AddComponent(spawnedEntity, new Speed()
                        {
                            Value = 0f
                        });
                        ecb.AddComponent(spawnedEntity, new Acceleration()
                        {
                            Value = spawner.Acceleration
                        });
                        ecb.AddComponent(spawnedEntity, new FacingAngle()
                        {
                            Value = rand.NextFloat() * math.PI * 2f
                        });
                        ecb.AddComponent(spawnedEntity, new Brightness()
                        {
                            Value = rand.NextFloat(.75f, 1.25f)
                        });
                    }

                }).Run();


            ecb.Playback(EntityManager);
        }
    }
}