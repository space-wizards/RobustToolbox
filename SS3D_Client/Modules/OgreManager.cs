using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using Mogre;
using Miyagi;
using Miyagi.Common;
using SS3D.Modules.Network;
namespace SS3D.Modules
{
    public abstract class LoadingTracker //Very simple abstract class. 
    { //Simply used to provide a common interface for this stuff. Like an interface but uglier.
        public float loadingPercent = 0;
        public string loadingText = "";
    }

  /************************************************************************/
  /* ogre manager                                                         */
  /************************************************************************/

  public class OgreManager
  {
    private Root mRoot;
    public RenderWindow mWindow
    {
        private set;
        get;
    }
    private SceneManager mSceneMgr;
    public Camera mCamera
    {
        private set;
        get;
    }
    private Viewport mViewport;
    private bool mRenderingActive;
    public NetworkManager mNetworkMgr;
    public MiyagiSystem mMiyagiSystem;
    public StateManager mStateMgr;

    private double scalarX;
    private double scalarY;


    // camera zoom parameters
 
    private const int maxCameraDistance = 300;
    private const int minCameraDistance = 60;

    private int cameraDistance = 240;

    public int CameraDistance
    {
        get { return cameraDistance; }
        // clamp to min/max
        set { cameraDistance = System.Math.Min(System.Math.Max(minCameraDistance, value), maxCameraDistance); }
    }

    public double ScalarX
    {
        private set { scalarX = value; }
        get { return scalarX; }
    }

    public double ScalarY
    {
        private set { scalarY = value; }
        get { return scalarY; }
    }

    // flag is true if rendering is currently active
    public bool RenderingActive
    {
      get { return mRenderingActive; }
    }

    public RenderWindow Window
    {
      get { return mWindow; }
    }

    public Root Root
    {
        get { return mRoot; }
    }

    public SceneManager SceneMgr
    {
      get { return mSceneMgr; }
    }

    public Camera Camera
    {
      get { return mCamera; }
    }

    // events raised when direct 3D device is lost or restored
    public event EventHandler<OgreEventArgs> DeviceLost;
    public event EventHandler<OgreEventArgs> DeviceRestored;

    internal OgreManager() //constructor
    {
      mRoot = null;
      mWindow = null;
      mSceneMgr = null;
      mCamera = null;
      mViewport = null;
      mRenderingActive = false;
      mMiyagiSystem = null;
    }


    #region Startup, Shutdown, Update
    internal bool Startup(Configuration config)
    {
        // check if already initialized
        if (mRoot != null)
            return false;

        // create ogre root
        mRoot = new Root("plugins.cfg", "settings.cfg", "mogre.log");
        //Not sure if we should load all this stuff from files. Cheaters?
        //See http://www.ogre3d.org/tikiwiki/Mogre+Basic+Tutorial+5 about hardcoding it.

        // set directx render system
        RenderSystem renderSys = mRoot.GetRenderSystemByName("Direct3D9 Rendering Subsystem");
        mRoot.RenderSystem = renderSys;

        // register event to get notified when application lost or regained focus
        mRoot.RenderSystem.EventOccurred += OnRenderSystemEventOccurred;

        // initialize engine
        mRoot.Initialise(false);

        // optional parameters
        NameValuePairList parm = new NameValuePairList();
        parm["vsync"] = config.VSync.ToString();
        parm["FSAA"] = config.FSAA.ToString();
        parm["monitorIndex"] = "0";
        parm["left"] = "0";
        parm["top"] = "0";
        
        // create window
        mWindow = mRoot.CreateRenderWindow("SpaceStation3D", (uint)config.DisplayWidth, (uint)config.DisplayHeight, config.Fullscreen, parm);

        // Assign scale vars - Base Resolution = 1024 x 768
        scalarX = (double)config.DisplayWidth / (double)1024;
        scalarY = (double)config.DisplayHeight / (double)768;

        // create scene manager
        mSceneMgr = mRoot.CreateSceneManager(SceneType.ST_GENERIC, "DefaultSceneManager");

        // Add options & config entries for stuff below.
        MaterialManager.Singleton.SetDefaultTextureFiltering(TextureFilterOptions.TFO_ANISOTROPIC);
        MaterialManager.Singleton.DefaultAnisotropy = 8;

        TextureManager.Singleton.DefaultNumMipmaps = 8;

        // create default camera
        mCamera = mSceneMgr.CreateCamera("DefaultCamera");
        mCamera.AutoAspectRatio = true;
        mCamera.NearClipDistance = 1.0f;
        mCamera.FarClipDistance = 1500.0f;
        mCamera.Position = new Mogre.Vector3(0, cameraDistance,0);
        mCamera.LookAt(0, 32, 0);
        // create default viewport
        mViewport = mWindow.AddViewport(mCamera);
   
        // set rendering active flag
        mRenderingActive = true;

        // OK
        return true;
    }

    internal void Shutdown()
    {
        // shutdown ogre root
        if (mRoot != null)
        {
            // deregister event to get notified when application lost or regained focus
            mRoot.RenderSystem.EventOccurred -= OnRenderSystemEventOccurred;

            // shutdown ogre
            mRoot.Dispose();
        }
        mRoot = null;

        // forget other references to ogre systems
        mWindow = null;
        mSceneMgr = null;
        mCamera = null;
        mViewport = null;
        mRenderingActive = false;
    }

    public void OneUpdate()
    {   //Updates Engine (render, input), state manager and network manager.
        mNetworkMgr.UpdateNetwork();
        Update();
        mStateMgr.Update(0);
    }

    internal void Update()
    {
        // check if ogre manager is initialized
        if (mRoot == null)
            return;

        // process windows event queue (only if no external window is used)
        WindowEventUtilities.MessagePump();

        // render next frame
        if (mRenderingActive)
            mRoot.RenderOneFrame();
    } 
    #endregion

    // handle device lost and device restored events
    private void OnRenderSystemEventOccurred( string eventName, Const_NameValuePairList parameters )
    {
      EventHandler<OgreEventArgs> evt = null;
      OgreEventArgs args;

      // check which event occured
      switch( eventName )
      {
        // direct 3D device lost
        case "DeviceLost":
          // don't set mRenderingActive to false here, because ogre will try to restore the
          // device in the RenderOneFrame function and mRenderingActive needs to be set to true
          // for this function to be called

          // event to raise is device lost event
          evt = DeviceLost;

          // on device lost, create empty ogre event args
          args = new OgreEventArgs();
          break;

        // direct 3D device restored
        case "DeviceRestored":
          uint width;
          uint height;
          uint depth;

          // event to raise is device restored event
          evt = DeviceRestored;

          // get metrics for the render window size
          mWindow.GetMetrics( out width, out height, out depth );

          // on device restored, create ogre event args with new render window size
          args = new OgreEventArgs( (int) width, (int) height );
          break;

        default:
          return;
      }

      // raise event with provided event args
      if( evt != null )
        evt( this, args );
    }

    // create a simple object just consisting of a scenenode with a mesh
    internal SceneNode CreateSimpleObject( string _name, string _mesh )
    {
      // if scene manager already has an object with the requested name, fail to create it again
      if( mSceneMgr.HasEntity( _name ) || mSceneMgr.HasSceneNode( _name ) )
        return null;

      // create entity and scenenode for the object
      Entity entity;
      try
      {
        // try to create entity from mesh
        entity = mSceneMgr.CreateEntity( _name, _mesh );
      }
      catch
      {
        // failed to create entity
        return null;
      }

      // add entity to scenenode
      SceneNode node = mSceneMgr.CreateSceneNode( _name );

      // connect entity to the scenenode
      node.AttachObject( entity );

      // return the created object
      return node;
    }

    // destroy an object
    internal void DestroyObject( SceneNode _node )
    {
      // check if object has a parent node...
      if( _node.Parent != null )
      {
        // ...if so, remove it from its parent node first
        _node.Parent.RemoveChild( _node );
      }

      // first remove all child nodes (they are not destroyed here !)
      _node.RemoveAllChildren();

      // create a list of references to attached objects
      List<MovableObject> objList = new List<MovableObject>();

      // get number of attached objects
      ushort count = _node.NumAttachedObjects();

      // get all attached objects references
      for( ushort i = 0; i < count; ++i )
        objList.Add( _node.GetAttachedObject( i ) );

      // detach all objects from node
      _node.DetachAllObjects();

      // destroy all previously attached objects
      foreach( MovableObject obj in objList )
        mSceneMgr.DestroyMovableObject( obj );

      // destroy scene node
      mSceneMgr.DestroySceneNode( _node );
    }

    // add an object to the scene 
    internal void AddObjectToScene( SceneNode _node )
    {
      // check if object is already has a parent
      if( _node.Parent != null )
      {
        // check if object is in scene already, then we are done
        if( _node.Parent == mSceneMgr.RootSceneNode )
          return;

        // otherwise remove the object from its current parent
        _node.Parent.RemoveChild( _node );
      }

      // add object to scene
      mSceneMgr.RootSceneNode.AddChild( _node );
    }

    // add an object to another object as child
    internal void AddObjectToObject( SceneNode _node, SceneNode _newParent )
    {
      // check if object is already has a parent
      if( _node.Parent != null )
      {
        // check if object is in scene already, then we are done
        if( _node.Parent == _newParent )
          return;

        // otherwise remove the object from its current parent
        _node.Parent.RemoveChild( _node );
      }

      // add object to scene
      _newParent.AddChild( _node );
    }

    // remove object from scene 
    internal void RemoveObjectFromScene( SceneNode _node )
    {
      // if object is attached to a node
      if( _node.Parent != null )
      {
        // remove object from its parent
        _node.Parent.RemoveChild( _node );
      }
    }

  }

}
