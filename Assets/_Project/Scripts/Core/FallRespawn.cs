using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Страховка от «выпадения за карту». Каждый кадр проверяет мировую позицию игрока (головы)
    /// и, если он провалился ниже fallY (или, опционально, улетел за maxHorizontal по X/Z),
    /// возвращает его на SpawnPoint ТЕКУЩЕЙ контентной сцены через PlayerRig.TeleportTo.
    ///
    /// Висит на ПОСТОЯННОМ PlayerRig (рядом с HeadCollisionPushback). Работает во всех сценах
    /// автоматически — добавлять в каждую сцену не нужно: SpawnPoint ищется в загруженной сцене.
    /// </summary>
    [DisallowMultipleComponent]
    public class FallRespawn : MonoBehaviour
    {
        [Header("Порог падения")]
        [Tooltip("Если Y игрока опустится до этого значения или ниже — респаун (провал сквозь пол).")]
        [SerializeField] private float fallY = -1000f;

        [Tooltip("Если |X| или |Z| игрока превысит это значение — тоже респаун. 0 = не проверять (по умолчанию).")]
        [SerializeField] private float maxHorizontal = 0f;

        [Tooltip("Минимальная пауза между респаунами, сек (чтобы не срабатывало каждый кадр).")]
        [SerializeField] private float cooldown = 0.5f;

        private float _nextAllowedTime;
        private bool _warnedNoSpawn;

        private void Update()
        {
            if (Time.unscaledTime < _nextAllowedTime) return;

            // во время перехода между сценами риг двигает SceneLoader — не вмешиваемся
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning) return;

            var rig = PlayerRig.Instance;
            if (rig == null) return;

            // позиция игрока = голова (камера) в мире; если камеры нет — корень рига
            Vector3 p;
            Camera cam = rig.Origin != null ? rig.Origin.Camera : null;
            if (cam != null) p = cam.transform.position;
            else p = (rig.Origin != null ? rig.Origin.transform.position : rig.transform.position);

            bool outOfBounds = p.y <= fallY;
            if (!outOfBounds && maxHorizontal > 0f &&
                (Mathf.Abs(p.x) > maxHorizontal || Mathf.Abs(p.z) > maxHorizontal))
                outOfBounds = true;
            if (!outOfBounds) return;

            var sp = Object.FindFirstObjectByType<SpawnPoint>();
            if (sp == null)
            {
                if (!_warnedNoSpawn)
                {
                    Debug.LogWarning("[FallRespawn] Игрок за картой, но SpawnPoint в сцене не найден — респаун невозможен.");
                    _warnedNoSpawn = true;
                }
                return;
            }
            _warnedNoSpawn = false;

            // телепорт с выключением CharacterController, чтобы контроллер не сопротивлялся переносу
            var cc = rig.GetComponent<CharacterController>();
            bool hadCC = cc != null && cc.enabled;
            if (hadCC) cc.enabled = false;

            rig.TeleportTo(sp.transform);

            if (hadCC)
            {
                Physics.SyncTransforms();
                cc.enabled = true;
            }

            _nextAllowedTime = Time.unscaledTime + Mathf.Max(0.1f, cooldown);
            Debug.Log("[FallRespawn] Игрок вышел за карту — возвращён на SpawnPoint текущей сцены.");
        }
    }
}
