using System.Linq;
using DOTSRATS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class SetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        const float k_yRangeSize = 0.03f;
        
        var random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);

        Entities
            .WithStructuralChanges()
            .WithAll<BoardSpawner>()
            .ForEach((Entity entity, in BoardSpawner boardSpawner) =>
            {
                var size = boardSpawner.boardSize;
                int playerNumber = 0;

                // Spawn the GameState
                var gameState = EntityManager.CreateEntity();
                EntityManager.AddComponent<GameState>(gameState);
                EntityManager.SetComponentData(gameState, new GameState{boardSize = size});
                var cellStructs = EntityManager.AddBuffer<CellStruct>(gameState);
                
                
                for (int z = 0; z < size; ++z)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        // Spawn tiles
                        var tile = EntityManager.Instantiate(boardSpawner.tilePrefab);
                        var yValue = random.NextFloat(-k_yRangeSize, k_yRangeSize);
                        var translation = new Translation() { Value = new float3(x, yValue - 0.5f, z) };
                        EntityManager.SetComponentData(tile, translation);
                        var cell = new CellStruct();

                        // Spawn outer walls
                        if (x == 0 || x == size - 1)
                            SpawnWall(boardSpawner, new int2(x, z), x == 0 ? Direction.West : Direction.East, ref cell);
                        if (z == 0 || z == size - 1)
                            SpawnWall(boardSpawner, new int2(x, z), z == 0 ? Direction.South : Direction.North, ref cell);

                        //spawn goals
                        if (ShouldPlaceGoalTile(new int2(x, z), size))
                            SpawnGoal(boardSpawner, new int2(x, z), playerNumber++, ref cell);

                        cellStructs.Append(cell);
                    }
                }

                EntityManager.DestroyEntity(entity);
            }).Run();
    }

    public Entity SpawnWall(BoardSpawner boardSpawner, int2 coord, Direction direction, ref CellStruct cellStruct)
    {
        Entity wall = default;
        
        float3 position = new float3(coord.x, 0.25f, coord.y);
        quaternion rotation = quaternion.identity;

        switch (direction)
        {
            case Direction.North:
                position += new float3(0f, 0f, 0.5f);
                rotation = quaternion.AxisAngle(math.up(), math.radians(90f));
                break;
            
            case Direction.South:
                position += new float3(0f, 0f, -0.5f);
                rotation = quaternion.AxisAngle(math.up(), math.radians(90f));
                break;
            
            case Direction.East:
                position += new float3(0.5f, 0f, 0f);
                break;
            
            case Direction.West:
                position += new float3(-0.5f, 0f, 0f);
                break;
        }
        
        wall = EntityManager.Instantiate(boardSpawner.wallPrefab);
        EntityManager.SetComponentData(wall, new Translation() { Value = position });
        EntityManager.SetComponentData(wall, new Rotation() { Value = rotation });

        cellStruct.wallLayout |= direction;

        return wall;
    }
    
    public Entity SpawnGoal(BoardSpawner boardSpawner, int2 coord, int playerNumber,  ref CellStruct cellStruct)
    {
        Entity goal = default;
        float3 position = new float3(coord.x, -0.5f, coord.y);

        goal = EntityManager.Instantiate(boardSpawner.goalPrefab);
        EntityManager.SetComponentData(goal, new Translation() { Value = position });
        EntityManager.SetComponentData(goal, new Goal() { playerNumber = playerNumber });

        cellStruct.goal = goal;

        return goal;
    }

    // TODO: also return player id as an out variable
    public bool ShouldPlaceGoalTile(int2 coord, int boardSize)
    {
        var halfSize = boardSize / 2;
        var midCoord = halfSize + (boardSize % 2 == 0 ? 1 : 2);
        var offsetFromCenter = halfSize / 6;
        
        if ((coord.x == (halfSize - offsetFromCenter - 1) || coord.x == (midCoord + offsetFromCenter - 1)) &&
            (coord.y == (halfSize - offsetFromCenter - 1) || coord.y == (midCoord + offsetFromCenter - 1)))
        {
            return true;
        }
        
        return false;
    }

}
