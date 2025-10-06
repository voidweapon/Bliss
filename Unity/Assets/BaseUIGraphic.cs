using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BaseUIGraphic : MonoBehaviour
{
    static readonly int ID_MaxValue = Shader.PropertyToID("_MaxValue");
    static readonly int ID_DiagramData = Shader.PropertyToID("_DiagramData");
    static readonly int ID_DataOffset = Shader.PropertyToID("_DataOffset");
    static readonly int ID_DataCount = Shader.PropertyToID("_DataCount");
    static readonly int ID_CategoryCount = Shader.PropertyToID("_CategoryCount");
    static readonly int ID_CategoryMask = Shader.PropertyToID("_CategoryMask");
    static readonly int ID_CategoryColors = Shader.PropertyToID("_CategoryColors");

    private const int CategoryCountLimit = 8;
    private const int CapacityLimit = 64;
    
    public Material DiagramMaterial;
    public Graphic Renderer;

    [Range(1, 60)]
    public int SamoleCountPerSecond;
    
    [Range(1, CapacityLimit)]
    public int Capacity;

    [Range(1, CategoryCountLimit)]
    public int CategoryCount;

    public ECategoryEnable CategoryMask = ECategoryEnable.ALL;

    public Color[] CategoryColors = new Color[8];
    
    private Material _runtimeMaterial;
    //ring buffer
    protected float[] _diagramData { get; private set; }
    protected int _head { get; private set; }
    protected int _tail { get; private set; }

    private bool _isDiagramDataDirty;
    private float _sampleIntervalS;
    private float _sampleTimer;
    private float _maxValue;
    
    private void Awake()
    {
        Capacity = Mathf.Clamp(Capacity, 1, CapacityLimit);
        CategoryCount = Mathf.Clamp(CategoryCount, 1, CategoryCountLimit);
        
        _diagramData = new float[Capacity * CategoryCount];
        _isDiagramDataDirty = true;
        _maxValue = float.MinValue;
        _runtimeMaterial = new Material(DiagramMaterial);
        Renderer.material = _runtimeMaterial;
        _sampleIntervalS = 1.0f / SamoleCountPerSecond;
    }
    

    // Update is called once per frame
    void Update()
    {
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= _sampleIntervalS)
        {
            _sampleTimer -= _sampleIntervalS;
            Sample();
        }
        //_head = (_head + CategoryCount) % Capacity;
    }

    void LateUpdate()
    {
        if (_isDiagramDataDirty)
        {
            _isDiagramDataDirty = false;
            _runtimeMaterial.SetInt(ID_DataCount, Capacity);
            _runtimeMaterial.SetInt(ID_DataOffset, _head);
            _runtimeMaterial.SetFloatArray(ID_DiagramData,  _diagramData);
            _runtimeMaterial.SetFloat(ID_MaxValue, _maxValue);
            _runtimeMaterial.SetFloat(ID_CategoryCount, CategoryCount);
            _runtimeMaterial.SetInt(ID_CategoryMask, (int)CategoryMask);
            _runtimeMaterial.SetColorArray(ID_CategoryColors, CategoryColors);
        }
    }

    private void Sample()
    {
        int indexBase = (_head - 1 + Capacity) % Capacity;
        for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
        {
            float value = CollectData(categoryIndex);

            _diagramData[indexBase * CategoryCount + categoryIndex] = value;
        }

        float newMaxValue = GetMax();
        _maxValue = newMaxValue;
        
        _head = (_head + 1) % Capacity;
        _isDiagramDataDirty = true;
    }

    protected virtual float CollectData(int categoryIndex)
    {
        return Random.Range(categoryIndex * 25, categoryIndex * 25 + 50f);
    }

    protected virtual float GetMax()
    {
        float max = float.MinValue;
        for (int i = 0; i < _diagramData.Length; i++)
        {
            if (max < _diagramData[i])
            {
                max = _diagramData[i];
            }
        }
        return max;
    }


    public void EnableCategory(int categoryIndex, bool enable)
    {
        if (enable)
        {
            CategoryMask = (ECategoryEnable)((int)CategoryMask | (1 << categoryIndex));
        }
        else
        {
            CategoryMask = (ECategoryEnable)((int)CategoryMask & (~(1 << categoryIndex)));
        }
    }
    
    [Flags]
    public enum ECategoryEnable : byte
    {
        None = 0,
        
        Category1 = 1 << 0,
        Category2 = 1 << 1,
        Category3 = 1 << 2,
        Category4 = 1 << 3,
        Category5 = 1 << 4,
        Category6 = 1 << 5,
        Category7 = 1 << 6,
        Category8 = 1 << 7,
        
        ALL = Category1 |  Category2 | Category3 | Category4 | Category5 | Category6 | Category7 | Category8
    }
}
