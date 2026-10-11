using UnityEngine;
using GoldenRing.Core;

namespace GoldenRing.DebugTools
{
    /// <summary>
    /// ВРЕМЕННЫЙ тестовый помощник. На старте «проходит» викторины первых городов,
    /// чтобы открыть доступ к нужному городу на карте без реального прохождения.
    /// Использует штатный GameManager.CompleteQuiz, так что прогрессия остаётся консистентной.
    /// ВНИМАНИЕ: ломает линейную прогрессию — выключить (runOnStart=false) или удалить перед сдачей.
    /// </summary>
    public class DebugUnlock : MonoBehaviour
    {
        [Tooltip("Открыть города с order < этого значения (проходит викторины предыдущих). 0 = ничего не делать.")]
        public int unlockUpToOrder = 0;

        [Tooltip("Выполнять на старте сцены.")]
        public bool runOnStart = true;

        private void Start()
        {
            if (runOnStart) Unlock();
        }

        public void Unlock()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Config == null) return;
            for (int o = 0; o < unlockUpToOrder; o++)
            {
                var c = gm.Config.GetByOrder(o);
                if (c != null) gm.CompleteQuiz(c);
            }
            Debug.Log("[DebugUnlock] ТЕСТ: открыты города до order<" + unlockUpToOrder);
        }
    }
}
