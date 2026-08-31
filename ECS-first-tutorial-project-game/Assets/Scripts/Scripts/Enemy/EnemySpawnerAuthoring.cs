using ECS_Tutorial_Game.CharacterAttack;
using ECS_Tutorial_Game.CharacterAuthoring;
using ECS_Tutorial_Game.CharacterHealth;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECS_Tutorial_Game.EnemySpawner
{
    public struct EnemySpawnerData : IComponentData
    {
        public Entity EnemyPrefab;
        public float EnemySpawnInterval;
        public float EnemySpawnDistance;
    }

    public struct EnemySpawnerState : IComponentData
    {
        public Random Random;
        public double ExpirationTime;

    }

    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject EnemyPrefab;
        public float EnemySpawnInterval;
        public float EnemySpawnDistance;

        public uint RandomSeed;

        private class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new EnemySpawnerData
                {
                    EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                    EnemySpawnInterval = authoring.EnemySpawnInterval,
                    EnemySpawnDistance = authoring.EnemySpawnDistance,
                });

                AddComponent(entity, new EnemySpawnerState
                {
                    Random = new Random(authoring.RandomSeed)
                });
            }
        }

    }

    public partial struct EnemySpawnerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            var ecbBuff = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbBuff.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (spawnerData, spawnerState) in SystemAPI.Query<RefRW<EnemySpawnerData>, RefRW<EnemySpawnerState>>())
            {
                if (elapsedTime < spawnerState.ValueRW.ExpirationTime)
                    return;

                spawnerState.ValueRW.ExpirationTime = elapsedTime + spawnerData.ValueRW.EnemySpawnInterval;

                var enemy = ecb.Instantiate(spawnerData.ValueRO.EnemyPrefab);

                float3 playerPosition = float3.zero;

                foreach (var playerPos in SystemAPI.Query<LocalToWorld>().WithAll<PlayerTag>())
                {
                    playerPosition = playerPos.Position;
                }

                float angle = spawnerState.ValueRW.Random.NextFloat(0, math.PI2);

                var enemyPosition = playerPosition + new float3(math.cos(angle), math.sin(angle), 0f) * spawnerData.ValueRO.EnemySpawnDistance;

                ecb.SetComponent(enemy, LocalTransform.FromPositionRotationScale(enemyPosition, Quaternion.identity, 1.5f));
            }
        }
    }
}
