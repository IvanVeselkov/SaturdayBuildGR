using UnityEngine;
using Unity.XR.CoreUtils;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Не даёт ГОЛОВЕ игрока проходить сквозь коллайдеры при движении, которое идёт
    /// МИМО локомоции: физическая room-scale ходьба и перемещение в XR Device Simulator
    /// двигают позу камеры напрямую, CharacterController при этом не участвует.
    ///
    /// Каждый кадр делаются два НЕЗАВИСИМЫХ дела:
    /// 1) Главный CharacterController рига подгоняется по высоте головы (пол->голова) —
    ///    он нужен локомоции по стику и гравитации; его форму мы не ломаем.
    /// 2) Отдельная небольшая капсула-ПРОБНИК ездит ВОКРУГ ГОЛОВЫ (центр по камере, а не
    ///    от пола). Если голова зашла в геометрию — риг выталкивается по горизонтали.
    ///    Пробник висит высоко и НЕ цепляет ступеньки/пороги у пола (из-за чего раньше
    ///    риг отталкивало назад и он не мог подняться по лестнице). При подъёме/спуске
    ///    Q/E в симуляторе пробник идёт ЗА головой, а не растягивается от пола.
    ///
    /// Вешать на PlayerRig (там же XROrigin + CharacterController).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class HeadCollisionPushback : MonoBehaviour
    {
        [Tooltip("XR Origin рига. Пусто = берётся с этого объекта.")]
        public XROrigin origin;
        [Tooltip("Главный CharacterController рига (локомоция/гравитация). Пусто = берётся с XR Origin.")]
        public CharacterController controller;
        [Tooltip("Слои, сквозь которые голову не пускаем (стены/здание/забор). По умолчанию Default.")]
        public LayerMask collisionLayers = 1;

        [Header("Главный контроллер (локомоция) — подгон высоты")]
        [Tooltip("Минимальная высота капсулы контроллера (сидя/низко).")]
        public float minHeight = 0.3f;
        [Tooltip("Максимальная высота капсулы контроллера.")]
        public float maxHeight = 2.5f;

        [Header("Пробник головы (хитбокс, едет за головой)")]
        [Tooltip("Радиус капсулы-пробника вокруг головы.")]
        public float headRadius = 0.2f;
        [Tooltip("Высота капсулы-пробника (голова + немного корпуса). Низ держится выше ступенек.")]
        public float headHeight = 1.0f;
        [Tooltip("Сколько раз за кадр разрешать столкновения (углы).")]
        public int solverIterations = 3;

        Camera _cam;
        CapsuleCollider _headProbe;

        void Awake()
        {
            if (origin == null) origin = GetComponentInParent<XROrigin>();
            if (controller == null && origin != null) controller = origin.GetComponent<CharacterController>();
            EnsureProbe();
        }

        void OnEnable()
        {
            if (origin != null) _cam = origin.Camera;
            EnsureProbe();
        }

        void EnsureProbe()
        {
            if (_headProbe != null) return;
            if (origin == null) return;
            GameObject go = new GameObject("HeadCollisionProbe");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = 2; // Ignore Raycast — ни во что не упирается физически
            go.transform.SetParent(origin.transform, false);
            _headProbe = go.AddComponent<CapsuleCollider>();
            _headProbe.isTrigger = true; // только геометрия для ComputePenetration, в физике не участвует
            _headProbe.direction = 1;    // вдоль Y
        }

        void LateUpdate()
        {
            if (origin == null || controller == null) return;
            if (_cam == null) { _cam = origin.Camera; if (_cam == null) return; }
            if (_headProbe == null) { EnsureProbe(); if (_headProbe == null) return; }

            Vector3 c = origin.CameraInOriginSpacePos;

            // 1) Главный контроллер — пол->голова, как раньше (для стика/гравитации). Форму не трогаем.
            float h = Mathf.Clamp(origin.CameraInOriginSpaceHeight, minHeight, maxHeight);
            controller.height = h;
            controller.center = new Vector3(c.x, h * 0.5f + controller.skinWidth, c.z);

            // 2) Пробник головы — центр ПО КАМЕРЕ (едет за головой, не растягивается от пола).
            float hh = Mathf.Max(headHeight, headRadius * 2f);
            _headProbe.radius = headRadius;
            _headProbe.height = hh;
            _headProbe.center = c; // локально в origin: прямо на уровне головы

            for (int pass = 0; pass < solverIterations; pass++)
            {
                if (!ResolveOnce(hh)) break;
            }
        }

        bool ResolveOnce(float hh)
        {
            Vector3 centerWorld = origin.transform.TransformPoint(_headProbe.center);
            float radius = _headProbe.radius;
            float half = Mathf.Max(0f, hh * 0.5f - radius);
            Vector3 up = origin.transform.up;
            Vector3 top = centerWorld + up * half;
            Vector3 bottom = centerWorld - up * half;

            Collider[] cols = Physics.OverlapCapsule(top, bottom, radius, collisionLayers, QueryTriggerInteraction.Ignore);
            bool moved = false;
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null || col == controller || col == _headProbe) continue;
                if (col.transform.IsChildOf(origin.transform)) continue; // своё тело/риг

                Vector3 dir;
                float dist;
                if (Physics.ComputePenetration(
                        _headProbe, _headProbe.transform.position, _headProbe.transform.rotation,
                        col, col.transform.position, col.transform.rotation,
                        out dir, out dist))
                {
                    Vector3 push = dir * dist;
                    push.y = 0f; // только горизонталь — не подбрасывать и не топить игрока
                    if (push.sqrMagnitude > 1e-8f)
                    {
                        origin.transform.position += push;
                        moved = true;
                    }
                }
            }
            return moved;
        }

        void OnDisable()
        {
            if (_headProbe != null)
            {
                if (Application.isPlaying) Destroy(_headProbe.gameObject);
                else DestroyImmediate(_headProbe.gameObject);
                _headProbe = null;
            }
        }
    }
}
