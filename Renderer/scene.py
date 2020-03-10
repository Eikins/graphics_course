# ===== Imports =====
from transform import vec

class Scene:

  def __init__(self, camera, light = (vec(0, 0, 1))):
    self.camera = camera
    self.objects3D = []
    self.light = light

  def Add3DObject(self, object3D):
    self.objects3D += [object3D]

class Object3D:

  def __init__(self, id, transform, mesh, material, materialPropertyBlock = None):
    self.id = id
    self.transform = transform
    self.mesh = mesh
    self.material = material
    self.materialPropertyBlock = materialPropertyBlock