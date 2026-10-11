using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Мост «стенд в лобби → прогресс игры».
    /// Вешается на узел Socket рядом с XRSocketInteractor.
    /// Принимает в гнездо ТОЛЬКО свой артефакт (по artifactId) — работает как select-фильтр сокета.
    /// При установке своего артефакта засчитывает постановку в GameManager (тот сам ловит финал).
    /// При сбросе прогресса (OnProgressReset) освобождает гнездо, чтобы визуал совпал с обнулённым прогрессом
    /// — нужно для повторного прохождения без перезапуска приложения.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class StandSocket : MonoBehaviour, IXRSelectFilter
    {
        [Tooltip("Город, чей артефакт принимает этот стенд.")]
        [SerializeField] private CityData city;

        /// <summary>Город стенда.</summary>
        public CityData City => city;

        private XRSocketInteractor _socket;
        private bool _gmHooked;

        // IXRSelectFilter: фильтр активен, пока компонент включён.
        public bool canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _socket = GetComponent<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            if (_socket != null)
            {
                _socket.selectFilters.Add(this);
                _socket.selectEntered.AddListener(OnSelectEntered);
            }
            TryHookGameManager();
        }

        private void Start()
        {
            // GameManager (персистентный из Bootstrap) может появиться позже сокета — доподписываемся здесь.
            TryHookGameManager();
        }

        private void OnDisable()
        {
            if (_socket != null)
            {
                _socket.selectFilters.Remove(this);
                _socket.selectEntered.RemoveListener(OnSelectEntered);
            }
            if (_gmHooked && GameManager.Instance != null)
                GameManager.Instance.OnProgressReset -= HandleProgressReset;
            _gmHooked = false;
        }

        private void TryHookGameManager()
        {
            if (_gmHooked) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnProgressReset += HandleProgressReset;
            _gmHooked = true;
        }

        /// <summary>Сокет пускает в гнездо только артефакт «своего» города.</summary>
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (city == null)
                return false;

            var pickup = interactable.transform.GetComponent<ArtifactPickup>();
            if (pickup == null || pickup.City == null)
                return false;

            return pickup.City.artifactId == city.artifactId;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (city == null)
                return;

            // PlaceArtifact идемпотентен; при постановке последнего артефакта GameManager сам поднимет OnAllArtifactsPlaced.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaceArtifact(city);
                // Постановка артефакта на подиум в лобби ОТКРЫВАЕТ следующий город (CompleteQuiz идемпотентен).
                GameManager.Instance.CompleteQuiz(city);
            }
            else
                Debug.Log($"[StandSocket] '{city.artifactDisplayName}' поставлен на стенд; GameManager отсутствует — песочница.", this);
        }

        /// <summary>
        /// Сброс прогресса: вытолкнуть артефакт из гнезда, чтобы стенд визуально опустей.
        /// Физический предмет после этого свободен (упадёт/будет пересоздан по данным при новом заходе).
        /// </summary>
        private void HandleProgressReset()
        {
            if (_socket == null)
                return;

            var mgr = _socket.interactionManager;
            if (mgr == null)
                return;

            // Копия списка: SelectExit меняет исходный interactablesSelected во время итерации.
            var selected = new List<IXRSelectInteractable>(_socket.interactablesSelected);
            for (int i = 0; i < selected.Count; i++)
                mgr.SelectExit(_socket, selected[i]);
        }
    }
}
