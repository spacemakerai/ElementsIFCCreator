using FormaTypes;

namespace ElementsIFCCreator.IfcGeometryCreators
{
    internal class RoadElementIFCCreator
    {
        public SmRoadElement RoadElement { get; }
        public RoadElementIFCCreator(SmRoadElement roadElement)
        {
            RoadElement = roadElement;
        }

        public bool Create()
        {
            try
            {
                // Create the road
                // IfcRoad road = new IfcRoad(IFCCreatorBase.DbModel, RoadElement.GetProperties().Name ?? "Road");
                // road.RefElevation = RoadElement.GetProperties().Elevation ?? 0;

                return true;
            }
            catch (System.Exception e)
            {
                // LogErrorMethod(e.Message);
                return false;
            }
        }

    }
}
