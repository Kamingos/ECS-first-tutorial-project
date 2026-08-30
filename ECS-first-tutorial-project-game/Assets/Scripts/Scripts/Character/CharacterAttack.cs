
using Unity.Entities;
using ECS_Tutorial_Game.CharacterHealth;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;
using Unity.Physics.Systems;

namespace ECS_Tutorial_Game.CharacterAttack
{
    public struct EnemyAttackComponent : IComponentData
    {
        public int Damage;
        public float CooldownTime;

    }

    public struct IsCharacterRechargedAttack : IComponentData, IEnableableComponent
    {
        public double StartRechargingTime;

    }

    [BurstCompile]
    public partial struct UpdateCharacterRechargedAttack : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (rechargeEnabledComp, rechargeComp, attackComp) in SystemAPI.Query<EnabledRefRW<IsCharacterRechargedAttack>, RefRO<IsCharacterRechargedAttack>, RefRO<EnemyAttackComponent>>())
            {
                if (rechargeEnabledComp.ValueRO == false)
                    continue;

                if (SystemAPI.Time.ElapsedTime > rechargeComp.ValueRO.StartRechargingTime + attackComp.ValueRO.CooldownTime)
                {
                    rechargeEnabledComp.ValueRW = false;
                }
            }
        }
    }

    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new EnemyCollisionDamageEventJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,

                playerTagLookup = SystemAPI.GetComponentLookup<PlayerTag>(),
                enemyTagLookup = SystemAPI.GetComponentLookup<EnemyTag>(),

                enemyAttackLookup = SystemAPI.GetComponentLookup<EnemyAttackComponent>(),

                playerAttackBufferLookup = SystemAPI.GetBufferLookup<CharacterAttackBufferComponent>(false),

                isEnemyRechargedAttackLookup = SystemAPI.GetComponentLookup<IsCharacterRechargedAttack>(false),
            };

            state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyCollisionDamageEventJob : ICollisionEventsJob
    {
        public double ElapsedTime;

        public ComponentLookup<PlayerTag> playerTagLookup;
        public ComponentLookup<EnemyTag> enemyTagLookup;

        public BufferLookup<CharacterAttackBufferComponent> playerAttackBufferLookup;
        public ComponentLookup<EnemyAttackComponent> enemyAttackLookup;

        public ComponentLookup<IsCharacterRechargedAttack> isEnemyRechargedAttackLookup;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity enemyEntity;

            if (playerTagLookup.HasComponent(collisionEvent.EntityA) && enemyTagLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                enemyEntity = collisionEvent.EntityB;
            }
            else if (playerTagLookup.HasComponent(collisionEvent.EntityB) && enemyTagLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                enemyEntity = collisionEvent.EntityA;
            }
            else
                return;

            if (isEnemyRechargedAttackLookup.IsComponentEnabled(enemyEntity))
                return;

            isEnemyRechargedAttackLookup.GetRefRW(enemyEntity).ValueRW.StartRechargingTime = ElapsedTime;
            isEnemyRechargedAttackLookup.SetComponentEnabled(enemyEntity, true);

            var playerBuffer = playerAttackBufferLookup[playerEntity];
            var enemyAttack = enemyAttackLookup[enemyEntity];

            playerBuffer.Add(new CharacterAttackBufferComponent
            {
                Value = enemyAttack.Damage,
            });
        }
    }
}
