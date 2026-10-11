using UnityEngine;
using GoldenRing.Core;

namespace GoldenRing.DebugTools
{
    /// <summary>
    /// ТЕСТ-ХЕЛПЕР (удалить/выключить перед сдачей).
    /// Форсит условие финала без реального прохождения всех городов: отмечает все артефакты
    /// собранными и поставленными (последний PlaceArtifact стреляет OnAllArtifactsPlaced),
    /// опционально задаёт тестовую статистику (время/ошибки) для проверки экрана финала.
    ///
    /// Вызов: кнопкой в лобби (метод TriggerFinale) или ПКМ по компоненту → Trigger Finale (Test).
    /// </summary>
    public class DebugFinaleTrigger : MonoBehaviour
    {
        [Tooltip("Также выставить тестовую статистику (иначе покажется реальное время/0 ошибок).")]
        [SerializeField] private bool setTestStats = true;
        [Tooltip("Тестовое время прохождения, минуты (для проверки медали).")]
        [SerializeField] private float testMinutes = 12f;
        [Tooltip("Тестовое число ошибок.")]
        [SerializeField] private int testMistakes = 3;

        [ContextMenu("Trigger Finale (Test)")]
        public void TriggerFinale()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogWarning("[DebugFinale] GameManager не найден."); return; }
            var cfg = gm.Config;
            if (cfg == null || cfg.CityCount == 0) { Debug.LogWarning("[DebugFinale] GameConfig пуст."); return; }

            // статистику задаём ДО постановки, чтобы фиксация времени на финале её не перезаписала
            if (setTestStats)
                gm.DebugSetStatsForTesting(testMinutes * 60f, testMistakes);

            for (int i = 0; i < cfg.CityCount; i++)
            {
                var city = cfg.GetByOrder(i);
                if (city == null) continue;
                gm.CollectArtifact(city);
                gm.PlaceArtifact(city); // на последнем сработает OnAllArtifactsPlaced -> сиквенс финала
            }
            Debug.Log($"[DebugFinale] Все артефакты поставлены ({cfg.CityCount}). Финал запущен.");
        }
    }
}
