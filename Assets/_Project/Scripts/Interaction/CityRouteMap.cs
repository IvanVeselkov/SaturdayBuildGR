using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Карта-маршрут в лобби. Управляет точками городов, подписями и путями между ними по прогрессу:
    ///  - город ПРОЙДЕН (IsQuizCompleted)      → зелёный маркер, не кликабелен;
    ///  - город ТЕКУЩИЙ (открыт, не пройден)   → жёлтый маркер, клик → сразу в город;
    ///  - город ЕЩЁ ЗАКРЫТ                       → маркер скрыт.
    /// Путь (серая прямая) между двумя соседними городами появляется, когда открывается следующий.
    /// Когда пройдены ВСЕ — все пути становятся золотыми и добавляется замыкающий отрезок (последний→первый).
    /// Подпись с названием города — под каждым видимым маркером.
    /// Вешать на объект Map (родитель маркеров Marker_*). Пути/подписи строятся в рантайме.
    /// </summary>
    public class CityRouteMap : MonoBehaviour
    {
        public static CityRouteMap Instance { get; private set; }

        [Header("Цвета маркеров")]
        [SerializeField] private Color currentColor = new Color(1f, 0.78f, 0.08f, 1f);    // жёлтый — текущий
        [SerializeField] private Color completedColor = new Color(0.20f, 0.80f, 0.25f, 1f); // зелёный — пройден

        [Header("Пути")]
        [SerializeField] private Color pathColor = new Color(0.62f, 0.62f, 0.66f, 1f);     // серый путь
        [SerializeField] private Color goldColor = new Color(1f, 0.82f, 0.20f, 1f);        // золото в финале
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float lineZOffset = 0.02f;   // чуть к стене, чтобы маркеры были перед линией

        [Header("Подписи")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private float labelDrop = 0.24f;     // ниже маркера
        [SerializeField] private float labelDepth = -0.03f;   // чуть перед стеной (к игроку)
        [SerializeField] private float labelScale = 0.12f;
        [SerializeField] private float labelFontSize = 22f;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField] private Vector2 labelRectSize = new Vector2(22f, 4f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<CityMarker> _markers = new List<CityMarker>();     // по порядку city.order
        private readonly List<TMP_Text> _labels = new List<TMP_Text>();          // заранее созданы в сцене
        private readonly List<LineRenderer> _segments = new List<LineRenderer>(); // i: order i -> i+1; последний — замыкающий
        private Shader _lineShader;
        private bool _built;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnQuizCompleted += OnCityEvent;
                gm.OnCityUnlocked += OnCityEvent;
                gm.OnProgressReset += OnResetEvent;
            }
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnQuizCompleted -= OnCityEvent;
                gm.OnCityUnlocked -= OnCityEvent;
                gm.OnProgressReset -= OnResetEvent;
            }
        }

        private void OnCityEvent(CityData c) => Refresh();
        private void OnResetEvent() => Refresh();

        private void Start()
        {
            Build();
            Refresh();
        }

        private void Build()
        {
            if (_built) return;

            var found = GetComponentsInChildren<CityMarker>(true);
            _markers.Clear();
            foreach (var m in found)
                if (m != null && m.City != null) _markers.Add(m);
            _markers.Sort((a, b) => a.City.order.CompareTo(b.City.order));

            _lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_lineShader == null) _lineShader = Shader.Find("Sprites/Default");

            // Подписи — заранее созданы в сцене (Map/Labels/Label_<id>); здесь только находим их.
            _labels.Clear();
            foreach (var m in _markers)
            {
                Transform lt = transform.Find("Labels/Label_" + m.City.id);
                TMP_Text tmp = lt != null ? lt.GetComponent<TMP_Text>() : null;
                if (tmp == null)
                    Debug.LogWarning("[CityRouteMap] Нет подписи Labels/Label_" + m.City.id + " в сцене.", this);
                _labels.Add(tmp);
            }

            // Отрезки пути: по одному между соседними (order i -> i+1) + замыкающий (последний -> первый)
            int n = _markers.Count;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Segment_" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.widthMultiplier = lineWidth;
                lr.numCapVertices = 4;
                lr.numCornerVertices = 2;
                lr.material = new Material(_lineShader);
                int a = i;
                int b = (i + 1) % n;
                Vector3 pa = _markers[a].transform.position; pa.z += lineZOffset;
                Vector3 pb = _markers[b].transform.position; pb.z += lineZOffset;
                lr.SetPosition(0, pa);
                lr.SetPosition(1, pb);
                _segments.Add(lr);
            }
            _built = true;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            int n = _markers.Count;
            if (n == 0) return;

            bool allDone = true;
            for (int i = 0; i < n; i++)
                if (!gm.IsQuizCompleted(_markers[i].City)) { allDone = false; break; }

            for (int i = 0; i < n; i++)
            {
                var m = _markers[i];
                var city = m.City;
                bool completed = gm.IsQuizCompleted(city);
                bool unlocked = gm.IsCityUnlocked(city);
                bool visible = completed || unlocked;

                if (m.gameObject.activeSelf != visible) m.gameObject.SetActive(visible);
                if (_labels[i] != null && _labels[i].gameObject.activeSelf != visible)
                    _labels[i].gameObject.SetActive(visible);

                if (visible)
                {
                    SetMarkerColor(m, completed ? completedColor : currentColor);
                    bool clickable = !completed && unlocked && CanEnter(city);
                    if (m.Interactable != null) m.Interactable.enabled = clickable;
                }
            }

            for (int i = 0; i < n; i++)
            {
                var lr = _segments[i];
                bool closing = (i == n - 1);
                bool visible = closing ? allDone : gm.IsCityUnlocked(_markers[i + 1].City);
                lr.enabled = visible;
                SetLineColor(lr, allDone ? goldColor : pathColor);
            }
        }

        private bool CanEnter(CityData city)
        {
            return city != null
                && !string.IsNullOrEmpty(city.citySceneName)
                && Application.CanStreamedLevelBeLoaded(city.citySceneName);
        }

        private void SetMarkerColor(CityMarker m, Color c)
        {
            var r = m.MarkerRenderer;
            if (r == null) return;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);
        }

        private void SetLineColor(LineRenderer lr, Color c)
        {
            lr.startColor = c;
            lr.endColor = c;
            if (lr.material != null)
            {
                if (lr.material.HasProperty(BaseColorId)) lr.material.SetColor(BaseColorId, c);
                if (lr.material.HasProperty(ColorId)) lr.material.SetColor(ColorId, c);
            }
        }

        /// <summary>Клик по маркеру: входим в город только если это текущий (жёлтый) и сцена доступна.</summary>
        public void OnMarkerClicked(CityMarker m)
        {
            var gm = GameManager.Instance;
            if (gm == null || m == null || m.City == null) return;
            var city = m.City;
            bool clickable = !gm.IsQuizCompleted(city) && gm.IsCityUnlocked(city) && CanEnter(city);
            if (!clickable) return;
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GoToCity(city);
        }
    }
}
