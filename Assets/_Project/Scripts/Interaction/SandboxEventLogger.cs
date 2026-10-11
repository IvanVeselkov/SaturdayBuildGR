using UnityEngine;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Отладочный логгер прогресса для песочницы/тестов.
    /// Вешать на объект с GameManager (или в сцену, где есть GameManager.Instance).
    /// Пишет в консоль сбор/постановку/финал/открытие города — чтобы видеть, что мосты работают.
    /// В боевые сцены не тащить (это тест-хелпер).
    /// </summary>
    public class SandboxEventLogger : MonoBehaviour
    {
        private GameManager _gm;
        private int _collectN;
        private int _placeN;
        private int _resetN;


        private void OnEnable()
        {
            _gm = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
            if (_gm == null)
            {
                Debug.LogWarning("[GR] SandboxEventLogger: GameManager не найден.", this);
                return;
            }
            _gm.OnArtifactCollected += OnCollected;
            _gm.OnArtifactPlaced += OnPlaced;
            _gm.OnAllArtifactsPlaced += OnAllPlaced;
            _gm.OnCityUnlocked += OnCityUnlocked;
            _gm.OnProgressReset += OnReset;
        }

        private void OnDisable()
        {
            if (_gm == null) return;
            _gm.OnArtifactCollected -= OnCollected;
            _gm.OnArtifactPlaced -= OnPlaced;
            _gm.OnAllArtifactsPlaced -= OnAllPlaced;
            _gm.OnCityUnlocked -= OnCityUnlocked;
            _gm.OnProgressReset -= OnReset;
        }

private void OnCollected(CityData c) => Debug.Log($"[GR] #{++_collectN} Собран: {c.artifactDisplayName} ({c.artifactId}). Всего собрано: {_gm.CollectedCount}");
private void OnPlaced(CityData c) => Debug.Log($"[GR] #{++_placeN} Поставлен на стенд: {c.artifactDisplayName}. На стендах: {_gm.PlacedCount}/{_gm.CityCount}");
        private void OnAllPlaced() => Debug.Log("[GR] ВСЕ артефакты на стендах — условие финала выполнено!");
        private void OnCityUnlocked(CityData c) => Debug.Log($"[GR] Открыт город: {c.displayName}");
private void OnReset() => Debug.Log($"[GR] #{++_resetN} Прогресс сброшен.");
    }
}
