using UnityEngine;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Тест-кнопка «Перезапуск» для песочницы.
    /// Вешать на объект с UI Button. По клику: сбрасывает прогресс (GameManager.ResetProgress)
    /// и возвращает все артефакты на стартовые позиции. В боевые сцены не тащить.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestartButton : MonoBehaviour
    {
        private void Start()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Restart);
        }

        public void Restart()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetProgress();
            else
                Debug.LogWarning("[RestartButton] GameManager отсутствует — сброс невозможен.", this);

            var artifacts = Object.FindObjectsByType<ArtifactPickup>(FindObjectsSortMode.None);
            for (int i = 0; i < artifacts.Length; i++)
                artifacts[i].ResetToStart();

            Debug.Log("[GR] Перезапуск: прогресс сброшен, артефакты возвращены на старт.");
        }
    }
}
