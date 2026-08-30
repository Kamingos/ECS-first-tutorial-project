using ECS_Tutorial_Game.CharacterHealth;
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
            }
        }
    }
    public partial struct MovePlasmaBlastSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Задаём постоянную скорость полёта, а не накапливаем её
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
            };

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = job.Schedule(simulationSingleton, state.Dependency);

            if (!_loggedOnce)
            {
                _loggedOnce = true;
                Debug.Log("[Blast] PlasmaBlastAttackSystem запущен");
            }
        }
    }

    public struct PlasmaBlastAttackJob : ITriggerEventsJob
    {
        public ComponentLookup<PlasmaBlastData> PlasmaBlastLookup;
        public ComponentLookup<EnemyTag> EnemyLookup;

        public BufferLookup<CharacterAttackBufferComponent> AttackBufferLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            // ВРЕМЕННАЯ ДИАГНОСТИКА
            Debug.Log($"[Blast] Trigger event: A={triggerEvent.EntityA.Index} B={triggerEvent.EntityB.Index}");

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

            Debug.Log($"[Blast] Попадание! Урон {PlasmaBlastLookup[plasmaBlastEntity].AttackDamage} -> враг {enemyEntity.Index}");

            var buffer = AttackBufferLookup[enemyEntity];

            buffer.Add(new CharacterAttackBufferComponent
            {
                Value = PlasmaBlastLookup[plasmaBlastEntity].AttackDamage
            });
        }
    }
}
