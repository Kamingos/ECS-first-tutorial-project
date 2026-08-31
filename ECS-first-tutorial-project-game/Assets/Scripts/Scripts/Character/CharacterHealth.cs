
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
    public partial struct ApplyDamageBufferToCharacterPerFrame : ISystem
    {
        private static int TotalDamage(in DynamicBuffer<CharacterAttackBufferComponent> buff)
        {
            int sum = 0;
            foreach (var item in buff) sum += item.Value;
            return sum;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, buff, entity) in SystemAPI.Query<RefRW<HealthPointComponent>, DynamicBuffer<CharacterAttackBufferComponent>>().WithDisabled<DestroyEntityFlag>().WithEntityAccess())
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
