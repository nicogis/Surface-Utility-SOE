//-----------------------------------------------------------------------
// <copyright file="SurfaceUtilityException.cs" company="Studio A&T s.r.l.">
// Surface Utility Exception
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]

    /// <summary>
    /// class Elevations Soe Exception
    /// </summary>
    [Serializable]
    public class SurfaceUtilityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the SurfaceUtilityException class
        /// </summary>
        public SurfaceUtilityException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SurfaceUtilityException class
        /// </summary>
        /// <param name="message">message error</param>
        public SurfaceUtilityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SurfaceUtilityException class
        /// </summary>
        /// <param name="message">message error</param>
        /// <param name="innerException">object Exception</param>
        public SurfaceUtilityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SurfaceUtilityException class
        /// </summary>
        /// <param name="info">object SerializationInfo</param>
        /// <param name="context">object StreamingContext</param>
        protected SurfaceUtilityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
