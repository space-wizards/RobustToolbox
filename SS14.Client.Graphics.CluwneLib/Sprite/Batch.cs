using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class Batch
    {
        private string _batchName;
        private List<ICluwneDrawable> _sprites;
        private Vector2 _position;
        private Boolean _needsUpdating;



        #region Constructor
       /// <summary>
       /// Constructs a new Batch
       /// </summary>
       /// Used to draw lots of Sprites at once, best use is for map drawing
       /// <param name="BatchName"> Name of Batch</param>
       /// <param name="AmountOfRenderTargets"> </param>
        public Batch(string BatchName, int AmountOfRenderTargets)
        {
           _sprites  = new List<ICluwneDrawable>();
           _needsUpdating = false;
        }
        #endregion


        #region Methods

        /// <summary>
        /// Adds a Drawable to the end of List/Batch
        /// </summary>
        /// <param name="Drawable"> Drawable to Add </param>
        public void Add(ICluwneDrawable Drawable)
        {
            _sprites.Add(Drawable);
            _needsUpdating = true;
        }

        /// <summary>
        /// Inserts a Drawable at a specified Position
        /// </summary>
        /// <param name="pos"> Position of insert </param>
        /// <param name="Drawable"> ICluwneDrawable to Insert </param>
        public void Insert(int pos, ICluwneDrawable Drawable)
        {
            _sprites.Insert(pos, Drawable);
            _needsUpdating = true;
        }

        /// <summary>
        /// Returns the IndexOf a Drawable in Batch List<ICluwneDrawable>
        /// </summary>
        /// <param name="Drawable"> Drawable to find </param>
        /// <returns> IndexOf Drawable, Exception if null </returns>
        public int IndexOf(ICluwneDrawable Drawable)
        {
            if (Drawable == null)
                throw new ArgumentNullException("Drawable Cannot be null");

            return _sprites.IndexOf(Drawable);
        }

        /// <summary>
        /// Removes a Sprite from the list at a given Position
        /// </summary>
        /// <param name="pos"> position of ICluwneDrawable to remove </param>
        public void Remove(int pos)
        {
            _sprites.RemoveAt(pos);
            _needsUpdating = true;


        }

        /// <summary>
        /// Draws the Batch of Sprites
        /// </summary>
        public void Draw()
        {
            foreach (ICluwneDrawable Drawable in _sprites)
            {
                Drawable.Draw();
            }
        }

        /// <summary>
        /// Clears the entire ICluwneDrawable List
        /// </summary>
        public void Clear()
        {
            _sprites.Clear();
        }

        public void AddClone (CluwneSprite CSprite)
        {

        }
        #endregion

        public void Dispose()
        {
            Clear();
        }


        #region Accessors
        public int Count 
        {
            get { return _sprites.Count; } 
        }

        public AlphaBlendOperation SourceBlend { get; set; }

        public AlphaBlendOperation DestinationBlend { get; set; }

        public AlphaBlendOperation SourceBlendAlpha { get; set; }

        public AlphaBlendOperation DestinationBlendAlpha { get; set; }

        #endregion
    }
}
