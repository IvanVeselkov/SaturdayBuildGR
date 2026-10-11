using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Мост «физический артефакт → прогресс игры».
    /// Вешается на префаб артефакта рядом с XRGrabInteractable.
    /// Когда игрок берёт артефакт РУКОЙ (не сокетом-стендом), засчитывает сбор в GameManager.
    /// Знает свой город через ссылку на CityData (artifactId используется стендом для проверки «свой/чужой»).
    /// Умеет вернуться на стартовую позицию (для теста перезапуска в песочнице).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ArtifactPickup : MonoBehaviour
    {
        [Tooltip("Город, которому принадлежит этот артефакт. Задаёт artifactId/имя.")]
        [SerializeField] private CityData city;

        /// <summary>Город артефакта (для стенда и UI).</summary>
        public CityData City => city;

        /// <summary>Уникальный ид артефакта или null, если город не назначен.</summary>
        public string ArtifactId => city != null ? city.artifactId : null;

        private XRGrabInteractable _grab;
        private Rigidbody _rb;
        private Vector3 _startPos;
        private Quaternion _startRot;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        private void OnEnable()
        {
            if (_grab != null)
                _grab.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            if (_grab != null)
                _grab.selectEntered.RemoveListener(OnSelectEntered);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Сокет-стенд тоже «выбирает» артефакт — но это постановка, а не сбор. Игнорируем.
            if (args.interactorObject is XRSocketInteractor)
                return;

            if (city == null)
            {
                Debug.LogWarning($"[ArtifactPickup] {name}: не назначен CityData — сбор не засчитан.", this);
                return;
            }

            // CollectArtifact идемпотентен (HashSet в GameManager), повторный вызов безопасен.
            if (GameManager.Instance != null)
                GameManager.Instance.CollectArtifact(city);
            else
                Debug.Log($"[ArtifactPickup] Взят '{city.artifactDisplayName}' ({city.artifactId}); GameManager отсутствует — песочница.", this);
        }

        /// <summary>
        /// Вернуть артефакт на стартовую позицию.
        /// Ключевой момент: на один кадр выключаем XRGrabInteractable — это сразу снимает выбор
        /// (рука/сокет) и не даёт сокету мгновенно перехватить артефакт обратно, пока не обновятся
        /// физические триггеры после перемещения. Иначе предмет «прилипает» к стенду.
        /// </summary>
        public void ResetToStart()
        {
            if (!isActiveAndEnabled)
            {
                // корутину не запустить на выключенном объекте — просто телепортируем.
                HardMoveToStart();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(ResetRoutine());
        }

        private IEnumerator ResetRoutine()
        {
            if (_grab != null)
                _grab.enabled = false; // снимает выбор и убирает из hovered у любого интерактора

            HardMoveToStart();

            // дать физике обновить триггеры сокета, пока артефакт уже уехал
            yield return new WaitForFixedUpdate();
            yield return null;

            if (_grab != null)
                _grab.enabled = true;
        }

        private void HardMoveToStart()
        {
            transform.SetPositionAndRotation(_startPos, _startRot);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    

/// <summary>
        /// Назначить город артефакту в рантайме. Нужно, когда артефакт пересоздаётся по данным
        /// в лобби (LobbyArtifactSpawner): один префаб-плейсхолдер может обслуживать любой город.
        /// Читается в OnSelectEntered при следующем взятии, так что смена безопасна.
        /// </summary>
        public void SetCity(CityData value)
        {
            city = value;
        }
}
}
