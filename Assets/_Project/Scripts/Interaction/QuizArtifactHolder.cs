using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Держатель артефакта в СЦЕНЕ ВИКТОРИНЫ (не стенд лобби!). Игрок кладёт сюда артефакт своего города,
    /// чтобы освободить руки для головоломки. Пока интерактивная викторина НЕ пройдена — артефакт нельзя
    /// забрать: он переводится на спец. interaction-слой, которого нет у рук игрока (сокет=Everything его
    /// держит, а рука взять не может). Как только викторина пройдена (completionIndicator становится активным),
    /// артефакту возвращается обычный слой — игрок забирает его, и это переносит в лобби.
    /// PlaceArtifact тут НЕ вызывается (постановка на стенд — только в лобби).
    /// Вешается на узел с XRSocketInteractor.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class QuizArtifactHolder : MonoBehaviour, IXRSelectFilter
    {
        [Tooltip("Город, чей артефакт принимает держатель.")]
        [SerializeField] private CityData city;

        [Tooltip("Индикатор прохождения интерактивной викторины (панель 'верно'). Активен → держатель разблокируется.")]
        [SerializeField] private GameObject completionIndicator;

        // Слой, которого НЕТ у рук игрока (у рук Default+Map). Сокет (Everything) всё равно держит артефакт,
        // а рука на этом слое взять его не может — так реализуется блокировка до прохождения.
        private const int LockLayerValue = 1 << 20;

        private XRSocketInteractor _socket;
        private bool _unlocked;
        private XRGrabInteractable _held;
        private int _savedLayers = 1; // обычный слой артефакта (Default)

        public bool canProcess => isActiveAndEnabled;

        private void Awake() => _socket = GetComponent<XRSocketInteractor>();

        private void OnEnable()
        {
            _socket.selectFilters.Add(this);
            _socket.selectEntered.AddListener(OnSelectEntered);
            _socket.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _socket.selectFilters.Remove(this);
            _socket.selectEntered.RemoveListener(OnSelectEntered);
            _socket.selectExited.RemoveListener(OnSelectExited);
        }

        // Пускает в гнездо только свой артефакт (по artifactId).
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (city == null) return false;
            var pickup = interactable.transform.GetComponent<ArtifactPickup>();
            return pickup != null && pickup.City != null && pickup.City.artifactId == city.artifactId;
        }

        private void Update()
        {
            if (!_unlocked && completionIndicator != null && completionIndicator.activeInHierarchy)
            {
                _unlocked = true;
                // Разблокировка: вернуть артефакту обычный слой — теперь рука снова может его взять.
                if (_held != null) _held.interactionLayers = _savedLayers;
            }

            // Страж: держим коллайдеры артефакта не-триггерами, пока он в держателе.
            if (_held != null) ForceCollidersSolid(_held);
        }

        private static void ForceCollidersSolid(XRGrabInteractable held)
        {
            if (held == null) return;
            var cols = held.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null || c is MeshCollider) continue; // меши не трогаем
                if (c.isTrigger) c.isTrigger = false;
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            _held = args.interactableObject as XRGrabInteractable;
            if (_held == null) return;

            // Пока артефакт в держателе — его коллайдеры должны быть НЕ триггерами,
            // иначе дальний луч не наводится на него (луч игнорирует триггеры) и взять нельзя.
            ForceCollidersSolid(_held);

            // Держателем управляем сами — отключаем 'липкость' (возврат в руку).
            var sticky = _held.GetComponent<StickyArtifact>();
            if (sticky != null) sticky.enabled = false;

            // Пока викторина не пройдена — заблокировать изъятие рукой сменой interaction-слоя.
            if (!_unlocked)
            {
                _savedLayers = _held.interactionLayers.value;
                _held.interactionLayers = LockLayerValue;
            }
            else
            {
                // Уже разблокировано (на всякий случай) — оставить обычный слой.
                _held.interactionLayers = _savedLayers;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            // Сюда попадаем, когда артефакт покинул держатель. Пока заблокировано, рука взять его не могла,
            // значит реальный выход — только после разблокировки: игрок забрал артефакт → переходим в лобби.
            if (_unlocked && SceneLoader.Instance != null)
                SceneLoader.Instance.GoToLobby();
            _held = null;
        }
    }
}
