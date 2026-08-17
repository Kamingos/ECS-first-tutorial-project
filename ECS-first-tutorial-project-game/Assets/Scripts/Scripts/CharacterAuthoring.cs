using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Unity.Physics;

namespace ECS_Tutorial_Game
{
    public struct CharacterMoveDirection : IComponentData
    {
        public float2 value;
    }

    public struct CharacterMoveSpeed: IComponentData
    {
        public float value;
    }

    public class CharacterAuthoring : MonoBehaviour
    {
        public float MoveSpeed;

        private class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new CharacterMoveDirection());
                AddComponent(entity, new CharacterMoveSpeed
                {
                    value = authoring.MoveSpeed
                });
            }
        }
    }

    public partial struct CharacterMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach(var (vel, dir, speed) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterMoveSpeed>())
            {
                var moveStep2D = dir.value * speed.value;

                vel.ValueRW.Linear += new float3(moveStep2D, 0f);
            }
        }
    }
}
