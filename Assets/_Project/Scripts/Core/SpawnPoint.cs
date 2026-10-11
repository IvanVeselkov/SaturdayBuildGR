using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Маркер точки появления игрока в загружаемой сцене (лобби/город/викторина).
    /// SceneLoader после additive-загрузки переносит постоянный XR Origin в этот трансформ
    /// (позиция на полу + рыскание). Ставить один на боевую сцену; вращение задаёт направление взгляда.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
        }
#endif
    }
}
