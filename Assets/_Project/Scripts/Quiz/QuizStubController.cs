using UnityEngine;
using GoldenRing.Core;

namespace GoldenRing.Quiz
{
    /// <summary>
    /// ВРЕМЕННАЯ заглушка сцены викторины города. Нужна, чтобы прогнать полный цикл
    /// город → викторина → лобби до появления реальной викторины (пакет Тани).
    /// Две кнопки на мир-UI:
    ///  - «Викторина пройдена» → CompleteAndReturn(): GameManager.CompleteQuiz(city) (открывает следующий город) + возврат в лобби.
    ///  - «Назад в город»       → BackToCity(): вернуться в город переслушать гида.
    /// Когда придёт боевая викторина — этот контроллер заменяется её логикой (держатель артефакта + активация).
    /// </summary>
    public class QuizStubController : MonoBehaviour
    {
        [Tooltip("Город этой викторины.")]
        public CityData city;

        public void CompleteAndReturn()
        {
            if (GameManager.Instance != null && city != null)
                GameManager.Instance.CompleteQuiz(city);

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GoToLobby();
            else
                Debug.LogWarning("[QuizStub] SceneLoader отсутствует — нет перехода в лобби.");
        }

        public void BackToCity()
        {
            if (SceneLoader.Instance != null && city != null)
                SceneLoader.Instance.GoToCity(city);
            else
                Debug.LogWarning("[QuizStub] SceneLoader/city отсутствует — нет перехода в город.");
        }
    }
}
