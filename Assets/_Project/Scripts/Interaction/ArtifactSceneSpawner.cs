using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Единый data-driven спавнер артефактов для ЛЮБОЙ контентной сцены (город/лобби).
    /// Единственный физический артефакт нигде не хранится в сцене жёстко — он пересоздаётся
    /// по состоянию GameManager на старте сцены и появляется ровно там, где велит прогресс,
    /// поэтому «переезжает в руке» между сценами и не задваивается.
    ///
    /// Для каждого города конфигурации:
    ///   поставлен            → на его стенд, если такой стенд есть в этой сцене (лобби);
    ///   собран и не поставлен → в левую руку (в любой сцене — так артефакт «едет» с игроком);
    ///   не собран            → на подставку, ТОЛЬКО в сцене своего города (sceneCity + pedestalAnchor).
    ///
    /// Вешать в каждую контентную сцену: в лобби sceneCity/pedestalAnchor пустые (работают стенды+рука),
    /// в сцене города sceneCity = город этой сцены, pedestalAnchor = точка на столе/подставке.
    /// </summary>
    public class ArtifactSceneSpawner : MonoBehaviour
    {
        [Tooltip("Город, которому принадлежит ЭТА сцена (для сцены города). В лобби оставить пустым.")]
        [SerializeField] private CityData sceneCity;

        [Tooltip("Куда положить НЕ собранный артефакт в сцене города (стол/подставка). Только для города.")]
        [SerializeField] private Transform pedestalAnchor;

        [Tooltip("Куда спавнить артефакт «в руку», если левый интерактор не найден (запасной якорь).")]
        [SerializeField] private Transform handFallbackAnchor;

        [Tooltip("Квест-поиск: несобранный артефакт этого города спавнится ДЕАКТИВИРОВАННЫМ и передаётся роботу-гиду (EVE), который покажет его в укрытии после рассказа. Если выключено — артефакт просто лежит на pedestalAnchor (старое поведение).")]
        [SerializeField] private bool hideForGuide = false;

        [Tooltip("Робот-гид, которому отдать спрятанный артефакт. Пусто = найти в сцене.")]
        [SerializeField] private GoldenRing.Guide.RobotGuide guide;

        [Tooltip("Режим ВИКТОРИНЫ: собранный артефакт НЕ вкладывать в руку, а поставить на parkAnchor — руки свободны для головоломки. StickyArtifact на нём отключается, чтобы он не приклеивался обратно к руке.")]
        [SerializeField] private bool parkCollectedHere = false;

        [Tooltip("Куда поставить собранный артефакт в режиме викторины (parkCollectedHere=true). Пусто = на объект спавнера.")]
        [SerializeField] private Transform parkAnchor;

        private void Start() => StartCoroutine(SpawnRoutine());

        private IEnumerator SpawnRoutine()
        {
            yield return null; // дать ригу/менеджерам проснуться после переноса рига загрузчиком

            // Подчистить возможные дубли/выжившие артефакты (напр. артефакт, переживший смену сцены в руке),
            // чтобы не склонировать: единственный источник правды — состояние GameManager, всё ниже спавнится заново.
            foreach (var stray in FindObjectsByType<ArtifactPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (stray != null) Destroy(stray.gameObject);
            yield return null; // дать уничтожению примениться (снять select с руки/сокета)

            var gm = GameManager.Instance;
            if (gm == null || gm.Config == null)
            {
                Debug.LogWarning("[ArtifactSceneSpawner] Нет GameManager/Config — спавнить нечего.", this);
                yield break;
            }

            // Стенды, присутствующие в ЭТОЙ сцене: cityId -> StandSocket.
            var stands = new Dictionary<string, StandSocket>();
            foreach (var s in FindObjectsByType<StandSocket>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (s.City != null && !stands.ContainsKey(s.City.id))
                    stands.Add(s.City.id, s);

            bool handUsed = false; // «в руке» одновременно максимум один артефакт

            foreach (var city in gm.Config.cities)
            {
                if (city == null || city.artifactPrefab == null)
                    continue;

                bool placed = gm.IsArtifactPlaced(city);
                bool collected = gm.IsArtifactCollected(city);

                if (placed)
                {
                    // Поставленный артефакт — на свой стенд, если он есть в этой сцене (лобби).
                    if (stands.TryGetValue(city.id, out var stand) && stand != null)
                    {
                        var go = Instantiate(city.artifactPrefab, stand.transform.position, stand.transform.rotation);
                        BindCity(go, city);
                        ForceSelect(GetSocket(stand), GetGrab(go));
                    }
                    // Стенда нет (например, сцена города) → артефакт живёт в лобби, здесь не показываем.
                }
                else if (collected)
                {
                    // Режим викторины: не в руку, а на держатель/пол — руки свободны для головоломки.
                    if (parkCollectedHere)
                    {
                        Vector3 pp = parkAnchor != null ? parkAnchor.position : transform.position;
                        Quaternion pr = parkAnchor != null ? parkAnchor.rotation : Quaternion.identity;
                        var pgo = Instantiate(city.artifactPrefab, pp, pr);
                        BindCity(pgo, city);
                        var prb = pgo.GetComponent<Rigidbody>();
                        if (prb != null) prb.isKinematic = true;
                        var psticky = pgo.GetComponent<StickyArtifact>();
                        if (psticky != null) psticky.enabled = false; // не приклеивать к руке
                        continue;
                    }

                    // Собранный, но не поставленный — «в руке», в любой сцене.
                    if (handUsed)
                    {
                        Debug.LogWarning($"[ArtifactSceneSpawner] Ещё один артефакт '{city.id}' в статусе 'в руке' — пропущен (в руке уже занято).", this);
                        continue;
                    }

                    var left = PlayerRig.Instance != null ? PlayerRig.Instance.LeftInteractor : null;
                    Vector3 pos = left != null ? left.transform.position
                                : handFallbackAnchor != null ? handFallbackAnchor.position
                                : transform.position;

                    var go = Instantiate(city.artifactPrefab, pos, Quaternion.identity);
                    BindCity(go, city);

                    var grab = GetGrab(go);
                    if (left != null && grab != null)
                    {
                        ForceSelect(left, grab);
                        handUsed = true;
                    }
                    else if (left == null)
                    {
                        Debug.LogWarning($"[ArtifactSceneSpawner] Левый интерактор не найден — '{city.id}' спавнен без вложения в руку.", this);
                    }
                }
                else
                {
                    // Не собран → ТОЛЬКО в сцене своего города.
                    if (sceneCity != null && city.artifactId == sceneCity.artifactId)
                    {
                        if (hideForGuide)
                        {
                            // Квест-поиск: спавним деактивированным и отдаём роботу-гиду —
                            // он покажет артефакт в укрытии (hidePoint) после рассказа.
                            var g = guide != null ? guide : FindAnyObjectByType<GoldenRing.Guide.RobotGuide>();
                            Transform hp = g != null ? g.hidePoint : null;
                            Vector3 p = hp != null ? hp.position
                                      : pedestalAnchor != null ? pedestalAnchor.position
                                      : transform.position;
                            Quaternion r = hp != null ? hp.rotation : Quaternion.identity;

                            var go = Instantiate(city.artifactPrefab, p, r);
                            BindCity(go, city);
                            go.SetActive(false);

                            if (g != null) g.hiddenArtifact = go;
                            else Debug.LogWarning("[ArtifactSceneSpawner] hideForGuide включён, но RobotGuide в сцене не найден — артефакт останется скрытым.", this);
                        }
                        else if (pedestalAnchor != null)
                        {
                            var go = Instantiate(city.artifactPrefab, pedestalAnchor.position, pedestalAnchor.rotation);
                            BindCity(go, city);
                        }
                    }
                }
            }
        }

        private void BindCity(GameObject go, CityData city)
        {
            if (go == null) return;
            var pickup = go.GetComponent<ArtifactPickup>();
            if (pickup != null)
                pickup.SetCity(city);
        }

        private XRGrabInteractable GetGrab(GameObject go)
            => go != null ? go.GetComponent<XRGrabInteractable>() : null;

        private XRSocketInteractor GetSocket(StandSocket stand)
            => stand != null ? stand.GetComponent<XRSocketInteractor>() : null;

        private void ForceSelect(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (interactor == null || interactable == null)
                return;

            var mgr = FindAnyObjectByType<XRInteractionManager>();
            if (mgr == null)
            {
                Debug.LogWarning("[ArtifactSceneSpawner] Нет XRInteractionManager — force-select не выполнен.", this);
                return;
            }
            mgr.SelectEnter(interactor, interactable);
        }
    }
}
