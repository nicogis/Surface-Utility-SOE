//-----------------------------------------------------------------------
// <copyright file="SurfaceUtility.cs" company ="Studio A&T s.r.l.">
//
// Original code by John Grayson (ESRI) - Applications Prototype Lab, ESRI
// May 2010 - Original conversion from Geoprocessing tool to SOE
// Nov 2010 - Addes support for multipart polylines
// Mar 2011 - Added support for MSD
// Jul 2011 - Update to 10.1 beta 1
// Jan 2012 - Update to 10.1 pre-release, exposed capabilities
// Jul 2012 - Update to 10.1 final
// Feb 2013 - Customize by Studio AT (added methods surface - renamed SurfaceUtility) 
//
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------

namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.DataSourcesRaster;
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.Server;
    using ESRI.ArcGIS.SOESupport;

    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Warning StyleCop - Error Code ESRI - pSOH")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Warning FxCop - Error Code ESRI - Capabilities")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]

    /// <summary>
    /// Surface Utility SOE
    /// IServerObjectExtension methods: This is a mandatory interface that must be supported by all SOEs. This interface
    /// is used by the Server Object to manage the lifecycle of the SOE and includes two methods: init() and shutdown().
    /// The Server Object cocreates the SOE and calls the init() method handing it a back reference to the Server Object
    /// via the Server Object Helper argument. The Server Object Helper implements a weak reference on the Server Object.
    /// The extension can keep a strong reference on the Server Object Helper (for example, in a member variable) but
    /// should not.
    /// The log entries are merely informative and completely optional.
    /// </summary>
    [ComVisible(true)]
    [Guid("6B2D4203-A249-4DA1-BA60-E0F463C06AC5")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
    DisplayName = "Surface Utility",
    Description = "Surface Utility",
    Properties = "interpolationCount=100;maxDataValues=10000",
    DefaultCapabilities = "Elevation at lon-lat,Elevations",
    AllCapabilities = "Elevation at lon-lat,Elevations,Elevation data,Line of sight,Steepest path,Contour,Slope,Aspect,Surface length,Normal,Locate",
    HasManagerPropertiesConfigurationPane = false,
    SupportsREST = true,
    SupportsSOAP = false)]
    public class SurfaceUtility : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        /// <summary>
        /// name of soe
        /// </summary>
        private string soeName = "SurfaceUtility";

        /// <summary>
        /// interpolation count (property)
        /// </summary>
        private int interpolationCount;
        
        /// <summary>
        /// max Data Values (property)
        /// </summary>
        private int maxDataValues;

        /// <summary>
        /// object serverObject
        /// </summary>
        private IServerObjectHelper serverObjectHelper;

        // private ServerLogger serverLog;

        /// <summary>
        /// object rest request Handler
        /// </summary>
        private IRESTRequestHandler reqHandler;

        /// <summary>
        /// object MapServer
        /// </summary>
        private IMapServer3 mapServer;

        /// <summary>
        /// object mapServerDataAccess
        /// </summary>
        private IMapServerDataAccess mapServerDataAccess;

        /// <summary>
        /// object layerInfos
        /// </summary>
        private IMapLayerInfos layerInfos;

        /// <summary>
        /// list of SurfaceLayerInfo
        /// </summary>
        private List<SurfaceLayerInfo> surfaceLayerInfos;

        /// <summary>
        /// Initializes a new instance of the SurfaceUtility class
        /// </summary>
        public SurfaceUtility()
        {
            this.reqHandler = new SoeRestImpl(this.soeName, this.CreateRestSchema()) as IRESTRequestHandler;
        }

        #region IServerObjectExtension Members

        /// <summary>
        /// init() is called once, when the instance of the SOE is created.
        /// </summary>
        /// <param name="pSOH">object server Object</param>
        public void Init(IServerObjectHelper pSOH)
        {
            ////System.Diagnostics.Debugger.Launch();

            this.serverObjectHelper = pSOH;
            
            this.mapServer = (IMapServer3)this.serverObjectHelper.ServerObject;
            this.mapServerDataAccess = (IMapServerDataAccess)this.mapServer;
            this.layerInfos = this.mapServer.GetServerInfo(this.mapServer.DefaultMapName).MapLayerInfos;
            this.surfaceLayerInfos = this.GetSurfaceLayerInfos();

            // this.serverLog = new ServerLogger();
        }

        /// <summary>
        /// shutdown() is called once when the Server Object's context is being shut down and is about to go away.
        /// </summary>
        public void Shutdown()
        {
            this.serverObjectHelper = null;
            this.mapServer = null;
            this.mapServerDataAccess = null;
            this.layerInfos = null;
            this.surfaceLayerInfos = null;

            // this.serverLog = null;
        }

        #endregion

        #region IObjectConstruct Members

        /****************************************************************************************************************************
        * IObjectConstruct: This is an optional interface for SOEs. If your SOE includes configuration properties or
        * requires any additional initialization logic, you need to implement the IObjectConstruct interface.
        * 
        * This interface includes a single method called construct().
        ****************************************************************************************************************************/
        
        /// <summary>
        /// construct() is called only once, when the SOE is created, after IServerObjectExtension.init() is called. This
        /// method hands back the configuration properties for the SOE as a property set. You should include any expensive
        /// initialization logic for your SOE within your implementation of construct().
        /// </summary>
        /// <param name="props">object propertySet</param>
        public void Construct(IPropertySet props)
        {
            this.interpolationCount = Convert.ToInt32((string)props.GetProperty("interpolationCount"), CultureInfo.InvariantCulture);
            this.maxDataValues = Convert.ToInt32((string)props.GetProperty("maxDataValues"), CultureInfo.InvariantCulture);
        }

        #endregion

        #region IRESTRequestHandler Members

        /// <summary>
        /// Get schema 
        /// </summary>
        /// <returns>return schema</returns>
        public string GetSchema()
        {
            return this.reqHandler.GetSchema();
        }

        /// <summary>
        /// handle rest request
        /// </summary>
        /// <param name="Capabilities">capabilities of soe</param>
        /// <param name="resourceName">name of resource</param>
        /// <param name="operationName">name of operation</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>return handle rest request</returns>
        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return this.reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        /// <summary>
        /// This method returns the resource hierarchy of a REST based SOE in JSON format.
        /// </summary>
        /// <returns>return RestSchema</returns>
        private RestResource CreateRestSchema()
        {
            RestResource soeResource = new RestResource(this.soeName, false, this.RootResHandler);

            RestResource infoResource = new RestResource("Info", false, this.InfoResHandler);
            soeResource.resources.Add(infoResource);

            RestResource helpResource = new RestResource("Help", false, this.HelpResHandler);
            soeResource.resources.Add(helpResource);

            RestResource surfaceLayerResource = new RestResource("SurfaceLayers", true, this.SurfaceLayer);

            RestOperation getElevationAtLonLatOper = new RestOperation("GetElevationAtLonLat", new string[] { "lon", "lat" }, new string[] { "json" }, this.GetElevationAtLonLatOperHandler, "Elevation at lon-lat");

            RestOperation getElevationsOper = new RestOperation("GetElevations", new string[] { "geometries" }, new string[] { "json" }, this.GetElevationsOperHandler, "Elevations");

            RestOperation getElevationsDataOper = new RestOperation("GetElevationData", new string[] { "extent", "rows", "columns" }, new string[] { "json" }, this.GetElevationDataOperHandler, "Elevation data");

            RestOperation getLineOfSightOper = new RestOperation("GetLineOfSight", new string[] { "geometry", "offsetObserver", "offsetTarget", "applyCurvature", "applyRefraction", "refractionFactor" }, new string[] { "json" }, this.GetLineOfSightOperHandler, "Line of sight");

            RestOperation getSteepestPathOper = new RestOperation("GetSteepestPath", new string[] { "geometry" }, new string[] { "json" }, this.GetSteepestPathOperHandler, "Steepest path");

            RestOperation getContourOperHandler = new RestOperation("GetContour", new string[] { "geometry" }, new string[] { "json" }, this.GetContourOperHandler, "Contour");

            RestOperation getSlopeOperHandler = new RestOperation("GetSlope", new string[] { "geometry", "units" }, new string[] { "json" }, this.GetSlopeOperHandler, "Slope");

            RestOperation getAspectOperHandler = new RestOperation("GetAspect", new string[] { "geometry", "units" }, new string[] { "json" }, this.GetAspectOperHandler, "Aspect");

            RestOperation getSurfaceLengthOperHandler = new RestOperation("GetSurfaceLength", new string[] { "geometry", "stepSize" }, new string[] { "json" }, this.GetSurfaceLengthOperHandler, "Surface length");

            RestOperation getNormalOperHandler = new RestOperation("GetNormal", new string[] { "geometry" }, new string[] { "json" }, this.GetNormalHandler, "Normal");

            RestOperation getLocateOperHandler = new RestOperation("GetLocate", new string[] { "geometry", "useOffsetFromPoint", "offsetFromPoint", "useOffsetToPoint", "offsetToPoint", "hint" }, new string[] { "json" }, this.GetLocateHandler, "Locate");

            RestOperation getLocateAllOperHandler = new RestOperation("GetLocateAll", new string[] { "geometry", "useOffsetFromPoint", "offsetFromPoint", "useOffsetToPoint", "offsetToPoint", "hint" }, new string[] { "json" }, this.GetLocateAllHandler, "Locate");

            surfaceLayerResource.operations.Add(getElevationAtLonLatOper);
            surfaceLayerResource.operations.Add(getElevationsOper);
            surfaceLayerResource.operations.Add(getElevationsDataOper);
            surfaceLayerResource.operations.Add(getLineOfSightOper);
            surfaceLayerResource.operations.Add(getSteepestPathOper);
            surfaceLayerResource.operations.Add(getContourOperHandler);
            surfaceLayerResource.operations.Add(getSlopeOperHandler);
            surfaceLayerResource.operations.Add(getAspectOperHandler);
            surfaceLayerResource.operations.Add(getSurfaceLengthOperHandler);
            surfaceLayerResource.operations.Add(getNormalOperHandler);
            surfaceLayerResource.operations.Add(getLocateOperHandler);
            surfaceLayerResource.operations.Add(getLocateAllOperHandler);
            
            soeResource.resources.Add(surfaceLayerResource);

            return soeResource;
        }

        /// <summary>
        ///  Returns JSON representation of the root resource.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of the root resource.</returns>
        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();
            result.AddString("description", "Get elevation values at a location, along geometries, or interpolated over an extent, LOS (line of sight), contour, aspect, slope, locate, locate all, steepest path, normal, surface lenght ...");

            JsonObject[] elevLayersJson = this.GetSurfaceLayerInfosAsJsonObjects();
            result.AddArray("surfaceLayers", elevLayersJson);

            return Encoding.UTF8.GetBytes(result.ToJson());
        }

        /// <summary>
        /// Returns JSON representation of Info resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of Info resource.</returns>
        private byte[] InfoResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();
            AddInPackageAttribute addInPackage = (AddInPackageAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AddInPackageAttribute), false)[0];
            result.AddString("agsVersion", addInPackage.TargetVersion);
            result.AddString("soeVersion", addInPackage.Version);
            result.AddString("author", addInPackage.Author);
            result.AddString("company", addInPackage.Company);
            result.AddLong("interpolationCount", this.interpolationCount);
            result.AddLong("maxDataValues", this.maxDataValues);

            return result.JsonByte();
        }

        /// <summary>
        /// Returns JSON representation of Help resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of Help resource.</returns>
        private byte[] HelpResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();

            JsonObject datasetRequirements = new JsonObject();
            datasetRequirements.AddString("spatial reference", "datasets that have projected spatial reference");
            datasetRequirements.AddString("datum transformations", "datasets with projections that don’t need transformations (method GetElevationAtLonLat)");
            datasetRequirements.AddString("Z values", "datasets where the Z values are in the same units as the XY");
            datasetRequirements.AddString("Raster or Mosaic layer in the map", "Map Service with at least one elevation layer; the elevation layer must be a single band Raster Layer or Mosaic Layer");
            datasetRequirements.AddString("Capabilities", "Select the operations you want to allow for the SOE");
            result.AddJsonObject("Requirements", datasetRequirements);

            JsonObject soeProperties = new JsonObject();
            soeProperties.AddString("interpolationCount", "Number of points to interpolate along a geometry; used in the GetElevations operation when the input geometry is a polyline or polygon.");
            soeProperties.AddString("maxDataValues", "Maximum number of values to send back; used in the GetElevationData operation and is calculated by multiplying the Rows and Columns input values.");
            result.AddJsonObject("Properties", soeProperties);

            JsonObject soeResources = new JsonObject();
            soeResources.AddString("ElevationLayers", "A list of Raster or Mosaic layers in the map.");
            result.AddJsonObject("Resources", soeResources);

            JsonObject getElevationAtLonLatInputs = new JsonObject();
            getElevationAtLonLatInputs.AddString("lon", "(double) The longitude of the location in WGS84 coordinates. Value must be between -180.0 and 180.0");
            getElevationAtLonLatInputs.AddString("lat", "(double) The latitude of the location in WGS84 coordinates. Value must be between -90.0 and 90.0");
           
            JsonObject getElevationAtLonLatOutput = new JsonObject();
            getElevationAtLonLatOutput.AddString("elevation", "(double) Elevation at input location");
            JsonObject getElevationAtLonLatParams = new JsonObject();
            getElevationAtLonLatParams.AddString("Info", "Get the elevation at a location. <b><font color='#FF0000'>Input geometry (lon,lat) are projected to the elevation source dataset but transformations are not supported</font></b>");
            getElevationAtLonLatParams.AddJsonObject("Inputs", getElevationAtLonLatInputs);
            getElevationAtLonLatParams.AddJsonObject("Outputs", getElevationAtLonLatOutput);

            JsonObject getElevationsInputs = new JsonObject();
            getElevationsInputs.AddString("geometries", "(geometry[]) The array of geometries");
           
            JsonObject getElevationsOutput = new JsonObject();
            getElevationsOutput.AddString("geometries", "(geometry[]) The array of interpolated geometries which are densified and contain Z values at each vertex or point.");
            JsonObject getElevationsParams = new JsonObject();
            getElevationsParams.AddString("Info", "Get the elevations along a set of geometries. Supported geometries are point, multipoint, polyline and polygon. To learn more about formatting the input geometries, please visit the 'Geometry Objects' section of the ArcGIS Server REST documentation.");
            getElevationsParams.AddJsonObject("Inputs", getElevationsInputs);
            getElevationsParams.AddJsonObject("Outputs", getElevationsOutput);

            JsonObject getElevationDataInputs = new JsonObject();
            getElevationDataInputs.AddString("extent", "(extent) The interpolation extent");
            getElevationDataInputs.AddString("rows", "(int) Number of rows. Note: (rows * columns) must be less than maxDataValues as defined by admin");
            getElevationDataInputs.AddString("columns", "(int) Number of columns; Note: (rows * columns) must be less than maxDataValues as defined by admin");
           
            JsonObject getElevationDataOutput = new JsonObject();
            getElevationDataOutput.AddString("nCols", "(int) Number of columns");
            getElevationDataOutput.AddString("nRows", "(int) Number of rows");
            getElevationDataOutput.AddString("xLLCenter", "(double) X coordinate of the center of the lower left cell");
            getElevationDataOutput.AddString("yLLCenter", "(double) Y coordinate of the center of the lower left cell");
            getElevationDataOutput.AddString("cellSize", "(double) Cell size of the interpolated raster");
            getElevationDataOutput.AddString("noDataValue", "(number) 'No Data' value");
            getElevationDataOutput.AddString("spatialReference", "(SpatialReference) Spatial reference of the interpolated raster");
            getElevationDataOutput.AddString("data", "(number[]) Interpolated elevation values as array of numbers");
            JsonObject getElevationDataOutputRP = new JsonObject();
            getElevationDataOutputRP.AddString("isInteger", "(boolean) Are the values integers");
            getElevationDataOutputRP.AddString("datasetMin", "(number/NaN) The minimum value of the entire elevation dataset");
            getElevationDataOutputRP.AddString("datasetMax", "(number/NaN)  The maximum value of the entire elevation dataset");
            getElevationDataOutput.AddJsonObject("rasterProperties", getElevationDataOutputRP);
            JsonObject getElevationDataParams = new JsonObject();
            getElevationDataParams.AddString("Info", "Get interpolated elevation values within an extent.");
            getElevationDataParams.AddJsonObject("Inputs", getElevationDataInputs);
            getElevationDataParams.AddJsonObject("Outputs", getElevationDataOutput);
            
            JsonObject getLineOfSightInputs = new JsonObject();
            getLineOfSightInputs.AddString("geometry", "(geometry) The line of sight between two points, an observer and target (end points of line)");
            getLineOfSightInputs.AddString("offsetObserver", "(double/NaN) optional offset of observer from surface");
            getLineOfSightInputs.AddString("offsetTarget", "(double/NaN) optional offset of target from surface");
            getLineOfSightInputs.AddString("applyCurvature", "(boolean) optional true to have earth curvature taken into consideration. The default is false. It can be set to true if the surface has a defined projected coordinate system that includes defined ZUnits");
            getLineOfSightInputs.AddString("applyRefraction", "(boolean) optional true to have refraction of visible light taken into consideration. The default is false. It can be set to true if the surface has a defined projected coordinate system that includes defined ZUnits");
            getLineOfSightInputs.AddString("refractionFactor", "(number/NaN) optional The default refraction factor is 0.13");
            
            JsonObject getLineOfSightOutput = new JsonObject();
            getLineOfSightOutput.AddString("pointObstruction", "(geometry) the location of the first obstruction point");
            getLineOfSightOutput.AddString("visibleLines", "(geometry) represent that which is seen from the observation point");
            getLineOfSightOutput.AddString("invisibleLines", "(geometry) represent that which isn't seen from the observation point");
            getLineOfSightOutput.AddString("isVisible", "(boolean) whether the target is visible");

            JsonObject getLineOfSightParams = new JsonObject();
            getLineOfSightParams.AddString("Info", "Computes the visibility of a line-of-sight from the observer to the target.");
            getLineOfSightParams.AddJsonObject("Inputs", getLineOfSightInputs);
            getLineOfSightParams.AddJsonObject("Outputs", getLineOfSightOutput);

            JsonObject getSteepestPathInputs = new JsonObject();
            getSteepestPathInputs.AddString("geometry", "(geometry) Point");

            JsonObject getSteepestPathOutput = new JsonObject();
            getSteepestPathOutput.AddString("geometry", "(geometry) Polyline The steepest downhill path. The resulting polyline pointer will be set to Nil (nothing) if the query point falls outside the surface or on a flat area");

            JsonObject getSteepestPathParams = new JsonObject();
            getSteepestPathParams.AddString("Info", "Returns the steepest downhill path, the direction of steepest slope, from the specified query point. It will start at the query point and end in a pit or the edge of the surface. The returned polyline will be 3D.");
            getSteepestPathParams.AddJsonObject("Inputs", getSteepestPathInputs);
            getSteepestPathParams.AddJsonObject("Outputs", getSteepestPathOutput);

            JsonObject getContourInputs = new JsonObject();
            getContourInputs.AddString("geometry", "(geometry) Point");

            JsonObject getContourOutput = new JsonObject();
            getContourOutput.AddString("geometry", "(geometry) Polyline The contour");
            getContourOutput.AddString("elevation", "(number) double/NaN The height corresponding to a specified query point.");

            JsonObject getContourParams = new JsonObject();
            getContourParams.AddString("Info", "Returns the contour and height corresponding to a specified query point.");
            getContourParams.AddJsonObject("Inputs", getContourInputs);
            getContourParams.AddJsonObject("Outputs", getContourOutput);

            JsonObject getSlopeInputs = new JsonObject();
            getSlopeInputs.AddString("geometry", "(geometry) Point");
            getSlopeInputs.AddString("units", "(string) percent|degrees|radians Optional(default:percent)");

            JsonObject getSlopeOutput = new JsonObject();
            getSlopeOutput.AddString("slope", "(double) Slope");
            getSlopeOutput.AddString("units", "(string) percent|degrees|radians");

            JsonObject getSlopeParams = new JsonObject();
            getSlopeParams.AddString("Info", "Returns the slope at the specified location in percent or degrees or radians.");
            getSlopeParams.AddJsonObject("Inputs", getSlopeInputs);
            getSlopeParams.AddJsonObject("Outputs", getSlopeOutput);

            JsonObject getAspectInputs = new JsonObject();
            getAspectInputs.AddString("geometry", "(geometry) Point");
            getAspectInputs.AddString("units", "(string) degrees|radians Optional(default:degrees)");

            JsonObject getAspectOutput = new JsonObject();
            getAspectOutput.AddString("aspect", "(double) Aspect");
            getAspectOutput.AddString("units", "(string) degrees|radians");

            JsonObject getAspectParams = new JsonObject();
            getAspectParams.AddString("Info", "Returns the aspect at the specified location in degrees or radians. Aspect is defined as the direction of steepest slope. The possible range of values falls between 0.0 and 360. 0.0 represents a north facing slope with increasing values changing aspect in a clockwise direction. For example, 90 degrees is due east, 180 degrees due south, and 270 degrees due west");
            getAspectParams.AddJsonObject("Inputs", getAspectInputs);
            getAspectParams.AddJsonObject("Outputs", getAspectOutput);

            JsonObject getSurfaceLengthInputs = new JsonObject();
            getSurfaceLengthInputs.AddString("geometry", "(geometry) Polyline");
            getSurfaceLengthInputs.AddString("stepSize", "(double/NaN) Generally, the smaller the interval the greater the detail (unless smaller than 1/2 cellsize), but at an increased cost in processing time and size of resulting geometry. The default stepSize for raster based surface is set equal to the cellsize");

            JsonObject getSurfaceLengthOutput = new JsonObject();
            getSurfaceLengthOutput.AddString("surfaceLength", "(double) Returns the 3D length of the polyline by interpolating heights from the surface and calculating the sum of 3D distances between the vertices");

            JsonObject getSurfaceLengthParams = new JsonObject();
            getSurfaceLengthParams.AddString("Info", "Returns the 3D length of the polyline by interpolating heights from the surface and calculating the sum of 3D distances between the vertices. Portions of the line falling outside the interpolation zone are excluded from the calculation");
            getSurfaceLengthParams.AddJsonObject("Inputs", getSurfaceLengthInputs);
            getSurfaceLengthParams.AddJsonObject("Outputs", getSurfaceLengthOutput);

            JsonObject getNormalInputs = new JsonObject();
            getNormalInputs.AddString("geometry", "(geometry) Point");

            JsonObject getNormalOutput = new JsonObject();
            getNormalOutput.AddString("geometry", "(geometry) input location");
            getNormalOutput.AddString("vector3D", "(array) double Components (x,y,z) of vector normal (versor)");

            JsonObject getNormalParams = new JsonObject();
            getNormalParams.AddString("Info", "Returns the normal vector corresponding to a specified query point.");
            getNormalParams.AddJsonObject("Inputs", getNormalInputs);
            getNormalParams.AddJsonObject("Outputs", getNormalOutput);

            JsonObject getLocateInputs = new JsonObject();
            getLocateInputs.AddString("geometry", "(geometry) Line");
            getLocateInputs.AddString("offsetFromPoint", "(double/NaN) offset from surface. If NaN uses Z from start point line");
            getLocateInputs.AddString("offsetToPoint", "(double/NaN) offset from surface. If NaN uses Z from end point line");
            getLocateInputs.AddString("hint", "(int/NaN) Optional Default = 0");

            JsonObject getLocateOutput = new JsonObject();
            getLocateOutput.AddString("geometry", "(geometry) point location");

            JsonObject getLocateParams = new JsonObject();
            getLocateParams.AddString("Info", "Returns the intersection of the query ray (origin: start point of line) and the surface");
            getLocateParams.AddJsonObject("Inputs", getLocateInputs);
            getLocateParams.AddJsonObject("Outputs", getLocateOutput);

            JsonObject getLocateAllInputs = new JsonObject();
            getLocateAllInputs.AddString("geometry", "(geometry) Line");
            getLocateAllInputs.AddString("offsetFromPoint", "(double/NaN) offset from surface. If NaN uses Z from start point line");
            getLocateAllInputs.AddString("offsetToPoint", "(double/NaN) offset from surface. If NaN uses Z from end point line");
            getLocateAllInputs.AddString("hint", "(int/NaN) Optional Default = 0");

            JsonObject getLocateAllOutput = new JsonObject();
            getLocateAllOutput.AddString("distances", "(array double)  The distances of intersections of the query ray (origin: start point of line) and the surface");

            JsonObject getLocateAllParams = new JsonObject();
            getLocateAllParams.AddString("Info", "Returns the distances of intersections of the query ray (origin: start point of line) and the surface.");
            getLocateAllParams.AddJsonObject("Inputs", getLocateAllInputs);
            getLocateAllParams.AddJsonObject("Outputs", getLocateAllOutput);

            JsonObject soeOperations = new JsonObject();
            soeOperations.AddJsonObject("GetElevationAtLonLat", getElevationAtLonLatParams);
            soeOperations.AddJsonObject("GetElevations", getElevationsParams);
            soeOperations.AddJsonObject("GetElevationData", getElevationDataParams);
            soeOperations.AddJsonObject("GetLineOfSight", getLineOfSightParams);
            soeOperations.AddJsonObject("GetSteepestPath", getSteepestPathParams);
            soeOperations.AddJsonObject("GetContour", getContourParams);
            soeOperations.AddJsonObject("GetSlope", getSlopeParams);
            soeOperations.AddJsonObject("GetAspect", getAspectParams);
            soeOperations.AddJsonObject("GetSurfaceLength", getSurfaceLengthParams);
            soeOperations.AddJsonObject("GetNormal", getNormalParams);
            soeOperations.AddJsonObject("GetLocate", getLocateParams);
            soeOperations.AddJsonObject("GetLocateAll", getLocateAllParams);
            result.AddJsonObject("Operations", soeOperations);

            return result.JsonByte();
        }

        /// <summary>
        /// Returns JSON representation of an element of SurfaceLayers collection. This element is represented by resourceId parameter.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of SurfaceLayers resource.</returns>
        private byte[] SurfaceLayer(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            if (boundVariables["SurfaceLayersID"] == null)
            {
                // http://[server]/[instance]/rest/services/[folder]/[mapservicename]/MapServer/SurfaceUtility
                JsonObject result = new JsonObject();
                JsonObject[] surfaceLayersJson = this.GetSurfaceLayerInfosAsJsonObjects();
                result.AddArray("SurfaceLayers", surfaceLayersJson);
                return result.JsonByte();
            }
            else
            {
                // http://[server]/[instance]/rest/services/[folder]/[mapservicename]/MapServer/SurfaceUtility/[layerid]
                int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);
                SurfaceLayerInfo surfaceLayerInfo = this.GetSurfaceLayerInfo(layerID);
                return surfaceLayerInfo.ToJsonObject().JsonByte();
            }
        }

        /// <summary>
        /// Convert layerInfos in the map to a list of SurfaceLayerInfos
        /// </summary>
        /// <returns>A list of SurfaceLayerInfos</returns>
        private List<SurfaceLayerInfo> GetSurfaceLayerInfos()
        {
            List<SurfaceLayerInfo> surfaceLayerInfos = new List<SurfaceLayerInfo>();

            for (int layerIndex = 0; layerIndex < this.layerInfos.Count; layerIndex++)
            {
                IMapLayerInfo layerInfo = this.layerInfos.get_Element(layerIndex);
                object dataSource = this.mapServerDataAccess.GetDataSource(this.mapServer.DefaultMapName, layerInfo.ID);
                IRaster raster = dataSource as IRaster;
                IRasterBandCollection rasterBC = dataSource as IRasterBandCollection;

                if ((raster != null) && (rasterBC != null))
                {
                    if (rasterBC.Count == 1)
                    {
                        surfaceLayerInfos.Add(new SurfaceLayerInfo(layerInfo, raster));
                    }
                }
            }

            return surfaceLayerInfos;
        }

        /// <summary>
        /// Get all SurfaceLayerInfos as an array of JsonObjects
        /// </summary>
        /// <returns>Array of JsonObjects</returns>
        private JsonObject[] GetSurfaceLayerInfosAsJsonObjects()
        {
            return System.Array.ConvertAll(this.surfaceLayerInfos.ToArray(), element => element.ToJsonObject());
        }

        /// <summary>
        /// Get the SurfaceLayerInfo for a map layer based on the layer id
        /// </summary>
        /// <param name="layerID">The layer id</param>
        /// <returns>The SurfaceLayerInfo for the layer</returns>
        private SurfaceLayerInfo GetSurfaceLayerInfo(int layerID)
        {
            if (layerID < 0)
            {
                throw new SurfaceUtilityException("Invalid layer id: " + layerID);
            }

            SurfaceLayerInfo surfaceLayerInfo = this.surfaceLayerInfos.Find(i => i.GetId() == layerID); 
                
            if (surfaceLayerInfo != null)
            {
                return surfaceLayerInfo;
            }
            else
            {
                throw new SurfaceUtilityException("Could not find layer id: " + layerID);
            }
        }

        /// <summary>
        /// bandIndex from operationInput (default: 0)
        /// </summary>
        /// <param name="operationInput">object JsonObject</param>
        /// <returns>index of band</returns>
        private int GetBandIndex(JsonObject operationInput)
        {
            long? indexBand;
            bool found = operationInput.TryGetAsLong("bandIndex", out indexBand);
            if (!found || !indexBand.HasValue || indexBand.Value < 0)
            {
                return 0;
            }

            return (int)indexBand.Value;
        }

        /// <summary>
        /// Method for implementing REST operation "GetElevationAtLonLat"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetElevationAtLonLatOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            double? lon;
            bool found = operationInput.TryGetAsDouble("lon", out lon);
            if (!found || !lon.HasValue)
            {
                throw new SurfaceUtilityException("lon is wrong!");
            }

            if ((lon.Value < -180.0) || (lon.Value > 180.0))
            {
                throw new SurfaceUtilityException("lon value must be between -180 and 180");
            }

            double? lat;
            found = operationInput.TryGetAsDouble("lat", out lat);
            if (!found || !lat.HasValue)
            {
                throw new SurfaceUtilityException("lat is wrong!");
            }

            if ((lat.Value < -90.0) || (lat.Value > 90.0))
            {
                throw new SurfaceUtilityException("lat value must be between -90 and 90");
            }

            Type factoryType = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            object objSpatialReferenceFactory = Activator.CreateInstance(factoryType);
            ISpatialReferenceFactory3 spatialReferenceFactory = objSpatialReferenceFactory as ISpatialReferenceFactory3;
            IGeographicCoordinateSystem WGS84 = spatialReferenceFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);

            IPoint point = new PointClass();
            point.SpatialReference = WGS84;
            point.PutCoords(lon.Value, lat.Value);

            IGeometry interpolatedPoint = null;
            this.GetInterpolatedGeometry(analysisSurface, (IGeometry)point, out interpolatedPoint);

            double elevation = ((IPoint)interpolatedPoint).Z;

            JsonObject result = new JsonObject();
            result.AddDouble("elevation", elevation);
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetElevations"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetElevationsOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            object[] jsonGeomArray;
            if (!operationInput.TryGetArray("geometries", out jsonGeomArray))
            {
                throw new SurfaceUtilityException("geometries is wrong!");
            }

            IGeometry interpolatedGeom = null;

            List<JsonObject> interpolatedJsonGeomArray = new List<JsonObject>();
            for (int geomIndex = 0; geomIndex < jsonGeomArray.Length; geomIndex++)
            {
                JsonObject jsonGeom = jsonGeomArray[geomIndex] as JsonObject;
                IGeometry geom = jsonGeom.ConvertAnyJsonGeom();
                if (geom != null)
                {
                    this.GetInterpolatedGeometry(analysisSurface, geom, out interpolatedGeom);
                    JsonObject interpolatedJsonGeom = Conversion.ToJsonObject(interpolatedGeom);
                    interpolatedJsonGeomArray.Add(interpolatedJsonGeom);
                }
                else
                {
                    interpolatedJsonGeomArray.Add(jsonGeom);
                }
            }

            JsonObject result = new JsonObject();
            result.AddArray("geometries", interpolatedJsonGeomArray.ToArray());

            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetElevationData"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetElevationDataOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            // NUMBER OF ROWS
            long? rows;
            bool found = operationInput.TryGetAsLong("rows", out rows);
            if (!found || !rows.HasValue)
            {
                throw new SurfaceUtilityException("Rows is wrong!");
            }

            // NUMBER OF COLUMNS
            long? columns;
            found = operationInput.TryGetAsLong("columns", out columns);
            if (!found || !columns.HasValue)
            {
                throw new SurfaceUtilityException("Columns is wrong!");
            }

            // ASKING FOR TOO MUCH DATA?
            if ((rows.Value * columns.Value) > this.maxDataValues)
            {
                throw new SurfaceUtilityException("Requesting too much data; please reduce the number of rows and columns.");
            }

            // EXTENT
            JsonObject jsonExtent;
            if (!operationInput.TryGetJsonObject("extent", out jsonExtent))
            {
                throw new SurfaceUtilityException("Extent is wrong!");
            }

            // ENVELOPE
            IEnvelope inputEnvelope = (IEnvelope)jsonExtent.ConvertAnyJsonGeom();

            // RASTER
            IRaster raster = analysisSurface.Raster;
            IRasterProps rasterProps = (IRasterProps)raster;

            // RESAMPLE METHOD
            raster.ResampleMethod = rstResamplingTypes.RSP_BilinearInterpolation;

            // SET THE SPATIAL REFERENCE, EXTENT, AND NUMBER OF ROWS AND COLUMNS
            rasterProps.SpatialReference = inputEnvelope.SpatialReference;
            rasterProps.Extent = inputEnvelope;
            rasterProps.Width = (int)columns.Value;
            rasterProps.Height = (int)rows.Value;

            // CREATE OUTPUT JSON OBJECT            
            JsonObject result = new JsonObject();

            // ADD RASTER VALUES
            result.AddLong("nCols", rasterProps.Width);
            result.AddLong("nRows", rasterProps.Height);
            result.AddDouble("xLLCenter", rasterProps.Extent.XMin + (rasterProps.MeanCellSize().X * 0.5));
            result.AddDouble("yLLCenter", rasterProps.Extent.YMin + (rasterProps.MeanCellSize().Y * 0.5));
            result.AddDouble("cellSize", rasterProps.MeanCellSize().X);

            // CREATE PIXEL BLOCK
            IPnt tlc = new PntClass();
            tlc.SetCoords(0, 0);
            IPnt blocksize = new PntClass();
            blocksize.SetCoords(columns.Value, rows.Value);
            IPixelBlock3 pixelblock = raster.CreatePixelBlock(blocksize) as IPixelBlock3;

            // READ ELEVATION DATA
            raster.Read(tlc, (IPixelBlock)pixelblock);

            // GET PIXEL TYPE
            rstPixelType pixelType = rasterProps.PixelType;
            result.AddObject("pixelType", pixelType.ToString());

            //// GET NODATA VALUE
            ////object noDataValue = rasterProps.NoDataValue;

            ////if (noDataValue == null)
            ////{
            object noDataValue = Helper.GetDefaultNoDataValue(analysisSurface);
            ////}

            ////if (noDataValue.GetType().IsArray)
            ////{
            ////    noDataValue = (noDataValue as System.Array).GetValue(0);
            ////}

            // ADD NODATA VALUE
            result.AddObject("noDataValue", noDataValue);

            // GET SPATIAL REFERENCE
            IJSONConverterGeometry jsonConvert = new JSONConverterGeometryClass();
            IJSONObject spatRefObj = new JSONObjectClass();
            jsonConvert.QueryJSONSpatialReference(rasterProps.SpatialReference, spatRefObj);
            JsonObject spatRefJson = new JsonObject(spatRefObj.ToJSONString(null));

            // ADD SPATIAL REFERENCE
            result.AddJsonObject("spatialReference", spatRefJson);

            // RASTER PROPERTIES
            JsonObject rasterPropsJson = new JsonObject();

            // IS INTEGER
            rasterPropsJson.AddBoolean("isInteger", rasterProps.IsInteger);

            // MIN MAX RASTER DATASET VALUES            
            IRasterBand rasterBand = analysisSurface.RasterBand;
            bool hasStatistics = false;
            rasterBand.HasStatistics(out hasStatistics);
            if (hasStatistics)
            {
                IRasterStatistics rasterStats = rasterBand.Statistics;
                rasterPropsJson.AddDouble("datasetMin", rasterStats.Minimum);
                rasterPropsJson.AddDouble("datasetMax", rasterStats.Maximum);
            }
            else
            {
                rasterPropsJson.AddDouble("datasetMin", double.NaN);
                rasterPropsJson.AddDouble("datasetMax", double.NaN);
            }

            // ADD RASTER PROPERTIES
            result.AddObject("rasterProperties", rasterPropsJson);

            // TRANSPOSE DATA ARRAY SO IT'S ROW MAJOR AND ADJUST NODATA VALUES
            System.Array elevationData = Helper.TransposeArray(pixelblock, noDataValue);

            // ADD ELEVATION VALUES
            result.AddObject("data", elevationData);

            // RETURN RESULT OBJECT
            return result.JsonByte();
        }

        /// <summary>
        /// INTERPOLATE ELEVATION VALUES FOR INPUT GEOMETRY
        /// </summary>
        /// <param name="analysisSurface">Analysis surface</param>
        /// <param name="geom">Input geometry</param>        
        /// <param name="interpolatedGeom">Output geometry that has Z along interpolated vertex or points</param>
        private void GetInterpolatedGeometry(AnalysisSurface analysisSurface, IGeometry geom, out IGeometry interpolatedGeom)
        {
            // PROJECT INPUT GEOMETRY TO THE ANALYSIS ELEVATION SURFACE SPATIAL REFERENCE
            interpolatedGeom = (IGeometry)((IClone)geom).Clone();
            interpolatedGeom.Project(analysisSurface.SpatialReference);

            // MAKE Z AWARE
            IZAware zaware = (IZAware)interpolatedGeom;
            zaware.ZAware = true;

            switch (interpolatedGeom.GeometryType)
            {
                case esriGeometryType.esriGeometryPolygon:
                case esriGeometryType.esriGeometryPolyline:
                    
                    // POLY
                    IPolycurve3 polyCurve = (IPolycurve3)interpolatedGeom;

                    // SIMPLIFY POLY?                    
                    if (Helper.ShouldSimplify(polyCurve))
                    {
                        ITopologicalOperator2 topoPoly = (ITopologicalOperator2)polyCurve;
                        topoPoly.IsKnownSimple_2 = false;
                        topoPoly.Simplify();
                    }

                    // DENSIFY POLY                    
                    double stepSize = polyCurve.Length / (this.interpolationCount - 2);
                    polyCurve.Densify((double)stepSize, 0d);

                    // INTERPOLATE VERTICES
                    analysisSurface.Surface.InterpolateShapeVertices(interpolatedGeom, out interpolatedGeom);
                    
                    if (interpolatedGeom == null)
                    {
                        throw new SurfaceUtilityException("Portion of the input feature falls outside the surface!");
                    }

                    break;

                case esriGeometryType.esriGeometryMultipoint:
                    // GET ENUMERATION OF POINTS
                    IEnumVertex vertices = ((IPointCollection)interpolatedGeom).EnumVertices;
                    Helper.InterpolateVertices(analysisSurface, vertices);
                    break;

                case esriGeometryType.esriGeometryPoint:
                    // GET ELEVATION AT POINT LOCATION                    
                    double elevation = Helper.GetSurfaceElevation(analysisSurface, (IPoint)interpolatedGeom);

                    // ASSIGN Z USING ELEVATION
                    ((IPoint)interpolatedGeom).Z = elevation;
                    break;

                default:
                    break;
            }

            // RESET SPATIAL REFERENCE
            interpolatedGeom.SpatialReference = analysisSurface.SpatialReference;

            // PROJECT BACK TO INPUT SPATIAL REFERENCE
            interpolatedGeom.Project(geom.SpatialReference);
        }

        /// <summary>
        /// Method for implementing REST operation "GetLineOfSight"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetLineOfSightOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            // offsetObserver
            double? offsetObserver;
            operationInput.TryGetAsDouble("offsetObserver", out offsetObserver);

            // offsetTarget
            double? offsetTarget;
            operationInput.TryGetAsDouble("offsetTarget", out offsetTarget);

            JsonObject jsonLine;
            if (!operationInput.TryGetJsonObject("geometry", out jsonLine))
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            IPolyline polyline = jsonLine.ConvertAnyJsonGeom() as IPolyline;
            if (polyline == null)
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            bool? applyCurvature;
            operationInput.TryGetAsBoolean("applyCurvature", out applyCurvature);

            if (!applyCurvature.HasValue)
            {
                applyCurvature = false;
            }

            bool? applyRefraction;
            operationInput.TryGetAsBoolean("applyRefraction", out applyRefraction);

            if (!applyRefraction.HasValue)
            {
                applyRefraction = false;
            }

            double? refractionFactor;
            operationInput.TryGetAsDouble("refractionFactor", out refractionFactor);
            object refractionFactorValue = Type.Missing;

            if (refractionFactor.HasValue)
            {
                refractionFactorValue = refractionFactor.Value;
            }

            IPoint pointFrom = polyline.FromPoint;
            IPoint pointTo = polyline.ToPoint;
            ISurface surface = analysisSurface.Surface;
            pointFrom.Z = surface.GetElevation(pointFrom);
            pointTo.Z = surface.GetElevation(pointTo);

            if (surface.IsVoidZ(pointFrom.Z) || surface.IsVoidZ(pointTo.Z))
            {
                throw new SurfaceUtilityException("End points line not valid!");
            }

            if (offsetObserver.HasValue)
            {
                pointFrom.Z += offsetObserver.Value;
            }

            if (offsetTarget.HasValue)
            {
                pointTo.Z += offsetTarget.Value;
            }

            IPoint pointObstruction;
            IPolyline visibleLines;
            IPolyline invisibleLines;
            bool isVisible;

            // applyCurvature and applyRefraction can be true if the surface has a defined projected coordinate system that includes defined ZUnits. 
            try
            {
                if (!surface.CanDoCurvature)
                {
                    applyCurvature = false;
                    applyRefraction = false;
                }
            }
            catch
            {
                applyCurvature = false;
                applyRefraction = false;
            }

            Type t = Type.GetTypeFromProgID("esriGeodatabase.GeoDatabaseHelper");
            object obj = Activator.CreateInstance(t);
            IGeoDatabaseBridge2 geoDatabaseBridge = obj as IGeoDatabaseBridge2;
            geoDatabaseBridge.GetLineOfSight(surface, pointFrom, pointTo, out pointObstruction, out visibleLines, out invisibleLines, out isVisible, applyCurvature.Value, applyRefraction.Value, ref refractionFactorValue);

            JsonObject result = new JsonObject();
            result.AddJsonObject("pointObstruction", Conversion.ToJsonObject(pointObstruction, true));
            result.AddJsonObject("visibleLines", Conversion.ToJsonObject(visibleLines));
            result.AddJsonObject("invisibleLines", Conversion.ToJsonObject(invisibleLines));
            result.AddBoolean("isVisible", isVisible);
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetSteepestPath"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetSteepestPathOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPoint;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPoint))
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            IPoint point = jsonPoint.ConvertAnyJsonGeom() as IPoint;
            if (point == null)
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            ISurface surface = analysisSurface.Surface;
            double z = surface.GetElevation(point);

            if (surface.IsVoidZ(z))
            {
                throw new SurfaceUtilityException("Point not valid!");
            }

            IPolyline polyline = surface.GetSteepestPath(point);

            JsonObject result = new JsonObject();
            result.AddJsonObject("geometry", Conversion.ToJsonObject(polyline, true));
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetContour"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetContourOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPoint;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPoint))
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            IPoint point = jsonPoint.ConvertAnyJsonGeom() as IPoint;
            if (point == null)
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            ISurface surface = analysisSurface.Surface;
            double z = surface.GetElevation(point);

            if (surface.IsVoidZ(z))
            {
                throw new SurfaceUtilityException("Point not valid!");
            }

            IPolyline polyline;
            double elevation;
            surface.GetContour(point, out polyline, out elevation);

            JsonObject result = new JsonObject();
            result.AddJsonObject("geometry", Conversion.ToJsonObject(polyline, true));
            result.AddDouble("elevation", elevation);
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetSlope"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetSlopeOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPoint;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPoint))
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            IPoint point = jsonPoint.ConvertAnyJsonGeom() as IPoint;
            if (point == null)
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            ISurface surface = analysisSurface.Surface;
            double z = surface.GetElevation(point);

            if (surface.IsVoidZ(z))
            {
                throw new SurfaceUtilityException("point not valid!");
            }

            string unit;
            SlopeUnits unitValue = SlopeUnits.Percent;
            if (operationInput.TryGetString("units", out unit) && !string.IsNullOrEmpty(unit))
            {
                if (Enum.IsDefined(typeof(SlopeUnits), unit))
                {
                    unitValue = (SlopeUnits)Enum.Parse(typeof(SlopeUnits), unit, true);
                }
            }

            double slope = double.NaN;
            if (unitValue == SlopeUnits.Percent)
            {
                slope = surface.GetSlopePercent(point);
            }
            else if (unitValue == SlopeUnits.Degrees)
            {
                slope = surface.GetSlopeDegrees(point);
            }
            else if (unitValue == SlopeUnits.Radians)
            {
                slope = surface.GetSlopeRadians(point);   
            }

            JsonObject result = new JsonObject();
            result.AddDouble("slope", slope);
            result.AddString("units", Enum.GetName(typeof(SlopeUnits), unitValue));
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetAspect"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetAspectOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPoint;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPoint))
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            IPoint point = jsonPoint.ConvertAnyJsonGeom() as IPoint;
            if (point == null)
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            ISurface surface = analysisSurface.Surface;
            double z = surface.GetElevation(point);

            if (surface.IsVoidZ(z))
            {
                throw new SurfaceUtilityException("Point not valid!");
            }

            string unit;
            AspectUnits unitValue = AspectUnits.Degrees;
            if (operationInput.TryGetString("units", out unit) && !string.IsNullOrEmpty(unit))
            {
                if (Enum.IsDefined(typeof(AspectUnits), unit))
                {
                    unitValue = (AspectUnits)Enum.Parse(typeof(AspectUnits), unit, true);
                }
            }

            double aspect = double.NaN;
            if (unitValue == AspectUnits.Degrees)
            {
                aspect = surface.GetAspectDegrees(point);
            }
            else if (unitValue == AspectUnits.Radians)
            {
                aspect = surface.GetAspectRadians(point);
            }

            JsonObject result = new JsonObject();
            result.AddDouble("aspect", aspect);
            result.AddString("units", Enum.GetName(typeof(AspectUnits), unitValue));
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetSurfaceLength"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetSurfaceLengthOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPolyline;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPolyline))
            {
                throw new SurfaceUtilityException("Polyline is wrong!");
            }

            IPolyline polyline = jsonPolyline.ConvertAnyJsonGeom() as IPolyline;
            if (polyline == null)
            {
                throw new SurfaceUtilityException("Polyline is wrong!");
            }

            double? stepSize;
            operationInput.TryGetAsDouble("stepSize", out stepSize);
            object stepSizeValue = Type.Missing;

            if (stepSize.HasValue)
            {
                stepSizeValue = stepSize.Value;
            }

            double surfaceLength;
            analysisSurface.Surface.QuerySurfaceLength(polyline, out surfaceLength, ref stepSizeValue);

            JsonObject result = new JsonObject();
            result.AddDouble("surfaceLength", surfaceLength);
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetNormal"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetNormalHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonPoint;
            if (!operationInput.TryGetJsonObject("geometry", out jsonPoint))
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            IPoint point = jsonPoint.ConvertAnyJsonGeom() as IPoint;
            if (point == null)
            {
                throw new SurfaceUtilityException("Point is wrong!");
            }

            ISurface surface = analysisSurface.Surface;
            double z = surface.GetElevation(point);

            if (surface.IsVoidZ(z))
            {
                throw new SurfaceUtilityException("Point not valid!");
            }

            IVector3D normal = new Vector3DClass();
            surface.QueryNormal(point, normal);

            double cx;
            double cy;
            double cz;
            normal.QueryComponents(out cx, out cy, out cz);
            JsonObject result = new JsonObject();

            result.AddJsonObject("geometry", Conversion.ToJsonObject(point, true));
            result.AddArray("vector3D", new object[] { cx, cy, cz });
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetLocate"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetLocateHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonGeometry;
            if (!operationInput.TryGetJsonObject("geometry", out jsonGeometry))
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            IPolyline polyline = jsonGeometry.ConvertAnyJsonGeom() as IPolyline;
            if (polyline == null)
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            IPoint pointFrom = polyline.FromPoint;
            IPoint pointTo = polyline.ToPoint;
            ISurface surface = analysisSurface.Surface;

            // offsetFromPoint
            double? offsetFromPoint;
            if (operationInput.TryGetAsDouble("offsetFromPoint", out offsetFromPoint) && offsetFromPoint.HasValue && offsetFromPoint != double.NaN)
            {
                pointFrom.Z = surface.GetElevation(pointFrom);
                pointFrom.Z += offsetFromPoint.Value;
            }

            if (surface.IsVoidZ(pointFrom.Z))
            {
                throw new SurfaceUtilityException("Start point line not valid!");
            }

            // offsetToPoint
            double? offsetToPoint;
            if (operationInput.TryGetAsDouble("offsetToPoint", out offsetToPoint) && offsetToPoint.HasValue && offsetToPoint != double.NaN)
            {
                pointTo.Z = surface.GetElevation(pointTo);
                pointTo.Z += offsetToPoint.Value;
            }

            if (surface.IsVoidZ(pointTo.Z))
            {
                throw new SurfaceUtilityException("End point line not valid!");
            }

            // hint
            long? hint;
            operationInput.TryGetAsLong("hint", out hint);
            int hintValue = 0;
            if (operationInput.TryGetAsLong("hint", out hint) && hint.HasValue)
            {
                hintValue = (int)hint.Value;
            }

            IRay ray = new RayClass();
            ray.Origin = pointFrom;
            ray.Vector = GeometryUtility.ConstructVector3D(pointTo.X - pointFrom.X, pointTo.Y - pointFrom.Y, pointTo.Z - pointFrom.Z);

            IPoint point = surface.Locate(ray, hintValue);

            JsonObject result = new JsonObject();
            result.AddJsonObject("geometry", Conversion.ToJsonObject(point, true));
            
            return result.JsonByte();
        }

        /// <summary>
        /// Method for implementing REST operation "GetLocateAll"'s functionality.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of output</returns>
        private byte[] GetLocateAllHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = System.Convert.ToInt32(boundVariables["SurfaceLayersID"], CultureInfo.InvariantCulture);

            AnalysisSurface analysisSurface = this.GetSurfaceLayerInfo(layerID).GetAnalysisSurface();

            JsonObject jsonGeometry;
            if (!operationInput.TryGetJsonObject("geometry", out jsonGeometry))
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            IPolyline polyline = jsonGeometry.ConvertAnyJsonGeom() as IPolyline;
            if (polyline == null)
            {
                throw new SurfaceUtilityException("Geometry is wrong!");
            }

            IPoint pointFrom = polyline.FromPoint;
            IPoint pointTo = polyline.ToPoint;
            ISurface surface = analysisSurface.Surface;

            // offsetFromPoint
            double? offsetFromPoint;
            if (operationInput.TryGetAsDouble("offsetFromPoint", out offsetFromPoint) && offsetFromPoint.HasValue && offsetFromPoint != double.NaN)
            {
                pointFrom.Z = surface.GetElevation(pointFrom);
                pointFrom.Z += offsetFromPoint.Value;
            }

            if (surface.IsVoidZ(pointFrom.Z))
            {
                throw new SurfaceUtilityException("Start point line not valid!");
            }

            // offsetToPoint
            double? offsetToPoint;
            if (operationInput.TryGetAsDouble("offsetToPoint", out offsetToPoint) && offsetToPoint.HasValue && offsetToPoint != double.NaN)
            {
                pointTo.Z = surface.GetElevation(pointTo);
                pointTo.Z += offsetToPoint.Value;
            }

            if (surface.IsVoidZ(pointTo.Z))
            {
                throw new SurfaceUtilityException("End point line not valid!");
            }
            
            // hint
            long? hint;
            operationInput.TryGetAsLong("hint", out hint);
            int hintValue = 0;
            if (operationInput.TryGetAsLong("hint", out hint) && hint.HasValue)
            {
                hintValue = (int)hint.Value;
            }

            IRay ray = new RayClass();
            ray.Origin = pointFrom;
            ray.Vector = GeometryUtility.ConstructVector3D(pointTo.X - pointFrom.X, pointTo.Y - pointFrom.Y, pointTo.Z - pointFrom.Z);

            IDoubleArray doubleArray = surface.LocateAll(ray, hintValue);
            List<object> distances = new List<object>();
            for (int i = 0; i < doubleArray.Count; i++)
            {
                distances.Add(doubleArray.get_Element(i));
            }

            JsonObject result = new JsonObject();
            result.AddArray("distances", distances.ToArray());

            return result.JsonByte();
        }
    }
}
