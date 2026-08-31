using ECS_Tutorial_Game.Gem;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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
            
            var gemLookup = SystemAPI.GetComponentLookup<CharacterDropGemComponent>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalToWorld>();

            foreach (var (_flag, entity) in SystemAPI.Query<DestroyEntityFlag>().WithEntityAccess())
            {
                if (gemLookup.HasComponent(entity))
                {
                    var begEcbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                    var begEcb = endEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);

                    var obj = gemLookup[entity].GemEntityPrefab;

                    var inst = begEcb.Instantiate(obj);

                    var enemyPos = transformLookup[entity].Position;

                    begEcb.SetComponent(inst, LocalTransform.FromPositionRotationScale(enemyPos, Quaternion.identity, 0.5f));
                }

                endEcb.DestroyEntity(entity);
            }
        }
    }
}
