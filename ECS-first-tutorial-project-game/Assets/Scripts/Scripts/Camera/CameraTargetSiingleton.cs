using UnityEngine;

namespace ECS_Tutorial_Game
{
    public class CameraTargetSiingleton : MonoBehaviour
    {
        public static CameraTargetSiingleton Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            
            else
                Destroy(gameObject);
        }
    }
}
