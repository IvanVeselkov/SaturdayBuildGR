using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// «Липкий» артефакт: пока игрок держит его рукой, выронить куда попало нельзя.
    /// Единственный легальный выход из руки — постановка в сокет стенда (XRSocketInteractor).
    /// Если рука отпускает артефакт НЕ в сокет — он тут же возвращается в левую руку.
    ///
    /// Обоснование дизайна: локомоция на правой руке, поэтому берём/держим артефакт только левой;
    /// донести его можно исключительно до своего стенда. Побочно убирает баг «бросил на пол →
    /// вернулся в лобби → снова в руке»: бросить на пол теперь попросту нельзя.
    ///
    /// Вешается на префаб артефакта рядом с XRGrabInteractable. Пока артефакт лежит на подставке
    /// (его ещё не брали) — не вмешивается; включается после первого взятия рукой.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class StickyArtifact : MonoBehaviour
    {
        private XRGrabInteractable _grab;
        private Coroutine _recheck;

        private void Awake() => _grab = GetComponent<XRGrabInteractable>();

        private void OnEnable()
        {
            _grab.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _grab.selectExited.RemoveListener(OnSelectExited);
            if (_recheck != null) { StopCoroutine(_recheck); _recheck = null; }
        }

private void OnSelectExited(SelectExitEventArgs args)
        {
            // Выход из сокета стенда (игрок снял артефакт со стенда рукой) — не мешаем.
            if (args.interactorObject is XRSocketInteractor)
                return;

            // Не запускать корутину, если артефакт сейчас уничтожается вместе со сценой или идёт переход.
            // При выгрузке сцены XRI отменяет select уже во время teardown объекта: isActiveAndEnabled
            // может быть ещё true, но StartCoroutine на уничтожаемом объекте кидает "GameObject is inactive".
            // Надёжные признаки: сцена артефакта уже не загружена / SceneLoader в процессе перехода.
            if (!isActiveAndEnabled) return;
            if (!gameObject.scene.isLoaded) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning) return;

            // Рука отпустила в живой сцене: проверить через несколько кадров — если артефакт никто
            // не держит (не ушёл в сокет стенда), вернуть его в левую руку.
            if (_recheck != null) StopCoroutine(_recheck);
            _recheck = StartCoroutine(ReturnToHandIfDropped());
        }

private IEnumerator ReturnToHandIfDropped()
        {
            // Ждём несколько кадров: даём сокету стенда шанс перехватить артефакт при постановке,
            // чтобы случайно не выдернуть его из гнезда назад в руку. Если за это время его кто-то
            // взял (сокет или снова рука) — всё в порядке, выходим.
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                if (_grab.isSelected)
                {
                    _recheck = null;
                    yield break;
                }
            }
            _recheck = null;

            // Никто не держит — значит игрок пытался выронить мимо стенда. Возвращаем в левую руку.
            var left = PlayerRig.Instance != null ? PlayerRig.Instance.LeftInteractor : null;
            var mgr = _grab.interactionManager != null
                ? _grab.interactionManager
                : FindAnyObjectByType<XRInteractionManager>();

            if (left == null || mgr == null)
                yield break;

            mgr.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)_grab);
        }
    }
}
