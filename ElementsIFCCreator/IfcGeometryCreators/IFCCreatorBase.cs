using FormaAPI;
using GeometryGym.Ifc;

namespace ElementsIFCCreator.IfcGeometryCreators
{
    public abstract class IFCCreatorBase
    {
        /// <summary>
        /// Proposal Tree to be shared among 
        /// </summary>
        protected static ProposalTree ProposalTree { get; private set; }

        /// <summary>
        /// Method for logging
        /// </summary>
        public static Action<string> LogMethod { get; set; }
        /// <summary>
        /// Method for error reporting
        /// </summary>
        public static Action<string> LogErrorMethod { get; set; }
        /// <summary>
        /// The IFC Database Model shared among all the child classes.
        /// </summary>
        protected static DatabaseIfc DbModel { get; } = new DatabaseIfc(ModelView.Ifc4NotAssigned);
        /// <summary>
        /// GLTF Reader for reading the GLTF files
        /// </summary>
        protected static GLTFReader GLTFReader { get; set; }

        public IFCCreatorBase(ProposalTree proposalTree, Action<string> logMethod, Action<string> logErrorMethod)
        {
            ProposalTree = proposalTree;
            LogMethod = logMethod;
            LogErrorMethod = logErrorMethod;
            GLTFReader = new GLTFReader();
        }

        internal IFCCreatorBase()
        {
        }

        /// <summary>
        /// Create the elements in the IFC file. Specific implementation depends on the class type.
        /// </summary>
        public abstract bool Create();
    }
}
