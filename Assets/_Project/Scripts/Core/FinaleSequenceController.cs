using System.Collections;
using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Сиквенс финала в ЛОББИ. По событию GameManager.OnAllArtifactsPlaced (все 9 артефактов на стендах):
    ///   1) загорается «кольцо» на карте (ringGlow) + звук;
    ///   2) поднимается раздвижная дверь (door едет вверх на doorRaiseHeight за doorRaiseDuration);
    ///   3) открывается проход — когда игрок ПОДХОДИТ к двери (голова в зоне doorwayZone),
    ///      грузится сцена финала через SceneLoader.GoToFinale().
    ///
    /// Плейсхолдер-геометрия (кольцо-тор, дверь-бокс, зона-коллайдер) заменяется моделями Никиты позже —
    /// логика от вида не зависит. После рестарта лобби пересоздаётся, состояние сбрасывается само.
    /// </summary>
    public class FinaleSequenceController : MonoBehaviour
    {
        [Header("Визуал замыкания кольца")]
        [Tooltip("Объект свечения кольца на карте (выключен на старте).")]
        [SerializeField] private GameObject ringGlow;
        [Tooltip("Доп. объекты, включаемые в финале (подсветка маркеров и т.п.).")]
        [SerializeField] private GameObject[] enableOnFinale;

        [Header("Раздвижная дверь")]
        [SerializeField] private Transform door;
        [SerializeField] private float doorRaiseHeight = 3f;
        [SerializeField] private float doorRaiseDuration = 2f;
        [SerializeField] private float delayBeforeDoor = 1f;

        [Header("Проход в финал")]
        [Tooltip("Зона у дверного проёма: вход головы игрока запускает переход в финал.")]
        [SerializeField] private Collider doorwayZone;

        [Header("Звук (необязательно)")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip ringClip;

        [Header("Створки-модели (Animator)")]
        [Tooltip("Проигрываются при замыкании кольца — и по тест-кнопке, и при реальной установке всех артефактов.")]
        [SerializeField] private DoorAnimation[] doorAnimators;

        [Header("Автопереход в финал")]
        [Tooltip("Уйти в финал после открытия дверей, не дожидаясь подхода игрока к зоне.")]
        [SerializeField] private bool autoGoToFinale = true;
        [Tooltip("Пауза после открытия створок до автоперехода, сек.")]
        [SerializeField] private float finaleDelayAfterDoors = 2.5f;

        [System.Serializable]
        public class DoorAnimation
        {
            [Tooltip("Animator створки.")]
            public Animator animator;
            [Tooltip("Имя состояния открытия (напр. doorLeft / doorRight).")]
            public string openState = "Open";
        }

        private bool _sequenceRan;
        private bool _armed;        // дверь открыта, ждём подхода игрока
        private bool _wentToFinale;

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnAllArtifactsPlaced += HandleAllPlaced;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnAllArtifactsPlaced -= HandleAllPlaced;
        }

        private void Start()
        {
            // исходное состояние — кольцо погашено
            if (ringGlow != null) ringGlow.SetActive(false);

            // Подстраховка: если в лобби зашли уже с полным кольцом — сразу проиграть сиквенс.
            var gm = GameManager.Instance;
            if (gm != null && gm.AreAllArtifactsPlaced())
                HandleAllPlaced();
        }

        private void HandleAllPlaced()
        {
            if (_sequenceRan) return;
            _sequenceRan = true;
            StartCoroutine(RunSequence());
        }

private IEnumerator RunSequence()
        {
            // 1) кольцо замкнулось
            if (ringGlow != null) ringGlow.SetActive(true);
            if (enableOnFinale != null)
                foreach (var go in enableOnFinale)
                    if (go != null) go.SetActive(true);
            if (sfx != null && ringClip != null) sfx.PlayOneShot(ringClip);

            yield return new WaitForSeconds(delayBeforeDoor);

            // 2) открыть створки-модели (DoorPan Left/Right)
            PlayDoorAnimations();

            // 2b) плейсхолдер-дверь (бокс) — поднять лёрпом, если назначена
            if (door != null)
            {
                Vector3 from = door.position;
                Vector3 to = from + Vector3.up * doorRaiseHeight;
                float t = 0f;
                float dur = Mathf.Max(0.01f, doorRaiseDuration);
                while (t < dur)
                {
                    t += Time.deltaTime;
                    door.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
                    yield return null;
                }
                door.position = to;
            }

            // 3) открыть проход и/или автоматически уйти в финал
            _armed = true;
            if (autoGoToFinale)
            {
                yield return new WaitForSeconds(finaleDelayAfterDoors);
                GoToFinaleOnce();
            }
        }

private void PlayDoorAnimations()
        {
            if (doorAnimators == null) return;
            foreach (var d in doorAnimators)
            {
                if (d == null || d.animator == null || string.IsNullOrEmpty(d.openState)) continue;
                d.animator.Play(d.openState, 0, 0f);
            }
        }

        private void GoToFinaleOnce()
        {
            if (_wentToFinale) return;
            _wentToFinale = true;
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GoToFinale();
        }


private void Update()
        {
            if (!_armed || _wentToFinale || doorwayZone == null) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning) return;

            var cam = Camera.main;
            if (cam == null) return;

            if (doorwayZone.bounds.Contains(cam.transform.position))
                GoToFinaleOnce();
        }
    }
}
