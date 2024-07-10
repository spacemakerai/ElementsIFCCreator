namespace ElementsIFCCreator.IfcGeometryCreators
{
    /// <summary>
    /// Common interface for creating the elements to IFC.
    /// </summary>
    public interface ICreator
    {
        /// <summary>
        /// Create the geometry associated with the name of the concrete class
        /// </summary>
        void Create();
        /// <summary>
        /// Logging method to use for logging messages
        /// </summary>
        Action<string> LogMethod { get; set; }

        /// <summary>
        /// Logging method for use to log errors
        /// </summary>
        Action<string> LogErrorMethod { get; set; }


    }
}
