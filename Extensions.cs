//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio AT s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.SOESupport;

        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]
        
        /// <summary>
        /// class for extension
        /// </summary>
        internal static class Extensions
        {
            /// <summary>
            /// return the json in byte UTF8 from a object jsonObject
            /// </summary>
            /// <param name="jsonObject">obejct Json Object</param>
            /// <returns>json in byte UTF8</returns>
            internal static byte[] JsonByte(this JsonObject jsonObject)
            {
                string json = jsonObject.ToJson();
                return Encoding.UTF8.GetBytes(json);
            }

            /// <summary>
            /// Convert any Json geometry to its corresponding IGeometry
            /// </summary>
            /// <param name="jsonGeom">The Json geometry</param>
            /// <returns>Converted IGeometry</returns>
            internal static IGeometry ConvertAnyJsonGeom(this JsonObject jsonGeom)
            {
                object[] geomParts;
                if (jsonGeom.TryGetArray("rings", out geomParts))
                {
                    return Conversion.ToGeometry(jsonGeom, esriGeometryType.esriGeometryPolygon);
                }

                if (jsonGeom.TryGetArray("paths", out geomParts))
                {
                    return Conversion.ToGeometry(jsonGeom, esriGeometryType.esriGeometryPolyline);
                }

                if (jsonGeom.TryGetArray("points", out geomParts))
                {
                    return Conversion.ToGeometry(jsonGeom, esriGeometryType.esriGeometryMultipoint);
                }

                double? coordX;
                if (jsonGeom.TryGetAsDouble("x", out coordX))
                {
                    return Conversion.ToGeometry(jsonGeom, esriGeometryType.esriGeometryPoint);
                }

                double? minX;
                if (jsonGeom.TryGetAsDouble("xmin", out minX))
                {
                    return Conversion.ToGeometry(jsonGeom, esriGeometryType.esriGeometryEnvelope);
                }

                return null;
            }
        }
}
