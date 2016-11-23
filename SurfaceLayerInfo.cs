//-----------------------------------------------------------------------
// <copyright file="SurfaceLayerInfo.cs" company ="Studio A&T s.r.l.">
//     Original code by John Grayson (ESRI)
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------

namespace Studioat.ArcGis.Soe.Rest
{
    using System.Diagnostics.CodeAnalysis;
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.SOESupport;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]

    /// <summary>
    /// The SurfaceLayerInfo class holds properties of the layer and creates an instanse of AnalysisSurface
    /// </summary>
    internal class SurfaceLayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the SurfaceLayerInfo class from layer information
        /// </summary>
        /// <param name="mapLayerInfo">The layer information</param>
        /// <param name="rasterFromLayer">The raster from the layer</param>
        public SurfaceLayerInfo(IMapLayerInfo mapLayerInfo, IRaster rasterFromLayer)
        {
            this.MapLayerInfo = mapLayerInfo;
            this.AnalysisSurface = new AnalysisSurface(rasterFromLayer);
        }

        /// <summary>
        /// Prevents a default instance of the SurfaceLayerInfo class from being created
        /// </summary>
        private SurfaceLayerInfo()
        {
        }

        /// <summary>
        /// Gets or sets mapLayerInfo
        /// </summary>
        private IMapLayerInfo MapLayerInfo { get; set; }

        /// <summary>
        /// Gets or sets analysisSurface
        /// </summary>
        private AnalysisSurface AnalysisSurface { get; set; }
        
        /// <summary>
        /// Get the map layer ID
        /// </summary>
        /// <returns>return the ID of layer</returns>
        public int GetId()
        {
            return this.MapLayerInfo.ID;
        }
       
        /// <summary>
        /// Get the AnalysisSurface
        /// </summary>
        /// <returns>return the Analysis Surface</returns>
        public AnalysisSurface GetAnalysisSurface()
        {
            return this.AnalysisSurface;
        }

        /// <summary>
        /// Convert to JsonObject
        /// </summary>
        /// <returns>return JsonObject of instance</returns>
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject.AddLong("id", this.MapLayerInfo.ID);
            jsonObject.AddString("name", this.MapLayerInfo.Name);
            jsonObject.AddString("type", this.MapLayerInfo.Type);
            JsonObject env = Conversion.ToJsonObject((IGeometry)this.MapLayerInfo.Extent, true);
            jsonObject.AddJsonObject("full extent", env);
            jsonObject.AddString("description", this.MapLayerInfo.Description);

            return jsonObject;
        }
    }
}
