using ECS_Tutorial_Game.CharacterAuthoring;
using ECS_Tutorial_Game.CharacterHealth;
using ECS_Tutorial_Game.Gem;
using ECS_Tutorial_Game.WorldUI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ECS_Tutorial_Game
{
    public struct PlayerTag : IComponentData { };
    public struct CameraTarget : IComponentData 
    {
        public UnityObjectRef<Transform> cameraTransform;
    };
    public struct PlayerTransform : IComponentData 
    {
        public float2 position;
    };
    public struct InitializeCameraTargetTag : IComponentData { }

    public struct  PlayerAttackData : IComponentData
    {
        public Entity AttackPrefab;
        public float CooldownTime;
        public float3 DetectionSize;
        public CollisionFilter CollisionFilter;
    }

    public struct PlayerCooldownExpirationTimestamp : IComponentData
    {
        public double Value;
    }


    [RequireComponent(typeof(CharacterAuthoring.CharacterAuthoring))]
    public class PlayerAuthoring : MonoBehaviour
    {
        public int MaxHp;
        public GameObject BlasterBlastPrefab;
        public float AttackCooldownTime;
        public float DetectionSize;

        public UnityObjectRef<GameObject> HealthBarSlider;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<PlayerTag>(entity);
                AddComponent<InitializeCameraTargetTag>(entity);
                AddComponent<CameraTarget>(entity);
                AddComponent<PlayerTransform>(entity);

                // Helath
                AddComponent(entity, new HealthPointComponent
                {
                    MaxHp = authoring.MaxHp,
                    CurrentHp = authoring.MaxHp
                });


                // Attack
                var EnemyLayer = LayerMask.NameToLayer("Enemy");
                var enemyLayerMask = (uint)1<<EnemyLayer;

                var attackCollisionFilter = new CollisionFilter
                {
                    BelongsTo = uint.MaxValue,
                    CollidesWith = enemyLayerMask
                };

                AddComponent(entity, new PlayerAttackData
                {
                    AttackPrefab = GetEntity(authoring.BlasterBlastPrefab, TransformUsageFlags.Dynamic),
                    CooldownTime = authoring.AttackCooldownTime,
                    DetectionSize = new float3(authoring.DetectionSize),
                    CollisionFilter = attackCollisionFilter
                });

                AddComponent<PlayerCooldownExpirationTimestamp>(entity);

                // GemScore
                AddComponent<GemScore>(entity);

                // World UI Bar
                AddComponent(entity, new WorldUIComponentPrefab
                {
                    HealthBarSliderPrefab = authoring.HealthBarSlider
                });
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CameraInitializationSystem : ISystem 
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeCameraTargetTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (CameraTargetSiingleton.Instance == null) return;

            var cameraTargetTransform = CameraTargetSiingleton.Instance.transform;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (cameraTarget, entity) in SystemAPI.Query<RefRW<CameraTarget>>().WithAll<InitializeCameraTargetTag, PlayerTag>().WithEntityAccess())
            {
                cameraTarget.ValueRW.cameraTransform = cameraTargetTransform;
                ecb.RemoveComponent<InitializeCameraTargetTag>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, cameraTarget) in SystemAPI.Query<LocalToWorld, CameraTarget>().WithAll<PlayerTag>().WithNone<InitializeCameraTargetTag>())
            {
                cameraTarget.cameraTransform.Value.position = transform.Position;
            }
        }
    }

    public partial class PlayerInputSystem : SystemBase
    {
        private InputAction action;

        protected override void OnCreate()
        {
            action = InputSystem.actions.FindAction("Player/Move");

            action.Enable();
        }

        protected override void OnUpdate()
        {
            foreach (var direction in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
            {
                float2 _currInput = (float2) action.ReadValue<Vector2>();

                direction.ValueRW.Value = _currInput;
            }
        }
    }
    public partial struct SetPlayerPositionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerTransform, position) in SystemAPI.Query<RefRW<PlayerTransform>, LocalToWorld>().WithAll<PlayerTag>())
            {
                playerTransform.ValueRW.position = new float2(position.Position.x, position.Position.y);
            }
        }
    }

    public partial struct PlayerAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            foreach (var (expirationTimeStamp, attackData, transform) in SystemAPI.Query<RefRW<PlayerCooldownExpirationTimestamp>, PlayerAttackData, LocalTransform>())
            {
                if (expirationTimeStamp.ValueRO.Value > elapsedTime) continue;

                var spawnPos = transform.Position;

                var minDetectPos = spawnPos - attackData.DetectionSize;
                var maxDetectPos = spawnPos + attackData.DetectionSize;

                var aabbInput = new OverlapAabbInput
                {
                    Aabb = new Aabb
                    {
                        Min = minDetectPos,
                        Max = maxDetectPos,
                    },

                    Filter = attackData.CollisionFilter
                };

                var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);

                if (!physicsWorldSingleton.OverlapAabb(aabbInput, ref overlapHits))
                    continue;


                var maxDistanceSq = float.MaxValue;

                var closestEnemyPosition = float3.zero;

                foreach (var overlapHit in overlapHits)
                {
                    var curEnemyPosition = physicsWorldSingleton.Bodies[overlapHit].WorldFromBody.pos;

                    var distanceToPlayer = math.distancesq(spawnPos.xy, curEnemyPosition.xy);

                    if (distanceToPlayer < maxDistanceSq)
                    {
                        closestEnemyPosition = curEnemyPosition;
                        maxDistanceSq = distanceToPlayer;
                    }
                }


                var vectorToClosestEnemy = closestEnemyPosition - spawnPos;

                var angleToClosestEnemy = math.atan2(vectorToClosestEnemy.y, vectorToClosestEnemy.x);

                var spawnOrientation = quaternion.Euler(0f, 0f, angleToClosestEnemy);


                var newAttack = ecb.Instantiate(attackData.AttackPrefab);

                ecb.SetComponent(newAttack, LocalTransform.FromPositionRotation(spawnPos, spawnOrientation));

                expirationTimeStamp.ValueRW.Value = elapsedTime + attackData.CooldownTime;
            }
        }
    }
}
