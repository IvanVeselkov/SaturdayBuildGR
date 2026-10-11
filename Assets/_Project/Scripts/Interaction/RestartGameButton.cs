using UnityEngine;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Кнопка «Перезапустить игру» (боевая версия для лобби/паузы).
    /// Полный сброс прогресса (GameManager.ResetProgress: закрыть все города кроме первого, очистить
    /// собранные/поставленные артефакты) + перезагрузка лобби с чистого листа: артефакты и стенды
    /// пересоздаются по обнулённым данным (LobbyArtifactSpawner на старте сцены → в лобби пусто).
    /// Отличается от тест-хелпера RestartButton (тот только для песочницы).
    /// Вешать на uGUI Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestartGameButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(Restart);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Restart);
        }

private void Restart()
        {
            // Сброс прогресса делается внутри RestartToLobby под чёрным экраном, когда сцена уже
            // выгружена (иначе стенд успевает снова захватить артефакт). Здесь только запуск.
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.RestartToLobby();
            else
                Debug.LogWarning("[RestartGameButton] SceneLoader отсутствует (нет Bootstrap?).", this);
        }
    }
}
