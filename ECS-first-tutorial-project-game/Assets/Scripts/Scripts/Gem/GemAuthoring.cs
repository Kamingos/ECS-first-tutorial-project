using ECS_Tutorial_Game.DestroyCharacter;
using TMG.Survivors;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace ECS_Tutorial_Game.Gem
{
    public struct CharacterDropGemComponent : IComponentData
    {
        public Entity GemEntityPrefab;
    }
    public struct GemData : IComponentData
    {
        public int GemValue;
    }

    public struct GemScore : IComponentData
    {
        public int Count;
    }

    public struct GemUIUpdateTag : IComponentData, IEnableableComponent { }

    public class GemAuthoring : MonoBehaviour
    {
        public int Value;

        private class Baker : Baker<GemAuthoring>
        {
            public override void Bake(GemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);

                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);

                AddComponent(entity, new GemData
                {
                    GemValue = authoring.Value
                });

                AddComponent<GemUIUpdateTag>(entity);
                SetComponentEnabled<GemUIUpdateTag>(entity, false);
            }
        }
    }

    public partial struct GemSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var job = new GemTriggerJob
            {
                GemScoreLookup = SystemAPI.GetComponentLookup<GemScore>(),
                GemDataLookup = SystemAPI.GetComponentLookup<GemData>(),
                DestroyFlag = SystemAPI.GetComponentLookup<DestroyEntityFlag>(),
                GemUIUpdateFlag = SystemAPI.GetComponentLookup<GemUIUpdateTag>(),
            };

            var singletom = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = job.Schedule(singletom, state.Dependency);
        }
    }

    public partial struct GemTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<GemScore> GemScoreLookup;
        public ComponentLookup<GemData> GemDataLookup;

        public ComponentLookup<DestroyEntityFlag> DestroyFlag;
        public ComponentLookup<GemUIUpdateTag> GemUIUpdateFlag;

        [BurstCompile]
        public void Execute(TriggerEvent collisionEvent)
        {
            Entity gemScore;
            Entity gemData;

            if (GemScoreLookup.HasComponent(collisionEvent.EntityA) && GemDataLookup.HasComponent(collisionEvent.EntityB))
            {
                gemScore = collisionEvent.EntityA;
                gemData = collisionEvent.EntityB;
            }
            else if (GemScoreLookup.HasComponent(collisionEvent.EntityB) && GemDataLookup.HasComponent(collisionEvent.EntityA))
            {
                gemScore = collisionEvent.EntityB;
                gemData = collisionEvent.EntityA;
            }
            else
                return;

            GemScoreLookup.GetRefRW(gemScore).ValueRW.Count += GemDataLookup[gemData].GemValue;

            DestroyFlag.SetComponentEnabled(gemData, true);
            GemUIUpdateFlag.SetComponentEnabled(gemData, true);
        }
    }

    public partial struct UpdateGemUiCountSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            int gemValue = -1;

            foreach (var data in SystemAPI.Query<RefRO<GemScore>>().WithAll<PlayerTag>())
            {
                gemValue = data.ValueRO.Count;
            }

            if (gemValue == -1)
                return;

            foreach (var (data, flag) in SystemAPI.Query<GemUIUpdateTag, EnabledRefRW<GemUIUpdateTag>>())
            {
                GameUIController.Instance.UpdateGemsCollectedText(gemValue);

                flag.ValueRW = false;
            }
        }
    }
}
