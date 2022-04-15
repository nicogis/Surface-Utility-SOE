# Surface Utility SOE Rest


I have extended SOE Elevations 10.1 of Esri (GetElevationAtLonLat, GetElevations and GetElevationData) adding methods of interface ISurface and I have applied my personal refacting (stylecop and fxcop). 
However this extension is another soe so you don't confuse with SOE Elevations 10.1 of Esri.

This solution (developed in c#) creates a SOE Rest in arcgis server 10.4 for these operations and they can be enabled/disabled from capabilities:

- GetElevationAtLonLat
- GetElevations
- GetElevationData
- GetLineOfSight
- GetSteepestPath
- GetContour
- GetSlope
- GetAspect
- GetSurfaceLength
- GetNormal
- GetLocate
- GetLocateAll

# Installation:

1. upload file Studioat.ArcGis.Soe.Rest.SurfaceUtility.soe (see https://resources.arcgis.com/en/help/main/10.1/0154/0154000004sm000000.htm)

2. create a service map and enable in capabilities the extension. In your mxd you must have at least with at least one elevation layer; the elevation layer must be a single band Raster Layer or Mosaic Layer. For requirements see details in help point 3).

3. from service directory you can see the help
    https://hostname/instanceags/rest/services/yourservice/MapServer/exts/SurfaceUtility/Help

4. enabled/disabled capabilies of soe because default aren't enabled all capabilities of soe.


I have added an example in api esri javascript to see how to use it (folder Client). 
In Config.js change your url and in header of SurfaceUtility.js

I also have added a sample WPF (ArcGIS Runtime SDK for .NET v100.0.0) for call the method GetLineOfSight of soe. It's a start point.  

The solutions are checked 100% with stylecop and fxcop.

[Live](https://sit.sistemigis.it/samples/elevations)

[Help live](https://sit.sistemigis.it/sit/rest/services/Demo/Surface/MapServer/exts/SurfaceUtility/Help)

[Blog](https://nicogis.blogspot.it/2013/02/alziamo-il-livello-3d-surface.html)
