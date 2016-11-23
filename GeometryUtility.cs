//-----------------------------------------------------------------------
// <copyright file="GeometryUtility.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio AT s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using ESRI.ArcGIS.Geometry;

    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Warning StyleCop - zAware")]
    
    /// <summary>
    /// class GeometryUtility
    /// </summary>
    internal static class GeometryUtility
    {
        /// <summary>
        /// object missing
        /// </summary>
        private static object missing = Type.Missing;

        /// <summary>
        /// create a ZAware
        /// </summary>
        /// <param name="geometry">object geometry</param>
        public static void MakeZAware(IGeometry geometry)
        {
            IZAware zAware = geometry as IZAware;
            zAware.ZAware = true;
        }

        /// <summary>
        /// create a Vector3D from components
        /// </summary>
        /// <param name="componentX">component X</param>
        /// <param name="componentY">component Y</param>
        /// <param name="componentZ">component Z</param>
        /// <returns>object Vector 3D</returns>
        public static IVector3D ConstructVector3D(double componentX, double componentY, double componentZ)
        {
            IVector3D vector3D = new Vector3DClass();
            vector3D.SetComponents(componentX, componentY, componentZ);

            return vector3D;
        }

        /// <summary>
        /// convert decimal degrees in radians
        /// </summary>
        /// <param name="decimalDegrees">decimal degrees</param>
        /// <returns>number in radians</returns>
        public static double GetRadians(double decimalDegrees)
        {
            return decimalDegrees * (Math.PI / 180);
        }

        /// <summary>
        /// Construct point 3D
        /// </summary>
        /// <param name="x">coordinate X</param>
        /// <param name="y">coordinate Y</param>
        /// <param name="z">coordinate Z</param>
        /// <returns>object Point 3D</returns>
        public static IPoint ConstructPoint3D(double x, double y, double z)
        {
            IPoint point = ConstructPoint2D(x, y);
            point.Z = z;

            MakeZAware(point as IGeometry);

            return point;
        }

        /// <summary>
        /// Construct point 2D
        /// </summary>
        /// <param name="x">coordinate X</param>
        /// <param name="y">coordinate Y</param>
        /// <returns>object Point 2D</returns>
        public static IPoint ConstructPoint2D(double x, double y)
        {
            IPoint point = new PointClass();
            point.X = x;
            point.Y = y;

            return point;
        }

        /// <summary>
        /// Construct multi patch outline
        /// </summary>
        /// <param name="multiPatchGeometry">multi patch geometry</param>
        /// <returns>object implements IGeometryCollection</returns>
        public static IGeometryCollection ConstructMultiPatchOutline(IGeometry multiPatchGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IGeometryCollection multiPatchGeometryCollection = multiPatchGeometry as IGeometryCollection;

            for (int i = 0; i < multiPatchGeometryCollection.GeometryCount; i++)
            {
                IGeometry geometry = multiPatchGeometryCollection.get_Geometry(i);

                switch (geometry.GeometryType)
                {
                    case esriGeometryType.esriGeometryTriangleStrip:
                        outlineGeometryCollection.AddGeometryCollection(ConstructTriangleStripOutline(geometry));
                        break;
                    case esriGeometryType.esriGeometryTriangleFan:
                        outlineGeometryCollection.AddGeometryCollection(ConstructTriangleFanOutline(geometry));
                        break;
                    case esriGeometryType.esriGeometryTriangles:
                        outlineGeometryCollection.AddGeometryCollection(ConstructTrianglesOutline(geometry));
                        break;
                    case esriGeometryType.esriGeometryRing:
                        outlineGeometryCollection.AddGeometry(ConstructRingOutline(geometry), ref missing, ref missing);
                        break;
                    default:
                        throw new Exception("Unhandled Geometry Type. " + geometry.GeometryType);
                }
            }

            return outlineGeometryCollection;
        }

        /// <summary>
        /// Construct triangle strip outline
        /// </summary>
        /// <param name="triangleStripGeometry">object triangle strip geometry</param>
        /// <returns>triangle strip outline</returns>
        public static IGeometryCollection ConstructTriangleStripOutline(IGeometry triangleStripGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection triangleStripPointCollection = triangleStripGeometry as IPointCollection;

            // TriangleStrip: a linked strip of triangles, where every vertex (after the first two) completes a new triangle.
            //                A new triangle is always formed by connecting the new vertex with its two immediate predecessors.
            for (int i = 2; i < triangleStripPointCollection.PointCount; i++)
            {
                IPointCollection outlinePointCollection = new PolylineClass();

                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 2), ref missing, ref missing);
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 1), ref missing, ref missing);
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i), ref missing, ref missing);

                // Simulate: Polygon.Close
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 2), ref missing, ref missing);

                IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                MakeZAware(outlineGeometry);

                outlineGeometryCollection.AddGeometry(outlineGeometry, ref missing, ref missing);
            }

            return outlineGeometryCollection;
        }

        /// <summary>
        /// Construct triangle fan outline
        /// </summary>
        /// <param name="triangleFanGeometry">triangle fan geometry</param>
        /// <returns>object implements IGeometryCollection</returns>
        public static IGeometryCollection ConstructTriangleFanOutline(IGeometry triangleFanGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection triangleFanPointCollection = triangleFanGeometry as IPointCollection;

            // TriangleFan: a linked fan of triangles, where every vertex (after the first two) completes a new triangle. 
            //              A new triangle is always formed by connecting the new vertex with its immediate predecessor 
            //              and the first vertex of the part.
            for (int i = 2; i < triangleFanPointCollection.PointCount; i++)
            {
                IPointCollection outlinePointCollection = new PolylineClass();

                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(0), ref missing, ref missing);
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(i - 1), ref missing, ref missing);
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(i), ref missing, ref missing);

                // Simulate: Polygon.Close
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(0), ref missing, ref missing);

                IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                MakeZAware(outlineGeometry);

                outlineGeometryCollection.AddGeometry(outlineGeometry, ref missing, ref missing);
            }

            return outlineGeometryCollection;
        }

        /// <summary>
        /// Construct triangles outline
        /// </summary>
        /// <param name="trianglesGeometry">triangles geometry</param>
        /// <returns>object implements IGeometryCollection</returns>
        public static IGeometryCollection ConstructTrianglesOutline(IGeometry trianglesGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection trianglesPointCollection = trianglesGeometry as IPointCollection;

            // Triangles: an unlinked set of triangles, where every three vertices completes a new triangle.
            if ((trianglesPointCollection.PointCount % 3) != 0)
            {
                throw new Exception("Triangles Geometry Point Count Must Be Divisible By 3. " + trianglesPointCollection.PointCount);
            }
            else
            {
                for (int i = 0; i < trianglesPointCollection.PointCount; i += 3)
                {
                    IPointCollection outlinePointCollection = new PolylineClass();

                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i), ref missing, ref missing);
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i + 1), ref missing, ref missing);
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i + 2), ref missing, ref missing);

                    // Simulate: Polygon.Close
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i), ref missing, ref missing); 

                    IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                    MakeZAware(outlineGeometry);

                    outlineGeometryCollection.AddGeometry(outlineGeometry, ref missing, ref missing);
                }
            }

            return outlineGeometryCollection;
        }

        /// <summary>
        /// Construct ring outline
        /// </summary>
        /// <param name="ringGeometry">ring Geometry</param>
        /// <returns>object implements Geometry</returns>
        public static IGeometry ConstructRingOutline(IGeometry ringGeometry)
        {
            IGeometry outlineGeometry = new PolylineClass();

            IPointCollection outlinePointCollection = outlineGeometry as IPointCollection;

            IPointCollection ringPointCollection = ringGeometry as IPointCollection;

            for (int i = 0; i < ringPointCollection.PointCount; i++)
            {
                outlinePointCollection.AddPoint(ringPointCollection.get_Point(i), ref missing, ref missing);
            }

            // Simulate: Polygon.Close
            outlinePointCollection.AddPoint(ringPointCollection.get_Point(0), ref missing, ref missing); 

            MakeZAware(outlineGeometry);

            return outlineGeometry;
        }
    }
}
