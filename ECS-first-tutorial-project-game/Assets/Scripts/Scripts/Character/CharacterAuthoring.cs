using ECS_Tutorial_Game.CharacterHealth;
using ECS_Tutorial_Game.DestroyCharacter;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

namespace ECS_Tutorial_Game.CharacterAuthoring
{
    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent { }

    public struct CharacterMoveDirection : IComponentData
    {
        public float2 Value;
    }

    public struct CharacterMoveSpeed : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_FacingDirection")]
    public struct FacingDirectionOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_AnimationIndex")]
    public struct AnimationIndexOverride : IComponentData
    {
        public float Value;
    }

    public enum AnimationIndex : byte
    {
        Movement = 0,
        Idle = 1,

        None = byte.MaxValue
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
                AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });

                // Shader's
                AddComponent(entity, new FacingDirectionOverride { Value = 1 });
                AddComponent(entity, new AnimationIndexOverride { Value = 0 });

                // Health
                AddBuffer<CharacterAttackBufferComponent>(entity);

                // Destroy
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
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

    [UpdateAfter(typeof(PlayerInputSystem))]
    public partial struct CharacterMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (vel, dir, speed) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<CharacterMoveDirection>, RefRO<CharacterMoveSpeed>>())
            {
                var moveStep2D = dir.ValueRO.Value * speed.ValueRO.Value;

                vel.ValueRW.Linear += new float3(moveStep2D, 0f);
            }
        }
    }
    [UpdateAfter(typeof(CharacterMoveSystem))]
    public partial struct CharacterDirectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (faceDir, vel) in SystemAPI.Query<RefRW<FacingDirectionOverride>, CharacterMoveDirection>())
            {
                float res = vel.Value.x / math.abs(vel.Value.x);

                if (math.abs(res) != 1) continue;

                faceDir.ValueRW.Value = res;
            }
        }
    }
    [UpdateAfter(typeof(CharacterDirectionSystem))]
    public partial struct CharacterAnimationIndexSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (animIndex, vel) in SystemAPI.Query<RefRW<AnimationIndexOverride>, CharacterMoveDirection>())
            {
                if (math.abs(vel.Value.x) > 0.15f)
                    animIndex.ValueRW.Value = (float)AnimationIndex.Movement;

                else
                    animIndex.ValueRW.Value = (float)AnimationIndex.Idle;
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
            Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float)SystemAPI.Time.ElapsedTime);
        }
    }
}
