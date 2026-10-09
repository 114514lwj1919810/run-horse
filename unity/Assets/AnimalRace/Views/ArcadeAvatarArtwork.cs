using UnityEngine;

// Resolution-independent UI geometry. The ROM animal artwork is unchanged.
public static class ArcadeAvatarArtwork
{
    public const int FrameSize = 384;
    private static readonly Color[] BandColors = {
        new Color(1f,.70f,.13f), new Color(1f,.40f,.66f),
        new Color(.18f,.76f,1f), new Color(.98f,.39f,.73f),
        new Color(.60f,.43f,1f), new Color(.22f,.84f,.40f) };
    private static readonly float[] LampAngles = { -150f,-175f,160f,135f,110f,85f,60f,35f,5f,-25f };

    // The recovered ROM artwork contains two high/low rows. Their bulbs are
    // offset from each other, and alternating those two complete images is
    // what creates the apparent rotation. Rebuild both states at high
    // resolution, preserving that two-frame mechanism.
    public static Texture2D CreateFrame(int index, bool alternate)
    {
        index = Mathf.Clamp(index,0,5);
        var pixels = new Color[FrameSize*FrameSize];
        var lamps = new Vector2[LampAngles.Length];
        for(int i=0;i<lamps.Length;i++)
        {
            float angle=LampAngles[i]+(alternate?16f:0f);
            if(angle>180f) angle-=360f;
            float a=angle*Mathf.Deg2Rad;
            lamps[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.87f;
        }
        Color band=BandColors[index];
        Color outline=new Color(.23f,.20f,.16f);
        for(int y=0;y<FrameSize;y++) for(int x=0;x<FrameSize;x++)
        {
            // Match the painted bounds of the old 107x97 crop, including its side padding.
            var p=new Vector2(((x+.5f)/FrameSize-.5f)/.455f,
                (.5f-(y+.5f)/FrameSize)/.49f);
            float r=p.magnitude;
            Color c=outline;
            if(r<.968f && r>.755f)
            {
                c=band;
                if(p.y<-.35f) c=Color.Lerp(c,Color.white,.25f);
            }
            else if(r<.727f) c=new Color(.95f,.96f,.96f);
            if(r>.77f && r<.98f)
            {
                float distance=10f;
                for(int i=0;i<lamps.Length;i++)
                {
                    float candidate=(p-lamps[i]).sqrMagnitude;
                    if(candidate<distance) distance=candidate;
                }
                distance=Mathf.Sqrt(distance);
                if(distance<.066f)
                {
                    Color rim=index==4?new Color(.25f,.26f,1f):
                        (index==5?new Color(.1f,.62f,.2f):new Color(.98f,.80f,.12f));
                    Color idleCore=new Color(1f,.72f,.28f);
                    Color bulb=distance>.049f?rim:
                        Color.Lerp(idleCore,Color.white,
                            Mathf.Clamp01((.05f-distance)/.035f)*.72f);
                    c=Color.Lerp(c,bulb,Mathf.Clamp01((.066f-distance)*FrameSize*.45f));
                }
            }
            c.a=Mathf.Clamp01((1f-r)*FrameSize*.45f);
            pixels[y*FrameSize+x]=c;
        }
        return MakeTexture(FrameSize,pixels,"Arcade ring "+index+
            (alternate?" frame B":" frame A"));
    }

    public static Texture2D CreateHistoryPortrait(Texture2D source, Rect uv)
    {
        // Find the painted silhouette within the original crop. Different animals
        // have different margins (in particular the elephant's trunk).
        int w=Mathf.Max(1,Mathf.RoundToInt(uv.width*source.width));
        int h=Mathf.Max(1,Mathf.RoundToInt(uv.height*source.height));
        int minX=w,minY=h,maxX=-1,maxY=-1;
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            if(source.GetPixelBilinear(uv.x+(x+.5f)/w*uv.width,uv.y+(y+.5f)/h*uv.height).a<.1f) continue;
            minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);
            minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
        }
        if(maxX>=minX && maxY>=minY)
            uv=new Rect(uv.x+uv.width*minX/w,uv.y+uv.height*minY/h,
                uv.width*(maxX-minX+1)/w,uv.height*(maxY-minY+1)/h);
        float aspect=(uv.width*source.width)/(uv.height*source.height);
        float height=.87f, width=height*aspect;
        if(width>.87f){height*=.87f/width;width=.87f;}
        return CreateMaskedPortrait(source,uv,new Rect((1f-width)*.5f,(1f-height)*.5f,width,height),true);
    }

    // placement is in top-left-origin normalized coordinates within the destination frame.
    public static Texture2D CreateMaskedPortrait(Texture2D source,Rect uv,Rect placement,bool history)
    {
        int size=history?192:FrameSize;
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            float nx=(x+.5f)/size, ny=1f-(y+.5f)/size;
            float u=(nx-placement.x)/placement.width, v=(ny-placement.y)/placement.height;
            if(u<0f || u>1f || v<0f || v>1f) continue;
            float rx=history ? .433f : .455f*.985f, ry=history ? .433f : .49f*.985f;
            float radial=new Vector2((nx-.5f)/rx,(ny-.5f)/ry).magnitude;
            Color c=source.GetPixelBilinear(uv.x+u*uv.width,uv.y+(1f-v)*uv.height);
            c.a*=Mathf.Clamp01((1f-radial)*size*.43f);
            pixels[y*size+x]=c;
        }
        return MakeTexture(size,pixels,history?"History portrait mask":"Bottom portrait mask");
    }

    private static Texture2D MakeTexture(int size,Color[] pixels,string name)
    {
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        texture.name=name;
        texture.wrapMode=TextureWrapMode.Clamp;
        texture.filterMode=FilterMode.Bilinear;
        texture.SetPixels(pixels);
        texture.Apply(false,false);
        return texture;
    }
}
