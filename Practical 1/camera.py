from transform import vec, lookat, translate, perspective

class Camera:

    def __init__(self, position, fov, aspect, near, far):
        self.position = position
        self._ProjectionMatrix = perspective(fov, aspect, near, far)
        self._ViewMatrix = lookat(position, vec(0, 0, 0), vec(0, 1, 0))
        self._ProjectionViewMatrix = self._ProjectionMatrix @ self._ViewMatrix

    def SetPosition(self, position):
        offset = position - self.position
        self.position = position
        self._ViewMatrix = translate(-offset) @ self._ViewMatrix
        self._ProjectionViewMatrix = self._ProjectionMatrix @ self._ViewMatrix

    def LookAt(self, target):
        self._ViewMatrix = lookat(self.position, target, vec(0, 1, 0))
        self._ProjectionViewMatrix = self._ProjectionMatrix @ self._ViewMatrix