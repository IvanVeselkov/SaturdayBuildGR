using System.Collections;
using UnityEngine;

namespace GoldenRing.Core
{
    /// <summary>
    /// Затемнение экрана в VR через чёрную сферу вокруг камеры (шейдер GoldenRing/ScreenFade).
    /// Вешается на объект-сферу, которая живёт как ребёнок VR-камеры в постоянной Bootstrap-сцене.
    /// Cull Front + ZTest Always в шейдере → сфера рисуется поверх всего и накрывает оба глаза.
    /// Живёт в персистентном риге, поэтому один на всю игру — доступен через Instance.
    /// SceneLoader гасит экран (FadeOut) перед сменой сцены и проявляет (FadeIn) после.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [Tooltip("Renderer сферы-затемнителя. Если пусто — берётся с этого объекта.")]
        [SerializeField] private Renderer fadeRenderer;

        [Tooltip("Длительность одного перехода (секунды).")]
        [SerializeField] private float duration = 0.45f;

        [Tooltip("Цвет затемнения (обычно чёрный).")]
        [SerializeField] private Color fadeColor = Color.black;

        private Material _mat;               // инстанс материала (не трогаем общий ассет)
        private float _alpha;                // текущая непрозрачность 0..1
        private Coroutine _running;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Экран полностью чёрный.</summary>
        public bool IsOpaque => _alpha >= 0.999f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // На всякий случай — второй фейдер не нужен.
                Destroy(this);
                return;
            }
            Instance = this;

            if (fadeRenderer == null)
                fadeRenderer = GetComponent<Renderer>();

            if (fadeRenderer != null)
                _mat = fadeRenderer.material; // инстансим материал

            SetAlpha(0f); // старт — прозрачно
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Мгновенно выставить непрозрачность (0 = прозрачно, 1 = чёрный экран).</summary>
        public void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            if (_mat != null)
            {
                var c = fadeColor;
                c.a = _alpha;
                _mat.SetColor(BaseColorId, c);
            }
            // Прячем рендер, когда полностью прозрачно (мелкая экономия).
            if (fadeRenderer != null)
                fadeRenderer.enabled = _alpha > 0.0001f;
        }

        /// <summary>Плавно затемнить в чёрный. Дождаться корутины перед сменой сцены.</summary>
        public IEnumerator FadeOut() => FadeTo(1f);

        /// <summary>Плавно проявить из чёрного.</summary>
        public IEnumerator FadeIn() => FadeTo(0f);

        /// <summary>Плавно довести альфу до target (0..1).</summary>
        public IEnumerator FadeTo(float target)
        {
            target = Mathf.Clamp01(target);
            if (_running != null)
                StopCoroutine(_running);

            _running = StartCoroutine(FadeRoutine(target));
            yield return _running;
        }

        private IEnumerator FadeRoutine(float target)
        {
            float start = _alpha;
            if (Mathf.Approximately(start, target) || duration <= 0f)
            {
                SetAlpha(target);
                _running = null;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // не зависим от Time.timeScale (паузы/переходы)
                SetAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            SetAlpha(target);
            _running = null;
        }
    }
}
