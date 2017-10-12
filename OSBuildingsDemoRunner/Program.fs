open System
open System.Drawing
open Buildings.Drawer

[<EntryPoint>]
let main argv = 
    drawBuildings @"C:\Data\OSBuildings" @"*.shp" (PointF(505880.f,177380.f)) (PointF(509700.f,174140.f)) 10000 Color.Crimson Color.AntiqueWhite 
        @"c:\Data\heathrow.bmp"

    0