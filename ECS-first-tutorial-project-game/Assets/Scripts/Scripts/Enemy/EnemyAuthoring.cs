using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace ECS_Tutorial_Game
{
    public struct EnemyTag : IComponentData { };

    [RequireComponent(typeof(CharacterAuthoring))]
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

            foreach (var player in SystemAPI.Query<RefRO<PlayerTransform>>().WithAll<PlayerTag>())
            {
                playerPosition = player.ValueRO.position;
                break;
            }

            if (playerPosition == null)
                return;

            foreach (var (vel, selfTransform) in SystemAPI.Query<RefRW<CharacterMoveDirection>, RefRO<LocalToWorld>>())
            {

                float2 enemyPos = new float2(selfTransform.ValueRO.Position.x, selfTransform.ValueRO.Position.y);

                float2 direction = playerPosition.Value - enemyPos;

                if (math.lengthsq(direction) > 0.0001f)
                {
                    direction = math.normalize(direction);
                }
                else
                {
                    direction = float2.zero;
                }

                vel.ValueRW.value = direction;
            }
        }
    }
}
