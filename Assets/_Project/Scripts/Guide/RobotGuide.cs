using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Guide
{
    /// <summary>
    /// Робот-гид (EVE). Конечный автомат тура по городу (Follow / GoToExhibit / PrePlaced),
    /// beat пряток артефакта (fade + активация), затем offer-quiz на экране.
    /// Поворотом рулит ТОЛЬКО этот скрипт (agent.updateRotation=false): в движении смотрит
    /// по ходу, стоя — на игрока. faceYawOffset правит разворот модели (её лицо может быть не по +Z).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public class RobotGuide : MonoBehaviour
    {
        [Header("Ссылки")]
        public NavMeshAgent agent;
        public AudioSource audioSource;
        [Tooltip("Трансформ-ориентир (экран EVE). Пусто = этот объект.")]
        public Transform screen;
        [Tooltip("Корень экранного UI (кнопка викторины). Прячется до нужного момента.")]
        public GameObject screenUI;
        public Button quizButton;

        [Header("Город / артефакт")]
        public CityData city;
        public GameObject hiddenArtifact;
        public Transform hidePoint;

        [Header("Настройки")]
        public float followStandoff = 1.0f;
        public float followResumeMargin = 0.4f;
        public float turnSpeed = 360f;
        [Tooltip("Поправка разворота модели, град. Если EVE смотрит спиной — поставь 180.")]
        public float faceYawOffset = 90f;
        public float hintDelay = 60f;
        public AudioClip hideLine;
        public AudioClip hintLine;

        [Header("Тест")]
        public bool autoStart = true;
        public bool verboseLog = true;

        Transform player;
        readonly List<GuideStop> stops = new List<GuideStop>();
        bool artifactCollected;
        bool running;

        void Reset() { agent = GetComponent<NavMeshAgent>(); audioSource = GetComponent<AudioSource>(); }

        void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (screen == null) screen = transform;
            if (agent != null) agent.updateRotation = false; // поворотом рулим сами
        }

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnArtifactCollected += HandleCollected;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnArtifactCollected -= HandleCollected;
        }

        void HandleCollected(CityData c)
        {
            if (city == null || c == city) { artifactCollected = true; Log("артефакт собран -> предложу викторину"); }
        }

        bool ArtifactCollected()
        {
            if (artifactCollected) return true;
            var gm = GameManager.Instance;
            if (gm != null && city != null && gm.IsArtifactCollected(city)) artifactCollected = true;
            return artifactCollected;
        }

        IEnumerator Start()
        {
            var cam = Camera.main;
            player = cam != null ? cam.transform : null;
            if (player == null) Log("ВНИМАНИЕ: Camera.main не найдена — следование/подход не будут работать.");

            // подписка ещё и здесь: OnEnable мог отработать до Awake GameManager (гонка порядка выполнения)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnArtifactCollected -= HandleCollected;
                GameManager.Instance.OnArtifactCollected += HandleCollected;
            }

            stops.Clear();
            stops.AddRange(FindObjectsByType<GuideStop>(FindObjectsInactive.Exclude));
            stops.Sort((a, b) => a.order.CompareTo(b.order));
            Log("остановок в туре: " + stops.Count);

            if (hiddenArtifact != null) hiddenArtifact.SetActive(false);
            if (screenUI != null) screenUI.SetActive(false);

            if (autoStart) yield return RunTour();
        }

        public IEnumerator RunTour()
        {
            if (running) yield break;
            running = true;
            for (int i = 0; i < stops.Count; i++)
            {
                Log("остановка #" + i + " (" + stops[i].mode + ")");
                yield return RunStop(stops[i]);
            }
            Log("тур завершён, жду сбор артефакта");
            yield return new WaitUntil(ArtifactCollected);
            yield return OfferQuiz();
            running = false;
        }

        IEnumerator RunStop(GuideStop stop)
        {
            if (stop.mode == GuideStop.StopMode.Follow)
            {
                float dur = ClipLen(stop);
                Narrate(stop);
                float t = 0f;
                while (t < dur) { FollowPlayer(); t += Time.deltaTime; yield return null; }
                StopAgent();
            }
            else
            {
                if (stop.mode == GuideStop.StopMode.GoToExhibit)
                {
                    if (agent != null) agent.isStopped = false;
                    Vector3 dest;
                    if (SamplePos(stop.transform.position, out dest) && agent != null) agent.SetDestination(dest);
                    while (Pathing()) { FaceMove(); yield return null; }
                    StopAgent();
                }
                else
                {
                    // PrePlaced: гид уже стоит у экспоната. Если не там — телепорт (в бою робот там изначально/это другой экземпляр).
                    if (agent != null && agent.isOnNavMesh && PlanarDist(transform.position, stop.transform.position) > 0.5f)
                        agent.Warp(stop.transform.position);
                    StopAgent();
                }

                // ждём подхода игрока, лицом к нему
                while (player != null && PlanarDist(player.position, stop.transform.position) > stop.approachRadius)
                {
                    if (stop.facesPlayerWhileWaiting) FaceTowards(player.position);
                    yield return null;
                }
                FaceTowards(player != null ? player.position : transform.position + transform.forward);
                float dur = ClipLen(stop);
                Narrate(stop);
                float t = 0f;
                while (t < dur) { FaceTowards(player != null ? player.position : transform.position + transform.forward); t += Time.deltaTime; yield return null; }
            }

            if (stop.hideArtifactAfter) yield return HideArtifactBeat();
        }

        IEnumerator HideArtifactBeat()
        {
            Log("beat: прячу артефакт");
            if (hideLine != null) { audioSource.PlayOneShot(hideLine); yield return new WaitForSeconds(hideLine.length); }
            else { Beep(); yield return new WaitForSeconds(1.0f); }

            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeOut();

            if (hiddenArtifact != null)
            {
                if (hidePoint != null) hiddenArtifact.transform.SetPositionAndRotation(hidePoint.position, hidePoint.rotation);
                hiddenArtifact.SetActive(true);
                Log("артефакт активирован в укрытии");
            }

            if (fader != null) yield return fader.FadeIn();

            float t = 0f;
            while (!ArtifactCollected())
            {
                t += Time.deltaTime;
                if (t >= hintDelay)
                {
                    Log("подсказка: напоминаю, что надо найти");
                    if (hintLine != null) audioSource.PlayOneShot(hintLine); else Beep();
                    t = 0f;
                }
                yield return null;
            }
        }

        IEnumerator OfferQuiz()
        {
            Log("подъезжаю к игроку с предложением викторины");
            Vector3 dest;
            if (player != null && SamplePos(player.position, out dest) && agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(dest);
            }
            float guard = 0f;
            while (Pathing() && guard < 6f) { FaceMove(); guard += Time.deltaTime; yield return null; }
            StopAgent();
            FaceTowards(player != null ? player.position : transform.position + transform.forward);
            if (screenUI != null) screenUI.SetActive(true);
            Log("экран EVE: кнопка 'Пройти викторину' показана");
        }

        public void GoQuiz()
        {
            if (SceneLoader.Instance != null && city != null && !string.IsNullOrEmpty(city.quizSceneName))
                SceneLoader.Instance.GoToQuiz(city);
            else
                Log("GoToQuiz (в песочнице нет сцены викторины) city=" + (city != null ? city.name : "null"));
        }

        // ---------- helpers ----------
        float ClipLen(GuideStop s) { return s.narration != null ? s.narration.length : Mathf.Max(0.1f, s.placeholderDuration); }

        void Narrate(GuideStop s) { if (s.narration != null) audioSource.PlayOneShot(s.narration); else Beep(); }

        void FollowPlayer()
        {
            if (player == null || agent == null) return;
            Vector3 to = transform.position - player.position; to.y = 0f;
            float d = to.magnitude;
            if (d > followStandoff + followResumeMargin)
            {
                agent.isStopped = false;
                Vector3 dir = d > 0.001f ? to.normalized : transform.forward;
                Vector3 target = player.position + dir * followStandoff;
                Vector3 dest;
                if (SamplePos(target, out dest)) agent.SetDestination(dest);
                FaceMove();
            }
            else
            {
                StopAgent();
                FaceTowards(player.position);
            }
        }

        // Повернуться ЛИЦОМ к точке (с поправкой faceYawOffset).
        void FaceTowards(Vector3 worldPoint)
        {
            Vector3 dir = worldPoint - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, faceYawOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        // Повернуться по направлению движения (с поправкой faceYawOffset).
        void FaceMove()
        {
            if (agent == null) return;
            Vector3 v = agent.velocity; v.y = 0f;
            if (v.sqrMagnitude < 0.01f) return;
            Quaternion want = Quaternion.LookRotation(v) * Quaternion.Euler(0f, faceYawOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        bool Pathing()
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return true;
            return agent.remainingDistance > agent.stoppingDistance + 0.05f;
        }

        void StopAgent() { if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; } }

        static float PlanarDist(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        static bool SamplePos(Vector3 p, out Vector3 result)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(p, out hit, 3f, NavMesh.AllAreas)) { result = hit.position; return true; }
            result = p; return false;
        }

        static AudioClip _beep;
        void Beep()
        {
            if (audioSource == null) return;
            if (_beep == null) _beep = MakeBeep();
            audioSource.PlayOneShot(_beep);
        }

        static AudioClip MakeBeep()
        {
            int sr = 44100;
            int len = (int)(sr * 0.15f);
            var c = AudioClip.Create("guide_beep", len, 1, sr, false);
            var data = new float[len];
            for (int i = 0; i < len; i++)
                data[i] = 0.2f * Mathf.Sin(2f * Mathf.PI * 660f * i / sr) * (1f - (float)i / len);
            c.SetData(data, 0);
            return c;
        }

        void Log(string m) { if (verboseLog) Debug.Log("[Guide] " + m); }
    }
}
