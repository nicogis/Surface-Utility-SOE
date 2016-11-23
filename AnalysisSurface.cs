//-----------------------------------------------------------------------
// <copyright file="AnalysisSurface.cs" company ="Studio A&T s.r.l.">
//     Original code by John Grayson (ESRI)
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------

namespace Studioat.ArcGis.Soe.Rest
{
    using ESRI.ArcGIS.Analyst3D;
    using ESRI.ArcGIS.DataSourcesRaster;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;

    /// <summary>
    /// The AnalysisSurface class holds properties of the raster in the layer and creates an ISurface via RasterSurface
    /// </summary>
    internal class AnalysisSurface
    {
        /// <summary>
        /// Initializes a new instance of the AnalysisSurface class
        /// </summary>
        /// <param name="rasterFromLayer">The raster from the layer</param>
        public AnalysisSurface(IRaster rasterFromLayer) : this(rasterFromLayer, 0)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the AnalysisSurface class
        /// </summary>
        /// <param name="rasterFromLayer">The raster from the layer</param>
        /// <param name="bandIndex">Index of the band</param>
        public AnalysisSurface(IRaster rasterFromLayer, int bandIndex)
        {
            IRasterBandCollection rasterBC = (IRasterBandCollection)rasterFromLayer;
            this.RasterBand = rasterBC.Item(bandIndex);
            this.RasterDataset = this.RasterBand.RasterDataset;
            this.Raster = this.RasterDataset.CreateDefaultRaster();

            IGeoDataset rasterGDS = (IGeoDataset)this.RasterDataset;
            this.SpatialReference = rasterGDS.SpatialReference;
            
            IRasterSurface rasterSurface = new RasterSurfaceClass();
            rasterSurface.PutRaster(this.Raster, bandIndex);
            this.Surface = (ISurface)rasterSurface;                                
        }

        /// <summary>
        /// Prevents a default instance of the AnalysisSurface class from being created
        /// </summary>
        private AnalysisSurface()
        { 
        }

        /// <summary>
        /// Gets or sets rasterBand of the AnalysisSurface
        /// </summary>
        public IRasterBand RasterBand { get; set; }
        
        /// <summary>
        /// Gets or sets rasterDataset of the AnalysisSurface
        /// </summary>
        public IRasterDataset RasterDataset { get; set; }
        
        /// <summary>
        /// Gets or sets raster of the AnalysisSurface
        /// </summary>
        public IRaster Raster { get; set; }

        /// <summary>
        /// Gets or sets spatialReference of the AnalysisSurface
        /// </summary>
        public ISpatialReference SpatialReference { get; set; }

        /// <summary>
        /// Gets or sets surface of the AnalysisSurface
        /// </summary>
        public ISurface Surface { get; set; }
    }
}
