using src.DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace src.DOTS.Systems
{
    public class PlatformQueueManagementSystem : SystemBase
    {
        private EntityCommandBufferSystem CommandBufferSystem;

        private float embarkingTimerField = -10.0f;
        //EntityQuery query;

        protected override void OnCreate()
        {
            CommandBufferSystem
                = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {

            embarkingTimerField -= Time.DeltaTime;

            if (embarkingTimerField < -10.0f)
            {
                embarkingTimerField = 5.0f;
            }

            float embarkingTimer = embarkingTimerField;
            
            var queryDesc = new EntityQueryDesc
            {
                None = new ComponentType[] { typeof(WalkingTag)},
                All = new ComponentType[] {ComponentType.ReadOnly<LookingForQueueTag>(), ComponentType.ReadOnly<SwitchingPlatformData>()}
                
            };
            
            EntityQuery query = GetEntityQuery(queryDesc);


            NativeArray<Entity> commuters = query.ToEntityArray(Allocator.TempJob);
            NativeArray<SwitchingPlatformData> commuterPlatformData = query.ToComponentDataArray<SwitchingPlatformData>(Allocator.TempJob);

            var blob = this.GetSingleton<MetroBlobContainer>();

            var ecb = CommandBufferSystem.CreateCommandBuffer();

            // the queuing tag is added as soon as we switch platforms and start moving, make sure we are done moving (WalkingTag) before processing the queue 
            Entities.WithReadOnly(commuters).
            WithReadOnly(commuterPlatformData).ForEach((
                in Entity queue,
                in Translation queueTranslation, 
                in PlatformQueue queueInfo
                ) =>
            {
                int platformIndex = queueInfo.platformIndex;


                for (int i = 0; i < commuters.Length; ++i)
                {
                    if (commuterPlatformData[i].platformTo == platformIndex)
                    {
                        
                        ecb.RemoveComponent<LookingForQueueTag>(commuters[i]);
                        
                        // add to list
                        ecb.AppendToBuffer(queue, new CommuterQueueData
                        {
                            entity = commuters[i]
                        });
                        
                    }
                }
            }).WithDisposeOnCompletion(commuters).WithDisposeOnCompletion(commuterPlatformData).Run();

            
            
            var allWalkingTags = GetComponentDataFromEntity<WalkingTag>(true);
            var allWaitingTrains = GetComponentDataFromEntity<TrainWaiting>(true);
            var ecb2 = CommandBufferSystem.CreateCommandBuffer();

            Entities.WithReadOnly(allWalkingTags).ForEach((
                ref DynamicBuffer<CommuterQueueData> commuterQueue,
                ref PlatformQueue queueInfo,
                ref FirstQueuePassenger firstPassenger, 
                in Entity queue,
                in Translation queuePos
                
            ) =>
            {
                float3 dir = new float3(0.0f, 0.0f, 1.0f);
                float spacing = 0.5f;
                
               for (int i = 0; i < commuterQueue.Length; ++i)
               {
                   if (!allWalkingTags.HasComponent(commuterQueue[i].entity))
                   {
                       ecb2.AddComponent<WalkingTag>(commuterQueue[i].entity);
                       ecb2.AppendToBuffer(commuterQueue[i].entity, new PathData
                       {
                           point = queuePos.Value + dir * i * spacing
                       });
                       
                   }
               }
                 
                if (firstPassenger.passenger == Entity.Null)
                {
                    if(commuterQueue.Length > 0)
                    {
                        if (!allWalkingTags.HasComponent(commuterQueue[0].entity))
                        {
                            firstPassenger.passenger = commuterQueue[0].entity;
                            commuterQueue.RemoveAt(0);
                        }
                    }
                }

            }).Schedule();

            CommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}