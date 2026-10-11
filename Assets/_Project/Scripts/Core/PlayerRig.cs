using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace GoldenRing.Core
{
    /// <summary>
    /// Точка доступа к постоянному VR-ригу игрока (XR Origin), который живёт в Bootstrap-сцене
    /// и переживает загрузку контентных сцен (лобби/город/викторина).
    ///
    /// Зачем: контентные сцены НЕ содержат своего рига — они грузятся additive поверх Bootstrap.
    /// SceneLoader через этот компонент переносит риг в SpawnPoint новой сцены, а лобби-спавнер
    /// через LeftInteractor вкладывает собранный артефакт прямо в левую руку.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        [Tooltip("XR Origin рига (обычно этот же объект). Если пусто — ищется на объекте/в детях.")]
        [SerializeField] private XROrigin origin;

        [Tooltip("Левый интерактор (Near-Far / Direct) — им игрок берёт артефакты. Нужен, чтобы вложить артефакт в руку при телепорте в лобби.")]
        [SerializeField] private XRBaseInteractor leftHandInteractor;

        /// <summary>XR Origin рига.</summary>
        public XROrigin Origin => origin;

        /// <summary>Левый интерактор (для force-select артефакта в руку). Может быть null.</summary>
        public XRBaseInteractor LeftInteractor => leftHandInteractor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (origin == null)
                origin = GetComponentInChildren<XROrigin>(true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Перенести риг так, чтобы игрок оказался в точке target (позиция на полу + рыскание).
        /// Ставим корень XR Origin в позицию цели и доворачиваем по Y на разницу направлений взгляда.
        /// Камера отслеживается относительно корня, поэтому смещение по высоте сохраняется.
        /// </summary>
        public void TeleportTo(Transform target)
        {
            if (target == null) return;
            var t = origin != null ? origin.transform : transform;

            // Рыскание сначала: развернуть риг так, чтобы «вперёд» совпало с target.forward (только Y).
            Vector3 fwd = target.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

            // Позиция: ставим так, чтобы на точку встал ИГРОК (камера по XZ), а не корень Origin.
            // HMD смещён внутри play area, поэтому если ставить корень — игрок появляется В СТОРОНЕ от SpawnPoint.
            // Компенсируем горизонтальное смещение камеры от корня; высоту (пол) берём из target.y.
            Camera cam = origin != null ? origin.Camera : null;
            if (cam != null)
            {
                Vector3 camOffset = cam.transform.position - t.position;
                camOffset.y = 0f;
                t.position = target.position - camOffset;
            }
            else
            {
                t.position = target.position;
            }
        }
    }
}
