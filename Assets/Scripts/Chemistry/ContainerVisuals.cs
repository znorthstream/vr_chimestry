using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>
    /// Визуализация содержимого сосуда: уровень жидкости, цвет (в т.ч. индикатор),
    /// осадок на дне, пузырьки газа. Работает с простыми примитивами (цилиндры),
    /// чтобы не зависеть от финальных моделей посуды.
    /// </summary>
    public class ContainerVisuals : MonoBehaviour
    {
        [Header("Ссылки (задаёт редакторский билдер сцены)")]
        public Transform liquid;         // цилиндр-жидкость
        public Transform precipitate;    // цилиндр-осадок
        public Transform bubblesRoot;    // корень пузырьков

        [Header("Геометрия")]
        [Tooltip("Высота внутренней полости, м (при полном заполнении)")]
        public float innerHeight = 0.08f;
        [Tooltip("Y (локально) дна полости")]
        public float bottomY = 0.02f;
        [Tooltip("Радиус полости, м")]
        public float innerRadius = 0.03f;

        [Header("Материалы")]
        public Material liquidMaterial;    // экземпляр создаётся на старте
        public Material precipitateMaterial;

        private ChemicalContainer _container;
        private Renderer _liquidRenderer;
        private Renderer _precipRenderer;
        private Transform[] _bubbles;
        private float _bubbleTime;
        private bool _broken;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            _container = GetComponent<ChemicalContainer>();
            if (liquid != null)
            {
                _liquidRenderer = liquid.GetComponent<Renderer>();
                if (liquidMaterial != null && _liquidRenderer != null)
                {
                    liquidMaterial = new Material(liquidMaterial);
                    _liquidRenderer.sharedMaterial = liquidMaterial;
                }
            }
            if (precipitate != null)
            {
                _precipRenderer = precipitate.GetComponent<Renderer>();
                if (precipitateMaterial != null && _precipRenderer != null)
                {
                    precipitateMaterial = new Material(precipitateMaterial);
                    _precipRenderer.sharedMaterial = precipitateMaterial;
                }
            }
            if (bubblesRoot != null)
            {
                _bubbles = new Transform[bubblesRoot.childCount];
                for (int i = 0; i < bubblesRoot.childCount; i++) _bubbles[i] = bubblesRoot.GetChild(i);
            }
            HideAll();
        }

        void Start()
        {
            // Первичная отрисовка (например, у стакана уже есть вода из сцены)
            Refresh();
        }

        void OnEnable()
        {
            if (_container != null) _container.Changed += OnChanged;
        }

        void OnDisable()
        {
            if (_container != null) _container.Changed -= OnChanged;
        }

        void Update()
        {
            // Плавная анимация пузырьков газа
            if (_bubbles != null && _bubbles.Length > 0 && _container.gasTimer > 0f && !_broken)
            {
                _bubbleTime += Time.deltaTime;
                float frac = Mathf.Clamp01(_container.FillFraction);
                float height = Mathf.Max(0.01f, innerHeight * frac);
                bubblesRoot.gameObject.SetActive(true);
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    var b = _bubbles[i];
                    float t = Mathf.Repeat(_bubbleTime * 0.35f + i * 0.13f, 1f);
                    float angle = i * 2.39996f; // золотой угол — равномерное распределение
                    float r = innerRadius * 0.6f * ((i % 3) + 1) / 3f;
                    b.localPosition = new Vector3(Mathf.Cos(angle) * r, bottomY + t * height, Mathf.Sin(angle) * r);
                    if (t > 0.93f) b.localScale = Vector3.one * 0.2f;
                    else if (t < 0.07f) b.localScale = Vector3.one * 0.3f;
                    else b.localScale = Vector3.one * 0.55f;
                }
            }
            else if (bubblesRoot != null && bubblesRoot.gameObject.activeSelf)
            {
                bubblesRoot.gameObject.SetActive(false);
            }
        }

        private void OnChanged(ChemicalContainer c) => Refresh();

        public void SetBroken(bool broken)
        {
            _broken = broken;
            if (broken) HideAll();
        }

        private void HideAll()
        {
            if (liquid != null) liquid.gameObject.SetActive(false);
            if (precipitate != null) precipitate.gameObject.SetActive(false);
            if (bubblesRoot != null) bubblesRoot.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (_broken || _container == null) return;

            float vol = _container.TotalVolume;
            float frac = _container.FillFraction;

            // Жидкость
            if (liquid != null)
            {
                if (vol > 0.05f)
                {
                    liquid.gameObject.SetActive(true);
                    float h = innerHeight * frac;
                    liquid.localScale = new Vector3(innerRadius * 2f, Mathf.Max(0.005f, h * 0.5f), innerRadius * 2f);
                    liquid.localPosition = new Vector3(0f, bottomY + h * 0.5f, 0f);
                    if (liquidMaterial != null)
                    {
                        var col = _container.GetLiquidColor();
                        liquidMaterial.SetColor(BaseColorId, col);
                        if (_liquidRenderer != null) _liquidRenderer.enabled = true;
                    }
                }
                else
                {
                    liquid.gameObject.SetActive(false);
                }
            }

            // Осадок
            if (precipitate != null)
            {
                if (_container.precipitateMl > 0.05f)
                {
                    precipitate.gameObject.SetActive(true);
                    float sedH = Mathf.Clamp(_container.precipitateMl * 0.0015f, 0.004f, innerHeight * 0.35f);
                    precipitate.localScale = new Vector3(innerRadius * 1.9f, sedH * 0.5f, innerRadius * 1.9f);
                    precipitate.localPosition = new Vector3(0f, bottomY + sedH * 0.5f, 0f);
                    if (precipitateMaterial != null)
                    {
                        var db = ChemistryDatabaseHolder.Instance;
                        var s = db != null ? db.GetSubstance(_container.precipitateSubstanceId) : null;
                        var col = s != null ? s.Color : new Color(0.7f, 0.7f, 0.7f, 1f);
                        col.a = 1f;
                        precipitateMaterial.SetColor(BaseColorId, col);
                    }
                }
                else
                {
                    precipitate.gameObject.SetActive(false);
                }
            }
        }
    }
}
