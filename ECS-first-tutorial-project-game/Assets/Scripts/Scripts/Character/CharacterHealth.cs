
using ECS_Tutorial_Game.DestroyCharacter;
using TMG.Survivors;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace ECS_Tutorial_Game.CharacterHealth
{

    public struct HealthPointComponent : IComponentData
    {
        public int MaxHp;
        public int CurrentHp;
    }

    public struct CharacterAttackBufferComponent : IBufferElementData
    {
        public int Value;
    }

    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [BurstCompile]
    public partial struct ApplyDamageBufferToCharacterPerFrame : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, buff, entity) in SystemAPI.Query<RefRW<HealthPointComponent>, DynamicBuffer<CharacterAttackBufferComponent>>().WithPresent<DestroyEntityFlag>().WithEntityAccess())
            {
                foreach (var item in buff)
                {
                    health.ValueRW.CurrentHp -= item.Value;
                }

                buff.Clear();

                if (health.ValueRW.CurrentHp <= 0)
                {
                    if (SystemAPI.HasComponent<PlayerTag>(entity))
                    {
                        GameUIController.Instance.ShowGameOverUI();
                    }

                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
            }
        }
    }
}
