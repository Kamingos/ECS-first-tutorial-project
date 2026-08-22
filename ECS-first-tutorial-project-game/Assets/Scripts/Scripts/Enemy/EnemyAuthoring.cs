using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace ECS_Tutorial_Game
{
    public struct EnemyTag : IComponentData { };

    public class EnemyAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<EnemyTag>(entity);
            }
        }
    }

    public partial struct EnemyPursuitSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float2? playerPosition = null;

            foreach (var playerTransfromItem in SystemAPI.Query<PlayerTransform>().WithAll<PlayerTag>())
            {
                playerPosition = playerTransfromItem.position;
                break;
            }

            if (playerPosition == null)
                return;

            foreach (var (vel, selfTransform) in SystemAPI.Query<RefRW<CharacterMoveDirection>, LocalToWorld>())
            {
                var direction = math.normalize(playerPosition.Value - new float2(selfTransform.Position.x, selfTransform.Position.y));

                vel.ValueRW.value = direction;
            }
        }
    }
}
