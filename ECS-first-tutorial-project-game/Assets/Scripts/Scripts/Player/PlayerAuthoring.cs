using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

namespace ECS_Tutorial_Game
{
    public struct PlayerTag : IComponentData { };

    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTag());
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

                direction.ValueRW.value = _currInput;
            }
        }
    }
}
