using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GoldenRing.Core;

namespace GoldenRing.Guide
{
    /// <summary>
    /// Зонно-триггерный гид по городу (EVE). Экскурсия строится из остановок:
    /// EVE встаёт на evaPoint, игрок входит в зону активации (zone) — EVE проигрывает реплику (clip).
    /// Порядок: вступление (intro) → экспонаты (stops) → задание найти артефакт (taskClip) →
    /// ожидание сбора артефакта → финал (finaleClip) → кнопка «Пройти викторину».
    /// Движение — простым transform'ом (без NavMesh), прыжок снаружи-внутрь делается мгновенно (teleportToPoint).
    /// Детект игрока в зоне — через bounds коллайдера (без зависимости от триггер-callback'ов).
    /// </summary>
    public class EvaZoneGuide : MonoBehaviour
    {
        [System.Serializable]
        public class Stop
        {
            public string label;
            [Tooltip("Куда встать EVE для этой реплики.")]
            public Transform evaPoint;
            [Tooltip("Зона: игрок вошёл сюда — EVE начинает рассказ.")]
            public Collider zone;
            [Tooltip("Реплика (аудиоклип).")]
            public AudioClip clip;
            [Tooltip("Мгновенно переместиться на точку (для прыжка снаружи внутрь комнаты).")]
            public bool teleportToPoint;
        }

        [Header("Ссылки")]
        public AudioSource audioSource;
        public CityData city;
        [Tooltip("Корень экранного UI EVE (кнопка викторины). Прячется до финала.")]
        public GameObject screenUI;
        public Button quizButton;

        [Header("Сценарий")]
        [Tooltip("Вступление у входа: StartEVAPoint + зона разговора + клип.")]
        public Stop intro;
        [Tooltip("Экспонаты по порядку (Цейс → метеорит → Спутник).")]
        public List<Stop> stops = new List<Stop>();

        [Header("Финал")]
        [Tooltip("Реплика-задание «найди гостя из космоса».")]
        public AudioClip taskClip;
        [Tooltip("Финальная реплика перед викториной.")]
        public AudioClip finaleClip;
        [Tooltip("Артефакт-метеорит в комнате (ArtifactPickup). Ждём его сбора игроком.")]
        public GameObject artifact;
        [Tooltip("Необязательно: объект-двойник, который скрыть в момент показа артефакта (напр. линза на стенде, которая 'упала' и исчезает — а на полу появляется настоящая).")]
        public GameObject decoyToHideOnReveal;

        [Header("Движение / поворот")]
        [Tooltip("Включить движение по NavMesh (agent.SetDestination). Выключено = идти по прямой между путевыми точками (evaPoint'ы и вейпоинты задают маршрут вручную).")]
        public bool useNavMesh = false;
        [Tooltip("NavMeshAgent EVE (используется только при useNavMesh).")]
        public NavMeshAgent agent;
        public float moveSpeed = 1.5f;
        public float turnSpeed = 360f;
        [Tooltip("Поправка разворота модели (как в RobotGuide). Для нашей EVE = 90.")]
        public float faceYawOffset = 90f;

        [Header("Отладка")]
        public bool verboseLog = true;

        private Transform player;
        private bool artifactCollected;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (useNavMesh && agent == null) agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnArtifactCollected += OnCollected;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnArtifactCollected -= OnCollected;
        }

        private void OnCollected(CityData c)
        {
            if (city == null || c == city) { artifactCollected = true; Log("артефакт собран"); }
        }

        private IEnumerator Start()
        {
            var cam = Camera.main;
            player = cam != null ? cam.transform : null;
            if (player == null) Log("ВНИМАНИЕ: Camera.main не найдена.");

            if (screenUI != null) screenUI.SetActive(false);

            // дублируем подписку (OnEnable мог отработать до Awake GameManager)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnArtifactCollected -= OnCollected;
                GameManager.Instance.OnArtifactCollected += OnCollected;
            }

            // Дождаться конца перехода: загрузчик переносит риг в SpawnPoint.
            // Иначе риг ещё в origin (внутри зоны разговора) и intro сработает преждевременно.
            float settle = 0f;
            while (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning && settle < 6f)
            {
                settle += Time.deltaTime;
                yield return null;
            }
            yield return null; // лишний кадр на устаканивание рига

            // Настройка агента: поворотом рулим сами; стартово агент ВЫКЛЮЧЕН
            // (EVE стоит снаружи, вне NavMesh — включим при первом прыжке внутрь через Warp).
            if (useNavMesh && agent != null)
            {
                agent.updateRotation = false;
                agent.updateUpAxis = false;
                agent.speed = moveSpeed;
                agent.angularSpeed = 0f;
                agent.stoppingDistance = 0.15f;
                agent.enabled = false;
            }

            // Ревизит: артефакт уже собран — тур не гоняем, метеорит прячем.
            if (GameManager.Instance != null && city != null && GameManager.Instance.IsArtifactCollected(city))
            {
                if (artifact != null) artifact.SetActive(false);
                Log("ревизит: артефакт уже собран, тур пропущен");
                yield break;
            }

            // Артефакт нельзя брать, пока EVE не скажет, что искать (включим после taskClip).
            SetArtifactGrabbable(false);

            // EVE на стартовой точке (у входа)
            if (intro != null && intro.evaPoint != null)
                transform.position = intro.evaPoint.position;

            // Вступление
            if (intro != null)
            {
                yield return WaitPlayerInZone(intro.zone);
                yield return Narrate(intro.clip, "intro");
            }

            // Экспонаты по порядку
            for (int i = 0; i < stops.Count; i++)
            {
                var s = stops[i];
                if (s.evaPoint != null)
                {
                    if (s.teleportToPoint) WarpTo(s.evaPoint.position);
                    else yield return MoveTo(s.evaPoint.position);
                }
                // Вейпоинт (без зоны и без клипа) — просто идём дальше.
                if (s.zone != null) yield return WaitPlayerInZone(s.zone);
                if (s.clip != null) yield return Narrate(s.clip, s.label);
            }

            // Задание — найти артефакт
            if (taskClip != null) yield return Narrate(taskClip, "task");

            // EVE сказала, что искать — показываем артефакт (если был спрятан — через затемнение).
            yield return RevealArtifact();

            // Ждём, пока игрок найдёт и возьмёт метеорит
            Log("жду сбор артефакта");
            while (!ArtifactCollected()) { FacePlayer(); yield return null; }

            // Финал + кнопка викторины
            if (finaleClip != null) yield return Narrate(finaleClip, "finale");
            if (screenUI != null) screenUI.SetActive(true);
            Log("экран EVE: кнопка 'Пройти викторину' показана");
        }

        /// <summary>Вызывается кнопкой викторины на экране EVE.</summary>
        public void GoQuiz()
        {
            if (SceneLoader.Instance != null && city != null && !string.IsNullOrEmpty(city.quizSceneName))
                SceneLoader.Instance.GoToQuiz(city);
            else
                Log("GoToQuiz: нет SceneLoader/quizSceneName");
        }

        private bool ArtifactCollected()
        {
            if (artifactCollected) return true;
            var gm = GameManager.Instance;
            if (gm != null && city != null && gm.IsArtifactCollected(city)) artifactCollected = true;
            return artifactCollected;
        }

        private IEnumerator WaitPlayerInZone(Collider zone)
        {
            if (zone == null) yield break;
            while (true)
            {
                if (player != null)
                {
                    Vector3 p = player.position;
                    p.y = zone.bounds.center.y; // игнорируем разницу по высоте
                    if (zone.bounds.Contains(p)) yield break;
                }
                FacePlayer();
                yield return null;
            }
        }

        private IEnumerator Narrate(AudioClip clip, string label)
        {
            float dur = clip != null ? clip.length : 1.5f;
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
            Log("реплика '" + label + "' (" + dur.ToString("0.0") + "с)");
            float t = 0f;
            while (t < dur) { FacePlayer(); t += Time.deltaTime; yield return null; }
        }

        /// <summary>Мгновенно оказаться на точке: через NavMesh (Warp) или transform'ом.</summary>
        private void WarpTo(Vector3 p)
        {
            if (useNavMesh && agent != null)
            {
                if (!agent.enabled) agent.enabled = true;
                if (!agent.Warp(p)) transform.position = p;
            }
            else transform.position = p;
        }

        private IEnumerator MoveTo(Vector3 target)
        {
            // По NavMesh (обход стен/стендов), если агент готов.
            if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target);
                while (agent.pathPending) yield return null;
                while (agent.remainingDistance > agent.stoppingDistance + 0.1f)
                {
                    FaceMove();
                    yield return null;
                }
                agent.isStopped = true;
                yield break;
            }

            // Запасной путь: простой transform (без NavMesh).
            target.y = transform.position.y;
            while (Vector3.Distance(transform.position, target) > 0.15f)
            {
                Vector3 dir = target - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion want = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, faceYawOffset, 0f);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                }
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>Смотреть по направлению движения агента.</summary>
        private void FaceMove()
        {
            if (agent == null) return;
            Vector3 v = agent.velocity; v.y = 0f;
            if (v.sqrMagnitude < 0.01f) return;
            Quaternion want = Quaternion.LookRotation(v) * Quaternion.Euler(0f, faceYawOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        private void FacePlayer()
        {
            if (player == null) return;
            Vector3 dir = player.position - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, faceYawOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Показать артефакт после рассказа. Если он был ВЫКЛЮЧЕН в сцене (спрятан) —
        /// включаем его под короткое затемнение ScreenFader («магический» показ, дизайн 29.09).
        /// Города, где артефакт виден с самого начала, фейд не трогает (обычный путь).
        /// </summary>
        private IEnumerator RevealArtifact()
        {
            bool hidden = artifact != null && !artifact.activeSelf;
            var fader = ScreenFader.Instance;
            if (hidden && fader != null)
            {
                Log("показ артефакта через затемнение");
                yield return fader.FadeOut();                 // экран в чёрный
                if (decoyToHideOnReveal != null) decoyToHideOnReveal.SetActive(false); // убрать двойника (линза со стенда)
                SetArtifactGrabbable(true);                   // включаем артефакт + grab в темноте
                yield return new WaitForSecondsRealtime(0.3f);// короткая пауза в темноте
                yield return fader.FadeIn();                  // проявляем
            }
            else
            {
                if (decoyToHideOnReveal != null) decoyToHideOnReveal.SetActive(false);
                SetArtifactGrabbable(true);                   // обычный путь (как было)
            }
        }

        /// <summary>Включить/выключить возможность взять артефакт рукой.</summary>
        private void SetArtifactGrabbable(bool on)
        {
            if (artifact == null) return;
            // Артефакт мог быть выключен в сцене (спрятан на время тура) — показываем при разблокировке.
            // Только включаем, никогда не выключаем: города с изначально активным артефактом не затрагиваются.
            if (on && !artifact.activeSelf) artifact.SetActive(true);
            var grab = artifact.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = on;
            Log("артефакт " + (on ? "можно брать" : "заблокирован до рассказа"));
        }

        private void Log(string m) { if (verboseLog) Debug.Log("[EvaZoneGuide] " + m); }
    }
}
