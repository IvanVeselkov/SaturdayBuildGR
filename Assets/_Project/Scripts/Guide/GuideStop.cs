using UnityEngine;

namespace GoldenRing.Guide
{
    /// <summary>
    /// Маркер одной остановки тура робота-гида. Ставится в сцене города В ТОЧКЕ,
    /// где робот стоит/куда подъезжает. Робот (RobotGuide) собирает все GuideStop
    /// в сцене и проходит их по возрастанию order.
    /// </summary>
    public class GuideStop : MonoBehaviour
    {
        public enum StopMode
        {
            /// <summary>Едет за игроком, пока рассказывает общее.</summary>
            Follow,
            /// <summary>Подъезжает к этой точке, встаёт лицом к игроку, рассказ — по подходу игрока.</summary>
            GoToExhibit,
            /// <summary>Уже стоит здесь; всегда повёрнут к игроку; рассказ — по подходу игрока.</summary>
            PrePlaced
        }

        [Tooltip("Порядок остановки в туре (по возрастанию).")]
        public int order = 0;

        [Tooltip("Режим этой остановки.")]
        public StopMode mode = StopMode.GoToExhibit;

        [Tooltip("Аудиоклип рассказа. Пусто = заглушка-бип длиной Placeholder Duration.")]
        public AudioClip narration;

        [Tooltip("Длительность заглушки (сек), если клип не задан.")]
        public float placeholderDuration = 4f;

        [Tooltip("Игрок должен подойти ближе этого радиуса (м), чтобы робот начал рассказ. Настраивается на каждый экспонат.")]
        public float approachRadius = 2f;

        [Tooltip("Поворачиваться к игроку, пока ждём его подхода (для GoToExhibit/PrePlaced).")]
        public bool facesPlayerWhileWaiting = true;

        [Tooltip("После рассказа на этой остановке запустить beat 'прячу артефакт' (затемнение + активация артефакта в укрытии + поиск).")]
        public bool hideArtifactAfter = false;

        void OnDrawGizmos()
        {
            Color c = mode == StopMode.Follow ? Color.cyan
                    : mode == StopMode.PrePlaced ? Color.magenta
                    : Color.yellow;
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Color rc = c; rc.a = 0.35f; Gizmos.color = rc;
            Gizmos.DrawWireSphere(transform.position, approachRadius);
            Gizmos.color = c;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
        }
    }
}
