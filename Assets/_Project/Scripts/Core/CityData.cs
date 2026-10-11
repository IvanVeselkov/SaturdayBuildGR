using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Данные одного города Научного кольца (ScriptableObject).
    /// Один объект = один город. Наполняется в инспекторе без кода.
    /// </summary>
    [CreateAssetMenu(fileName = "City_", menuName = "GoldenRing/City Data", order = 0)]
    public class CityData : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный строковый id города, напр. 'kostroma'. Используется в сохранении прогресса.")]
        public string id;

        [Tooltip("Отображаемое имя города, напр. 'Кострома'.")]
        public string displayName;

        [Tooltip("Порядковый номер в кольце (0 = первый доступный город).")]
        public int order;

        [Header("Сцены")]
        [Tooltip("Имя сцены города (должна быть в Build Settings).")]
        public string citySceneName;

        [Tooltip("Имя сцены викторины этого города.")]
        public string quizSceneName;

        [Header("Артефакт")]
        [Tooltip("Уникальный id артефакта, напр. 'kostroma_meteorite'.")]
        public string artifactId;

        [Tooltip("Отображаемое имя артефакта, напр. 'Обломок метеорита'.")]
        public string artifactDisplayName;

        [Tooltip("Префаб артефакта (модель, которая клеится к руке и ставится на стенд). Можно оставить пустым на этапе каркаса.")]
        public GameObject artifactPrefab;
    }
}
