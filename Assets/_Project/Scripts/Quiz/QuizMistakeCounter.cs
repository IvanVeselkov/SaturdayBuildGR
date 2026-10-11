using UnityEngine;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Quiz
{
    /// <summary>
    /// Вешается на кнопку закрытия окна «неверно» (quizbase/falsee/Button) в викторинах Тани.
    /// Каждое нажатие = один неверный ответ -> GameManager.RegisterMistake().
    /// Счётчик суммируется по всем городам и показывается на экране финала.
    /// Существующий onClick кнопки (SetActive(false) окна ошибки) не трогается:
    /// слушатель добавляется в рантайме поверх. Awake у объекта, стартующего неактивным,
    /// срабатывает один раз при первом включении — дубликатов подписки нет.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuizMistakeCounter : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(Register);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Register);
        }

        private void Register()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.RegisterMistake();
        }
    }
}
