using ECS_Tutorial_Game.CharacterAttack;
using ECS_Tutorial_Game.CharacterAuthoring;
using ECS_Tutorial_Game.CharacterHealth;
using ECS_Tutorial_Game.DestroyCharacter;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace ECS_Tutorial_Game
{
    public struct EnemyTag : IComponentData { };

    [RequireComponent(typeof(CharacterAuthoring.CharacterAuthoring))]
    public class EnemyAuthoring : MonoBehaviour
    {
        [SerializeField] private int MaxHp;
        [SerializeField] private int Damage;
        [SerializeField] private float CooldownTime;

        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<EnemyTag>(entity);

                // Helath
                AddComponent(entity, new HealthPointComponent
                {
                    MaxHp = authoring.MaxHp,
                    CurrentHp = authoring.MaxHp
                });

                // Attack
                AddComponent<IsCharacterRechargedAttack>(entity);
                SetComponentEnabled<IsCharacterRechargedAttack>(entity, false);

                AddComponent(entity, new EnemyAttackComponent
                {
                    Damage = authoring.Damage,
                    CooldownTime = authoring.CooldownTime,
                });
            }
        }
    }

    [BurstCompile]
    public partial struct EnemyMoveToPlayerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float2? playerPosition = null;

            foreach (var item in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerTag>())
            {
                playerPosition = item.ValueRO.Position.xy;
            }

            if (playerPosition == null)
                return;

            var moveToPlayerJob = new EnemyMoveToPlayerJob
            {
                playerPos = playerPosition.Value
            };

            state.Dependency = moveToPlayerJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    public partial struct EnemyMoveToPlayerJob : IJobEntity
    {
        public float2 playerPos;

        public void Execute(ref CharacterMoveDirection dir, in LocalTransform transform)
        {
            var vecToPlayer = playerPos - transform.Position.xy;

            dir.Value = math.normalize(vecToPlayer);
        }
    }
}
