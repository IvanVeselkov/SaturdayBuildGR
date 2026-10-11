using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoldenRing.Core
{
    /// <summary>
    /// Загрузчик сцен «Научного кольца». Живёт в постоянной Bootstrap-сцене рядом с GameManager
    /// и ригом (PlayerRig), переживает загрузки (DontDestroyOnLoad).
    ///
    /// Модель: Bootstrap — постоянная сцена (менеджеры + XR Origin). Контентная сцена (лобби/город/
    /// викторина) грузится additive ПОВЕРХ и по переходу выгружается. Всегда открыта максимум одна
    /// контентная сцена. Между переходами экран затемняется через ScreenFader (fade-сфера на камере),
    /// поэтому пересоздание объектов (в т.ч. артефакт в руке) игрок не видит.
    ///
    /// Порядок перехода: FadeOut → выгрузка текущей → additive-загрузка новой → SetActiveScene →
    /// перенос рига в SpawnPoint новой сцены → кадр на Start сцены (спавнеры) → FadeIn.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Сцены")]
        [Tooltip("Имя сцены лобби (должна быть в Build Settings).")]
        [SerializeField] private string lobbySceneName = "Lobby";

        [Tooltip("Грузить сцену на старте автоматически (для отладки без лобби-кнопок).")]
        [SerializeField] private bool loadOnStart = false;

        [Tooltip("Что грузить на старте, если loadOnStart включён (пусто = лобби).")]
        [SerializeField] private string startSceneName = "";

        [Tooltip("Имя сцены финала (должна быть в Build Settings).")]
        [SerializeField] private string finaleSceneName = "Finale";

        /// <summary>Имя текущей загруженной контентной сцены (или пусто).</summary>
        public string CurrentContentScene { get; private set; }

        /// <summary>Идёт ли переход прямо сейчас (чтобы не запускать второй).</summary>
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

private IEnumerator Start()
        {
            if (loadOnStart)
            {
                // дать GameManager/PlayerRig проснуться
                yield return null;
                string target = string.IsNullOrEmpty(startSceneName) ? lobbySceneName : startSceneName;
                Debug.Log($"[SceneLoader] Авто-загрузка стартовой сцены '{target}'.");
                GoToScene(target);
            }
        }

        // ================= ПУБЛИЧНОЕ API =================

        /// <summary>Перейти в лобби.</summary>
        public void GoToLobby() => GoToScene(lobbySceneName);

        /// <summary>Перейти в сцену финала (замыкание Научного кольца).</summary>
        public void GoToFinale() => GoToScene(finaleSceneName);

        /// <summary>Перейти в город по его данным.</summary>
        public void GoToCity(CityData city)
        {
            if (city == null || string.IsNullOrEmpty(city.citySceneName))
            {
                Debug.LogWarning("[SceneLoader] GoToCity: город или его citySceneName не заданы.", this);
                return;
            }
            // Старт таймера прохождения при первом входе в город (идёт до замыкания кольца).
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyCityEntered();
            GoToScene(city.citySceneName);
        }

        /// <summary>Перейти в сцену викторины города.</summary>
        public void GoToQuiz(CityData city)
        {
            if (city == null || string.IsNullOrEmpty(city.quizSceneName))
            {
                Debug.LogWarning("[SceneLoader] GoToQuiz: город или его quizSceneName не заданы.", this);
                return;
            }
            GoToScene(city.quizSceneName);
        }

        /// <summary>Перейти в произвольную сцену по имени (должна быть в Build Settings).</summary>
        public void GoToScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] GoToScene: пустое имя сцены.", this);
                return;
            }
            if (IsTransitioning)
            {
                Debug.LogWarning($"[SceneLoader] Переход уже идёт, '{sceneName}' проигнорирован.", this);
                return;
            }
            StartCoroutine(TransitionRoutine(sceneName));
        }

        // ================= ПЕРЕХОД =================

private IEnumerator TransitionRoutine(string sceneName)
        {
            IsTransitioning = true;

            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.FadeOut();

            yield return UnloadCurrentContentScene();
            yield return LoadContentScene(sceneName);

            if (fader != null)
                yield return fader.FadeIn();

            IsTransitioning = false;
        }

        /// <summary>Найти SpawnPoint только в пределах указанной сцены (не в Bootstrap).</summary>
        private SpawnPoint FindSpawnPointInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var sp = roots[i].GetComponentInChildren<SpawnPoint>(true);
                if (sp != null)
                    return sp;
            }
            return null;
        }
    

/// <summary>Загрузить контентную сцену additive, сделать активной, перенести риг в SpawnPoint.</summary>
        private IEnumerator LoadContentScene(string sceneName)
        {
            var existing = SceneManager.GetSceneByName(sceneName);
            if (!existing.IsValid() || !existing.isLoaded)
            {
                var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    Debug.LogError($"[SceneLoader] Не удалось начать загрузку '{sceneName}'. Она в Build Settings?", this);
                    yield break;
                }
                while (!load.isDone)
                    yield return null;
            }

            var loaded = SceneManager.GetSceneByName(sceneName);
            CurrentContentScene = sceneName;

            if (loaded.IsValid() && loaded.isLoaded)
                SceneManager.SetActiveScene(loaded);

            var spawn = FindSpawnPointInScene(loaded);
            if (spawn != null && PlayerRig.Instance != null)
                PlayerRig.Instance.TeleportTo(spawn.transform);
            else if (spawn == null)
                Debug.LogWarning($"[SceneLoader] В сцене '{sceneName}' нет SpawnPoint — риг не перенесён.", this);

            // Кадр(ы) на Start спавнеров сцены (ArtifactSceneSpawner и т.п.).
            yield return null;
            yield return null;
        }


/// <summary>Выгрузить текущую контентную сцену (если загружена) и обнулить ссылку.</summary>
        private IEnumerator UnloadCurrentContentScene()
        {
            if (!string.IsNullOrEmpty(CurrentContentScene))
            {
                var cur = SceneManager.GetSceneByName(CurrentContentScene);
                if (cur.IsValid() && cur.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(cur);
                    while (unload != null && !unload.isDone)
                        yield return null;
                }
                CurrentContentScene = null;
            }
        }


private IEnumerator RestartRoutine()
        {
            IsTransitioning = true;

            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.FadeOut();

            // Сначала выгружаем текущую сцену: без стендов/сокетов сброс не будет тут же перехвачен.
            yield return UnloadCurrentContentScene();

            if (GameManager.Instance != null)
                GameManager.Instance.ResetProgress();

            yield return LoadContentScene(lobbySceneName);

            if (fader != null)
                yield return fader.FadeIn();

            IsTransitioning = false;
        }


/// <summary>
        /// Перезапуск игры: полный сброс прогресса + возврат в лобби с чистого листа.
        /// Сброс делается ПОД чёрным экраном, когда контентная сцена уже выгружена — иначе стенд-сокет
        /// успевает перехватить только что выплюнутый артефакт и снова пометить его поставленным.
        /// </summary>
        public void RestartToLobby()
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[SceneLoader] Переход уже идёт — RestartToLobby проигнорирован.", this);
                return;
            }
            StartCoroutine(RestartRoutine());
        }
}
}
