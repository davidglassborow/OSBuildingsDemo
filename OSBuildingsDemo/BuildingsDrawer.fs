namespace Buildings

module Drawer =

    open System.Drawing
    open System.IO

    type PolyLine = | PolyLine of seq<PointF>
    type Building = | Building of seq<PolyLine>

    let notImplemented() =
        raise <| System.NotImplementedException()

    /// Get the buildings from the shape file set
    let loadBuildings (directoryPath : string) (filePattern : string) : seq<Building> =
        Directory.EnumerateFiles(directoryPath, filePattern)
        |> Seq.collect (fun fileName ->
            let sf = new EGIS.ShapeFileLib.ShapeFile(fileName)
            Seq.init sf.RecordCount (sf.GetShapeData))
        |> Seq.map (fun pointGroups ->
            pointGroups |> Seq.map (fun points -> points |> Seq.ofArray |> PolyLine))
        |> Seq.map Building

    /// Find buildings in an area
    let findBuildings (p1 : PointF) (p2 : PointF) (buildings : Building seq) : Building seq =
        let between min max n =
            let l, u = if min < max then min, max else max, min
            n >= l && n <= u
        let pointWithin (p : PointF) =
            p.X |> between p1.X p2.X
            && p.Y |> between p1.Y p2.Y
        let polyLineWithin (PolyLine polyLine) =
            polyLine
            |> Seq.exists pointWithin
        let buildingWithin (Building building) = 
            building 
            |> Seq.exists polyLineWithin

        buildings
        |> Seq.filter (buildingWithin)

    /// Render buildings on a bitmap
    let renderBuildings (p1 : PointF) (p2 : PointF) (bitmapWidth : int) (foreground : Color) (background : Color) (buildings : Building seq) : Bitmap =
        let aspectRatio =
            (p1.X - p2.X) / (p1.Y - p2.Y) |> abs |> float
        let bitmapHeight = (float bitmapWidth) / aspectRatio |> int
        let scalingFactor = (float32 bitmapWidth) / abs(p2.X - p1.X)
        let bitmap = new Bitmap(bitmapWidth, bitmapHeight)

        let minX = min p1.X p2.X
        let minY = min p1.Y p2.Y

        let graphics = Graphics.FromImage(bitmap)
        graphics.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias
        graphics.TranslateTransform(-minX*scalingFactor, -minY*scalingFactor)
        graphics.ScaleTransform(scalingFactor, scalingFactor)
        let brush = new SolidBrush(foreground)

        graphics.Clear(background)

        buildings
        |> Seq.iter (fun (Building building) ->
            building
            |> Seq.iter (fun (PolyLine polyLine) ->
                graphics.FillPolygon(brush, polyLine |> Array.ofSeq)))

        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY)
        bitmap

    /// Save the bitmap
    let saveBitmap (path : string) (bitmap : Bitmap) : unit =
        bitmap.Save(path)

    /// Get some buildings from a shapefile set, render them on a bitmap, and save the bitmap
    let drawBuildings (shapeFilePath : string) (filePattern : string) (p1 : PointF) (p2 : PointF) (bitmapWidth : int) (foreground : Color) (background : Color) (saveFilePath : string) =

        loadBuildings shapeFilePath filePattern
        |> findBuildings p1 p2
        |> renderBuildings p1 p2 bitmapWidth foreground background
        |> saveBitmap saveFilePath