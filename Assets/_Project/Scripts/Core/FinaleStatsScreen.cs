using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GoldenRing.Core
{
    /// <summary>
    /// Экран статистики финала (world-space uGUI, в сцене Finale).
    /// Показывает время прохождения и число ошибок, вычисляет медаль по времени
    /// и подсвечивает заработанную из трёх (золото/серебро/бронза).
    ///
    /// Пороги медалей (в минутах) настраиваются в инспекторе. Дольше серебряного порога — бронза
    /// (бронза гарантирована всегда). Данные берутся из GameManager (ElapsedSeconds, Mistakes).
    ///
    /// Кнопки сцены вешать на PlayAgain() (перезапуск) и Quit() (выход).
    /// </summary>
    public class FinaleStatsScreen : MonoBehaviour
    {
        [Header("Тексты")]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text mistakesText;
        [Tooltip("Необязательная строка-вердикт (напр. «Золото!»).")]
        [SerializeField] private TMP_Text verdictText;

        [Header("Медали (Image со спрайтом)")]
        [SerializeField] private Image goldImage;
        [SerializeField] private Image silverImage;
        [SerializeField] private Image bronzeImage;

        [Header("Пороги медалей, минуты (включительно)")]
        [Tooltip("Золото: время прохождения не больше этого (мин).")]
        [SerializeField] private float goldMaxMinutes = 10f;
        [Tooltip("Серебро: не больше этого (мин). Дольше — бронза.")]
        [SerializeField] private float silverMaxMinutes = 15f;

        [Header("Вид подсветки")]
        [SerializeField] private Color earnedTint = Color.white;
        [SerializeField] private Color dimTint = new Color(0.32f, 0.32f, 0.34f, 1f);
        [SerializeField] private float earnedScale = 1.18f;

        public enum Medal { Gold, Silver, Bronze }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>Перечитать статистику из GameManager и обновить экран.</summary>
        public void Refresh()
        {
            var gm = GameManager.Instance;
            float seconds = gm != null ? gm.ElapsedSeconds : 0f;
            int mistakes = gm != null ? gm.Mistakes : 0;

            if (timeText != null)
                timeText.text = FormatTime(seconds);
            if (mistakesText != null)
                mistakesText.text = mistakes.ToString();

            Medal medal = Evaluate(seconds);
            ApplyMedalHighlight(medal);

            if (verdictText != null)
                verdictText.text = medal == Medal.Gold ? "Золото!" : medal == Medal.Silver ? "Серебро!" : "Бронза!";
        }

        private Medal Evaluate(float seconds)
        {
            float minutes = seconds / 60f;
            if (minutes <= goldMaxMinutes) return Medal.Gold;
            if (minutes <= silverMaxMinutes) return Medal.Silver;
            return Medal.Bronze; // дольше — всё равно бронза
        }

        private void ApplyMedalHighlight(Medal medal)
        {
            SetMedal(goldImage, medal == Medal.Gold);
            SetMedal(silverImage, medal == Medal.Silver);
            SetMedal(bronzeImage, medal == Medal.Bronze);
        }

        private void SetMedal(Image img, bool earned)
        {
            if (img == null) return;
            img.color = earned ? earnedTint : dimTint;
            float s = earned ? earnedScale : 1f;
            img.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            var t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:00}:{1:00}", (int)t.TotalMinutes, t.Seconds);
        }

        // ===== Кнопки сцены =====

        /// <summary>Кнопка «Пройти заново»: полный сброс прогресса и возврат в лобби.</summary>
        public void PlayAgain()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.RestartToLobby();
        }

        /// <summary>Кнопка «Выход»: закрыть приложение (в редакторе — остановить Play).</summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
