using ECS_Tutorial_Game.CharacterHealth;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;


namespace ECS_Tutorial_Game.WorldUI
{
    public struct WorldUIComponent : ICleanupComponentData
    {
        public UnityObjectRef<Transform> CanvasTransform;
        public UnityObjectRef<Slider> HealthBarSlider;
    }

    public struct WorldUIComponentPrefab : IComponentData
    {
        public UnityObjectRef<GameObject> HealthBarSliderPrefab;
    }

    //[UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorldUISystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // тут будет добавляться WorldUIComponent, когда его ещё нету
            foreach (var (worldUI, entity) in SystemAPI.Query<WorldUIComponentPrefab>().WithNone<WorldUIComponent>().WithEntityAccess())
            {
                var newWorldUI = Object.Instantiate(worldUI.HealthBarSliderPrefab.Value);
                ecb.AddComponent(entity, new WorldUIComponent
                {
                    CanvasTransform = newWorldUI.transform,
                    HealthBarSlider = newWorldUI.GetComponentInChildren<Slider>()
                });
            }

            foreach (var (worldUI, playerTransform, health) in SystemAPI.Query<RefRW<WorldUIComponent>, LocalToWorld, HealthPointComponent>())
            {
                worldUI.ValueRW.CanvasTransform.Value.position = new float3(playerTransform.Position.x, playerTransform.Position.y, 0f);
                worldUI.ValueRW.HealthBarSlider.Value.value = (float) health.CurrentHp / health.MaxHp;
            }

            foreach (var (worldUI, entity) in SystemAPI.Query<WorldUIComponent>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (worldUI.HealthBarSlider.Value.gameObject != null)
                    Object.Destroy(worldUI.HealthBarSlider.Value.gameObject);

                ecb.RemoveComponent<WorldUIComponent>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
