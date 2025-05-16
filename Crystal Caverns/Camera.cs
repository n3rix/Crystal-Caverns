using System;
using System.Drawing;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.Utils
{
    public class Camera
    {
        
        public PointF Position { get; private set; }

        public SizeF ViewportSize { get; private set; }

        public SizeF WorldSize { get; private set; }

        private GameObject _target;

        private float _smoothingFactor = 0.1f;

        public Camera(SizeF viewportSize, SizeF worldSize)
        {
            ViewportSize = viewportSize;
            WorldSize = worldSize;
            Position = new PointF(0, 0);
        }
        
        public void SetTarget(GameObject target)
        {
            _target = target;
        }
     
        public void Update(GameTime gameTime)
        {
            if (_target == null) return;

            float targetX = _target.Position.X + (_target.Size.Width / 2) - (ViewportSize.Width / 2);
            float targetY = _target.Position.Y + (_target.Size.Height / 2) - (ViewportSize.Height / 2);

            Position = new PointF(
                Position.X + (_smoothingFactor * (targetX - Position.X)),
                Position.Y + (_smoothingFactor * (targetY - Position.Y))
            );
 
            Position = new PointF(
                Math.Max(0, Math.Min(Position.X, WorldSize.Width - ViewportSize.Width)),
                Math.Max(0, Math.Min(Position.Y, WorldSize.Height - ViewportSize.Height))
            );
        }

        
        public PointF WorldToScreen(PointF worldPosition)
        {
            return new PointF(
                worldPosition.X - Position.X,
                worldPosition.Y - Position.Y
            );
        }
        public PointF ScreenToWorld(PointF screenPosition)
        {
            return new PointF(
                screenPosition.X + Position.X,
                screenPosition.Y + Position.Y
            );
        }

        public bool IsInView(RectangleF objectBounds)
        {
            RectangleF viewRect = new RectangleF(Position, ViewportSize);
            return viewRect.IntersectsWith(objectBounds);
        }
    }
}