using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoldenRing.Guide
{
    /// <summary>Простой переход по кнопке: грузит указанную сцену (Single). Для дев-навигации в тест-билде.</summary>
    public class DevSceneButton : MonoBehaviour
    {
        public string sceneName = "Bootstrap";
        public void Load()
        {
            if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
        }
    }
}
