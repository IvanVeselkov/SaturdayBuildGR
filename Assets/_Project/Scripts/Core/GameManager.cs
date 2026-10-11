using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Ядро прогресса игры «Научное Золотое кольцо».
    /// Живёт в постоянной (Bootstrap) сцене, переживает загрузку сцен (DontDestroyOnLoad),
    /// поэтому хранит собранные артефакты и разблокировку между сценами.
    ///
    /// Отвечает за:
    ///  - линейную разблокировку городов (город N+1 открывается после викторины города N);
    ///  - хранение собранных и расставленных на стенды артефактов (переживает смену сцены);
    ///  - условие финала (все артефакты на стендах);
    ///  - полный сброс прогресса для повторного прохождения БЕЗ перезапуска приложения.
    ///
    /// Состояние хранится в памяти (по кейсу — сброс без перезапуска), между запусками не сохраняется.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Конфигурация маршрута (список городов по порядку).")]
        [SerializeField] private GameConfig config;

        // --- Рантайм-состояние прогресса ---
        private readonly HashSet<string> _collectedArtifacts = new HashSet<string>(); // взяты игроком
        private readonly HashSet<string> _placedArtifacts = new HashSet<string>();     // поставлены на стенд
        private readonly HashSet<string> _completedQuizzes = new HashSet<string>();    // пройдены викторины (id города)
        private int _highestUnlockedOrder;                                             // до какого порядка открыты города

        // --- Статистика прохождения (для экрана финала) ---
        private int _mistakes;        // суммарно неверных ответов в викторинах (хук RegisterMistake)
        private bool _timerStarted;   // таймер запущен (первый вход в город)
        private bool _timerStopped;   // таймер остановлен (кольцо замкнуто)
        private float _startTime;     // момент старта (unscaled)
        private float _stopTime;      // момент остановки (unscaled)

        // --- События для UI, лобби, финала ---
        public event Action<CityData> OnArtifactCollected;
        public event Action<CityData> OnArtifactPlaced;
        public event Action<CityData> OnQuizCompleted;
        public event Action<CityData> OnCityUnlocked;
        public event Action OnAllArtifactsPlaced;
        public event Action OnProgressReset;
        public event Action<int> OnMistakeRegistered;

        public GameConfig Config => config;

        private void Awake()
        {
            // Синглтон + переживание загрузки сцен
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitProgress();
        }

        /// <summary>Начальное состояние: открыт только первый город, ничего не собрано.</summary>
        public void InitProgress()
        {
            _collectedArtifacts.Clear();
            _placedArtifacts.Clear();
            _completedQuizzes.Clear();
            _highestUnlockedOrder = 0; // открыт только город с order == 0

            // статистика финала
            _mistakes = 0;
            _timerStarted = false;
            _timerStopped = false;
            _startTime = 0f;
            _stopTime = 0f;
        }

        // ============ РАЗБЛОКИРОВКА ГОРОДОВ ============

        /// <summary>Открыт ли город (доступен для входа с карты в лобби).</summary>
        public bool IsCityUnlocked(CityData city)
        {
            if (city == null) return false;
            return city.order <= _highestUnlockedOrder;
        }

        public bool IsCityUnlocked(string cityId) => IsCityUnlocked(config != null ? config.GetById(cityId) : null);

        // ============ АРТЕФАКТЫ ============

        /// <summary>Игрок взял артефакт города (клеится к левой руке). Вызывать при подборе.</summary>
        public void CollectArtifact(CityData city)
        {
            if (city == null || string.IsNullOrEmpty(city.artifactId)) return;
            if (_collectedArtifacts.Add(city.artifactId))
                OnArtifactCollected?.Invoke(city);
        }

        public bool IsArtifactCollected(CityData city)
            => city != null && _collectedArtifacts.Contains(city.artifactId);

        /// <summary>Игрок поставил артефакт на стенд в лобби. Вызывать из Socket-стенда.</summary>
        public void PlaceArtifact(CityData city)
        {
            if (city == null || string.IsNullOrEmpty(city.artifactId)) return;
            if (_placedArtifacts.Add(city.artifactId))
            {
                OnArtifactPlaced?.Invoke(city);
                if (AreAllArtifactsPlaced())
                {
                    StopRunTimer();                 // зафиксировать время прохождения
                    OnAllArtifactsPlaced?.Invoke();  // триггер финала (карта + дверь)
                }
            }
        }

        public bool IsArtifactPlaced(CityData city)
            => city != null && _placedArtifacts.Contains(city.artifactId);

        /// <summary>Все ли артефакты стоят на стендах (условие финала).</summary>
        public bool AreAllArtifactsPlaced()
        {
            if (config == null || config.CityCount == 0) return false;
            return _placedArtifacts.Count >= config.CityCount;
        }

        public int PlacedCount => _placedArtifacts.Count;

        // ============ ВИКТОРИНЫ / ОТКРЫТИЕ СЛЕДУЮЩЕГО ГОРОДА ============

        /// <summary>
        /// Игрок прошёл викторину города. Открывает следующий город кольца.
        /// Вызывать из сцены викторины при успехе.
        /// </summary>
        public void CompleteQuiz(CityData city)
        {
            if (city == null) return;
            _completedQuizzes.Add(city.id);
            OnQuizCompleted?.Invoke(city);

            int nextOrder = city.order + 1;
            if (nextOrder > _highestUnlockedOrder && config != null && nextOrder < config.CityCount)
            {
                _highestUnlockedOrder = nextOrder;
                OnCityUnlocked?.Invoke(config.GetByOrder(nextOrder));
            }
        }

        public bool IsQuizCompleted(CityData city)
            => city != null && _completedQuizzes.Contains(city.id);

        // ============ СБРОС (повторное прохождение без перезапуска) ============

        /// <summary>
        /// Полный сброс прогресса: закрыть все города кроме первого, очистить артефакты.
        /// Подписчики (стенды, лобби, карта) должны очистить визуал по событию OnProgressReset.
        /// </summary>
        public void ResetProgress()
        {
            InitProgress();
            OnProgressReset?.Invoke();
        }

        // ============ СТАТИСТИКА ФИНАЛА (время + ошибки) ============

        /// <summary>
        /// Хук для системы викторин: вызывать на КАЖДЫЙ неверный ответ игрока.
        /// В викторине (пакет Тани) в точке, где определяется «ответ неверный», добавить:
        ///     GameManager.Instance?.RegisterMistake();
        /// Счётчик суммируется по всем городам, показывается на экране финала и сбрасывается рестартом.
        /// </summary>
        public void RegisterMistake()
        {
            _mistakes++;
            OnMistakeRegistered?.Invoke(_mistakes);
        }

        /// <summary>Сколько раз игрок ошибся за прохождение (для экрана статистики финала).</summary>
        public int Mistakes => _mistakes;

        /// <summary>
        /// Запустить таймер прохождения. Вызывает SceneLoader при ПЕРВОМ входе в город.
        /// Повторные вызовы игнорируются — время идёт непрерывно до замыкания кольца.
        /// </summary>
        public void NotifyCityEntered()
        {
            if (_timerStarted) return;
            _timerStarted = true;
            _timerStopped = false;
            _startTime = Time.unscaledTime;
        }

        private void StopRunTimer()
        {
            if (!_timerStarted || _timerStopped) return;
            _timerStopped = true;
            _stopTime = Time.unscaledTime;
        }

        /// <summary>Время прохождения в секундах (с первого города до замыкания кольца).</summary>
        public float ElapsedSeconds
        {
            get
            {
                if (!_timerStarted) return 0f;
                float end = _timerStopped ? _stopTime : Time.unscaledTime;
                return Mathf.Max(0f, end - _startTime);
            }
        }

        /// <summary>
        /// ТОЛЬКО ДЛЯ ТЕСТА: задать готовую статистику (время, ошибки), чтобы проверить экран финала
        /// без реального прохождения всех городов. В боевом потоке не вызывать.
        /// </summary>
        public void DebugSetStatsForTesting(float elapsedSeconds, int mistakes)
        {
            _timerStarted = true;
            _timerStopped = true;
            _startTime = 0f;
            _stopTime = Mathf.Max(0f, elapsedSeconds);
            _mistakes = Mathf.Max(0, mistakes);
        }

        // ============ УДОБНЫЕ ГЕТТЕРЫ ============

        public int CollectedCount => _collectedArtifacts.Count;
        public int CityCount => config != null ? config.CityCount : 0;
    }
}
