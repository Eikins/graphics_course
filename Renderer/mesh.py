import numpy as np
import assimpcy as assimp

class Mesh:

  def __init__(self, 
    vertices = np.array([], 'f'), 
    normals = np.array([], 'f'),
    perVertexColor = np.array([], 'f'),
    indexes = np.array([], 'u4')):

    self.vertices = vertices
    self.normals = normals
    self.perVertexColor = perVertexColor
    self.indexes = indexes

  @staticmethod
  def LoadMeshes(file):
    """ load resources from file using assimp, return a mesh list """
    try:
        pp = assimp.aiPostProcessSteps
        flags = pp.aiProcess_Triangulate
        scene = assimp.aiImportFile(file, flags)
    except assimp.all.AssimpError as exception:
        print('ERROR loading', file + ': ', exception.args[0].decode())
        return []

    meshes = [Mesh(vertices=m.mVertices, normals=m.mNormals, perVertexColor=m.mNormals, indexes=m.mFaces) for m in scene.mMeshes]
    return meshes


