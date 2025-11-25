using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class CameraFeed : MonoBehaviour
{
    public RawImage rawImage;
    private WebCamTexture camTexture;
    public AspectRatioFitter fitter;

    void Start()
    {
        // Pick the first back-facing camera
        WebCamDevice[] devices = WebCamTexture.devices;
        string backCamName = null;
        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                backCamName = device.name;
                break;
            }
        }

        if (backCamName == null && devices.Length > 0)
        {
            backCamName = devices[0].name;
        }

        camTexture = new WebCamTexture(backCamName, Screen.width, Screen.height, 30);
        rawImage.texture = camTexture;
        camTexture.Play();

        int angle = camTexture.videoRotationAngle;

        RectTransform imageRect = rawImage.rectTransform;
        RectTransform parent = imageRect.parent as RectTransform;

        imageRect.localEulerAngles = new Vector3(0, 0, -angle);

        switch (angle)
        {
            case 90:
            case 270:
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parent.rect.height);
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parent.rect.width);
                break;
        }

        // Fix vertical mirroring
        if (camTexture.videoVerticallyMirrored)
        {
            imageRect.localScale = new Vector3(1, -1, 1);
        }
        else
        {
            imageRect.localScale = Vector3.one;
        }

        //// Flip vertically if needed
        //if (camTexture.videoVerticallyMirrored)
        //{
        //    Rect r = rawImage.uvRect;
        //    r = new Rect(r.x, 1 - r.y, r.width, -r.height);
        //    rawImage.uvRect = r;
        //}

        //RenderPipelineManager.endContextRendering += BlitToScreen;
    }

    //private void OnPreRender()
    //{

    //    Graphics.Blit(camTexture, (RenderTexture)null);
    //}

    //private void Update()
    //{
    //}

    //private void BlitToScreen(ScriptableRenderContext context, List<Camera> cameras)
    //{
    //    Graphics.Blit(camTexture, (RenderTexture)null);
    //}

    //private void OnDestroy()
    //{
    //    RenderPipelineManager.endContextRendering -= BlitToScreen;

    //}

    //void Update()
    //{
    //    // Optionally rotate the image based on device orientation
    //    rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -camTexture.videoRotationAngle);
    //}
}
