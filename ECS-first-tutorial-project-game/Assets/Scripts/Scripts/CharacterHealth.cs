
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
            foreach (var (health, buff) in SystemAPI.Query<RefRW<HealthPointComponent>, DynamicBuffer<CharacterAttackBufferComponent>>())
            {
                foreach (var item in buff)
                {
                    health.ValueRW.CurrentHp -= item.Value;
                }

                buff.Clear();
            }
        }
    }
}
