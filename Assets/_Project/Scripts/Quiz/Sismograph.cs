using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Sismograph : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform tapeOrigin;
    [SerializeField] private LineRenderer tapeLine;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private TMP_Text resultText;

    [Header("Параметры ленты")]
    [SerializeField] private float tapeSpeed = 0.5f;      // ⬇ медленнее в 2 раза
    [SerializeField] private float tapeLength = 1.0f;     // ⬇ короче
    [SerializeField] private float dotSpeed = 0.5f;       // совпадает с tapeSpeed

    [Header("Фон (мелкий шум)")]
    [SerializeField] private float noiseAmplitude = 0.008f; // совсем мелкий фон
    [SerializeField] private float noiseFrequency = 12f;

    [Header("Пик толчка")]
    [SerializeField] private float peakHeight = 0.15f;      // амплитуда
    [SerializeField] private float peakFrequency = 6f;      // частота колебаний (Гц)
    [SerializeField] private float peakDamping = 2.5f;      // затухание
    [SerializeField] private float peakDuration = 2.5f;     // сколько длится пик

    [Header("Пики (в секундах)")]
    [SerializeField] private float[] peakTimes = { 5f, 10f, 12f, 18f, 23f };



    [Header("События")]
    public UnityEvent OnAllFixed;
    public UnityEvent OnMissed;

    private class PeakRuntime
    {
        public float time;
        public GameObject dot;
        public bool spawned;
        public bool fixedDone;
        public bool consumed;
    }

    private bool _isRunning;
    private float _time;
    private int _currentScore;
    private int _totalPeaks;
    private readonly List<PeakRuntime> _peaks = new List<PeakRuntime>();

    // сегменты ленты (фиксированные мировые Y, лента едет по времени)
    private const int SEGMENTS = 200; // было 60

    private void Awake()
    {
        _totalPeaks = peakTimes.Length;
        if (tapeLine != null) tapeLine.positionCount = 0;
        UpdateUI();
    }

    public void StartAttempt()
    {
        if (_isRunning) return;
        ResetAttempt();
        _isRunning = true;
        StartCoroutine(AttemptRoutine());
    }

    private void ResetAttempt()
    {
        foreach (var p in _peaks)
            if (p.dot != null) Destroy(p.dot);
        _peaks.Clear();

        foreach (float t in peakTimes)
            _peaks.Add(new PeakRuntime { time = t });

        _time = 0f;
        _currentScore = 0;

        if (tapeLine != null) tapeLine.positionCount = 0;
        UpdateUI();
    }

    private IEnumerator AttemptRoutine()
    {
        float lastPeak = peakTimes[peakTimes.Length - 1];
        float endTime = lastPeak + tapeLength / tapeSpeed + 0.5f;

        while (_time < endTime)
        {
            _time += Time.deltaTime;

            DrawTape();
            SpawnPeaksIfNeeded();
            UpdateDots();

            yield return null;
        }

        EndAttempt();
    }

    // ==== ОТРИСОВКА ЛЕНТЫ ====
    private void DrawTape()
    {
        if (tapeLine == null) return;

        tapeLine.positionCount = SEGMENTS;

        Vector3 origin = tapeOrigin.position;
        float travelTime = tapeLength / tapeSpeed; // сколько секунд "умещается" в длине ленты

        for (int i = 0; i < SEGMENTS; i++)
        {
            float t = i / (float)(SEGMENTS - 1);          // 0 (верх) .. 1 (низ)
            float y = -t * tapeLength;

            // "возраст" сигнала в этой точке ленты
            float signalTime = _time - t * travelTime;

            // X в этой точке = смещение пера в момент signalTime
            float x = SampleSignal(signalTime);

            tapeLine.SetPosition(i, origin + new Vector3(x, y, 0f));
        }
    }

    // ==== СИГНАЛ: ровно + резкий пик ====
    private float SampleSignal(float time)
    {
        // 1. Мелкий фоновый шум (едва заметный)
        float x = Mathf.Sin(time * noiseFrequency) * noiseAmplitude;
        x += Mathf.Sin(time * noiseFrequency * 2.7f) * noiseAmplitude * 0.5f;

        // 2. Толчки — затухающая синусоида (маятник)
        foreach (var p in _peaks)
        {
            float d = time - p.time;

            // пик существует только "после" толчка, до этого — ничего
            if (d < 0f) continue;
            if (d > peakDuration) continue;  // затух

            // затухание: exp(-d * damping)
            float envelope = Mathf.Exp(-d * peakDamping);

            // само колебание: sin(d * frequency * 2π)
            // НО: в момент d=0 нужно резко "ударить" — начинаем с максимума
            // cos вместо sin даёт максимум в d=0
            float oscillation = Mathf.Cos(d * peakFrequency * Mathf.PI * 2f);

            x += peakHeight * envelope * oscillation;
        }

        return x;
    }

    // ==== СПАВН ТОЧЕК НА ПИКАХ ====
    private void SpawnPeaksIfNeeded()
    {
        foreach (var p in _peaks)
        {
            if (p.spawned) continue;
            if (_time < p.time) continue;

            p.spawned = true;

            // X = вершина пика (ровно peakHeight)
            // Y = верхний край ленты
            Vector3 spawnPos = tapeOrigin.position + new Vector3(peakHeight, 0f, 0f);
            if (dotPrefab != null)
                p.dot = Instantiate(dotPrefab, spawnPos, Quaternion.identity);
        }
    }

    // ==== ДВИЖЕНИЕ ТОЧЕК ВМЕСТЕ С ЛЕНТОЙ ====
    private void UpdateDots()
    {
        float bottomY = tapeOrigin.position.y - tapeLength;

        foreach (var p in _peaks)
        {
            if (p.dot == null || p.consumed) continue;

            p.dot.transform.position += Vector3.down * dotSpeed * Time.deltaTime;

            if (p.dot.transform.position.y <= bottomY)
            {
                Destroy(p.dot);
                p.dot = null;
                p.consumed = true;
            }
        }
    }

    // ==== ФИКСАЦИЯ ====
    public void TryFix()
    {
        if (!_isRunning) return;

        foreach (var p in _peaks)
        {
            if (p.fixedDone || p.consumed || p.dot == null) continue;

            p.fixedDone = true;
            p.consumed = true;
            _currentScore++;

            Destroy(p.dot);
            p.dot = null;

            Debug.Log($"✅ Зафиксирован пик на {p.time}с");
            UpdateUI();
            return;
        }

        Debug.Log("❌ Нет активных точек");
    }

    // ==== КОНЕЦ ====
    private void EndAttempt()
    {
        _isRunning = false;

        foreach (var p in _peaks)
        {
            if (p.dot != null) Destroy(p.dot);
            p.dot = null;
        }

        if (_currentScore == _totalPeaks)
        {
            Debug.Log("🏆 Всё зафиксировано!");
            OnAllFixed?.Invoke();
        }
        else
        {
            Debug.Log($"💀 {_currentScore}/{_totalPeaks}");
            OnMissed?.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (resultText != null)
            resultText.text = $"{_currentScore}/{_totalPeaks}";
    }
}