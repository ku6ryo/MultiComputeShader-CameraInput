using UnityEngine;
using UnityEngine.UI;

public class WebcamInput : MonoBehaviour
{
    struct ThreadSize
    {
      public uint x;
      public uint y;
      public uint z;

      public ThreadSize(uint x, uint y, uint z)
      {
        this.x = x;
        this.y = y;
        this.z = z;
      }
    }

    [SerializeField] Vector2Int _resolution = new Vector2Int(1024, 1024);
    [SerializeField] RawImage processedImage;
    [SerializeField] ComputeShader glitchShader;
    [SerializeField] ComputeShader grayScaleShader;
    [SerializeField] ComputeShader lendsShader;

    WebCamTexture _webcamTexture;
    RenderTexture _tmpRenderTexture1;
    RenderTexture _tmpRenderTexture2;

    void Start()
    {
        _webcamTexture = new WebCamTexture("", _resolution.x, _resolution.y);
        _tmpRenderTexture1 = new RenderTexture(_resolution.x, _resolution.y, 0);
        _tmpRenderTexture1.enableRandomWrite = true;
        _tmpRenderTexture1.Create();
        _tmpRenderTexture2 = new RenderTexture(_resolution.x, _resolution.y, 0);
        _tmpRenderTexture2.enableRandomWrite = true;
        _tmpRenderTexture2.Create();
        _webcamTexture.Play();
    }

    void OnDestroy()
    {
        if (_webcamTexture != null) Destroy(_webcamTexture);
        if (_tmpRenderTexture1 != null) Destroy(_tmpRenderTexture1);
        if (_tmpRenderTexture2 != null) Destroy(_tmpRenderTexture2);
    }

    void ApplyGrayScale (Texture source, RenderTexture result)
    {
        var kernelIndex = grayScaleShader.FindKernel("CSMain");
        ThreadSize threadSize = new ThreadSize();
        grayScaleShader.GetKernelThreadGroupSizes(kernelIndex, out threadSize.x, out threadSize.y, out threadSize.z);
        grayScaleShader.SetTexture(kernelIndex, "Input", source);
        grayScaleShader.SetTexture(kernelIndex, "Result", result);
        grayScaleShader.Dispatch(
          kernelIndex,
          _resolution.x / (int) threadSize.x,
          _resolution.y / (int) threadSize.y,
          (int) threadSize.z
        );
    }
    void ApplyGlitch (Texture source, RenderTexture result)
    {
        var kernelIndex = glitchShader.FindKernel("CSMain");
        ThreadSize threadSize = new ThreadSize();
        glitchShader.GetKernelThreadGroupSizes(kernelIndex, out threadSize.x, out threadSize.y, out threadSize.z);
        glitchShader.SetTexture(kernelIndex, "Input", source);
        glitchShader.SetTexture(kernelIndex, "Result", result);
        glitchShader.SetFloat("pos", Mathf.Abs(Mathf.Sin(Time.time)) * _resolution.y);
        glitchShader.Dispatch(
          kernelIndex,
          _resolution.x / (int) threadSize.x,
          _resolution.y / (int) threadSize.y,
          (int) threadSize.z
        );
    }

    void ApplyLends(Texture source, RenderTexture result)
    {
        var shader = lendsShader;
        var kernelIndex = shader.FindKernel("CSMain");
        ThreadSize threadSize = new ThreadSize();
        shader.GetKernelThreadGroupSizes(kernelIndex, out threadSize.x, out threadSize.y, out threadSize.z);
        shader.SetTexture(kernelIndex, "Input", source);
        shader.SetTexture(kernelIndex, "Result", result);
        shader.SetFloats("center", new float[]{
          Mathf.Abs(Mathf.Cos(Time.time)) * _resolution.y,
          Mathf.Abs(Mathf.Sin(Time.time)) * _resolution.y
        });
        shader.SetFloat("R", 500);
        shader.Dispatch(
          kernelIndex,
          _resolution.x / (int) threadSize.x,
          _resolution.y / (int) threadSize.y,
          (int) threadSize.z
        );
    }

    void Update()
    {
        if (!_webcamTexture.didUpdateThisFrame) return;

        var aspect1 = (float)_webcamTexture.width / _webcamTexture.height;
        var aspect2 = (float)_resolution.x / _resolution.y;
        var gap = aspect2 / aspect1;

        var vflip = _webcamTexture.videoVerticallyMirrored;
        var scale = new Vector2(gap, vflip ? -1 : 1);
        var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

        ApplyGrayScale(_webcamTexture, _tmpRenderTexture1);
        ApplyGlitch(_tmpRenderTexture1, _tmpRenderTexture2);
        ApplyLends(_tmpRenderTexture2, _tmpRenderTexture1);
        processedImage.texture = _tmpRenderTexture1;
    }
}
