

class Material:

  def __init__(self, shader):
    self.shader = shader

    def __hash__(self):
        return hash(self.shader)

    def __eq__(self, other):
        return self.shader == other.shader

    def __ne__(self, other):
        return not(self == other)
# =======================================

class MaterialPropertyBlock:

  def __init__(self):
    self.properties = dict()

  def SetProperty(self, id, value):
    self.properties[id] = value
