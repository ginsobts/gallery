Shader "Hidden/GalleryArtistic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TexelSize ("Texel Size", Vector) = (0.004, 0.004, 0, 0)
        _Style ("Style", Int) = 0
        _Intensity ("Intensity", Range(0, 1)) = 1
        _EdgeThreshold ("Edge Threshold", Float) = 0.15
        _EdgeColor ("Edge Color", Color) = (0.1, 0.08, 0.05, 1)
        _PaperColor ("Paper Color", Color) = (0.96, 0.94, 0.9, 1)
        _QuantizeLevels ("Quantize Levels", Float) = 6
        _PixelSize ("Pixel Size", Float) = 8
        _BrushSize ("Brush Size", Float) = 3
        _HatchDensity ("Hatch Density", Float) = 80
        _Time2 ("Time Anim", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; float4 color : COLOR; };

            sampler2D _MainTex;
            float4 _TexelSize;
            int _Style;
            float _Intensity;
            float _EdgeThreshold;
            float4 _EdgeColor;
            float4 _PaperColor;
            float _QuantizeLevels;
            float _PixelSize;
            float _BrushSize;
            float _HatchDensity;
            float _Time2;

            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float lum(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }
            float hsh(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float nse(float2 p) { float2 i = floor(p); float2 f = frac(p); f = f*f*(3.0-2.0*f); return lerp(lerp(hsh(i),hsh(i+float2(1,0)),f.x), lerp(hsh(i+float2(0,1)),hsh(i+float2(1,1)),f.x), f.y); }

            float sobelEdge(float2 uv, float2 ts)
            {
                float tl = lum(tex2D(_MainTex, uv + float2(-ts.x, ts.y)).rgb);
                float tm2 = lum(tex2D(_MainTex, uv + float2(0, ts.y)).rgb);
                float tr = lum(tex2D(_MainTex, uv + float2(ts.x, ts.y)).rgb);
                float ml = lum(tex2D(_MainTex, uv + float2(-ts.x, 0)).rgb);
                float mr = lum(tex2D(_MainTex, uv + float2(ts.x, 0)).rgb);
                float bl = lum(tex2D(_MainTex, uv + float2(-ts.x, -ts.y)).rgb);
                float bm = lum(tex2D(_MainTex, uv + float2(0, -ts.y)).rgb);
                float br = lum(tex2D(_MainTex, uv + float2(ts.x, -ts.y)).rgb);
                float gx = -tl - 2.0*ml - bl + tr + 2.0*mr + br;
                float gy = -tl - 2.0*tm2 - tr + bl + 2.0*bm + br;
                return sqrt(gx*gx + gy*gy);
            }

            // 1: Pencil — hatching + paper grain + subtle hand-tremble
            float4 stylePencil(float2 uv, float2 ts)
            {
                float t = _Time2;
                float2 tremble = float2(sin(t*1.3 + uv.y*40.0), cos(t*0.9 + uv.x*40.0)) * ts * 0.3;
                float2 uv2 = uv + tremble;
                float edge = sobelEdge(uv2, ts);
                float l = lum(tex2D(_MainTex, uv2).rgb);
                float d = _HatchDensity;
                float l1 = frac((uv2.x + uv2.y) * d);
                float l2 = frac((uv2.x - uv2.y) * d);
                float l3 = frac(uv2.x * d * 1.4);
                float s = 1.0;
                if (l < 0.25) s = step(0.6, l1) * step(0.6, l2) * step(0.6, l3);
                else if (l < 0.45) s = step(0.5, l1) * step(0.5, l2);
                else if (l < 0.65) s = step(0.45, l1);
                float pencil = s * (1.0 - smoothstep(_EdgeThreshold*0.4, _EdgeThreshold*2.5, edge));
                float paperGrain = nse(uv * 600.0) * 0.08;
                float3 paper = _PaperColor.rgb + paperGrain;
                float3 r = lerp(_EdgeColor.rgb * 0.9, paper, pencil);
                return float4(r, 1.0);
            }

            // 2: Oil — Kuwahara + vibrant saturation boost
            float4 styleOil(float2 uv, float2 ts)
            {
                int rad = clamp((int)_BrushSize, 1, 4);
                float n = float((rad+1)*(rad+1));
                float3 m0=float3(0,0,0); float v0=0;
                float3 m1=float3(0,0,0); float v1=0;
                float3 m2=float3(0,0,0); float v2=0;
                float3 m3=float3(0,0,0); float v3=0;
                for (int yy=0; yy<=rad; yy++) { for (int xx=0; xx<=rad; xx++) {
                    float3 c0=tex2D(_MainTex,uv+float2(-xx,-yy)*ts).rgb; m0+=c0; v0+=dot(c0,c0);
                    float3 c1=tex2D(_MainTex,uv+float2( xx,-yy)*ts).rgb; m1+=c1; v1+=dot(c1,c1);
                    float3 c2=tex2D(_MainTex,uv+float2(-xx, yy)*ts).rgb; m2+=c2; v2+=dot(c2,c2);
                    float3 c3=tex2D(_MainTex,uv+float2( xx, yy)*ts).rgb; m3+=c3; v3+=dot(c3,c3);
                } }
                m0/=n; v0=v0/n-dot(m0,m0); m1/=n; v1=v1/n-dot(m1,m1);
                m2/=n; v2=v2/n-dot(m2,m2); m3/=n; v3=v3/n-dot(m3,m3);
                float minV=v0; float3 res=m0;
                if(v1<minV){minV=v1;res=m1;} if(v2<minV){minV=v2;res=m2;} if(v3<minV){res=m3;}
                float g=lum(res); res=lerp(float3(g,g,g),res,1.4);
                float canvas = nse(uv * 300.0) * 0.04;
                res = res + canvas;
                return float4(saturate(res), 1.0);
            }

            // 3: Watercolor — wet edge bleed + paper texture + color pooling
            float4 styleWater(float2 uv, float2 ts)
            {
                float t = _Time2;
                float2 flow = float2(nse(uv*30.0+t*0.2), nse(uv*30.0+float2(5,5)+t*0.15)) - 0.5;
                float2 warpUV = uv + flow * ts * _BrushSize * 3.0;
                float3 col = float3(0,0,0);
                col += tex2D(_MainTex, warpUV).rgb * 0.3;
                col += tex2D(_MainTex, warpUV + flow*ts*1.5).rgb * 0.25;
                col += tex2D(_MainTex, warpUV - flow*ts*1.0).rgb * 0.25;
                col += tex2D(_MainTex, warpUV + float2(flow.y,-flow.x)*ts*0.8).rgb * 0.2;
                float edge = sobelEdge(uv, ts * 1.5);
                float wetEdge = smoothstep(0.1, 0.35, edge) * 0.25;
                col = col * (1.0 - wetEdge);
                float g = lum(col); col = lerp(float3(g,g,g), col, 0.8);
                float paper = nse(uv * 400.0) * 0.06;
                col = lerp(col, _PaperColor.rgb, 0.12 + paper);
                col = floor(col * 10.0 + 0.5) / 10.0;
                return float4(col, 1.0);
            }

            // 4: Pixel — CRT sub-pixel + scanlines
            float4 stylePixel(float2 uv)
            {
                float2 pu = floor(uv * _PixelSize) / _PixelSize;
                float3 c = tex2D(_MainTex, pu).rgb;
                c = floor(c * _QuantizeLevels) / _QuantizeLevels;
                float2 sub = frac(uv * _PixelSize);
                float3 mask = float3(1,1,1);
                if (sub.x < 0.33) mask = float3(1.2, 0.8, 0.8);
                else if (sub.x < 0.66) mask = float3(0.8, 1.2, 0.8);
                else mask = float3(0.8, 0.8, 1.2);
                c = c * mask;
                float scanBright = 0.92 + 0.08 * sin(sub.y * 3.14159);
                c = c * scanBright;
                return float4(saturate(c), 1.0);
            }

            // 5: Comic — bold ink lines + halftone dots + color pop
            float4 styleComic(float2 uv, float2 ts)
            {
                float3 col = tex2D(_MainTex, uv).rgb;
                col = floor(col * 5.0 + 0.5) / 5.0;
                float g = lum(col); col = lerp(float3(g,g,g), col, 1.8); col = saturate(col);
                float edge = sobelEdge(uv, ts * 1.8);
                float ink = smoothstep(_EdgeThreshold*0.3, _EdgeThreshold*1.2, edge);
                float l = lum(col);
                float dotSize = (1.0 - l) * 0.42;
                float2 dotUV = frac(uv * _HatchDensity) - 0.5;
                float dot2 = 1.0 - smoothstep(dotSize - 0.05, dotSize + 0.05, length(dotUV));
                float shade = lerp(1.0, 0.65, dot2 * step(l, 0.6));
                col = col * shade;
                col = lerp(col, _EdgeColor.rgb, ink * 0.9);
                float speed = step(0.97, hsh(floor(uv * float2(3, 80)))) * 0.15;
                col = col - speed;
                return float4(saturate(col), 1.0);
            }

            // 6: Impressionist — thick directional brush strokes
            float4 styleImpressionist(float2 uv, float2 ts)
            {
                float t = _Time2;
                float3 col = float3(0,0,0);
                float tw = 0.0;
                for (int s = 0; s < 12; s++)
                {
                    float ang = float(s) * 0.5236 + nse(uv * 20.0 + float2(float(s)*0.3, 0)) * 0.5;
                    float r = _BrushSize * (0.4 + 0.6 * nse(uv * 15.0 + float2(0, float(s)*0.2)));
                    float2 off = float2(cos(ang), sin(ang)) * ts * r;
                    float w = 1.0 / (1.0 + float(s) * 0.15);
                    col += tex2D(_MainTex, uv + off).rgb * w;
                    tw += w;
                }
                col /= tw;
                float g = lum(col); col = lerp(float3(g,g,g), col, 1.5);
                float canvas = nse(uv * 200.0 + t * 0.1) * 0.05;
                col = col + canvas;
                col = floor(col * 16.0 + 0.5) / 16.0;
                return float4(saturate(col), 1.0);
            }

            // 7: Pointillism — colored dots on warm paper
            float4 stylePointillism(float2 uv, float2 ts)
            {
                float cs = _BrushSize * 2.0;
                float2 cell = floor(uv / (ts*cs));
                float2 cc = (cell + 0.5) * ts * cs;
                float3 col = tex2D(_MainTex, cc).rgb;
                float g = lum(col); col = lerp(float3(g,g,g), col, 1.6);
                float d = length((uv - cc) / (ts*cs));
                float l = lum(col);
                float radius = 0.32 + l * 0.18;
                float circle = smoothstep(radius, radius - 0.08, d);
                float3 paper = _PaperColor.rgb * (0.95 + nse(uv*300.0)*0.06);
                float3 r = lerp(paper, col * 1.15, circle);
                return float4(saturate(r), 1.0);
            }

            // 8: Woodcut — deep contrast ink + wood grain
            float4 styleWoodcut(float2 uv, float2 ts)
            {
                float l = lum(tex2D(_MainTex, uv).rgb);
                float edge = sobelEdge(uv, ts * 1.3);
                float grain = sin((uv.x*0.6 + uv.y*0.4) * _HatchDensity + l*8.0 + nse(uv*50.0)*2.0) * 0.5 + 0.5;
                float thr = l * 0.85 + edge * 0.25;
                float ink = smoothstep(0.35, 0.45, grain * thr);
                float woodTex = nse(uv * float2(8, 80)) * 0.08;
                float3 inkC = _EdgeColor.rgb * (0.9 + woodTex);
                float3 paperC = _PaperColor.rgb * (0.95 + woodTex * 0.5);
                float3 r = lerp(inkC, paperC, ink);
                return float4(r, 1.0);
            }

            // 9: Charcoal — smudge + strong grain + paper texture
            float4 styleCharcoal(float2 uv, float2 ts)
            {
                float t = _Time2;
                float2 smudge = (float2(nse(uv*40.0+t*0.05), nse(uv*40.0+float2(3,3)+t*0.03)) - 0.5) * ts * 2.0;
                float l = lum(tex2D(_MainTex, uv + smudge).rgb);
                float edge = sobelEdge(uv, ts * 1.5);
                float grain1 = nse(uv * 600.0) * 0.35;
                float grain2 = nse(uv * 200.0 + float2(50, 50)) * 0.2;
                float val = l + grain1 - grain2 - edge * 0.6;
                val = smoothstep(0.1, 0.9, val);
                float paperTex = nse(uv * 400.0) * 0.08;
                float3 paper = _PaperColor.rgb * (0.93 + paperTex);
                float3 charcoal = _EdgeColor.rgb * (0.7 + grain1 * 0.3);
                float3 r = lerp(charcoal, paper, val);
                return float4(r, 1.0);
            }

            // 10: Line Art — variable thickness + cross-hatch shading
            float4 styleLineArt(float2 uv, float2 ts)
            {
                float e1 = sobelEdge(uv, ts);
                float e2 = sobelEdge(uv, ts * 2.5);
                float e3 = sobelEdge(uv, ts * 0.7);
                float combined = e1 * 0.5 + e2 * 0.3 + e3 * 0.2;
                float edgeLine = smoothstep(_EdgeThreshold*0.4, _EdgeThreshold*1.8, combined);
                float l = lum(tex2D(_MainTex, uv).rgb);
                float hatch1 = step(0.5, frac((uv.x + uv.y) * _HatchDensity * 0.5));
                float hatch2 = step(0.5, frac((uv.x - uv.y) * _HatchDensity * 0.5));
                float shade = 0.0;
                if (l < 0.3) shade = 0.3;
                else if (l < 0.5) shade = (1.0 - hatch1) * 0.2;
                else if (l < 0.7) shade = (1.0 - hatch1 * hatch2) * 0.1;
                float3 r = lerp(_PaperColor.rgb, _EdgeColor.rgb, max(edgeLine, shade));
                return float4(r, 1.0);
            }

            // 11: Stained Glass — glowing cells + dark leading + light refraction
            float4 styleStainedGlass(float2 uv, float2 ts)
            {
                float t = _Time2;
                float cs = _BrushSize * 3.0;
                float2 cell = floor(uv / (ts*cs));
                float2 cc = (cell + 0.5) * ts * cs;
                float3 col = tex2D(_MainTex, cc).rgb;
                col = floor(col * 5.0 + 0.5) / 5.0;
                float g = lum(col); col = lerp(float3(g,g,g), col, 1.6);
                float glow = 0.9 + 0.1 * sin(t * 1.5 + (cell.x + cell.y) * 2.0);
                col = col * glow * 1.2;
                float2 loc = frac(uv / (ts*cs));
                float edgeDist = min(min(loc.x, 1.0 - loc.x), min(loc.y, 1.0 - loc.y));
                float border = 1.0 - smoothstep(0.03, 0.08, edgeDist);
                float3 lead = float3(0.03, 0.03, 0.05);
                float3 r = lerp(col, lead, border);
                return float4(saturate(r), 1.0);
            }

            // 12: Mosaic — beveled tiles + grout shadow
            float4 styleMosaic(float2 uv, float2 ts)
            {
                float cs = _BrushSize * 2.5;
                float2 cell = floor(uv / (ts*cs));
                float2 cc = (cell + 0.5) * ts * cs;
                float3 col = tex2D(_MainTex, cc).rgb;
                col = floor(col * 10.0 + 0.5) / 10.0;
                float2 loc = frac(uv / (ts*cs));
                float edgeDist = min(min(loc.x, 1.0-loc.x), min(loc.y, 1.0-loc.y));
                float gap = 1.0 - smoothstep(0.02, 0.06, edgeDist);
                float bevelLight = smoothstep(0.06, 0.15, loc.x) * smoothstep(0.06, 0.15, loc.y);
                float bevelDark = smoothstep(0.06, 0.15, 1.0-loc.x) * smoothstep(0.06, 0.15, 1.0-loc.y);
                float bevel = lerp(1.08, 0.92, (1.0-bevelLight) * bevelDark);
                col = col * bevel;
                float3 grout = float3(0.55, 0.52, 0.48);
                float3 r = lerp(col, grout, gap);
                return float4(saturate(r), 1.0);
            }

            // 13: Pop Art — Warhol palette swap + halftone + bold outline
            float4 stylePopArt(float2 uv, float2 ts)
            {
                float t = _Time2;
                float3 col = tex2D(_MainTex, uv).rgb;
                float l = lum(col);
                float3 palette;
                if (l < 0.25) palette = float3(0.1, 0.0, 0.3);
                else if (l < 0.5) palette = float3(0.9, 0.1, 0.3);
                else if (l < 0.75) palette = float3(1.0, 0.8, 0.0);
                else palette = float3(1.0, 0.95, 0.8);
                float shift = frac(t * 0.15);
                palette = lerp(palette, palette.gbr, smoothstep(0.0, 0.05, frac(shift)));
                float dotSize = (1.0 - l) * 0.4;
                float2 dotUV = frac(uv * _HatchDensity * 0.8) - 0.5;
                float halftone = smoothstep(dotSize+0.05, dotSize-0.05, length(dotUV));
                palette = palette * (0.7 + halftone * 0.3);
                float edge = sobelEdge(uv, ts * 2.0);
                float ink = smoothstep(0.08, 0.2, edge);
                float3 r = lerp(palette, float3(0,0,0), ink * 0.85);
                return float4(saturate(r), 1.0);
            }

            // 14: Glitch
            float4 styleGlitch(float2 uv, float2 ts)
            {
                float t = _Time2;
                float blockY = floor(uv.y * 20.0 + t * 3.0);
                float glitchStr = step(0.92, hsh(float2(blockY, floor(t*6.0))));
                float shift = (hsh(float2(blockY, t)) - 0.5) * 0.04 * (1.0 + glitchStr * 4.0);
                float r = tex2D(_MainTex, uv + float2(shift + 0.004, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv + float2(-shift - 0.004, 0)).b;
                float3 col = float3(r, g, b);
                float scanline = sin(uv.y * 800.0 + t * 10.0) * 0.05;
                col = col + scanline;
                col = lerp(col, col.gbr, glitchStr);
                float blockGlitch = step(0.96, hsh(float2(floor(uv.x*8.0), floor(uv.y*12.0+t*2.0))));
                col = lerp(col, float3(hsh(float2(t,uv.y*50.0)), col.g, hsh(float2(uv.x*50.0,t))), blockGlitch * 0.6);
                float noise = hsh(uv * 1000.0 + float2(t, t)) * 0.07;
                col = col + noise;
                return float4(saturate(col), 1.0);
            }

            // 15: Ukiyo-e — flat color + bold outline + wood grain
            float4 styleUkiyoe(float2 uv, float2 ts)
            {
                float3 col = tex2D(_MainTex, uv).rgb;
                col = floor(col * 4.0 + 0.5) / 4.0;
                float g = lum(col);
                col = lerp(float3(g,g,g), col, 0.65);
                col = saturate(col * 1.1);
                float edge = sobelEdge(uv, ts * 2.0);
                float outline = smoothstep(_EdgeThreshold*0.3, _EdgeThreshold*1.5, edge);
                col = lerp(col, _EdgeColor.rgb, outline * 0.95);
                float grain = nse(uv * float2(6, 120)) * 0.06;
                col = col + grain;
                float fiber = sin(uv.y * 300.0 + nse(uv * float2(3, 50)) * 10.0) * 0.015;
                col = col + fiber;
                return float4(saturate(col), 1.0);
            }

            // 16: Low Poly — flat triangles with subtle color shift
            float4 styleLowPoly(float2 uv, float2 ts)
            {
                float cs = _BrushSize * 4.0;
                float2 scaled = uv / (ts*cs);
                float2 cell = floor(scaled);
                float2 loc = frac(scaled);
                float2 avg;
                float triID;
                if (loc.x + loc.y < 1.0) { avg = (cell*3.0 + float2(1,1)) / 3.0 * ts * cs; triID = 0.0; }
                else { avg = (cell*3.0 + float2(2,2)) / 3.0 * ts * cs; triID = 1.0; }
                float3 col = tex2D(_MainTex, avg).rgb;
                float variation = hsh(cell + triID) * 0.06 - 0.03;
                col = col + variation;
                float ed = min(min(loc.x, loc.y), min(1.0-loc.x, 1.0-loc.y));
                float diag = abs(loc.x + loc.y - 1.0);
                float wire = 1.0 - smoothstep(0.02, 0.05, min(ed, diag));
                col = lerp(col, col * 0.5, wire * 0.5);
                return float4(saturate(col), 1.0);
            }

            // 17: Emboss — 3D relief with directional lighting
            float4 styleEmboss(float2 uv, float2 ts)
            {
                float tl = lum(tex2D(_MainTex, uv + float2(-ts.x, ts.y)).rgb);
                float tr = lum(tex2D(_MainTex, uv + float2(ts.x, ts.y)).rgb);
                float bl = lum(tex2D(_MainTex, uv + float2(-ts.x, -ts.y)).rgb);
                float br = lum(tex2D(_MainTex, uv + float2(ts.x, -ts.y)).rgb);
                float ml = lum(tex2D(_MainTex, uv + float2(-ts.x, 0)).rgb);
                float mr = lum(tex2D(_MainTex, uv + float2(ts.x, 0)).rgb);
                float dx = (-tl - 2.0*ml - bl + tr + 2.0*mr + br) * 2.0;
                float dy = (-tl + bl - 2.0*lum(tex2D(_MainTex, uv+float2(0,ts.y)).rgb) + 2.0*lum(tex2D(_MainTex, uv-float2(0,ts.y)).rgb) + tr - br) * 2.0;
                float3 normal = normalize(float3(dx, dy, 0.5));
                float3 lightDir = normalize(float3(-0.5, 0.7, 1.0));
                float diffuse = max(dot(normal, lightDir), 0.0);
                float spec = pow(max(dot(reflect(-lightDir, normal), float3(0,0,1)), 0.0), 16.0) * 0.3;
                float val = diffuse * 0.7 + spec + 0.3;
                float3 col = tex2D(_MainTex, uv).rgb;
                float g = lum(col);
                float3 r = float3(g,g,g) * val * _PaperColor.rgb;
                return float4(saturate(r), 1.0);
            }

            // 18: Thermal — smooth gradient heat map
            float4 styleThermal(float2 uv)
            {
                float3 col = tex2D(_MainTex, uv).rgb;
                float l = lum(col);
                float3 r;
                if (l < 0.2) r = lerp(float3(0,0,0.4), float3(0,0.3,0.8), l*5.0);
                else if (l < 0.4) r = lerp(float3(0,0.3,0.8), float3(0,0.85,0.3), (l-0.2)*5.0);
                else if (l < 0.6) r = lerp(float3(0,0.85,0.3), float3(1,0.9,0), (l-0.4)*5.0);
                else if (l < 0.8) r = lerp(float3(1,0.9,0), float3(1,0.3,0), (l-0.6)*5.0);
                else r = lerp(float3(1,0.3,0), float3(1,1,0.9), (l-0.8)*5.0);
                float noise = hsh(uv * 300.0) * 0.03;
                r = r + noise;
                return float4(saturate(r), 1.0);
            }

            // 19: Negative — chromatic inversion + film grain
            float4 styleNegative(float2 uv)
            {
                float3 col = 1.0 - tex2D(_MainTex, uv).rgb;
                col = col * float3(1.05, 0.95, 0.85);
                float grain = (hsh(uv * 800.0) - 0.5) * 0.08;
                col = col + grain;
                float vignette = 1.0 - length(uv - 0.5) * 0.8;
                col = col * vignette;
                return float4(saturate(col), 1.0);
            }

            // 20: Cross Stitch — textured fabric + thread pattern
            float4 styleCrossStitch(float2 uv, float2 ts)
            {
                float cs = _BrushSize * 2.0;
                float2 cell = floor(uv / (ts*cs));
                float2 cc = (cell + 0.5) * ts * cs;
                float3 col = tex2D(_MainTex, cc).rgb;
                col = floor(col * _QuantizeLevels) / _QuantizeLevels;
                float2 loc = frac(uv / (ts*cs));
                float thread1 = smoothstep(0.1, 0.13, abs(loc.x - loc.y));
                float thread2 = smoothstep(0.1, 0.13, abs(loc.x - (1.0 - loc.y)));
                float stitch = 1.0 - min(thread1, thread2);
                float gap = step(0.93, max(loc.x, loc.y)) + step(loc.x, 0.07) + step(loc.y, 0.07);
                gap = saturate(gap);
                float fabricTex = nse(uv * 800.0) * 0.05;
                float3 fabric = float3(0.88, 0.86, 0.8) + fabricTex;
                float threadShade = 0.9 + nse(uv * 400.0) * 0.1;
                float3 r = lerp(fabric, col * threadShade, stitch * (1.0 - gap));
                return float4(r, 1.0);
            }

            // 21: VHS
            float4 styleVHS(float2 uv, float2 ts)
            {
                float t = _Time2;
                float wobble = sin(uv.y * 60.0 + t * 3.0) * 0.001;
                float2 uv2 = uv + float2(wobble, 0);
                float r = tex2D(_MainTex, uv2 + float2(0.003, 0.001)).r;
                float g = tex2D(_MainTex, uv2).g;
                float b = tex2D(_MainTex, uv2 - float2(0.003, 0.001)).b;
                float3 col = float3(r, g, b);
                float scanline = sin(uv.y * 400.0 + t * 8.0) * 0.05;
                col = col - scanline;
                float jitter = (hsh(float2(floor(uv.y * 80.0), floor(t * 10.0))) - 0.5) * 0.008;
                col.r = tex2D(_MainTex, uv2 + float2(jitter, 0)).r * 0.4 + col.r * 0.6;
                col.b = tex2D(_MainTex, uv2 - float2(jitter, 0)).b * 0.4 + col.b * 0.6;
                float trackingLine = step(0.995, frac(uv.y * 2.0 + t * 0.3)) * 0.4;
                col = col + trackingLine;
                float noise = hsh(uv * 500.0 + float2(t, t)) * 0.05;
                col = col + noise;
                col = col * 0.93;
                float vignette = 1.0 - length(uv - 0.5) * 0.4;
                col = col * vignette;
                return float4(saturate(col), 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 orig = tex2D(_MainTex, i.uv) * i.color;
                float2 ts = _TexelSize.xy;
                float4 styled = orig;
                if (_Style == 1) styled = stylePencil(i.uv, ts);
                if (_Style == 2) styled = styleOil(i.uv, ts);
                if (_Style == 3) styled = styleWater(i.uv, ts);
                if (_Style == 4) styled = stylePixel(i.uv);
                if (_Style == 5) styled = styleComic(i.uv, ts);
                if (_Style == 6) styled = styleImpressionist(i.uv, ts);
                if (_Style == 7) styled = stylePointillism(i.uv, ts);
                if (_Style == 8) styled = styleWoodcut(i.uv, ts);
                if (_Style == 9) styled = styleCharcoal(i.uv, ts);
                if (_Style == 10) styled = styleLineArt(i.uv, ts);
                if (_Style == 11) styled = styleStainedGlass(i.uv, ts);
                if (_Style == 12) styled = styleMosaic(i.uv, ts);
                if (_Style == 13) styled = stylePopArt(i.uv, ts);
                if (_Style == 14) styled = styleGlitch(i.uv, ts);
                if (_Style == 15) styled = styleUkiyoe(i.uv, ts);
                if (_Style == 16) styled = styleLowPoly(i.uv, ts);
                if (_Style == 17) styled = styleEmboss(i.uv, ts);
                if (_Style == 18) styled = styleThermal(i.uv);
                if (_Style == 19) styled = styleNegative(i.uv);
                if (_Style == 20) styled = styleCrossStitch(i.uv, ts);
                if (_Style == 21) styled = styleVHS(i.uv, ts);
                styled.a = orig.a;
                return lerp(orig, styled, _Intensity);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
