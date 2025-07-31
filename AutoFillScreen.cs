using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LanyueUI;

public class AutoFillScreen : MonoBehaviour, IMeshModifier
{
    public Vector2 size;
    public bool enable = false;
    public bool bbb = true;
    public Vector2 wh;
    public Graphic graphic;
    public bool isModifyMesh;
    public void ModifyMesh(Mesh mesh)
    {
        
    }

    public void ModifyMesh(VertexHelper verts)
    {
        isModifyMesh = true;
    }

    public void SetSize()
    {
        graphic = GetComponent<Graphic>();
        if (!graphic || !graphic.mainTexture)
            return;
        float w = graphic.mainTexture.width;
        float h = graphic.mainTexture.height;
        if (w == wh.x && h == wh.y)
            return;
        float texRate = w / h;
        float screenRate = (float)Screen.width / Screen.height;
        float resultW, resultH;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var canvasW = (canvas.transform as RectTransform).rect.width;
        var canvasH = (canvas.transform as RectTransform).rect.height;
        if (screenRate < texRate)
        {
            resultW = canvasH * texRate;
            resultH = canvasH;
        }
        else
        {
            resultW = canvasW;
            resultH = canvasW / texRate;
        }
        (transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resultW);
        (transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resultH);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isModifyMesh)
        {
            isModifyMesh = false;
            SetSize();
        }
        if (!enable)
            return;
        if (bbb)
            (transform as RectTransform).sizeDelta = size;
        else
        {
            (transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            (transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
    }
}
