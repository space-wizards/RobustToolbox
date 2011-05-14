using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    public class geometryMeshManager
    {
        public Mesh floorMesh;
        public Mesh wallMesh;
        public Mesh spaceMesh;

        private float tileX = 8.0f; // How far from X = 0 relative to the tile it extends
        private float tileZ = 8.0f; // How far from Z = 0 relative to the tile it extends (this should always == tileX so i don't know why i put this here)
        private float tileBottom = 4.0f; // The "bottom" of all tiles - how far under Y = 0 it extends
        private float floorHeight = 0.0f; // How far above Y = 0 the top of a floor is
        private float wallHeight = 40.0f; // How far about Y = 0 the top of a wall is


        public geometryMeshManager()
        {

        }

        public float GetFloorHeight()
        {
            return floorHeight;
        }

        public float GetWallHeight()
        {
            return wallHeight;
        }

        public void CreateMeshes()
        {
            CreateFloorMesh();
            CreateWallMesh();
            CreateSpaceMesh();
        }

        private void CreateFloorMesh()
        {
            ManualObject mobject = new ManualObject("floorMesh");

            #region +Z side
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(tileX, floorHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(-tileX, floorHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_Z);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region -X side
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, floorHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, floorHeight, -tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region -Z side
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(-tileX, floorHeight, -tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(tileX, floorHeight, -tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region +X side
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, floorHeight, -tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, floorHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_X);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region +Y size
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, floorHeight, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(tileX, floorHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(-tileX, floorHeight, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(-tileX, floorHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_Y);
            mobject.Triangle(1,0,2);
            mobject.Triangle(2,3,1);
            mobject.End();
            #endregion

            #region -Y side
            mobject.Begin("FloorTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 0);  mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1);mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 0);mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            floorMesh = mobject.ConvertToMesh("floorMesh");

        }

        private void CreateWallMesh()
        {
            ManualObject mobject = new ManualObject("wallMesh");

            #region +Z side
            mobject.Begin("WallTextureSide", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(tileX, wallHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_Z);
            mobject.Position(-tileX, wallHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_Z);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region -X side
            mobject.Begin("WallTextureSide", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, wallHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Position(-tileX, wallHeight, -tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_X);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region -Z side
            mobject.Begin("WallTextureSide", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(-tileX, wallHeight, -tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Position(tileX, wallHeight, -tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Z);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region +X side
            mobject.Begin("WallTextureSide", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, wallHeight, -tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_X);
            mobject.Position(tileX, wallHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_X);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            #region +Y side
            mobject.Begin("WallTextureTop", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, wallHeight, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(tileX, wallHeight, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(-tileX, wallHeight, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.UNIT_Y);
            mobject.Position(-tileX, wallHeight, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.UNIT_Y);
            mobject.Triangle(1, 0, 2);
            mobject.Triangle(2, 3, 1);
            mobject.End();
            #endregion

            #region -Y side
            mobject.Begin("WallTextureTop", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, -tileBottom, -tileZ); mobject.TextureCoord(1, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(tileX, -tileBottom, tileZ); mobject.TextureCoord(1, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(-tileX, -tileBottom, -tileZ); mobject.TextureCoord(0, 1); mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Position(-tileX, -tileBottom, tileZ); mobject.TextureCoord(0, 0); mobject.Normal(Vector3.NEGATIVE_UNIT_Y);
            mobject.Triangle(0, 1, 2);
            mobject.Triangle(1, 3, 2);
            mobject.End();
            #endregion

            wallMesh = mobject.ConvertToMesh("wallMesh");
        }

        private void CreateSpaceMesh()
        {
            ManualObject mobject = new ManualObject("spaceMesh");

            #region +Y size
            mobject.Begin("SpaceTexture", RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            mobject.Position(tileX, floorHeight, -tileZ);
            mobject.Position(tileX, floorHeight, tileZ);
            mobject.Position(-tileX, floorHeight, -tileZ);
            mobject.Position(-tileX, floorHeight, tileZ);
            mobject.Triangle(1, 0, 2);
            mobject.Triangle(2, 3, 1);
            mobject.End();
            #endregion

            spaceMesh = mobject.ConvertToMesh("spaceMesh");
        }

    }
}
