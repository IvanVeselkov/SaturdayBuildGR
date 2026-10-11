using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Общий попап карты: по выбору маркера (CityMarker) показывает у нужной точки панель с кнопками
    /// «Отправиться в город» (SceneLoader.GoToCity; неактивна, если город закрыт или нет сцены → надпись «Скоро»)
    /// и «Вернуться к карте» (скрыть попап). Один на всю карту — одновременно открыт максимум один.
    /// Попап позиционируется у маркера и разворачивается лицом к игроку.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public static MapController Instance { get; private set; }

        [Header("Попап")]
        [Tooltip("Корень панели попапа (включается/выключается).")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button travelButton;
        [SerializeField] private TMP_Text travelLabel;
        [SerializeField] private Button backButton;

        [Header("Позиция попапа у маркера")]
        [Tooltip("Смещение попапа от точки маркера (в его локальных осях: X вправо, Y вверх, Z от стены).")]
        [SerializeField] private Vector3 markerLocalOffset = new Vector3(0f, 0.25f, 0.03f);
        [SerializeField] private string travelText = "Отправиться в город";
        [SerializeField] private string lockedText = "Скоро";

        private CityData _current;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (backButton != null) backButton.onClick.AddListener(Hide);
            if (travelButton != null) travelButton.onClick.AddListener(Travel);
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (backButton != null) backButton.onClick.RemoveListener(Hide);
            if (travelButton != null) travelButton.onClick.RemoveListener(Travel);
        }

        /// <summary>Показать попап для выбранного маркера.</summary>
        public void ShowFor(CityMarker marker)
        {
            if (marker == null || popupRoot == null) return;
            _current = marker.City;

            // разместить попап у маркера и развернуть к игроку
            Transform mt = marker.transform;
            popupRoot.transform.position = mt.TransformPoint(markerLocalOffset);
            FacePlayer(popupRoot.transform);

            bool hasScene = _current != null && !string.IsNullOrEmpty(_current.citySceneName);
            bool unlocked = _current != null && (GameManager.Instance == null || GameManager.Instance.IsCityUnlocked(_current));
            bool canTravel = hasScene && unlocked;

            if (titleLabel != null) titleLabel.text = _current != null ? _current.displayName : "—";
            if (travelButton != null) travelButton.interactable = canTravel;
            if (travelLabel != null) travelLabel.text = canTravel ? travelText : lockedText;

            popupRoot.SetActive(true);
        }

        /// <summary>Скрыть попап (кнопка «Вернуться к карте»).</summary>
        public void Hide()
        {
            _current = null;
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        private void Travel()
        {
            if (_current != null && SceneLoader.Instance != null)
                SceneLoader.Instance.GoToCity(_current);
            Hide();
        }

        private void FacePlayer(Transform t)
        {
            var cam = Camera.main;
            if (cam == null) return;
            // читаемая сторона world-space canvas — его +Z; развернём его так, чтобы +Z смотрел на камеру
            Vector3 dir = t.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
