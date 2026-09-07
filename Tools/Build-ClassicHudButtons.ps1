[CmdletBinding()]
param([string]$ProjectPath = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
Add-Type -AssemblyName System.Drawing
# The last reference has the cleanest normal-state artwork. Preserve its complete
# icons and captions; no text or columns from the narrow UI.nx buttons are stretched.
$drawingReferences = @('System.Drawing.Common', 'System.Drawing.Primitives', 'System.Runtime')
foreach ($name in @('System.Private.Windows.GdiPlus', 'System.Private.Windows.Core')) {
    if (Test-Path -LiteralPath (Join-Path $PSHOME ($name + '.dll'))) { $drawingReferences += $name }
}
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class ClassicHudArtBuilder
{
    const int W = 148, H = 68; // Two texture pixels per HUD pixel.
    static int Byte(double n) { return (int)Math.Max(0, Math.Min(255, Math.Round(n))); }
    static Color Mix(Color a, Color b, double amount)
    {
        return Color.FromArgb(a.A, Byte(a.R+(b.R-a.R)*amount),
            Byte(a.G+(b.G-a.G)*amount), Byte(a.B+(b.B-a.B)*amount));
    }
    static bool Inside(int x, int y)
    {
        // Cut away neighboring HUD pixels at the stepped corner bevels.
        int inset = y < 2 || y >= H-2 ? 6 : y < 4 || y >= H-4 ? 3 : 0;
        return x >= inset && x < W-inset;
    }
    public static void Build(string reference, string output)
    {
        string[] names = { "BtShop", "BtMenu", "BtShort" };
        Rectangle[] crops = { new Rectangle(2409,2062,206,96),
            new Rectangle(2618,2062,206,96), new Rectangle(2826,2062,207,96) };
        using (var source = new Bitmap(reference))
        {
            if (source.Width != 3840 || source.Height != 2160)
                throw new InvalidOperationException("Expected the unmodified 3840 x 2160 reference.");
            for (int button = 0; button < names.Length; button++)
            using (var normal = new Bitmap(W,H,PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(normal))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(source, new Rectangle(0,0,W,H), crops[button], GraphicsUnit.Pixel);
                }
                for (int y=0;y<H;y++) for(int x=0;x<W;x++)
                    if (!Inside(x,y)) normal.SetPixel(x,y,Color.Transparent);
                normal.Save(System.IO.Path.Combine(output,names[button]+"-normal.png"),ImageFormat.Png);
                // A content mask produces the same icon/caption halo as classic
                // mouseOver art, without lighting the whole rectangular hit area.
                var mask = new double[W,H];
                for(int y=8;y<H-6;y++) for(int x=10;x<W-10;x++)
                {
                    Color c=normal.GetPixel(x,y);
                    mask[x,y]=Math.Max(0,Math.Min(1,(Math.Min(c.R,c.G)-95)/130.0));
                }
                using(var hover = new Bitmap(W,H))
                using(var pressed = new Bitmap(W,H))
                using(var disabled = new Bitmap(W,H))
                {
                    for(int y=0;y<H;y++) for(int x=0;x<W;x++)
                    {
                        Color c=normal.GetPixel(x,y);
                        if(c.A==0) continue;
                        double glow=0, weight=0;
                        for(int dy=-8;dy<=8;dy++) for(int dx=-8;dx<=8;dx++)
                        {
                            int xx=x+dx, yy=y+dy;
                            if(xx<0||xx>=W||yy<0||yy>=H) continue;
                            double w=Math.Exp(-(dx*dx+dy*dy)/32.0);
                            glow+=mask[xx,yy]*w; weight+=w;
                        }
                        hover.SetPixel(x,y,Mix(c,Color.White,.07+.46*glow/weight));
                        // Keep the hit rectangle stationary. Move the inner face by
                        // one HUD pixel and reverse the light on its raised bevel.
                        Color p=c;
                        if(x>=5&&x<W-5&&y>=5&&y<H-4)
                            p=Mix(normal.GetPixel(Math.Max(5,x-2),Math.Max(5,y-2)),Color.Black,.12);
                        if(y<5||x<4) p=Mix(c,Color.Black,.45);
                        else if(y>=H-3||x>=W-3) p=Mix(c,Color.White,.22);
                        pressed.SetPixel(x,y,p);
                        double gray=.299*c.R+.587*c.G+.114*c.B;
                        int tone=Byte(95+gray*.38);
                        disabled.SetPixel(x,y,Color.FromArgb(c.A,tone,tone,tone));
                    }
                    hover.Save(System.IO.Path.Combine(output,names[button]+"-mouseOver.png"),ImageFormat.Png);
                    pressed.Save(System.IO.Path.Combine(output,names[button]+"-pressed.png"),ImageFormat.Png);
                    disabled.Save(System.IO.Path.Combine(output,names[button]+"-disabled.png"),ImageFormat.Png);
                }
            }
        }
    }
}
'@
$output = Join-Path $projectRoot 'Assets/Resources/UI/ClassicHudButtons'
New-Item -ItemType Directory -Path $output -Force | Out-Null
[ClassicHudArtBuilder]::Build((Join-Path $projectRoot 'Tools/ArtReferences/MapleClassic/09-character-stats.png'), $output)
foreach ($png in Get-ChildItem -LiteralPath $output -Filter '*.png') {
    $metaPath = $png.FullName + '.meta'
    if (Test-Path -LiteralPath $metaPath) { continue }
    $guid = [guid]::NewGuid().ToString('N')
    @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  serializedVersion: 12
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 8
  textureShape: 1
  spriteMode: 1
  spriteMeshType: 0
  spritePixelsToUnits: 100
  spritePivot: {x: 0.5, y: 0.5}
  alphaIsTransparency: 1
  maxTextureSize: 2048
  textureCompression: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    overridden: 0
"@ | Set-Content -LiteralPath $metaPath -Encoding utf8
}
Write-Output "Built 12 button sprites in $output"
