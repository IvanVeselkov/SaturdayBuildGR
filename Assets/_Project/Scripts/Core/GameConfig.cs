using System.Collections.Generic;
using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Конфигурация игры: упорядоченный список всех городов кольца.
    /// Единственный источник правды о маршруте. Наполняется в инспекторе.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GoldenRing/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Tooltip("Города в порядке прохождения. Индекс в списке = порядок разблокировки.")]
        public List<CityData> cities = new List<CityData>();

        public int CityCount => cities != null ? cities.Count : 0;

        /// <summary>Первый город кольца (или null, если список пуст).</summary>
        public CityData First => CityCount > 0 ? cities[0] : null;

        /// <summary>Город по индексу порядка (0-based). null, если вне диапазона.</summary>
        public CityData GetByOrder(int order)
        {
            if (order < 0 || order >= CityCount) return null;
            return cities[order];
        }

        /// <summary>Город по строковому id. null, если не найден.</summary>
        public CityData GetById(string id)
        {
            if (string.IsNullOrEmpty(id) || cities == null) return null;
            for (int i = 0; i < cities.Count; i++)
                if (cities[i] != null && cities[i].id == id) return cities[i];
            return null;
        }

        /// <summary>Индекс города в списке (порядок) или -1.</summary>
        public int IndexOf(CityData city)
        {
            if (city == null || cities == null) return -1;
            return cities.IndexOf(city);
        }
    }
}
