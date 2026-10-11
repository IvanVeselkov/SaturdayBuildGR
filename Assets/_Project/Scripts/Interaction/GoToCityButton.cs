using UnityEngine;
using UnityEngine.UI;
using GoldenRing.Core;

namespace GoldenRing.Interaction
{
    /// <summary>
    /// Кнопка «перейти в конкретный город» (для лобби / будущей карты).
    /// По клику просит SceneLoader загрузить город из назначенного CityData.
    /// Опционально уважает блокировку: если respectUnlock=true и город закрыт (IsCityUnlocked=false) — не пускает.
    /// Вешать на uGUI Button (world-space Canvas).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GoToCityButton : MonoBehaviour
    {
        [Tooltip("Город, в который ведёт кнопка.")]
        [SerializeField] private CityData city;

        [Tooltip("Не пускать в закрытый город (проверка GameManager.IsCityUnlocked).")]
        [SerializeField] private bool respectUnlock = false;

        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(Go);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Go);
        }

        private void Go()
        {
            if (city == null)
            {
                Debug.LogWarning("[GoToCityButton] Не назначен CityData.", this);
                return;
            }
            if (SceneLoader.Instance == null)
            {
                Debug.LogWarning("[GoToCityButton] SceneLoader отсутствует (нет Bootstrap?).", this);
                return;
            }
            if (respectUnlock && GameManager.Instance != null && !GameManager.Instance.IsCityUnlocked(city))
            {
                Debug.Log($"[GoToCityButton] Город '{city.displayName}' ещё закрыт.", this);
                return;
            }
            SceneLoader.Instance.GoToCity(city);
        }
    }
}
