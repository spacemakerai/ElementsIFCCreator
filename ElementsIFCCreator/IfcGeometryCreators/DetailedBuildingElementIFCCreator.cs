using FormaTypes;
using GeometryGym.Ifc;

namespace ElementsIFCCreator.IfcGeometryCreators
{
    internal class DetailedBuildingElementIFCCreator : IFCCreatorBase
    {
        public SmDetailedBuildingElement DetailedBuildingElement { get; }

        private static IfcSite? ParentSite { get; set; }
        internal DetailedBuildingElementIFCCreator(SmDetailedBuildingElement detailedBuildingElement) : base()
        {
            if (detailedBuildingElement == null)
            {
                LogErrorMethod("DetailedBuildingElement passed in is null");
                throw new ArgumentNullException(nameof(detailedBuildingElement));
            }
            this.DetailedBuildingElement = detailedBuildingElement;

            if (ParentSite == null)
                ParentSite = DbModel.OfType<IfcSite>().First();
        }

        public override bool Create()
        {
            try
            {
                // Create a building
                IfcBuilding building = new IfcBuilding(ParentSite, "Building");

                // Obtain geometry details
                var buildingDetails = this.DetailedBuildingElement.Details;

                if (buildingDetails == null)
                {
                    LogMethod("Building details are null, looking into its Representations");
                    CreateContextualBuildings(building);

                }
                else
                {
                    LogMethod("Building details are not null");
                    CreateDetailedBuildings(buildingDetails, building);
                }



                return true;
            }
            catch (Exception e)
            {
                LogErrorMethod(e.Message);
                return false;
            }
        }

        internal void CreateDetailedBuildings(BuildingDetails buildingDetails, IfcBuilding building)
        {

        }

        internal void CreateContextualBuildings(IfcBuilding building)
        {
            if (this.DetailedBuildingElement.Representations == null || !this.DetailedBuildingElement.Representations.Any())
            {
                LogErrorMethod($"No representations found for the building. Element Urn: {this.DetailedBuildingElement.Urn}");
                return;
            }


            // Get GeoJson information
            var representationDict = this.DetailedBuildingElement.Representations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (representationDict.ContainsKey("volume25DCollection"))
            {
                this.DetailedBuildingElement.GeoJsonFeatures = ProposalTree.GetGeoJsonFeatures(representationDict["volume25DCollection"]);
                Create25DRepresentation(building);
            }
            else if (this.DetailedBuildingElement.VolumeMeshRep != null)
            {
                CreateVolumeMesh(building);
            }
        }

        internal void Create25DRepresentation(IfcBuilding building)
        {
            LogMethod("");
            LogMethod($"Retrieved Volume25D representation for {this.DetailedBuildingElement.Urn}");
            var extrudableFeatures = this.DetailedBuildingElement.GeoJsonFeatures.Where(feature =>
                   feature.Geometry?.Coordinates?.Any() == true &&
                   feature.Properties?.Height > 0.0 == true);

            foreach (var feature in extrudableFeatures)
            {
                List<List<Point3D>> cordinates = feature.Geometry.Coordinates;
                List<Point3D> polygon = cordinates.First();
                var polygonHoles = cordinates.Skip(1);
                double height = feature.Properties.Height;

                building.Representation = new IfcProductDefinitionShape(
                    rep: new IfcShapeRepresentation(
                        extrudedAreaSolid: new IfcExtrudedAreaSolid(
                                                prof: new IfcArbitraryClosedProfileDef("curve",
                                                    new IfcPolyline(pts: polygon.Select(p => new IfcCartesianPoint(DbModel, p.X, p.Y, p.Z)))
                                                ),
                                                dir: new IfcDirection(DbModel, 0, 0, 1),
                                                depth: height
                                            )
                    )
                );
                LogMethod($"Volume25D representation created for urn: {this.DetailedBuildingElement.Urn}");
            }
        }

        internal void CreateVolumeMesh(IfcBuilding building)
        {
            LogMethod($"No volume25D representation exist for this element. Retrieved and creating volume mesh representation for {this.DetailedBuildingElement.Urn}");
            var volumeMesh = this.DetailedBuildingElement.VolumeMeshRep;
            GLTFReader gLTFReader = new GLTFReader();
            gLTFReader.ParseAndCacheGLB(volumeMesh.BlobId, ProposalTree.BinaryData.FirstOrDefault(kvp => kvp.BlobId == volumeMesh.BlobId).Data);

            var objectMeshes = gLTFReader.GetMeshesForNode(volumeMesh.BlobId, volumeMesh.Selection.Value, volumeMesh.SelectionIsExact);

            if (objectMeshes == null || !objectMeshes.Any())
            {
                LogErrorMethod($"No meshes found for the element. Element Urn: {this.DetailedBuildingElement.Urn}");
                return;
            }

            LogMethod($"Meshes obtained {objectMeshes.First().First().Count}");

            var actualMeshes = objectMeshes.First();
            // As IFC takes in a list of points and indices indicating how it is forming the point, we are converting it to that format.
            // Using AsKey() on the point3D as the Dictionary
            Dictionary<string, IndividualPoint3D> pointMapping = new Dictionary<string, IndividualPoint3D>();
            int index = 0;
            foreach (var mesh in actualMeshes)
            {
                foreach (var point in mesh)
                {
                    string key = point.AsKey(); // Key as in key to the dictionary
                    if (!pointMapping.ContainsKey(key))
                    {
                        pointMapping.Add(key, new IndividualPoint3D { Point = point, Key = key, Index = index });
                        index++;
                    }
                }
            }
            // IEnumerable<Tuple<double, double, double>> format. Tuple<double, double, double> would represent each point
            var convertedCordinates = pointMapping.Values
                .OrderBy(ip => ip.Index)
                .Select(ip => new Tuple<double, double, double>(ip.Point.X, ip.Point.Y, ip.Point.Z));

            // Getting indices for meshes
            List<Tuple<int, int, int>> indicesList = new List<Tuple<int, int, int>>();
            foreach (var mesh in actualMeshes)
            {

                var indexArray = mesh.Select(p => pointMapping[p.AsKey()]).Select(ip => ip.Index).ToArray();
                var tuple = new Tuple<int, int, int>(indexArray[0], indexArray[1], indexArray[2]);
                indicesList.Add(tuple);
            }


            IfcCartesianPointList3D pointList = new IfcCartesianPointList3D(DbModel, convertedCordinates);
            IfcTriangulatedFaceSet ifcTriangulatedFaceSet = new IfcTriangulatedFaceSet(pointList, indicesList);
            building.Representation = new IfcProductDefinitionShape(new IfcShapeRepresentation(ifcTriangulatedFaceSet));
            LogMethod($"Volume mesh representation created for urn: {this.DetailedBuildingElement.Urn}");
        }

        private class IndividualPoint3D
        {
            public Point3D Point { get; set; }
            public string Key { get; set; }
            public int Index { get; set; }
        }
    }
}
