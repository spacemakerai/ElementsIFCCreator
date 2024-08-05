using System.Collections.Concurrent;
using System.Numerics;

namespace ElementsIFCCreator
{
    public class GLTFReader
    {
        // An in-memory cache for glb requests for this operation. It's likely
        // that children and elements from the same batch will reuse the same
        // glb data with different nodeIds. The web request infrastructure will
        // cache the server response locally on disk, but we'd prefer not to have
        // to hit the disk and/or parse the GLB multiple times for large/complex
        // models if we don't have to.
        private readonly ConcurrentDictionary<string, SharpGLTF.Schema2.ModelRoot> glbModelRootCache
          = new ConcurrentDictionary<string, SharpGLTF.Schema2.ModelRoot>();

        public bool ParseAndCacheGLB(string key, byte[] bytes)
        {
            var settings = new SharpGLTF.Schema2.ReadSettings()
            {
                Validation = SharpGLTF.Validation.ValidationMode.TryFix
            };

            var model = SharpGLTF.Schema2.ModelRoot.ParseGLB(new ArraySegment<byte>(bytes), settings);

            if (model == null)
            {
                return false;
            }

            glbModelRootCache[key] = model;

            return true;
        }

        public FormaTypes.BoundingBox3D GetBoundingBoxForNodes(string meshKey, string filterNodeName, bool filterNodeNameExact)
        {
            if (!glbModelRootCache.TryGetValue(meshKey, out var modelRoot))
            {
                return null;
            }

            var bbox = new FormaTypes.BoundingBox3D();

            // Find the root(s) of the hierarchy that is being imported.
            if (!string.IsNullOrEmpty(filterNodeName))
            {
                var node = modelRoot.LogicalNodes.FirstOrDefault(n => filterNodeNameExact ? n.Name == filterNodeName : n.Name.StartsWith(filterNodeName));

                if (node != null)
                {
                    UpdateBoxForElement(bbox, node);
                }
            }
            else if (modelRoot.LogicalScenes.FirstOrDefault() is SharpGLTF.Schema2.Scene scene)
            {
                foreach (var node in scene.VisualChildren)
                {
                    UpdateBoxForElement(bbox, node);
                }
            }

            return bbox;
        }

        private void UpdateBoxForElement(FormaTypes.BoundingBox3D bbox, SharpGLTF.Schema2.Node node)
        {
            foreach (var child in node.VisualChildren)
            {
                UpdateBoxForElement(bbox, child);
            }

            if (node.Mesh == null)
            {
                return;
            }

            foreach (var primitive in node.Mesh.Primitives)
            {
                var accessor = primitive.GetVertices("POSITION");

                if (accessor?.Attribute.Encoding == SharpGLTF.Schema2.EncodingType.FLOAT
                    && accessor.Attribute.Dimensions == SharpGLTF.Schema2.DimensionType.VEC3)
                {
                    // Apply node transform and convert to Y up.
                    bbox.Add(accessor.AsVector3Array().
                      Select(pt => Vector3.Transform(pt, node.WorldMatrix)).
                      Select(pt => new FormaTypes.Point3D(pt.X, -pt.Z, pt.Y)), null);
                }
            }
        }

        public List<List<List<FormaTypes.Point3D>>> GetMeshesForNode(string meshKey, string filterNodeName, bool filterNodeNameExact)
        {
            if (!glbModelRootCache.TryGetValue(meshKey, out var modelRoot))
            {
                return null;
            }

            var objectMeshes = new List<List<List<FormaTypes.Point3D>>>();

            // Find the root(s) of the hierarchy that is being imported.
            if (!string.IsNullOrEmpty(filterNodeName))
            {
                foreach (var node in modelRoot.LogicalNodes.Where(n => filterNodeNameExact ? n.Name == filterNodeName : n.Name.StartsWith(filterNodeName)))
                {
                    CreateTrianglesFromNodes(objectMeshes, node);
                }
            }
            else if (modelRoot.LogicalScenes.FirstOrDefault() is SharpGLTF.Schema2.Scene scene)
            {
                foreach (var node in scene.VisualChildren)
                {
                    CreateTrianglesFromNodes(objectMeshes, node);
                }
            }

            return objectMeshes;
        }

        public Dictionary<string, List<List<List<FormaTypes.Point3D>>>> GetMeshesForNodes(string meshKey, HashSet<string> filterNodeName)
        {
            if (!glbModelRootCache.TryGetValue(meshKey, out var modelRoot))
            {
                return null;
            }

            var objectMeshes = new Dictionary<string, List<List<List<FormaTypes.Point3D>>>>();

            foreach (var node in modelRoot.LogicalNodes.Where(n => filterNodeName.Contains(n.Name)))
            {
                if (!objectMeshes.TryGetValue(node.Name, out var meshes))
                {
                    meshes = new List<List<List<FormaTypes.Point3D>>>();
                    objectMeshes[node.Name] = meshes;
                }

                CreateTrianglesFromNodes(meshes, node);
            }

            return objectMeshes;
        }

        private static void CreateTrianglesFromNodes(List<List<List<FormaTypes.Point3D>>> objectMeshes,
          SharpGLTF.Schema2.Node node)
        {
            foreach (var child in node.VisualChildren)
            {
                CreateTrianglesFromNodes(objectMeshes, child);
            }

            if (node.Mesh == null)
            {
                return;
            }

            foreach (var primitive in node.Mesh.Primitives)
            {
                var positionAccessor = primitive.GetVertices("POSITION");

                if (positionAccessor?.Attribute.Encoding != SharpGLTF.Schema2.EncodingType.FLOAT ||
                  positionAccessor.Attribute.Dimensions != SharpGLTF.Schema2.DimensionType.VEC3)
                {
                    continue;
                }

                var vertices = positionAccessor.AsVector3Array();

                // FUTURE TODO: apply any materials from the GLB data

                var triangles = new List<List<FormaTypes.Point3D>>();
                objectMeshes.Add(triangles);
                foreach (var tri in primitive.GetTriangleIndices())
                {
                    triangles.Add(new List<FormaTypes.Point3D>
          {
            GLTFVectorToPoint3D(node.WorldMatrix, vertices[tri.A]),
            GLTFVectorToPoint3D(node.WorldMatrix, vertices[tri.B]),
            GLTFVectorToPoint3D(node.WorldMatrix, vertices[tri.C])
          });
                }
            }
        }

        private static FormaTypes.Point3D GLTFVectorToPoint3D(Matrix4x4 gltfTransform, Vector3 vector)
        {
            var transformedVector = Vector3.Transform(vector, gltfTransform);

            // Convert from Y-Up to Z-Up
            return new FormaTypes.Point3D(transformedVector.X, -transformedVector.Z, transformedVector.Y);
        }

        public (List<FormaTypes.Point3D> Points, List<FormaTypes.Point3D> Normals, List<uint> Faces, string Error)
          ParseGLBToMesh(string meshKey, bool wantNormals)
        {
            if (!glbModelRootCache.TryGetValue(meshKey, out var model))
            {
                return (null, null, null, "Unable to parse terrain from glb bytes.");
            }

            // This assumes one mesh and one primitive, which is what there should be for terrain.
            // If there can be more than one, this will need updating.
            var prim = model.LogicalMeshes?.FirstOrDefault()?.Primitives?.FirstOrDefault();

            if (prim == null)
            {
                return (null, null, null, "Unable to get primitive for terrain.");
            }

            if (prim.IndexAccessor == null)
            {
                return (null, null, null, "Unable to get index accessor for terrain.");
            }

            var acc = prim.GetVertexAccessor("POSITION");

            if (acc?.Dimensions != SharpGLTF.Schema2.DimensionType.VEC3)
            {
                return (null, null, null, "Unable to get VEC3 POSITION vertex accessor.");
            }

            var pointsArray = acc.AsVector3Array();

            if (pointsArray == null)
            {
                return (null, null, null, "Unable to get points as Vector3Array.");
            }

            var facesArray = prim.IndexAccessor.AsIndicesArray();

            if (pointsArray.Count < 3 || facesArray.Count < 3)
            {
                return (null, null, null, "Not enough points or face indices found to create terrain.");
            }

            return (pointsArray.Select(MakePoint).ToList(),
              wantNormals ? GetNormals(prim, acc.Count) : null,
              facesArray.ToList(), null);
        }

        private static List<FormaTypes.Point3D> GetNormals(SharpGLTF.Schema2.MeshPrimitive prim, int count)
        {
            var acc = prim.GetVertexAccessor("NORMAL");

            if (acc?.Dimensions == SharpGLTF.Schema2.DimensionType.VEC3 && acc.Count == count)
            {
                return acc.AsVector3Array()?.Select(MakePoint).ToList();
            }

            return null;
        }

        private static FormaTypes.Point3D MakePoint(Vector3 pt) => new FormaTypes.Point3D(pt.X, -pt.Z, pt.Y);

        public bool MeshIsCached(string meshKey)
        {
            return meshKey != null && glbModelRootCache.ContainsKey(meshKey);
        }
    }
}
