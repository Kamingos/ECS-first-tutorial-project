using Unity.Entities;

namespace ECS_Tutorial_Game.DestroyCharacter
{
    public struct DestroyEntityFlag : IComponentData, IEnableableComponent { }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct DestroyCharacterSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var endEcbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var endEcb = endEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_flag, entity) in SystemAPI.Query<DestroyEntityFlag>().WithEntityAccess())
            {
                endEcb.DestroyEntity(entity);
            }
        }
    }
}
