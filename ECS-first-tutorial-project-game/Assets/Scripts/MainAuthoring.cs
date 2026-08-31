using Unity.Entities;
using UnityEngine;

namespace ECS_Tutorial_Game.Assets.Scripts
{
    public class MainAuthoring : MonoBehaviour
    {
        private class Baker : Baker<MainAuthoring>
        {
            public override void Bake(MainAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
            }
        }
    }
}
