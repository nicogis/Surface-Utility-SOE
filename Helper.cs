//-----------------------------------------------------------------------
// <copyright file="Helper.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio AT s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using ESRI.ArcGIS.DataSourcesRaster;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;

    /// <summary>
    /// unit (slope)
    /// </summary>
    internal enum SlopeUnits
    {
        /// <summary>
        /// unit degree
        /// </summary>
        Degrees,
        
        /// <summary>
        /// unit percent
        /// </summary>
        Percent,

        /// <summary>
        /// unit radian
        /// </summary>
        Radians
    }

    /// <summary>
    /// unit (aspect)
    /// </summary>
    internal enum AspectUnits
    {
        /// <summary>
        /// unit degree
        /// </summary>
        Degrees,

        /// <summary>
        /// unit radian
        /// </summary>
        Radians
    }

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]

    /// <summary>
    /// class helper
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// TRANSPOSE ARRAY
        /// </summary>
        /// <param name="pixelblock">PixelBlock of data values</param>
        /// <param name="noDataValue">Default NoData value</param>
        /// <returns>Array of values transposed</returns>
        internal static Array TransposeArray(IPixelBlock3 pixelblock, object noDataValue)
        {
            System.Array oldArray = (System.Array)pixelblock.get_PixelData(0);
            int cols = oldArray.GetLength(0);
            int rows = oldArray.GetLength(1);

            System.Array newArray = System.Array.CreateInstance(oldArray.GetType().GetElementType(), rows, cols);
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    object noDataMaskValue = pixelblock.GetNoDataMaskVal(0, col, row);
                    object pixelValue = (Convert.ToByte(noDataMaskValue, CultureInfo.InvariantCulture) == 1) ? oldArray.GetValue(col, row) : noDataValue;
                    newArray.SetValue(pixelValue, row, col);
                }
            }

            return newArray;
        }

        /// <summary>
        ///     DETERMINE IF POLYCURVE NEEDS TO BE SIMPLIFIED.  MULTI-PART GEOMETRIES ARE SUPPORTED
        ///     BUT WE NEED TO DETERMINE IF THENY NEED TO BE SIMPLIFIED.  THIS ROUTINE MEASURES THE 
        ///     DISTANCE BETWEEN GEOMETRY PARTS TO DETERMINE IF TWO CONSECUTIVE PARTS ARE SPATIALLY
        ///     ADJACENT.
        /// </summary>
        /// <param name="polyCurve">geometry polycurve </param>
        /// <returns>NON-GEODATABASE GEOMETRY = TRUE and RESULT OF ROUTING MULTIPLE STOPS = FALSE</returns>
        internal static bool ShouldSimplify(IPolycurve3 polyCurve)
        {
            IGeometryCollection subGeometries = (IGeometryCollection)polyCurve;
            int geometryCount = subGeometries.GeometryCount;
            if (geometryCount > 1)
            {
                for (int geomIndex = 0; geomIndex < (geometryCount - 1); geomIndex++)
                {
                    ICurve subGeom1 = (ICurve)subGeometries.get_Geometry(geomIndex);
                    ICurve subGeom2 = (ICurve)subGeometries.get_Geometry(geomIndex + 1);

                    IProximityOperator proximityOp = (IProximityOperator)subGeom1.ToPoint;
                    double distanceBetweenParts = proximityOp.ReturnDistance(subGeom2.FromPoint);
                    if (distanceBetweenParts > 0.0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// GET NODATA VALUE BASED ON PIXEL TYPE
        /// </summary>
        /// <param name="analysisSurface">object analysis surface</param>
        /// <returns>Object represents the NoData value</returns>
        internal static object GetDefaultNoDataValue(AnalysisSurface analysisSurface)
        {
            object defaultNoDataValue = null;

            // RASTER PROPERTIES            
            IRasterProps rasterProps = (IRasterProps)analysisSurface.Raster;
            object noDataValue = rasterProps.NoDataValue;

            switch (rasterProps.PixelType)
            {
                /*
                case rstPixelType.PT_U1:
                    defaultNoDataValue =.MinValue;
                    break;
                case rstPixelType.PT_U2:
                    defaultNoDataValue = .MinValue;
                    break;
                case rstPixelType.PT_U4:
                    defaultNoDataValue = .MinValue;
                    break;                
                */
                case rstPixelType.PT_CHAR:
                    sbyte[] sbyteValue = (sbyte[])((noDataValue != null) ? (sbyte[])noDataValue : new sbyte[] { sbyte.MinValue });
                    defaultNoDataValue = sbyteValue[0];
                    break;
                case rstPixelType.PT_UCHAR:
                    byte[] byteValue = (byte[])((noDataValue != null) ? (byte[])noDataValue : new byte[] { byte.MinValue });
                    defaultNoDataValue = byteValue[0];
                    break;
                case rstPixelType.PT_SHORT:
                case rstPixelType.PT_CSHORT:
                    short[] shortValue = (short[])((noDataValue != null) ? (short[])noDataValue : new short[] { short.MinValue });
                    defaultNoDataValue = shortValue[0];
                    break;
                case rstPixelType.PT_USHORT:
                    ushort[] ushortValue = (ushort[])((noDataValue != null) ? (ushort[])noDataValue : new ushort[] { ushort.MinValue });
                    defaultNoDataValue = ushortValue[0];
                    break;
                case rstPixelType.PT_LONG:
                case rstPixelType.PT_CLONG:
                    long[] longValue = (long[])((noDataValue != null) ? (long[])noDataValue : new long[] { long.MinValue });
                    defaultNoDataValue = longValue[0];
                    break;
                case rstPixelType.PT_ULONG:
                    uint[] ulongValue = (uint[])((noDataValue != null) ? (uint[])noDataValue : new uint[] { uint.MinValue });
                    defaultNoDataValue = ulongValue[0];
                    break;
                case rstPixelType.PT_FLOAT:
                case rstPixelType.PT_COMPLEX:
                    float[] floatValue = (float[])((noDataValue != null) ? (float[])noDataValue : new float[] { float.MinValue });
                    defaultNoDataValue = floatValue[0];
                    break;
                case rstPixelType.PT_DOUBLE:
                case rstPixelType.PT_DCOMPLEX:
                    double[] doubleValue = (double[])((noDataValue != null) ? (double[])noDataValue : new double[] { double.MinValue });
                    defaultNoDataValue = doubleValue[0];
                    break;
                case rstPixelType.PT_UNKNOWN:
                    break;
                default:
                    break;
            }

            return defaultNoDataValue;
        }

        /// <summary>
        /// GET ELEVATION VALUES OF VERTEX
        /// </summary>
        /// <param name="analysisSurface">Analysis surface</param>
        /// <param name="vertex">object vertex</param>
        /// <returns>elevation of vertex</returns>
        internal static double GetSurfaceElevation(AnalysisSurface analysisSurface, IPoint vertex)
        {
            // GET ELEVATION AT POINT LOCATION
            double elevation = analysisSurface.Surface.GetElevation(vertex);

            // MAKE SURE WE HAVE A VALID ELEVATION VALUE
            if (analysisSurface.Surface.IsVoidZ(elevation))
            {
                throw new SurfaceUtilityException("Portion of the input feature falls outside the surface");
            }

            return elevation;
        }

        /// <summary>
        /// INTERPOLATE ELEVATION VALUES FOR VERTICES
        /// </summary>
        /// <param name="analysisSurface">Analysis surface</param>
        /// <param name="vertices">Enumeration of vertices</param>
        internal static void InterpolateVertices(AnalysisSurface analysisSurface, IEnumVertex vertices)
        {
            // RESET ENUM
            vertices.Reset();

            // ITERATE VERTICES
            IPoint outVertex;
            int partIndex;
            int vertexIndex;
            vertices.Next(out outVertex, out partIndex, out vertexIndex);
            while (outVertex != null)
            {
                // GET ELEVATION AT POINT LOCATION                
                double elev = Helper.GetSurfaceElevation(analysisSurface, outVertex);

                // ASSIGN Z USING ELEVATION
                outVertex.Z = elev;
                
                // GET NEXT VERTEX
                vertices.Next(out outVertex, out partIndex, out vertexIndex);
            }
        }
    }
}
