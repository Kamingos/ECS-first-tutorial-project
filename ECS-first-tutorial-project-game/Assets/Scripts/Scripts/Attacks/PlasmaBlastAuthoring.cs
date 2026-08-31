using ECS_Tutorial_Game.CharacterHealth;
using ECS_Tutorial_Game.DestroyCharacter;
using ECS_Tutorial_Game.Gem;
using System.ComponentModel;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ECS_Tutorial_Game.Attacks.PlasmaBlast
{
    public struct PlasmaBlastData : IComponentData
    {
        public float MoveSpeed;
        public int AttackDamage;
    }

    public class PlasmaBlastAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        public int AttackDamage;

        private class Baker : Baker<PlasmaBlastAuthoring>
        {
            public override void Bake(PlasmaBlastAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlasmaBlastData
                {
                    MoveSpeed = authoring.MoveSpeed,
                    AttackDamage = authoring.AttackDamage,
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
    public partial struct MovePlasmaBlastSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (vel, loc, blastData) in SystemAPI.Query<RefRW<PhysicsVelocity>, LocalToWorld, PlasmaBlastData>())
            {
                vel.ValueRW.Linear = loc.Right * blastData.MoveSpeed;
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial struct PlasmaBlastAttackSystem : ISystem
    {
        private static bool _loggedOnce;

        public void OnUpdate(ref SystemState state)
        {
            var job = new PlasmaBlastAttackJob
            {
                PlasmaBlastLookup = SystemAPI.GetComponentLookup<PlasmaBlastData>(),
                EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(),
                AttackBufferLookup = SystemAPI.GetBufferLookup<CharacterAttackBufferComponent>(false),
                DestroyFlag = SystemAPI.GetComponentLookup<DestroyEntityFlag>(false),
            };

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = job.Schedule(simulationSingleton, state.Dependency);
        }
    }

    public struct PlasmaBlastAttackJob : ITriggerEventsJob
    {
        public ComponentLookup<PlasmaBlastData> PlasmaBlastLookup;
        public ComponentLookup<EnemyTag> EnemyLookup;

        public BufferLookup<CharacterAttackBufferComponent> AttackBufferLookup;

        public ComponentLookup<DestroyEntityFlag> DestroyFlag;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity plasmaBlastEntity;
            Entity enemyEntity;

            if (PlasmaBlastLookup.HasComponent(triggerEvent.EntityA) && EnemyLookup.HasComponent(triggerEvent.EntityB))
            {
                plasmaBlastEntity = triggerEvent.EntityA;
                enemyEntity = triggerEvent.EntityB;
            }

            else if (PlasmaBlastLookup.HasComponent(triggerEvent.EntityB) && EnemyLookup.HasComponent(triggerEvent.EntityA))
            {
                plasmaBlastEntity = triggerEvent.EntityB;
                enemyEntity = triggerEvent.EntityA;
            }

            else
                return;

            var buffer = AttackBufferLookup[enemyEntity];

            buffer.Add(new CharacterAttackBufferComponent
            {
                Value = PlasmaBlastLookup[plasmaBlastEntity].AttackDamage
            });

            DestroyFlag.SetComponentEnabled(plasmaBlastEntity, true);
        }
    }
}
