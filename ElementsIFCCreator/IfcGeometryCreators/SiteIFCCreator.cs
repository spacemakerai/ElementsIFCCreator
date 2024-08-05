using Autodesk.Forma.Elements;
using FormaTypes;
using GeometryGym.Ifc;

namespace ElementsIFCCreator.IfcGeometryCreators
{
    internal class SiteIFCCreator : IFCCreatorBase
    {

        public SmSiteLimitsElement SiteLimitsElement { get; }
        internal SiteIFCCreator(SmSiteLimitsElement siteLimitsElement) : base()
        {
            if (siteLimitsElement == null)
            {
                SiteLimitsElement = new SmSiteLimitsElement();
            }
            this.SiteLimitsElement = siteLimitsElement;
        }

        public override bool Create()
        {
            try
            {
                // Create the site
                IfcSite site = new IfcSite(IFCCreatorBase.DbModel, SiteLimitsElement.GetProperties().Name ?? "Site Limit");
                site.RefElevation = SiteLimitsElement.GetProperties().Elevation ?? 0;

                return true;


                // Length and width
                // Blob Id?
                if (SiteLimitsElement.Representations.TryGetValue("footprint", out Representation footprint))
                {
                    // Volume mesh or surface mesh?
                    // Parse the binary given the key
                    string blobId = footprint.BlobId;
                    var blobIdAndData = ProposalTree.BinaryData.SingleOrDefault(bd => bd.BlobId == blobId);
                    byte[] data = blobIdAndData.Data;
                    GLTFReader.ParseAndCacheGLB(footprint.BlobId, ProposalTree.BinaryData.Single(kvp => kvp.BlobId == footprint.BlobId).Data);

                    // Get the surface meshes for the site limit.
                    var (Points, Normals, Faces, Error) = GLTFReader.ParseGLBToMesh(footprint.BlobId, false);
                    // LogMethod("Faces");



                    //site.Representation = new IfcProductDefinitionShape(new IfcShapeRepresentation(new IfcTriangulatedFaceSet()));
                }


                // Longitude and latitude? How is 


                return true;
            }
            catch (Exception e)
            {
                LogErrorMethod(e.Message);
                return false;
            }
        }
    }
}
