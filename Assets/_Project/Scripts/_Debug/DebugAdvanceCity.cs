using UnityEngine;
using GoldenRing.Core;

namespace GoldenRing.DebugTools
{
    /// <summary>
    /// ТЕСТ-ХЕЛПЕР (удалить/выключить перед сдачей).
    /// Открывает города по одному: находит первый непройденный город по порядку и засчитывает его викторину
    /// (GameManager.CompleteQuiz) — текущий становится зелёным, открывается следующий. Удобно проверять карту
    /// без реального прохождения. Вешать на кнопку в лобби (метод AdvanceOne) или ПКМ → Advance One City.
    /// </summary>
    public class DebugAdvanceCity : MonoBehaviour
    {
        [ContextMenu("Advance One City (Test)")]
        public void AdvanceOne()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogWarning("[DebugAdvance] GameManager не найден."); return; }
            var cfg = gm.Config;
            if (cfg == null || cfg.CityCount == 0) { Debug.LogWarning("[DebugAdvance] GameConfig пуст."); return; }

            for (int i = 0; i < cfg.CityCount; i++)
            {
                var c = cfg.GetByOrder(i);
                if (c != null && !gm.IsQuizCompleted(c))
                {
                    gm.CompleteQuiz(c);
                    Debug.Log("[DebugAdvance] Пройден город: " + c.displayName);
                    return;
                }
            }
            Debug.Log("[DebugAdvance] Все города уже пройдены.");
        }
    }
}
