﻿
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class StaticObj_Render : MonoBehaviour 
{
    private RenderTexture mDecalTex;
    private Material[] mMats;
    private int mWidth = 128;
    private int mHeight = 128;
    private bool mNewMat = false;

    public int mSimuBufWidth = 200;            // Simulated rendertexture pixel information
    public int mSimuBufHeight = 200;           // Simulated rendertexture pixel information
    private List<int> mSimuBuf = new List<int>();

    private int mPositiveNum = 0;
    private int mOppositeNum = 0;

    private bool mUV = true;
    private bool mOnceRenderGScnRT = false;

	// Use this for initialization
	void Start ()
    {
        mMats = GetComponent<Renderer>().materials;

        for (int j = 0; j < mSimuBufHeight; j++)
        {
            for(int i = 0; i < mSimuBufWidth; i++)
            {
                mSimuBuf.Add(-1);
            }
        }    
	}
	
	// Update is called once per frame
	void Update ()
    {
        // Only execute once
        if (PaintResourceManager.Instance != null)
        {
            if (mDecalTex == null)
            {
                mWidth = PaintResourceManager.Instance.mDecalWidth;
                mHeight = PaintResourceManager.Instance.mDecalHeight;
                mDecalTex = new RenderTexture(mWidth, mHeight, 0);
                mDecalTex.Create();
            }
        }
	}

    public RenderTexture GetDecalTex() { return mDecalTex; }

    public void Render(Vector2 texcoord, Material quadmat, Color quadcolor, int coltype,
                        string decalname, float decalwidht, float decalheight, float decalrot)
    {
        // Use new material
        if (!mNewMat)
        {
            for (int i = 0; i < mMats.Length; i++ )
            {
                Material mat = mMats[i];
                if (mat == null)
                    continue;

                SetMaterial(mat);

                mat.SetTexture("_DecalTex", mDecalTex);

                Texture _Normal = PaintResourceManager.Instance.GetDecalBumpTex("T_Detail_Rocky_N");
                mat.SetTexture("_DecalBump", _Normal);

                Cubemap cube = PaintResourceManager.Instance.GetCubemap("SkyboxCube");
                mat.SetTexture("_DecalSky", cube);
            }

            mNewMat = true;
        }

        Graphics.SetRenderTarget(mDecalTex);

        // Render full screen once
        OnceRenderGlobalScreenRT(quadmat);

        PaintResourceManager.DecalInfo info = PaintResourceManager.Instance.GetDecalTex(decalname);
        if (info.texture == null)
            return;

        quadmat.SetTexture("_MainTex", info.texture);
        quadmat.SetColor("_Color", quadcolor);
        quadmat.SetFloat("_Rotation", decalrot);
        quadmat.SetVector("_CenterPos", texcoord);

        float width = decalwidht / 20.0f;
        float height = decalheight / 20.0f;

        int decalRow = info.row;
        int decalCol = info.column;

        int randomx = Random.Range(0, decalRow - 1);
        int randomy = Random.Range(0, decalCol - 1);

        float unitx = (1.0f / decalRow);
        float unity = (1.0f / decalCol);

        float startuvx = unitx * randomx;
        float startuvy = unity * randomy;

        quadmat.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        if (mUV)
            GL.TexCoord2(startuvx, startuvy);
        else
            GL.TexCoord2(0, 0);
        GL.Vertex3(texcoord.x - width, texcoord.y - height, 0);

        if (mUV)
            GL.TexCoord2(startuvx, startuvy + unity);
        else
            GL.TexCoord2(0, 1);
        GL.Vertex3(texcoord.x - width, texcoord.y + height, 0);

        if (mUV)
            GL.TexCoord2(startuvx + unitx, startuvy + unity);
        else
            GL.TexCoord2(1, 1);
        GL.Vertex3(texcoord.x + width, texcoord.y + height, 0);

        if (mUV)
            GL.TexCoord2(startuvx + unitx, startuvy);
        else
            GL.TexCoord2(1, 0);
        GL.Vertex3(texcoord.x + width, texcoord.y - height, 0);

        GL.End();
        GL.PopMatrix();

        Graphics.SetRenderTarget(Camera.main.targetTexture);

        WriteSimuBufData(width, height, texcoord, coltype);
    }

    // Write simulation data ......
    // There will be slight accuracy loss (the coverage area of the map is not square)
    private void WriteSimuBufData(float decalwid, float decalhei, Vector2 decaluv, int coltype)
    {
        int centerx = (int)(decaluv.x * mSimuBufWidth);
        int centery = (int)(decaluv.y * mSimuBufHeight);

        int width = (int)(decalwid * mSimuBufWidth);
        int height = (int)(decalhei * mSimuBufHeight);

        for (int j = (centery - height); j < (centery + height); j++)
        {
            for(int i = (centerx - width); i < (centerx + width); i++)
            {
                if (i < 0)
                    continue;

                if (i > (mSimuBufWidth - 1))
                    continue;

                if (j < 0)
                    continue;

                if (j > (mSimuBufHeight - 1))
                    continue;

                int pos = ((mSimuBufHeight - 1) - j) * mSimuBufWidth + i;
                if (pos > (mSimuBufWidth * mSimuBufHeight - 1))
                    continue;

                mSimuBuf[pos] = coltype;
            }
        }

        // Test whether the cached data is consistent with the texture data
#if false

        Texture2D testtex = new Texture2D(mSimuBufWidth, mSimuBufHeight, TextureFormat.RGB24, false);
        for (int j = 0; j < mSimuBufHeight; j++)
        {
            for(int i = 0; i < mSimuBufWidth; i++)
            {
                int pos = j * mSimuBufWidth + i;
                int type = mSimuBuf[pos];
                Color col = Color.black;
                switch(type)
                {
                    case (int)xEnumDefine.TeamFlag.Blue:
                        col = Color.blue;
                        break;
                    case (int)xEnumDefine.TeamFlag.Yellow:
                        col = Color.yellow;
                        break;
                }

                testtex.SetPixel(i, j, col);
            }
        }

        testtex.Apply();
        byte[] byt = testtex.EncodeToPNG();
        File.WriteAllBytes(name + ".png", byt);
#endif

    }

    public int GetPositiveNum() { return mPositiveNum; }
    public int GetOppositeNum() { return mOppositeNum; }

    public void ComputeSimuBuf(int positiveid, int oppositeid)
    {
        mPositiveNum = 0;
        mOppositeNum = 0;

        for (int j = 0; j < mSimuBufHeight; j++)
        {
            for (int i = 0; i < mSimuBufWidth; i++)
            {
                int pos = j * mSimuBufWidth + i;
                int type = mSimuBuf[pos];
                
                if (type == positiveid)
                {
                    mPositiveNum++;
                }
                else if(type == oppositeid)
                {
                    mOppositeNum++;
                }
            }
        }
    }

    public int GetSimuBufPixel(Vector2 uv)
    {
        int centerx = (int)(uv.x * mSimuBufWidth);
        int centery = (int)(uv.y * mSimuBufHeight);

        if (centerx < 0)
            return (int)xEnumDefine.TeamFlag.Invalid;

        if (centerx > (mSimuBufWidth - 1) )
            return (int)xEnumDefine.TeamFlag.Invalid;

        if (centery < 0)
            return (int)xEnumDefine.TeamFlag.Invalid;

        if (centery > (mSimuBufHeight - 1) )
            return (int)xEnumDefine.TeamFlag.Invalid;

        int pos = ( (mSimuBufHeight - 1) - centery) * mSimuBufWidth + centerx;


#if false
        Texture2D testtex = new Texture2D(mSimuBufWidth, mSimuBufHeight, TextureFormat.RGB24, false);
        for (int j = 0; j < mSimuBufHeight; j++)
        {
            for (int i = 0; i < mSimuBufWidth; i++)
            {
                int simupos = j * mSimuBufWidth + i;
                int type = mSimuBuf[simupos];
                Color col = Color.black;
                switch (type)
                {
                    case (int)xEnumDefine.TeamFlag.Blue:
                        col = Color.blue;
                        break;
                    case (int)xEnumDefine.TeamFlag.Yellow:
                        col = Color.yellow;
                        break;
                }

                testtex.SetPixel(i, j, col);
            }
        }

        for (int j = centery - 2; j < centery + 3; j++ )
        {
            for (int i = centerx - 2; i < centerx + 3; i++)
            {
                int simupos = j * mSimuBufWidth + i;
                int type = mSimuBuf[simupos];
                testtex.SetPixel(i, j, Color.green);
            }
        }

        testtex.Apply();
        byte[] byt = testtex.EncodeToPNG();
        File.WriteAllBytes("test" + ".png", byt);
#endif

        return mSimuBuf[pos];
    }

    void OnceRenderGlobalScreenRT(Material quadmat)
    {
        if (mOnceRenderGScnRT)
            return;

        quadmat.SetPass(1);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);

        GL.Vertex3(0, 0, 0);
        GL.Vertex3(0, 1, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(1, 0, 0);

        GL.End();
        GL.PopMatrix();

        mOnceRenderGScnRT = true;
    }


    void SetMaterial(Material mat)
    {
        mat.shader = Shader.Find("SpraySoldier/Mobile/StaticObjDecal");
        
    }
}
