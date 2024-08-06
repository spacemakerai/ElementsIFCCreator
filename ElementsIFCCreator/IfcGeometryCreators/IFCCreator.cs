using FormaAPI;
using FormaTypes;
using GeometryGym.Ifc;


namespace ElementsIFCCreator.IfcGeometryCreators
{
    /// <summary>
    /// Main entry point for creating IFC files
    /// </summary>
    public class IFCCreator : IFCCreatorBase
    {
        internal HashSet<SmElement> Created { get; set; }
        internal Dictionary<string, SmElement> ElementsMap { get; set; }
        public LevelOfDetailClient LodClient { get; }

        public IFCCreator(ProposalTree proposalTree, Action<string> logMethod, Action<string> logErrorMethod, LevelOfDetailClient lodClient) : base(proposalTree, logMethod, logErrorMethod)
        {
            Created = new HashSet<SmElement>();
            ElementsMap = proposalTree.Elements.ToDictionary(e => e.Urn, e => e);
            LodClient = lodClient;
        }

        /// <summary>
        /// Entry point for creating IFC files. This will write the file to the disk for consumption.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool Create()
        {
            bool isSuccessful = false;
            // Start with the proposalTree
            if (ProposalTree.ProposalElement?.Children == null || !ProposalTree.ProposalElement.Children.Any())
            {
                LogMethod("There is nothing to create from the proposal as it is empty.");
                return true;
            }

            // Create the site first.
            var siteLimitChild = ProposalTree.ProposalElement.Children.FirstOrDefault(c => ElementsMap[c.Urn].GetType().Name == "SmSiteLimitsElement");
            bool siteLimitExist = ElementsMap.TryGetValue(siteLimitChild.Urn, out var siteElement);
            siteElement.GeoJsonFeatures = ProposalTree.GetGeoJsonFeatures(siteElement.TerrainShapeRep ?? siteElement.FootprintRep);
            SiteIFCCreator siteIFCCreator = new SiteIFCCreator(siteElement as SmSiteLimitsElement);
            siteIFCCreator.Create();

            // Create the project
            var site = DbModel.OfType<IfcSite>().First();
            var IfcProject = new IfcProject(site, ProposalTree.ProjectId, IfcUnitAssignment.Length.Metre);

            // Create the rest of the element for the proposal
            foreach (var child in ProposalTree.ProposalElement.Children)
            {
                if (child == siteLimitChild)
                    continue;

                isSuccessful = CreateElement(child.Urn);
                if (!isSuccessful)
                {
                    LogErrorMethod("Failed to create elements");
                    break;
                }
            }
            var currentDirectory = Directory.GetCurrentDirectory();
            LogMethod($"Path: {Path.Combine(currentDirectory, "output.ifc")}");
            DbModel.WriteFile(Path.Combine(currentDirectory, "output.ifc"));

            return isSuccessful;
        }

        internal bool CreateElement(string urn)
        {
            if (!ElementsMap.ContainsKey(urn) || ElementsMap[urn] == null)
            {
                LogErrorMethod($"There is no corresponding element to this urn :{urn}");
                return false;
            }
            SmElement element = ElementsMap[urn];

            // This was already created
            if (this.Created.Contains(element))
            {
                return true;
            }
            bool isSuccessful = false;

            // Create the element based on the element type
            switch (element)
            {
                case SmDetailedBuildingElement:
                    var buildingElement = element as SmDetailedBuildingElement;
                    if (UrnHandling.ParseElementUrn(element.Urn)?.System != "basic" &&
                          (element.Volume25DCollectionRep != null ||
                           element.Children?.Select(child => ProposalTree.GetElementByUrn(child.Urn)).Any(e => e?.Volume25DCollectionRep != null) == true))
                    {
                        buildingElement.Details = this.LodClient.GetDetailedBuilding(ProposalTree.ProjectId, element.Urn);
                    }

                    DetailedBuildingElementIFCCreator detailedBuildingElementIFCCreator = new DetailedBuildingElementIFCCreator(buildingElement);
                    detailedBuildingElementIFCCreator.Create();
                    break;
                case SmRoadElement:
                    var roadElement = element as SmRoadElement;
                    RoadElementIFCCreator roadElementIFCCreator = new RoadElementIFCCreator(roadElement);
                    roadElementIFCCreator.Create();
                    break;
                case SmPropertyBoundaryElement: // Do we even need this?
                    break;
                case SmGroupElement: // Base is the group. Run the debugger and take a look at this.
                    if (element.Children != null && element.Children.Any())
                    {
                        foreach (var child in element.Children)
                        {
                            ElementsMap[child.Urn].GeoJsonFeatures = ProposalTree.GetGeoJsonFeatures(element.Volume25DCollectionRep);

                        }
                    }
                    break;
                case SmAXMElement: // What is SmAXElement? Just treat it as a volume mesh. or possibly brep.
                    element.GeoJsonFeatures = ProposalTree.GetGeoJsonFeatures(element.Volume25DCollectionRep ?? element.VolumeMeshRep);
                    break;
                case SmTerrainElement:
                    break;
                case SmElement:
                    element.GeoJsonFeatures = ProposalTree.GetGeoJsonFeatures(element.Volume25DCollectionRep ?? element.VolumeMeshRep);
                    break;

                default:
                    break;
            }

            // Adding this to the created list as to not create again

            this.Created.Add(element);

            if (element.Children == null || !element.Children.Any())
            {
                return true;
            }

            foreach (var child in element.Children)
            {
                isSuccessful = CreateElement(child.Urn);
            }

            return isSuccessful;
        }
    }
}
