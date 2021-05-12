using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class TrainAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float totalDistance;
    public float targetDistance;
    public float maxSpeed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TrainWaitTimer());
        dstManager.AddComponentData(entity, new TrainCurrDistance());
        dstManager.AddComponentData(entity, new TrainTargetDistance() { value = targetDistance });
        dstManager.AddComponentData(entity, new TrainTotalDistance() { value = totalDistance });
        dstManager.AddComponentData(entity, new TrainCurrSpeed());
        dstManager.AddComponentData(entity, new TrainMaxSpeed() { value = maxSpeed });
    }
}