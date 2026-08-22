using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Burst;
using Unity.Rendering;

namespace ECS_Tutorial_Game
{
    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent { }

    public struct CharacterMoveDirection : IComponentData
    {
        public float2 value;
    }

    public struct CharacterMoveSpeed: IComponentData
    {
        public float value;
    }

    [MaterialProperty("_FacingDirection")]
    public struct FacingDirectionOverride : IComponentData
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

                // Initialization's
                AddComponent(entity, new InitializeCharacterFlag());

                // Movement's
                AddComponent(entity, new CharacterMoveDirection());
                AddComponent(entity, new CharacterMoveSpeed { value = authoring.MoveSpeed });

                // Shader's
                AddComponent(entity, new FacingDirectionOverride { value = 1 });
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CharacterInitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (mass, shouldInitialize) in SystemAPI.Query<RefRW<PhysicsMass>, EnabledRefRW<InitializeCharacterFlag>>())
            {
                mass.ValueRW.InverseInertia = float3.zero;
                shouldInitialize.ValueRW = false;
            }
        }
    }

    public partial struct CharacterMoveSystem : ISystem
    {
        [BurstCompile]
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
    public partial struct CharacterDirectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach(var (faceDir, vel) in SystemAPI.Query<RefRW<FacingDirectionOverride>, CharacterMoveDirection>())
            {
                float res = vel.value.x / math.abs(vel.value.x);

                if (math.abs(res) != 1) return;

                faceDir.ValueRW.value = res;
            }
        }
    }
    public partial struct CharacterAnimationIndexSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (animIndex, vel) in SystemAPI.Query<RefRW<AnimationIndexOverride>, CharacterMoveDirection>())
            {
                if (math.abs(vel.value.x) > 0.15f)
                    animIndex.ValueRW.value = (float) AnimationIndex.Movement;

                else
                    animIndex.ValueRW.value = (float)AnimationIndex.Idle;
            }
        }
    }

    public partial struct GlobalTimeUpdateSystem : ISystem
    {
        private static int _globalTimeShaderPropertyID;

        public void OnCreate(ref SystemState state)
        {
            _globalTimeShaderPropertyID = Shader.PropertyToID("_GlobalTime");
        }

        public void OnUpdate(ref SystemState state)
        {
            Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float) SystemAPI.Time.ElapsedTime);
        }
    }
}
