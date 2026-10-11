using UnityEngine;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Временная кнопка «Вернуться в лобби» для сцены города.
    /// По клику просит SceneLoader перейти в лобби (артефакт в руке переедет туда сам —
    /// его пересоздаст LobbyArtifactSpawner по данным GameManager).
    /// Позже эта функция переедет на диегетичный UI робота-гида (ТВ в руках).
    /// Вешать на uGUI Button (world-space Canvas). Если поле button пусто — берётся с этого объекта.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ReturnToLobbyButton : MonoBehaviour
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
                button.onClick.AddListener(GoToLobby);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(GoToLobby);
        }

        private void GoToLobby()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GoToLobby();
            else
                Debug.LogWarning("[ReturnToLobbyButton] SceneLoader отсутствует (нет Bootstrap?).", this);
        }
    }
}
