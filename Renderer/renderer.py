#!/usr/bin/env python3
"""
Python OpenGL practical application.
"""

# ===== Imports =====

import os
import ctypes as ct

import OpenGL.GL as GL
import glfw
import numpy as np

from camera import Camera
from material import Material, MaterialPropertyBlock
from scene import Scene, Object3D
from mesh import Mesh
from vertex_array import VertexArray
from transform import vec, rotate, identity, translate

# ===== Classes =====

class Renderer:

  def __init__(self, scene, width=640, height=480):
    self.scene = scene
    self.loadedShaders = dict()
    self.materialPrograms = dict()
    self.win = None
    self.vertexArrays = dict()
    self.InitWindow(width, height)
    self.InitializeMeshes()
    self.InitializeMaterials()


  def InitWindow(self, width, height):
    # version hints: create GL window with >= OpenGL 3.3 and core profile
    glfw.window_hint(glfw.CONTEXT_VERSION_MAJOR, 3)
    glfw.window_hint(glfw.CONTEXT_VERSION_MINOR, 3)
    glfw.window_hint(glfw.OPENGL_FORWARD_COMPAT, GL.GL_TRUE)
    glfw.window_hint(glfw.OPENGL_PROFILE, glfw.OPENGL_CORE_PROFILE)
    glfw.window_hint(glfw.RESIZABLE, False)
    self.win = glfw.create_window(width, height, 'Viewer', None, None)

    # make win's OpenGL context current; no OpenGL calls can happen before
    glfw.make_context_current(self.win)

    # register event handlers
    glfw.set_key_callback(self.win, self.on_key)

    # useful message to check OpenGL renderer characteristics
    print('OpenGL', GL.glGetString(GL.GL_VERSION).decode() + ', GLSL',
          GL.glGetString(GL.GL_SHADING_LANGUAGE_VERSION).decode() +
          ', Renderer', GL.glGetString(GL.GL_RENDERER).decode())

    # initialize GL by setting viewport and default render characteristics
    GL.glClearColor(0.1, 0.1, 0.1, 0.1)
    GL.glEnable(GL.GL_DEPTH_TEST)
    GL.glEnable(GL.GL_CULL_FACE)

  def on_key(self, _win, key, _scancode, action, _mods):
    # Esc to quit
    if action == glfw.PRESS and key == glfw.KEY_ESCAPE:
        glfw.set_window_should_close(self.win, True)

  def InitializeMeshes(self):
    for object3D in self.scene.objects3D:
      mesh = object3D.mesh
      self.vertexArrays[object3D.id] = VertexArray((mesh.vertices, mesh.normals, mesh.perVertexColor), index = mesh.indexes)

  def InitializeMaterials(self):
    vertexShader = self._compile_shader("shaders/position.vert", GL.GL_VERTEX_SHADER)
    if(vertexShader):
      self.loadedShaders["shaders/position.vert"] = vertexShader

    for object3D in self.scene.objects3D:
      if not object3D.material in self.materialPrograms.keys():
        self.InitializeMaterial(object3D.material)
    

    for id in self.loadedShaders.values():
      GL.glDeleteShader(id)

    self.loadedShaders.clear()

  def __del__(self):
    self.vertexArrays.clear()
    GL.glUseProgram(0)
    for program in self.materialPrograms.values():
      GL.glDeleteProgram(program)
    
  def Run(self):
    """ Main render loop for this OpenGL window """
    while not glfw.window_should_close(self.win):
        # clear draw buffer
        GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT)

        # draw our scene objects
        for object3D in self.scene.objects3D:
          shader = self.materialPrograms[object3D.material]
          GL.glUseProgram(shader)


          # Bind camera matrix property, everyone uses Vertex...
          pv_ptr = GL.glGetUniformLocation(shader, "_ProjectionViewMatrix")
          GL.glUniformMatrix4fv(pv_ptr, 1, True, self.scene.camera._ProjectionViewMatrix)
          m_ptr = GL.glGetUniformLocation(shader, "_ModelMatrix")
          GL.glUniformMatrix4fv(m_ptr, 1, True, object3D.transform)

          light_ptr = GL.glGetUniformLocation(shader, "_LightDirection")
          GL.glUniform3fv(light_ptr, 1, self.scene.light)

          camera_ptr = GL.glGetUniformLocation(shader, "_CameraPos")
          GL.glUniform3fv(camera_ptr, 1, self.scene.camera.position)

          

          # if(object3D.materialPropertyBlock):
            # for (id, value) in object3D.materialPropertyBlock:
              # TODO


          vertexArray = self.vertexArrays[object3D.id]
          vertexArray.execute(GL.GL_TRIANGLES)

        GL.glBindVertexArray(0)
        # flush render commands, and swap draw buffers
        glfw.swap_buffers(self.win)

        # Poll for and process events
        glfw.poll_events()

  def InitializeMaterial(self, material):
    # First, check if shader is already loaded
    
    if (not material.shader in self.loadedShaders):
      frag = self._compile_shader(material.shader, GL.GL_FRAGMENT_SHADER)
      if(frag):
        self.loadedShaders[material.shader] = frag
    else:
      frag = self.loadedShaders[material.shader]
    
    glid = GL.glCreateProgram() # pylint: disable=E1111
    vert = self.loadedShaders["shaders/position.vert"]

    GL.glAttachShader(glid, vert)
    GL.glAttachShader(glid, frag)
    GL.glLinkProgram(glid)

    status = GL.glGetProgramiv(glid, GL.GL_LINK_STATUS)
    if not status:
      print(GL.glGetProgramInfoLog(glid).decode('ascii'))
      GL.glDeleteProgram(glid)
    else:
      self.materialPrograms[material] = glid

  @staticmethod
  def _compile_shader(src, shader_type):
    src = open(src, 'r').read() if os.path.exists(src) else src
    src = src.decode('ascii') if isinstance(src, bytes) else src
    shader = GL.glCreateShader(shader_type) # pylint: disable=E1111
    GL.glShaderSource(shader, src)
    GL.glCompileShader(shader)
    status = GL.glGetShaderiv(shader, GL.GL_COMPILE_STATUS)
    src = ('%3d: %s' % (i+1, l) for i, l in enumerate(src.splitlines()))
    if not status:
        log = GL.glGetShaderInfoLog(shader).decode('ascii')
        GL.glDeleteShader(shader)
        src = '\n'.join(src)
        print('Compile failed for %s\n%s\n%s' % (shader_type, log, src))
        return None
    return shader






# ===================

def main():
    """ create window, add shaders & scene objects, then run rendering loop """
    width = 640
    height = 480

    camera = Camera(vec(0, 0, -5), 60, height / width, .3, 1000)
    scene = Scene(camera, light=vec(-.57735026919, -.57735026919, .57735026919) * .5)
    
    pyramidMesh = Mesh(
          vertices = np.array(((0, .5, 0), (.5, -.5, 0), (-.5, -.5, 0)), 'f'),
          normals = np.array(((0, 0, -1), (0.70710678118, 0, -0.70710678118), (-0.70710678118, 0, -0.70710678118)), 'f'),
          perVertexColor = np.array(((1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, 0.0, 1.0)), 'f'),
          indexes = np.array((0, 1, 2), 'u4'))

    suzanne = Mesh.LoadMeshes("models/suzanne.obj")[0]

    scene.Add3DObject(
      Object3D(0,
        translate(-1.5, 0, 1)  @ rotate(vec(0.0, 1.0, 0.0), -200), 
        suzanne,
      Material("shaders/rainbow_shaded.frag")))

    scene.Add3DObject(
      Object3D(1,
        translate(1.5, 0, 1) @ rotate(vec(0.0, 1.0, 0.0), 200), 
        suzanne,
      Material("shaders/shaded.frag")))

    renderer = Renderer(scene)
    renderer.Run()


if __name__ == '__main__':
    glfw.init()
    main()
    glfw.terminate()