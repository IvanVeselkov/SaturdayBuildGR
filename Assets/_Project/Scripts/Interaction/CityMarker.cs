using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Точка города на карте лобби. XRSimpleInteractable на слое Map — навёл луч (любой рукой) + select →
    /// сообщаем CityRouteMap, который решает, можно ли войти (кликабелен только текущий жёлтый город).
    /// Видимостью, цветом (жёлтый текущий / зелёный пройден / скрыт) и подписью управляет CityRouteMap.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class CityMarker : MonoBehaviour
    {
        [Tooltip("Город, который представляет эта точка.")]
        [SerializeField] private CityData city;

        [Tooltip("Рендерер маркера для окраски по состоянию (по умолчанию — с этого объекта).")]
        [SerializeField] private Renderer highlightRenderer;

        private XRSimpleInteractable _interactable;

        public CityData City => city;
        public Renderer MarkerRenderer => highlightRenderer;
        public XRSimpleInteractable Interactable => _interactable;

        /// <summary>Назначить город в рантайме/редакторе (для клонирования маркеров).</summary>
        public void SetCity(CityData value) => city = value;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            if (highlightRenderer == null)
                highlightRenderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (CityRouteMap.Instance != null)
                CityRouteMap.Instance.OnMarkerClicked(this);
        }
    }
}
