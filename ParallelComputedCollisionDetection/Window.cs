﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using OpenTK.Platform;
using System.Diagnostics;

#region Assembly Collisions
using TK = OpenTK.Graphics.OpenGL;
using GL = OpenTK.Graphics.OpenGL.GL;
#endregion

namespace ParallelComputedCollisionDetection
{
    public class Window : GameWindow
    {
        #region Global Members
        MouseState old_mouse;
        float offsetX = 0f, offsetY = 0f;
        Vector3 eye, target, up;
        float mouse_sensitivity=0.2f;
        Matrix4 modelView;
        float scale_factor = 1;
        float rotation_speed = 2.5f;
        int fov;
        //public int sphere_precision = 20;
        float coord_transf;
        float wp_scale_factor = 3;
        double grid_edge = 3;
        int number_of_bodies = 5;
        int tiles;
        KeyboardState old_key;
        bool xRot;
        bool yRot;
        bool zRot;
        bool ortho = true;
        int picked = -1;
        MouseState mouse;
        float[] mat_specular = { 1.0f, 1.0f, 1.0f, 1.0f };
        float[] mat_shininess = { 50.0f };
        float[] light_position = { 1f, 5f, 0.1f, 0.0f };
        float[] light_ambient = { 0.5f, 0.5f, 0.5f, 1.0f };
        float oldX, oldY; 
        float[][] colors = new float[][]{   new float[]{1f, 0f, 0f, 0.0f}, new float[]{0f, 1f, 0f, 0.0f}, new float[]{0f, 0f, 1f, 0.0f},
                                            new float[]{1f, 1f, 1f, 0.0f}, new float[]{1f, 1f, 0f, 0.0f}, new float[]{1f, 0f, 1f, 0.0f},
                                            new float[]{1f, 1f, 0f, 0.0f}, new float[]{0.4f, 0.6f, 0.4f, 0.0f}, new float[]{0f, 1f, 1f, 0.0f},
                                            new float[]{1f, 0f, 1f, 0.0f}, new float[]{0f, 1f, 1f, 0.0f}, new float[]{0.4f, 0.4f, 0.6f, 0.0f}};
        float width;
        float height;
        char view = '0';
        double gizmosOffsetX = 10.5;
        double gizmosOffsetY = 10.5;
        double gizmosOffsetZ = 10.5;

        List<Body> bodies;

        float aspect_ratio;
        Matrix4 perspective;
        #endregion

        public Window()
            : base(1366, 768, new GraphicsMode(32, 0, 0, 4), "Parallel Computed Collision Detection")
        {
            WindowState = WindowState.Fullscreen;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

            GL.Material(TK.MaterialFace.Front, TK.MaterialParameter.Specular, mat_specular);
            GL.Material(TK.MaterialFace.Front, TK.MaterialParameter.Shininess, mat_shininess);
            GL.Light(TK.LightName.Light0, TK.LightParameter.Position, light_position);
            GL.Light(TK.LightName.Light0, TK.LightParameter.Ambient, light_ambient);
            GL.Light(TK.LightName.Light0, TK.LightParameter.Diffuse, mat_specular);

            GL.Enable(TK.EnableCap.CullFace);
            GL.Enable(TK.EnableCap.DepthTest);
            GL.ShadeModel(TK.ShadingModel.Smooth);
            GL.Enable(TK.EnableCap.ColorMaterial);
            GL.Enable(TK.EnableCap.Blend);
            GL.BlendFunc(TK.BlendingFactorSrc.SrcAlpha, TK.BlendingFactorDest.OneMinusSrcAlpha);
            GL.PolygonMode(TK.MaterialFace.FrontAndBack, TK.PolygonMode.Fill);
            
            GL.Viewport(0, 0, Width, Height);
            aspect_ratio = Width / (float)Height;
            checkAspectRatio();
            width *= wp_scale_factor;
            height *= wp_scale_factor;

            VSync = VSyncMode.On;
            eye = new Vector3(0, 0, height * 1.5f);
            target = new Vector3(0, 0, 0);
            up = new Vector3(0, 1, 0);
            fov = (int)Math.Round(height * 0.75f);

            old_mouse = OpenTK.Input.Mouse.GetState();
            old_key = OpenTK.Input.Keyboard.GetState();

            bodies = new List<Body>();
            /*bodies.Add(new Parallelepiped(new Vector3(5, 5, 5), 2));
            bodies.Add(new Parallelepiped(new Vector3(-5, -5, -5), 3.5));
            bodies.Add(new Parallelepiped(new Vector3(-3, 3, 0), 3));
            bodies.Add(new Parallelepiped(new Vector3(6, -4, 3), 2));
            bodies.Add(new Parallelepiped(new Vector3(-7, 7, 4), 1.5));*/
            generateRandomBodies(number_of_bodies, true);
            calculateGridEdge();

            mouse = OpenTK.Input.Mouse.GetState();
            coord_transf = Screen.PrimaryScreen.Bounds.Height / 30f;
        }

        protected override void OnUnload(EventArgs e)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            aspect_ratio = Width / (float)Height;
            perspective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(TK.MatrixMode.Projection);
            GL.LoadMatrix(ref perspective);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[OpenTK.Input.Key.Escape])
                this.Exit();

            if (Keyboard[OpenTK.Input.Key.Space])
                if (WindowState != WindowState.Fullscreen)
                    WindowState = WindowState.Fullscreen;
                else
                    WindowState = WindowState.Maximized;
            checkMouseInput();
            checkKeyboardInput();
            foreach (Body body in bodies)
                body.calculateBoundingSphere();
        }

        void checkMouseInput()
        {
            mouse = OpenTK.Input.Mouse.GetState();

            if (mouse.IsButtonDown(MouseButton.Left) && picked < 0)
                pickBody();
            else if (!mouse.IsButtonDown(MouseButton.Left))
                picked = -1;
            else if (mouse.IsButtonDown(MouseButton.Left) && old_mouse.IsButtonDown(MouseButton.Left) && picked >= 0)
            {
                float deltaX = Cursor.Position.X - oldX;
                float deltaY = Cursor.Position.Y - oldY;
                moveBody(deltaX, deltaY);
            }

            if (mouse.IsButtonDown(MouseButton.Right))
            {
                CursorVisible = false;
                offsetX += (mouse.X - old_mouse.X) * mouse_sensitivity;
                offsetY += (mouse.Y - old_mouse.Y) * mouse_sensitivity;
            }
            else
                CursorVisible = true;
            
            if(ortho){
                scale_factor += (mouse.WheelPrecise - old_mouse.WheelPrecise) * 0.5f;
                if(scale_factor > 50f)
                    scale_factor = 50f;
                else if(scale_factor < 0.5f)
                    scale_factor = 0.5f;
            }
            else{
                eye.Z -= (mouse.WheelPrecise - old_mouse.WheelPrecise);
                if (eye.Z < 3)
                    eye.Z = 3;
            }
            old_mouse = mouse;
            oldX = Cursor.Position.X;
            oldY = Cursor.Position.Y;
        }

        void checkKeyboardInput()
        {
            if (Keyboard[Key.R] && !old_key.IsKeyDown(Key.R))
                generateRandomBodies(number_of_bodies, true);
            if (Keyboard[Key.Left])
                offsetX -= rotation_speed;
            if (Keyboard[Key.Right])
                offsetX += rotation_speed;
            if (Keyboard[Key.Up])
                offsetY += rotation_speed;
            if (Keyboard[Key.Down])
                offsetY -= rotation_speed;

            if (Keyboard[Key.V] && !old_key.IsKeyDown(Key.V))
            {
                if (ortho)
                    ortho = false;
                else
                    ortho = true;
            }

            #region XYZ Grid Rotations
            if (Keyboard[Key.Z] || zRot)
            {
                zRot = true;
                if (offsetX > 0f)
                    offsetX -= rotation_speed;
                else if (offsetX < 0f)
                    offsetX += rotation_speed;
                if (offsetY > 0f)
                    offsetY -= rotation_speed;
                else if (offsetY < 0f)
                    offsetY += rotation_speed;
                if (offsetX  > -2f && offsetX < 2f)
                    offsetX = 0f;
                if (offsetY > -2f && offsetY < 2f)
                    offsetY = 0f;
            }

            if (Keyboard[Key.Y] || yRot)
            {
                yRot = true;
                if (offsetX > 0f)
                    offsetX -= rotation_speed;
                else if (offsetX < 0f)
                    offsetX += rotation_speed;
                if (offsetY > 90f)
                    offsetY -= rotation_speed;
                else if (offsetY < 90f)
                    offsetY += rotation_speed;
                if (offsetX > -2f && offsetX < 2f)
                    offsetX = 0f;
                if (offsetY > 88f && offsetY < 92f)
                    offsetY = 90f;
            }

            if (Keyboard[Key.X] || xRot)
            {
                xRot = true;
                if (offsetX > -90f)
                    offsetX -= rotation_speed;
                else if (offsetX < -90f)
                    offsetX += rotation_speed;
                if (offsetY > 0f)
                    offsetY -= rotation_speed;
                else if (offsetY < 0f)
                    offsetY += rotation_speed;
                if (offsetX < -88f && offsetX > -92f)
                    offsetX = -90f;
                if (offsetY > -2f && offsetY < 2f)
                    offsetY = 0f;
            }

            if (offsetX == -90f && offsetY == 0f)
            {
                xRot = false;
                view = 'x';
            }

            else if (offsetX == 0f && offsetY == 90f)
            {
                yRot = false;
                view = 'y';
            }

            else if (offsetX == 0f && offsetY == 0f)
            {
                zRot = false;
                view = 'z';
            }

            else
                view = '0';
            #endregion

            old_key = OpenTK.Input.Keyboard.GetState();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(TK.ClearBufferMask.DepthBufferBit | TK.ClearBufferMask.ColorBufferBit);

            if (ortho)
            {
                modelView = Matrix4.CreateOrthographic(width, height, -height, height);
                GL.MatrixMode(TK.MatrixMode.Projection);
                GL.PushMatrix();
                GL.LoadMatrix(ref modelView);
                GL.Scale(scale_factor, scale_factor, scale_factor);
            }
            else
            {
                modelView = Matrix4.LookAt(eye, target, up);
                GL.MatrixMode(TK.MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadMatrix(ref modelView);
            }
            GL.PushMatrix();
            GL.Rotate(offsetX, 0.0f, 1.0f, 0.0f);
            GL.Rotate(offsetY, 1.0f, 0.0f, 0.0f);

            DrawGrid();

            #region Draw 3D Polyhedrons
            
            GL.Color3(0.0, 0.5, 1.0);

            GL.Enable(TK.EnableCap.Light0);
            GL.Enable(TK.EnableCap.Lighting);

            int i = 0;
            foreach (Body body in bodies)
            {
                float[] color = colors[i % colors.Count()];
                color[3] = 1f;
                GL.Color4(color);
                //GL.PolygonMode(TK.MaterialFace.FrontAndBack, TK.PolygonMode.Fill);
                body.Draw();
                color[3] = 0.25f;
                GL.Color4(color);
                //GL.PolygonMode(TK.MaterialFace.FrontAndBack, TK.PolygonMode.Line);
                body.getBSphere().Draw();
                i++;
            }

            GL.Disable(TK.EnableCap.Lighting);
            GL.Disable(TK.EnableCap.Light0);
            #endregion

            #region Draw Gizmos
            drawCollisionIntervals();
            #endregion

            GL.PopMatrix();
            GL.PopMatrix();

            SwapBuffers();
        }

        void DrawGrid3x3()
        {
            GL.Begin(TK.PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 3.0, 3.0);
                GL.Vertex3(3.0, 3.0, 3.0);
                GL.Vertex3(3.0, -3.0, 3.0);
                GL.Vertex3(-3.0, -3.0, 3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 3.0, 1.0);
                GL.Vertex3(3.0, 3.0, 1.0);
                GL.Vertex3(3.0, -3.0, 1.0);
                GL.Vertex3(-3.0, -3.0, 1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 3.0, -1.0);
                GL.Vertex3(3.0, 3.0, -1.0);
                GL.Vertex3(3.0, -3.0, -1.0);
                GL.Vertex3(-3.0, -3.0, -1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 3.0, -3.0);
                GL.Vertex3(3.0, 3.0, -3.0);
                GL.Vertex3(3.0, -3.0, -3.0);
                GL.Vertex3(-3.0, -3.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 3.0, 3.0);
                GL.Vertex3(-3.0, 3.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(3.0, 3.0, 3.0);
                GL.Vertex3(3.0, 3.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(3.0, -3.0, 3.0);
                GL.Vertex3(3.0, -3.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, -3.0, 3.0);
                GL.Vertex3(-3.0, -3.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-1.0, 3.0, 3.0);
                GL.Vertex3(-1.0, 3.0, -3.0);
                GL.Vertex3(-1.0, -3.0, -3.0);
                GL.Vertex3(-1.0, -3.0, 3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(1.0, 3.0, 3.0);
                GL.Vertex3(1.0, 3.0, -3.0);
                GL.Vertex3(1.0, -3.0, -3.0);
                GL.Vertex3(1.0, -3.0, 3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(3.0, 1.0, 3.0);
                GL.Vertex3(3.0, 1.0, -3.0);
                GL.Vertex3(-3.0, 1.0, -3.0);
                GL.Vertex3(-3.0, 1.0, 3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(3.0, -1.0, 3.0);
                GL.Vertex3(3.0, -1.0, -3.0);
                GL.Vertex3(-3.0, -1.0, -3.0);
                GL.Vertex3(-3.0, -1.0, 3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-1.0, 1.0, 3.0);
                GL.Vertex3(-1.0, 1.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(1.0, 1.0, 3.0);
                GL.Vertex3(1.0, 1.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-1.0, -1.0, 3.0);
                GL.Vertex3(-1.0, -1.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(1.0, -1.0, 3.0);
                GL.Vertex3(1.0, -1.0, -3.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 1.0, 1.0);
                GL.Vertex3(3.0, 1.0, 1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, 1.0, -1.0);
                GL.Vertex3(3.0, 1.0, -1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, -1.0, 1.0);
                GL.Vertex3(3.0, -1.0, 1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-3.0, -1.0, -1.0);
                GL.Vertex3(3.0, -1.0, -1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-1.0, 3.0, 1.0);
                GL.Vertex3(-1.0, -3.0, 1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(1.0, 3.0, 1.0);
                GL.Vertex3(1.0, -3.0, 1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(-1.0, 3.0, -1.0);
                GL.Vertex3(-1.0, -3.0, -1.0);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(1.0, 1.0, 1.0);

                GL.Vertex3(1.0, 3.0, -1.0);
                GL.Vertex3(1.0, -3.0, -1.0);
            }
            GL.End();
        }

        void DrawGrid()
        {
            int half_fov = fov / 2;
            float offset;

            GL.Color4(1f, 0f, 0f, 1f);

            //grid y
            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Vertex3(-half_fov, -half_fov, half_fov);
                GL.Vertex3(half_fov, -half_fov, half_fov);
                GL.Vertex3(half_fov, -half_fov, -half_fov);
                GL.Vertex3(-half_fov, -half_fov, -half_fov);
            }
            GL.End();

            for (int i = 1; i < tiles; i++)
            {
                offset = (float)grid_edge * i;

                GL.Begin(PrimitiveType.Lines);
                {
                    GL.Vertex3(offset - half_fov, -half_fov, half_fov);
                    GL.Vertex3(offset - half_fov, -half_fov, -half_fov);

                    GL.Vertex3(-half_fov, -half_fov, offset - half_fov);
                    GL.Vertex3(half_fov, -half_fov, offset - half_fov);
                }
                GL.End();
            }

            GL.Color4(0f, 1f, 0f, 1f);

            //grid x
            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Vertex3(-half_fov, half_fov, half_fov);
                GL.Vertex3(-half_fov, half_fov, -half_fov);
                GL.Vertex3(-half_fov, -half_fov, -half_fov);
                GL.Vertex3(-half_fov, -half_fov, half_fov);
            }
            GL.End();

            for (int i = 1; i < tiles; i++)
            {
                offset = (float)grid_edge * i;

                GL.Begin(PrimitiveType.Lines);
                {
                    GL.Vertex3(-half_fov, -half_fov, offset - half_fov);
                    GL.Vertex3(-half_fov, half_fov, offset - half_fov);

                    GL.Vertex3(-half_fov, offset - half_fov, half_fov);
                    GL.Vertex3(-half_fov, offset - half_fov, -half_fov);
                }
                GL.End();
            }

            GL.Color4(0f, 0f, 1f, 1f);

            //grid z
            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Vertex3(-half_fov, -half_fov, -half_fov);
                GL.Vertex3(half_fov, -half_fov, -half_fov);
                GL.Vertex3(half_fov, half_fov, -half_fov);
                GL.Vertex3(-half_fov, half_fov, -half_fov);
            }
            GL.End();

            for (int i = 1; i < tiles; i++)
            {
                offset = (float)grid_edge * i;

                GL.Begin(PrimitiveType.Lines);
                {
                    GL.Vertex3(offset - half_fov, -half_fov, -half_fov);
                    GL.Vertex3(offset - half_fov, half_fov, -half_fov);

                    GL.Vertex3(-half_fov, offset - half_fov, -half_fov);
                    GL.Vertex3(half_fov, offset - half_fov, -half_fov);
                }
                GL.End();
            }

        }

        void checkAspectRatio()
        {
            if (aspect_ratio == 4 / 3f)
            {
                width = 4f;
                height = 3f;
                return;
            }
            if (aspect_ratio == 16 / 10f)
            {
                width = 16f;
                height = 10f;
                return;
            }
            else
            {
                width = 16f;
                height = 9f;
            }
        }

        void drawCollisionIntervals()
        {
            if (view == '0')
                return;

            GL.LineWidth(4.0f);
            int i = 0;
            float j = 0;

            foreach (Body body in bodies)
            {
                colors[i][3] = 0.5f;
                GL.Color4(colors[i]);
                Vector3 pos = body.getPos();
                Sphere bsphere = body.getBSphere();

                if (view == 'z')
                {
                    GL.Begin(PrimitiveType.Lines);
                    {
                        GL.Vertex3(pos.X - bsphere.radius, -(gizmosOffsetY + j), 10);
                        GL.Vertex3(pos.X + bsphere.radius, -(gizmosOffsetY + j), 10);

                        GL.Vertex3(-(gizmosOffsetX + j), pos.Y + bsphere.radius, 10);
                        GL.Vertex3(-(gizmosOffsetX + j), pos.Y - bsphere.radius, 10);
                    }
                    GL.End();
                }

                else if (view == 'x')
                {
                    GL.Begin(PrimitiveType.Lines);
                    {
                        GL.Vertex3(10, -(gizmosOffsetY + j), pos.Z - bsphere.radius);
                        GL.Vertex3(10, -(gizmosOffsetY + j), pos.Z + bsphere.radius);

                        GL.Vertex3(10, pos.Y + bsphere.radius, gizmosOffsetZ + j);
                        GL.Vertex3(10, pos.Y - bsphere.radius, gizmosOffsetZ + j);
                    }
                    GL.End();
                }

                else
                {
                    GL.Begin(PrimitiveType.Lines);
                    {
                        GL.Vertex3(-(gizmosOffsetX + j), 10, pos.Z + bsphere.radius);
                        GL.Vertex3(-(gizmosOffsetX + j), 10, pos.Z - bsphere.radius);

                        GL.Vertex3(pos.X - bsphere.radius, 10, gizmosOffsetZ + j);
                        GL.Vertex3(pos.X + bsphere.radius, 10, gizmosOffsetZ + j);
                    }
                    GL.End();
                }
                i++;
                j += 0.15f;
            }
            GL.LineWidth(1.0f);
        }

        void moveBody(float deltaX, float deltaY)
        {
            Body[] bodies_ = bodies.ToArray();
            Vector3 pos = bodies_[picked].getPos();
            switch (view)
            {
                case 'x':
                    pos.Z -= deltaX / coord_transf;
                    pos.Y -= deltaY / coord_transf;
                    break;
                case 'y':
                    pos.X += deltaX / coord_transf;
                    pos.Z += deltaY / coord_transf;
                    break;
                case 'z':
                    pos.X += deltaX / coord_transf;
                    pos.Y -= deltaY / coord_transf;
                    break;
                default:
                    break;
            }
            bodies_[picked].setPos(pos);
        }

        void pickBody()
        {
            float depthTest = -300;
            Body[] bodies_ = bodies.ToArray();
            switch (view)
            {
                case 'x':
                    for (int i = 0; i < bodies_.Count(); i++)
                    {
                        Sphere bsphere = bodies_[i].getBSphere();
                        Vector3 pos = bodies_[i].getPos();
                        if (Math.Abs(((Cursor.Position.X - Screen.PrimaryScreen.Bounds.Width * 0.5) / coord_transf)
                            + pos.Z) < bsphere.radius
                            && Math.Abs(((-(Cursor.Position.Y - Screen.PrimaryScreen.Bounds.Height * 0.5)) / coord_transf)
                            - pos.Y) < bsphere.radius
                            && pos.X > depthTest)
                        {
                            depthTest = pos.X;
                            picked = i;
                        }
                    }
                    break;

                case 'y':
                    for (int i = 0; i < bodies_.Count(); i++)
                    {
                        Sphere bsphere = bodies_[i].getBSphere();
                        Vector3 pos = bodies_[i].getPos();
                        if (Math.Abs(((Cursor.Position.X - Screen.PrimaryScreen.Bounds.Width * 0.5) / coord_transf)
                            - pos.X) < bsphere.radius
                            && Math.Abs(((-(Cursor.Position.Y - Screen.PrimaryScreen.Bounds.Height * 0.5)) / coord_transf)
                            + pos.Z) < bsphere.radius
                            && pos.Y > depthTest)
                        {
                            depthTest = pos.Y;
                            picked = i;
                        }
                    }
                    break;

                case 'z':
                    for (int i = 0; i < bodies_.Count(); i++)
                    {
                        Sphere bsphere = bodies_[i].getBSphere();
                        Vector3 pos = bodies_[i].getPos();
                        if (Math.Abs(((Cursor.Position.X - Screen.PrimaryScreen.Bounds.Width * 0.5) / coord_transf)
                            - pos.X) < bsphere.radius
                            && Math.Abs(((-(Cursor.Position.Y - Screen.PrimaryScreen.Bounds.Height * 0.5)) / coord_transf)
                            - pos.Y) < bsphere.radius
                            && pos.Z > depthTest)
                        {
                            depthTest = pos.Z;
                            picked = i;
                        }
                    }
                    break;

                default:
                    picked = -1;
                    return;
            }
        }

        void generateRandomBodies(int n, bool cube)
        {
            bodies.Clear();
            Random rand = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);
            int safe_area = (int)(fov * 0.5 - grid_edge * 0.5);
            for (int i = 0; i < n; i++)
            {
                float x = rand.Next(-safe_area, safe_area);
                float y = rand.Next(-safe_area, safe_area);
                float z = rand.Next(-safe_area, safe_area);
                //float length = rand.Next(2, 3);
                float length = 1.75f;
                float height, width;
                if (cube)
                {
                    height = length;
                    width = length;
                }
                else
                {
                    height = rand.Next(2, 3);
                    width = rand.Next(2, 3);
                }
                bodies.Add(new Parallelepiped(new Vector3(x, y, z), length, height, width, 0f));
            }
            calculateGridEdge();
        }

        void calculateGridEdge()
        {
            double maxRadius = 0;
            foreach (Body body in bodies)
                if (maxRadius < body.getBSphere().radius)
                    maxRadius = body.getBSphere().radius;
            grid_edge = maxRadius * 3;
            tiles = (int)(fov / grid_edge);
            grid_edge = fov / (double)tiles;
            Console.Write("\nfov: " + fov.ToString() +", tiles: " + tiles.ToString() + ", grid_edge: " + grid_edge.ToString() + "\n");
        }
    }
}
